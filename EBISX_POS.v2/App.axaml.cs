using System;
using System.Linq;
using System.Net;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using EBISX_POS.Services;
using EBISX_POS.ViewModels;
using EBISX_POS.Views;
using EBISX_POS.Views.Manager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EBISX_POS.API.Data;
using Microsoft.EntityFrameworkCore;
using EBISX_POS.Settings;
using EBISX_POS.API.Extensions;
using System.IO;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using EBISX_POS.ViewModels.Manager;
using EBISX_POS.v2.Views;

namespace EBISX_POS
{
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current!;
        public IServiceProvider Services { get; private set; } = null!;

        public override void Initialize()
        {
            Debug.WriteLine($"Application Base Directory: {AppContext.BaseDirectory}");
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            Services = ConfigureServices();

            try
            {
                // Ensure required directories exist
                var filePaths = Services.GetRequiredService<IOptions<FilePaths>>().Value;
                if (!Directory.Exists(filePaths.ImagePath))
                {
                    Directory.CreateDirectory(filePaths.ImagePath);
                }
                if (!Directory.Exists(filePaths.BackUp))
                {
                    Directory.CreateDirectory(filePaths.BackUp);
                }

                // Ensure report directories exist
                var salesReport = Services.GetRequiredService<IOptions<SalesReport>>().Value;
                var reportDirectories = new[]
                {
                    salesReport.Receipts,
                    salesReport.SearchedInvoice,
                    salesReport.DailySalesReport,
                    salesReport.XInvoiceReport,
                    salesReport.ZInvoiceReport,
                    salesReport.CashTrackReport,
                    salesReport.TransactionLogs,
                    salesReport.AuditTrailFolder,
                    salesReport.TransactionLogsFolder
                };

                foreach (var directory in reportDirectories)
                {
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }

                // Ensure database directory exists
                var dbSettings = Services.GetRequiredService<IOptions<DatabaseSettings>>().Value;

                // Initialize databases
                var dbInitializer = Services.GetRequiredService<IDatabaseInitializerService>();
                await dbInitializer.InitializeAsync();

                // Start connectivity monitoring
                var connectivity = Services.GetRequiredService<ConnectivityViewModel>();
                _ = connectivity.StartMonitoringCommand.ExecuteAsync(null);

                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    DisableAvaloniaDataAnnotationValidation();

                    CashierState.OnCashierStateChanged += () =>
                    {
                        desktop.MainWindow = !string.IsNullOrEmpty(CashierState.CashierName)
                            ? Services.GetRequiredService<MainWindow>()
                            : Services.GetRequiredService<LogInWindow>();
                    };

                    desktop.MainWindow = !string.IsNullOrEmpty(CashierState.CashierName)
                        ? Services.GetRequiredService<MainWindow>()
                        : Services.GetRequiredService<LogInWindow>();
                    //desktop.MainWindow  = Services.GetRequiredService<PosTerminalInfoView>();
                }
            }
            catch (Exception ex)
            {
                var logger = Services.GetRequiredService<ILogger<App>>();
                logger.LogError(ex, "Failed to initialize application");
                throw;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Add configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("posappsettings.json", optional: false, reloadOnChange: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            // Configure database settings
            services.Configure<DatabaseSettings>(configuration.GetSection("Database"));
            var dbSettings = configuration.GetSection("Database").Get<DatabaseSettings>() ?? new DatabaseSettings();

            // Configure file paths
            services.Configure<FilePaths>(configuration.GetSection("FilePaths"));

            // Configure sales report paths
            services.Configure<SalesReport>(configuration.GetSection("SalesReport"));

            // Add logging
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.AddDebug();
            });

            // Add database contexts with configured connection strings
            services.AddDbContext<DataContext>(options =>
                options.UseSqlite(dbSettings.PosConnectionString));
            services.AddDbContext<JournalContext>(options =>
                options.UseSqlite(dbSettings.JournalConnectionString));

            // Add database initializer and backup service
            services.AddSingleton<IDatabaseInitializerService, DatabaseInitializerService>();
            services.AddSingleton<IDatabaseBackupService, DatabaseBackupService>();

            // Register repositories
            services.AddAvaloniaApplicationServices(configuration);

            // Register services
            services.AddSingleton<AuthService>();
            services.AddSingleton<MenuService>();
            services.AddSingleton<OrderService>();
            services.AddSingleton<PaymentService>();
            services.AddSingleton<ReportService>();
            services.AddSingleton<CookieContainer>();
            services.AddSingleton<ConnectivityViewModel>();

            // Register ViewModels
            services.AddTransient<LogInWindowViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<ItemListViewModel>();
            services.AddTransient<OrderSummaryViewModel>();
            services.AddTransient<SubItemWindowViewModel>();
            services.AddTransient<TenderOrderViewModel>();
            services.AddTransient<AppUsersViewModel>();

            // Register Views
            services.AddTransient<LogInWindow>();
            services.AddTransient<MainWindow>();
            services.AddTransient<OrderSummaryView>();
            services.AddTransient<ItemListView>();
            services.AddTransient<ManagerWindow>();
            services.AddTransient<TenderOrderWindow>();
            services.AddTransient<SalesHistoryWindow>();
            services.AddTransient<AppUsersWindow>();
            services.AddTransient<CategoryWindow>();
            services.AddTransient<DrinkAndAddOnTypeWindow>();
            services.AddTransient<MenuWindow>();
            services.AddTransient<CouponPromoWindow>();
            services.AddTransient<PosTerminalInfoView>();

            return services.BuildServiceProvider();
        }
    }
}