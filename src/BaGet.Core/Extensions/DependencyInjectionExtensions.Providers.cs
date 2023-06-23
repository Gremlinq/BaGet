using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaGet.Core
{
    public static partial class DependencyInjectionExtensions
    {
        private static readonly string SearchTypeKey = $"{nameof(BaGetOptions.Search)}:{nameof(SearchOptions.Type)}";
        private static readonly string StorageTypeKey = $"{nameof(BaGetOptions.Storage)}:{nameof(StorageOptions.Type)}";

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

        /// <summary>
        /// Determine whether a search type is currently active.
        /// </summary>
        /// <param name="config">The application's configuration.</param>
        /// <param name="value">The search type that should be checked.</param>
        /// <returns>Whether the search type is active.</returns>
        public static bool HasSearchType(this IConfiguration config, string value)
        {
            return config[SearchTypeKey].Equals(value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determine whether a storage type is currently active.
        /// </summary>
        /// <param name="config">The application's configuration.</param>
        /// <param name="value">The storage type that should be checked.</param>
        /// <returns>Whether the database type is active.</returns>
        public static bool HasStorageType(this IConfiguration config, string value)
        {
            return config[StorageTypeKey].Equals(value, StringComparison.OrdinalIgnoreCase);
        }
    }
}
