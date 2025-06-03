using EBISX_POS.API.Data;
using EBISX_POS.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EBISX_POS.Services
{
    public interface IDatabaseBackupService
    {
        Task<bool> CreateBackupAsync();
        Task<bool> RestoreBackupAsync(string backupPath);
        Task CleanupOldBackupsAsync();
    }

    public class DatabaseBackupService : IDatabaseBackupService
    {
        private readonly ILogger<DatabaseBackupService> _logger;
        private readonly DatabaseSettings _dbSettings;
        private readonly DataContext _dataContext;
        private readonly JournalContext _journalContext;
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 1000;

        public DatabaseBackupService(
            ILogger<DatabaseBackupService> logger,
            IOptions<DatabaseSettings> dbSettings,
            DataContext dataContext,
            JournalContext journalContext)
        {
            _logger = logger;
            _dbSettings = dbSettings.Value;
            _dataContext = dataContext;
            _journalContext = journalContext;
        }

        public async Task<bool> CreateBackupAsync()
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var backupDir = Path.Combine(AppContext.BaseDirectory, _dbSettings.BackupDirectory);
                    Directory.CreateDirectory(backupDir);

                    // Close existing connections before backup
                    await _dataContext.Database.CloseConnectionAsync();
                    await _journalContext.Database.CloseConnectionAsync();

                    // Backup POS database
                    var posBackupPath = Path.Combine(backupDir, $"pos_backup_{timestamp}.db");
                    await BackupDatabaseAsync(_dataContext.Database.GetConnectionString(), posBackupPath);

                    // Backup Journal database
                    var journalBackupPath = Path.Combine(backupDir, $"journal_backup_{timestamp}.db");
                    await BackupDatabaseAsync(_journalContext.Database.GetConnectionString(), journalBackupPath);

                    _logger.LogInformation("Database backup completed successfully");
                    return true;
                }
                catch (Exception ex) when (attempt < MaxRetries && (ex is TaskCanceledException || ex is TimeoutException || ex is IOException))
                {
                    _logger.LogWarning(ex, $"Attempt {attempt} failed to create backup. Retrying in {RetryDelayMs}ms...");
                    await Task.Delay(RetryDelayMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating database backup");
                    return false;
                }
                finally
                {
                    try
                    {
                        // Ensure connections are reopened
                        await _dataContext.Database.OpenConnectionAsync();
                        await _journalContext.Database.OpenConnectionAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reopening database connections after backup");
                    }
                }
            }

            return false;
        }

        public async Task<bool> RestoreBackupAsync(string backupPath)
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    if (!File.Exists(backupPath))
                    {
                        _logger.LogError("Backup file not found: {BackupPath}", backupPath);
                        return false;
                    }

                    // Determine which database to restore based on the backup filename
                    var isPosBackup = Path.GetFileName(backupPath).StartsWith("pos_backup_");
                    var connectionString = isPosBackup 
                        ? _dataContext.Database.GetConnectionString()
                        : _journalContext.Database.GetConnectionString();

                    // Close existing connections
                    if (isPosBackup)
                        await _dataContext.Database.CloseConnectionAsync();
                    else
                        await _journalContext.Database.CloseConnectionAsync();

                    // Copy backup file to database location
                    var dbPath = connectionString.Replace("Data Source=", "");
                    File.Copy(backupPath, dbPath, true);

                    _logger.LogInformation("Database restore completed successfully");
                    return true;
                }
                catch (Exception ex) when (attempt < MaxRetries && (ex is TaskCanceledException || ex is TimeoutException || ex is IOException))
                {
                    _logger.LogWarning(ex, $"Attempt {attempt} failed to restore backup. Retrying in {RetryDelayMs}ms...");
                    await Task.Delay(RetryDelayMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error restoring database backup");
                    return false;
                }
                finally
                {
                    try
                    {
                        // Ensure connections are reopened
                        await _dataContext.Database.OpenConnectionAsync();
                        await _journalContext.Database.OpenConnectionAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reopening database connections after restore");
                    }
                }
            }

            return false;
        }

        public async Task CleanupOldBackupsAsync()
        {
            try
            {
                var backupDir = Path.Combine(AppContext.BaseDirectory, _dbSettings.BackupDirectory);
                if (!Directory.Exists(backupDir))
                    return;

                var cutoffDate = DateTime.Now.AddDays(-_dbSettings.BackupRetentionDays);
                var backupFiles = Directory.GetFiles(backupDir, "*_backup_*.db");

                foreach (var file in backupFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < cutoffDate)
                        {
                            fileInfo.Delete();
                            _logger.LogInformation("Deleted old backup file: {File}", file);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting old backup file: {File}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old backups");
            }
        }

        private async Task BackupDatabaseAsync(string connectionString, string backupPath)
        {
            var dbPath = connectionString.Replace("Data Source=", "");
            if (!File.Exists(dbPath))
            {
                _logger.LogWarning("Database file not found: {DbPath}", dbPath);
                return;
            }

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    using var sourceStream = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var destinationStream = new FileStream(backupPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await sourceStream.CopyToAsync(destinationStream);
                    return;
                }
                catch (IOException ex) when (attempt < MaxRetries)
                {
                    _logger.LogWarning(ex, $"Attempt {attempt} failed to backup database file. Retrying in {RetryDelayMs}ms...");
                    await Task.Delay(RetryDelayMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accessing database file: {DbPath}", dbPath);
                    throw;
                }
            }
        }
    }
} 