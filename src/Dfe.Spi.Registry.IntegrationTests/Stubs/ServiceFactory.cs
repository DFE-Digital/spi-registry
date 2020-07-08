using System;
using Microsoft.Extensions.DependencyInjection;

namespace Dfe.Spi.Registry.IntegrationTests.Stubs
{
    public class ServiceFactory<T>
    {
        private readonly IServiceProvider _provider;

        public ServiceFactory(IServiceProvider provider)
        {
            _provider = provider;
        }
        public T Create()
        {
            return _provider.GetService<T>();
        }
    }
}