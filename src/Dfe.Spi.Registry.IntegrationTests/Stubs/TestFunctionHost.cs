using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Domain.Data;
using Dfe.Spi.Registry.Domain.Sync;
using Dfe.Spi.Registry.Functions;
using Dfe.Spi.Registry.Functions.Sync;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dfe.Spi.Registry.IntegrationTests.Stubs
{
    public class TestFunctionHost
    {
        private ServiceProvider _serviceProvider;

        public TestFunctionHost()
        {
            var builder = new TestFunctionHostBuilder();
            var configuration = GetTestConfiguration();

            // Add default registrations
            var startup = new Startup();
            startup.Configure(builder, configuration);

            // Override relevant test registrations
            builder.Services
                .RemoveAll<ILoggerWrapper>()
                .RemoveAll<IRepository>()
                .RemoveAll<ISyncQueue>();

            builder.Services
                .AddScoped(provider => new ServiceFactory<ProcessEntityEvent>(provider))
                .AddScoped<Dfe.Spi.Common.UnitTesting.Infrastructure.LoggerWrapper>()
                .AddScoped<RepositoryStub>()
                .AddScoped<SyncQueueStub>()
                .AddScoped<ILoggerWrapper>(provider => provider.GetService<Dfe.Spi.Common.UnitTesting.Infrastructure.LoggerWrapper>())
                .AddScoped<IRepository>(provider => provider.GetService<RepositoryStub>())
                .AddScoped<ISyncQueue>(provider => provider.GetService<SyncQueueStub>());
            
            // Register functions (normally done by runtime)
            var allParameters = typeof(Startup).Assembly
                .GetTypes()
                .SelectMany(t => t.GetMethods())
                .SelectMany(m => m.GetParameters());
            var matchingParameters = allParameters
                .Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(HttpTriggerAttribute) ||
                                                        a.AttributeType == typeof(QueueTriggerAttribute)))
                .ToArray();
            var functionTypes = matchingParameters
                .Select(m => m.Member.DeclaringType)
                .ToArray();
            foreach (var functionType in functionTypes)
            {
                builder.Services.AddScoped(functionType);
            }

            _serviceProvider = builder.Services.BuildServiceProvider();
        }

        public TestScope GetScope()
        {
            var serviceScope = _serviceProvider.CreateScope();
            return new TestScope(serviceScope);
        }
        
        private IConfigurationRoot GetTestConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Sync:QueueConnectionString", "UseDevelopmentStorage=true"),
                    new KeyValuePair<string, string>("Data:CosmosDbUri", "http://localhost:9876"),
                    new KeyValuePair<string, string>("Data:CosmosDbKey", Convert.ToBase64String(Encoding.UTF8.GetBytes("NA"))),
                    new KeyValuePair<string, string>("Data:DatabaseName", "Db01"),
                    new KeyValuePair<string, string>("Data:ContainerName", "Ctr01"),
                }).Build();
        }

        private class TestFunctionHostBuilder : IFunctionsHostBuilder
        {
            public TestFunctionHostBuilder()
            {
                Services = new ServiceCollection();
            }

            public IServiceCollection Services { get; }
        }
    }
}