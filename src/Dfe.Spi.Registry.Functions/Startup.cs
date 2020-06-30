using System.IO;
using Dfe.Spi.Common.Context.Definitions;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application.Sync;
using Dfe.Spi.Registry.Domain.Configuration;
using Dfe.Spi.Registry.Domain.Data;
using Dfe.Spi.Registry.Domain.Sync;
using Dfe.Spi.Registry.Functions;
using Dfe.Spi.Registry.Infrastructure.AzureStorage.Sync;
using Dfe.Spi.Registry.Infrastructure.CosmosDb;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(Startup))]
namespace Dfe.Spi.Registry.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var rawConfiguration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", true)
                .AddEnvironmentVariables(prefix: "SPI_")
                .Build();

            Configure(builder, rawConfiguration);
        }
        public void Configure(IFunctionsHostBuilder builder, IConfigurationRoot rawConfiguration)
        {
            var services = builder.Services;
            
            AddConfiguration(services, rawConfiguration);
            AddLogging(services);
            AddData(services);
            AddSyncing(services);
        }

        private void AddConfiguration(IServiceCollection services, IConfigurationRoot rawConfiguration)
        {
            services.AddSingleton(rawConfiguration);
            
            var configuration = new RegistryConfiguration();
            rawConfiguration.Bind(configuration);
            services.AddSingleton(configuration);
            services.AddSingleton(configuration.Sync);
            services.AddSingleton(configuration.Data);
        }

        private void AddLogging(IServiceCollection services)
        {
            services.AddLogging();
            services.AddScoped<ILogger>(provider =>
                provider.GetService<ILoggerFactory>().CreateLogger(LogCategories.CreateFunctionUserCategory("Registry")));
            
            services.AddScoped<IHttpSpiExecutionContextManager, HttpSpiExecutionContextManager>();
            services.AddScoped<ISpiExecutionContextManager>((provider) =>
                (ISpiExecutionContextManager) provider.GetService(typeof(IHttpSpiExecutionContextManager)));
            services.AddScoped<ILoggerWrapper, LoggerWrapper>();
        }

        private void AddData(IServiceCollection services)
        {
            services
                .AddScoped<IRepository, CosmosDbRepository>();
        }
        
        private void AddSyncing(IServiceCollection services)
        {
            services
                .AddScoped<ISyncQueue, StorageQueueSyncQueue>()
                .AddScoped<ISyncManager, SyncManager>();
        }
    }
}