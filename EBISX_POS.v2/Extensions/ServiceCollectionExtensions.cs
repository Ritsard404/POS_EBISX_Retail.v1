using EBISX_POS.API.Data;
using EBISX_POS.API.Services;
using EBISX_POS.API.Services.Interfaces;
using EBISX_POS.API.Services.PDF;
using EBISX_POS.API.Services.Repositories;
using EBISX_POS.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace EBISX_POS.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAvaloniaApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add configuration services with default values
            var filePaths = configuration.GetSection("FilePaths").Get<FilePaths>() ?? FilePaths.CreateDefault();
            filePaths.EnsureDirectoriesExist();
            
            services.Configure<FilePaths>(options =>
            {
                options.ImagePath = filePaths.ImagePath;
                options.BackUp = filePaths.BackUp;
            });

            services.Configure<DatabaseSettings>(configuration.GetSection("Database"));

            // Register database contexts
            services.AddDatabaseContexts(configuration);

            // Register repositories
            services.AddRepositories();

            // Add logging
            //services.AddLogging();

            return services;
        }

        private static IServiceCollection AddDatabaseContexts(this IServiceCollection services, IConfiguration configuration)
        {
            var dbSettings = configuration.GetSection("Database").Get<DatabaseSettings>() 
                ?? throw new InvalidOperationException("Database settings are not configured properly.");

            services.AddDbContext<DataContext>((serviceProvider, options) =>
            {
                options.UseSqlite(dbSettings.PosConnectionString, sqliteOptions =>
                {
                    sqliteOptions.CommandTimeout(dbSettings.CommandTimeout);
                });

                if (dbSettings.EnableDetailedErrors)
                    options.EnableDetailedErrors();

                if (dbSettings.EnableSensitiveDataLogging)
                    options.EnableSensitiveDataLogging();

                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                options.UseLoggerFactory(loggerFactory);
            });

            services.AddDbContext<JournalContext>((serviceProvider, options) =>
            {
                options.UseSqlite(dbSettings.JournalConnectionString, sqliteOptions =>
                {
                    sqliteOptions.CommandTimeout(dbSettings.CommandTimeout);
                });

                if (dbSettings.EnableDetailedErrors)
                    options.EnableDetailedErrors();

                if (dbSettings.EnableSensitiveDataLogging)
                    options.EnableSensitiveDataLogging();

                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                options.UseLoggerFactory(loggerFactory);
            });

            return services;
        }

        private static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // Register PDF services
            services.AddScoped<AuditTrailPDFService>();
            services.AddScoped<TransactionListPDFService>();

            services.AddScoped<IAuth, AuthRepository>();
            services.AddScoped<IMenu, MenuRepository>();
            services.AddScoped<IOrder, OrderRepository>();
            services.AddScoped<IPayment, PaymentRepository>();
            services.AddScoped<IJournal, JournalRepository>();
            services.AddScoped<IReport, ReportRepository>();
            services.AddScoped<IInvoiceNumberService, InvoiceNumberService>();
            services.AddScoped<IData, DataRepository>();
            services.AddScoped<IEbisxAPI, EbisxAPIRepository>();

            return services;
        }
    }
}