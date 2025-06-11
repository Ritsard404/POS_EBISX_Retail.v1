using EBISX_POS.API.Data;
using EBISX_POS.API.Models;
using EBISX_POS.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace EBISX_POS.Services
{
    public interface IDatabaseInitializerService
    {
        Task InitializeAsync();
    }

    public class DatabaseInitializerService : IDatabaseInitializerService
    {
        private readonly ILogger<DatabaseInitializerService> _logger;
        private readonly DataContext _dataContext;
        private readonly JournalContext _journalContext;
        private readonly IDatabaseBackupService _backupService;
        private readonly DatabaseSettings _dbSettings;
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 1000;

        public DatabaseInitializerService(
            ILogger<DatabaseInitializerService> logger,
            DataContext dataContext,
            JournalContext journalContext,
            IDatabaseBackupService backupService,
            IOptions<DatabaseSettings> dbSettings)
        {
            _logger = logger;
            _dataContext = dataContext;
            _journalContext = journalContext;
            _backupService = backupService;
            _dbSettings = dbSettings.Value;
        }

        public async Task InitializeAsync()
        {
            try
            {
                Debug.WriteLine("Starting database initialization...");
                Debug.WriteLine($"POS Connection String: {_dbSettings.PosConnectionString}");
                Debug.WriteLine($"Journal Connection String: {_dbSettings.JournalConnectionString}");
                Debug.WriteLine($"Backup Directory: {_dbSettings.BackupDirectory}");

                // Create backup before any database operations if enabled
                if (_dbSettings.EnableAutomaticBackup)
                {
                    _logger.LogInformation("Creating database backup before initialization...");
                    //await _backupService.CreateBackupAsync();
                }

                // Initialize databases with retry logic
                await InitializeDatabaseWithRetryAsync(_dataContext, "POS");
                await InitializeDatabaseWithRetryAsync(_journalContext, "Journal");

                // Seed initial data only if needed
                await SeedInitialDataAsync();

                // Cleanup old backups
                //await _backupService.CleanupOldBackupsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database initialization error: {ex}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                _logger.LogError(ex, "An error occurred while initializing the databases");
                throw;
            }
        }

        private async Task InitializeDatabaseWithRetryAsync(DbContext context, string databaseName)
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    Debug.WriteLine($"Attempt {attempt} to initialize {databaseName} database");
                    await InitializeDatabaseAsync(context, databaseName);
                    return; // Success, exit retry loop
                }
                catch (Exception ex) when (attempt < MaxRetries && (ex is TaskCanceledException || ex is TimeoutException))
                {
                    Debug.WriteLine($"Attempt {attempt} failed for {databaseName}: {ex.Message}");
                    _logger.LogWarning(ex, $"Attempt {attempt} failed to initialize {databaseName} database. Retrying in {RetryDelayMs}ms...");
                    await Task.Delay(RetryDelayMs);
                }
            }

            // If we get here, all retries failed
            throw new InvalidOperationException($"Failed to initialize {databaseName} database after {MaxRetries} attempts");
        }

        private async Task InitializeDatabaseAsync(DbContext context, string databaseName)
        {
            Debug.WriteLine($"Initializing {databaseName} database...");

            try
            {
                // Ensure the database directory exists
                var connectionString = context.Database.GetConnectionString();
                Debug.WriteLine($"{databaseName} connection string: {connectionString}");

                var dbPath = connectionString.Replace("Data Source=", "");
                Debug.WriteLine($"{databaseName} database path: {dbPath}");

                var dbDirectory = Path.GetDirectoryName(dbPath);
                Debug.WriteLine($"{databaseName} database directory: {dbDirectory}");

                if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
                {
                    Debug.WriteLine($"Creating directory: {dbDirectory}");
                    Directory.CreateDirectory(dbDirectory);
                }

                // Close any existing connections
                await context.Database.CloseConnectionAsync();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_dbSettings.CommandTimeout));
                
                if (await context.Database.CanConnectAsync(cts.Token))
                {
                    Debug.WriteLine($"Connected to {databaseName} database. Applying migrations...");
                    await context.Database.MigrateAsync(cts.Token);
                    Debug.WriteLine($"{databaseName} database migrations completed successfully.");
                }
                else
                {
                    Debug.WriteLine($"Could not connect to {databaseName} database. Creating new database...");
                    await context.Database.EnsureCreatedAsync(cts.Token);
                    Debug.WriteLine($"New {databaseName} database created successfully.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing {databaseName} database: {ex}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                _logger.LogError(ex, $"Error initializing {databaseName} database");
                throw;
            }
            finally
            {
                try
                {
                    // Ensure connection is reopened
                    await context.Database.OpenConnectionAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error reopening connection to {databaseName} database: {ex}");
                    _logger.LogError(ex, $"Error reopening connection to {databaseName} database");
                }
            }
        }

        private async Task SeedInitialDataAsync()
        {
            try
            {
                Debug.WriteLine("Starting to seed initial data...");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_dbSettings.CommandTimeout));

                // Check if POS terminal data exists using a proper query
                var terminalExists = await _dataContext.PosTerminalInfo
                    .AnyAsync(cts.Token);

                Debug.WriteLine($"Terminal exists: {terminalExists}");

                if (!terminalExists)
                {
                    Debug.WriteLine("Seeding initial POS terminal data...");
                    var terminal = new PosTerminalInfo
                    {
                        PosSerialNumber = "POS-123456789",
                        MinNumber = "MIN-987654321",
                        AccreditationNumber = "ACC-00112233",
                        PtuNumber = "PTU-44556677",
                        DateIssued = DateTime.UtcNow,
                        ValidUntil = DateTime.UtcNow.AddYears(5),
                        RegisteredName = "EBISX Food Services",
                        OperatedBy = "EBISX Food, Inc.",
                        Address = "123 Main Street, Cebu City",
                        VatTinNumber = "123-456-789-000",
                        StoreCode = "Store 1"
                    };

                    await _dataContext.PosTerminalInfo.AddAsync(terminal, cts.Token);

                    if (!await _dataContext.User.AnyAsync())
                    {
                        var users = new User
                        {
                            UserEmail = "EBISX@POS.com",
                            UserFName = "Ebisx",
                            UserLName = "Pos",
                            UserRole = "Developer"
                        };

                        await _dataContext.User.AddAsync(users, cts.Token);
                    }

                    if (!await _dataContext.SaleType.AnyAsync())
                    {

                        var saleTypes = new SaleType[]
                        {
                            new SaleType { Name = "GCASH", Account = "A/R - GCASH", Type = "CHARGE" },
                            new SaleType { Name = "PAYMAYA", Account = "A/R - PAYMAYA", Type = "CHARGE" },
                            new SaleType { Name = "FOOD PANDA", Account = "A/R - FOOD PANDA", Type = "CHARGE" },
                            new SaleType { Name = "GRAB", Account = "A/R - FOOD PANDA", Type = "CHARGE" },
                            new SaleType { Name = "GIFT CHEQUE", Account = "A/R - PRODUCT GC", Type = "CHARGE" },
                            new SaleType { Name = "DEBIT", Account = "A/R - DEBIT", Type = "CHARGE" },
                            new SaleType { Name = "CREDIT", Account = "A/R - CREDIT", Type = "CHARGE" },
                        };

                        await _dataContext.SaleType.AddRangeAsync(saleTypes, cts.Token);
                    }
                    await _dataContext.SaveChangesAsync(cts.Token);
                    Debug.WriteLine("Initial POS terminal data seeded successfully.");
                }
                else
                {
                    Debug.WriteLine("POS terminal data already exists, skipping seed.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error seeding initial data: {ex}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                _logger.LogError(ex, "Error seeding initial data");
                throw;
            }
        }
    }
}