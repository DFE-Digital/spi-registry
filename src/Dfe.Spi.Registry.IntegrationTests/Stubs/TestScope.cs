using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Dfe.Spi.Registry.IntegrationTests.Stubs
{
    public class TestScope : IDisposable
    {
        private readonly IServiceScope _serviceScope;

        public TestScope(IServiceScope serviceScope)
        {
            _serviceScope = serviceScope;
        }

        public T GetService<T>()
        {
            return _serviceScope.ServiceProvider.GetService<T>();
        }

        public string GetLogs()
        {
            var logger = GetService<Dfe.Spi.Common.UnitTesting.Infrastructure.LoggerWrapper>();
            return logger.ReturnLog();
        }

        public void Dispose()
        {
            _serviceScope?.Dispose();
        }
    }
}