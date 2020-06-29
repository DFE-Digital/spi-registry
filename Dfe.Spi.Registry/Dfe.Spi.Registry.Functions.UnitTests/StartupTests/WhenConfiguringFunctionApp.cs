using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Dfe.Spi.Registry.Functions.HealthCheck;
using Dfe.Spi.Registry.Functions.Sync;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Functions.UnitTests.StartupTests
{
    public class WhenConfiguringFunctionApp
    {
        [Test]
        public void ThenAllFunctionsShouldBeResolvable()
        {
            var functions = GetFunctions();
            var builder = new TestFunctionHostBuilder();
            var configuration = GetTestConfiguration();

            var startup = new Startup();
            startup.Configure(builder, configuration);
            // Have to register the function so container can attempt to resolve them
            foreach (var function in functions)
            {
                builder.Services.AddScoped(function);
            }

            var provider = builder.Services.BuildServiceProvider();

            foreach (var function in functions)
            {
                try
                {
                    var resolvedFunction = provider.GetService(function);
                    if (resolvedFunction == null)
                    {
                        throw new NullReferenceException("Function resolved to null");
                    }
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Failed to resolved {function.Name}:\n{ex}");
                }
            }
        }

        private IConfigurationRoot GetTestConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Sync:QueueConnectionString", "UseDevelopmentStorage=true"),
                }).Build();
        }


        private Type[] GetFunctions()
        {
            var allParameters = typeof(Startup).Assembly
                .GetTypes()
                .SelectMany(t => t.GetMethods())
                .SelectMany(m => m.GetParameters());
            var matchingParameters = allParameters
                .Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(HttpTriggerAttribute)))
                .ToArray();
            var functionTypes = matchingParameters
                .Select(m => m.Member.DeclaringType)
                .ToArray();

            return functionTypes;
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