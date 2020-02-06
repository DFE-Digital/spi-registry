using System.IO;
using Dfe.Spi.Common.Context.Definitions;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application.Entities;
using Dfe.Spi.Registry.Application.LearningProviders;
using Dfe.Spi.Registry.Domain.Configuration;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Links;
using Dfe.Spi.Registry.Functions;
using Dfe.Spi.Registry.Infrastructure.AzureStorage.Entities;
using Dfe.Spi.Registry.Infrastructure.AzureStorage.Links;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RestSharp;

[assembly: FunctionsStartup(typeof(Startup))]
namespace Dfe.Spi.Registry.Functions
{
    public class Startup : FunctionsStartup
    {
        private IConfigurationRoot _rawConfiguration;
        private RegistryConfiguration _configuration;

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var rawConfiguration = BuildConfiguration();
            Configure(builder, rawConfiguration);
        }

        public void Configure(IFunctionsHostBuilder builder, IConfigurationRoot rawConfiguration)
        {
            var services = builder.Services;

            AddConfiguration(services, rawConfiguration);
            AddLogging(services);
            AddHttp(services);
            AddManagers(services);
            AddRepositories(services);
        }

        private IConfigurationRoot BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", true)
                .AddEnvironmentVariables(prefix: "SPI_")
                .Build();
        }

        private void AddConfiguration(IServiceCollection services, IConfigurationRoot rawConfiguration)
        {
            _rawConfiguration = rawConfiguration;
            services.AddSingleton(_rawConfiguration);
            
            _configuration = new RegistryConfiguration();
            _rawConfiguration.Bind(_configuration);
            services.AddSingleton(_configuration);
            services.AddSingleton(_configuration.Entities);
            services.AddSingleton(_configuration.Links);
        }

        private void AddLogging(IServiceCollection services)
        {
            services.AddLogging();
            services.AddScoped(typeof(ILogger<>), typeof(Logger<>));
            services.AddScoped<ILogger>(provider =>
                provider.GetService<ILoggerFactory>().CreateLogger(LogCategories.CreateFunctionUserCategory("Common")));
            
            services.AddScoped<IHttpSpiExecutionContextManager, HttpSpiExecutionContextManager>();
            services.AddScoped<ISpiExecutionContextManager>((provider) =>
                (ISpiExecutionContextManager) provider.GetService(typeof(IHttpSpiExecutionContextManager)));
            services.AddScoped<ILoggerWrapper, LoggerWrapper>();
        }

        private void AddHttp(IServiceCollection services)
        {
            services.AddScoped<IRestClient, RestClient>();
        }

        private void AddManagers(IServiceCollection services)
        {
            services.AddScoped<IEntityManager, EntityManager>();
            services.AddScoped<ILearningProviderManager, LearningProviderManager>();
        }

        private void AddRepositories(IServiceCollection services)
        {
            services
                .AddSingleton<IEntityRepository, TableEntityRepository>()
                .AddSingleton<ILinkRepository, TableLinkRepository>();
        }
    }
}