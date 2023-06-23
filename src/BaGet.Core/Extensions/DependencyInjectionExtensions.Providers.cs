using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaGet.Core
{
    public static partial class DependencyInjectionExtensions
    {
        /// <summary>
        /// Add a new provider to the dependency injection container. The provider may
        /// provide an implementation of the service, or it may return null.
        /// </summary>
        /// <typeparam name="TService">The service that may be provided.</typeparam>
        /// <param name="services">The dependency injection container.</param>
        /// <param name="func">A handler that provides the service, or null.</param>
        /// <returns>The dependency injection container.</returns>
        public static IServiceCollection AddProvider<TService>(
            this IServiceCollection services,
            Func<IServiceProvider, IConfiguration, TService> func)
            where TService : class
        {
            services.AddSingleton<IProvider<TService>>(new DelegateProvider<TService>(func));

            return services;
        }
    }
}
