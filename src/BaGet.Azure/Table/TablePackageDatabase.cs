using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Azure;

namespace BaGet.Azure
{
    /// <summary>
    /// Stores the metadata of packages using Azure Table Storage.
    /// </summary>
    public class TablePackageDatabase : IPackageDatabase
    {
        private static List<string> MinimalColumnSet => new List<string> { "PartitionKey" };

        private readonly TableClient _tableClient;
        private readonly TableOperationBuilder _operationBuilder;

        public TablePackageDatabase(
            TableOperationBuilder operationBuilder,
            TableServiceClient client,
            IOptionsSnapshot<AzureTableOptions> options)
        {
            _operationBuilder = operationBuilder ?? throw new ArgumentNullException(nameof(operationBuilder));
            _tableClient = client?.GetTableClient(options.Value.TableName) ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<PackageAddResult> AddAsync(Package package, CancellationToken cancellationToken)
        {
            try
            {
                var packageEntity = _operationBuilder.AddPackage(package);

                await _tableClient.AddEntityAsync(packageEntity, cancellationToken);
            }
            catch (RequestFailedException e) when (e.IsAlreadyExistsException())
            {
                return PackageAddResult.PackageAlreadyExists;
            }

            return PackageAddResult.Success;
        }

        public async Task<bool> ExistsAsync(
            string id,
            NuGetVersion version,
            CancellationToken cancellationToken)
        {
            var execution = await _tableClient.GetEntityIfExistsAsync<PackageEntity>(id.ToLowerInvariant(), version.ToNormalizedString().ToLowerInvariant(), MinimalColumnSet, cancellationToken);

            return execution.HasValue;
        }

        public async Task<IReadOnlyList<Package>> FindAsync(string id, bool includeUnlisted, CancellationToken cancellationToken)
        {
            var filter = TableClient.CreateQueryFilter<PackageEntity>(x => x.PartitionKey == id.ToLowerInvariant());

            if (!includeUnlisted)
            {
                filter = TableClient.CreateQueryFilter($"({filter} and ({TableClient.CreateQueryFilter<PackageEntity>(x => x.Listed == true)})");
            }

            var results = await _tableClient
                .QueryAsync<PackageEntity>(filter, cancellationToken: cancellationToken)
                .ToArrayAsync(cancellationToken);

            return results
                .Select(x => x.AsPackage())
                .OrderBy(p => p.Version)
                .ToList();
        }

        public async Task<Package> FindOrNullAsync(
            string id,
            NuGetVersion version,
            bool includeUnlisted,
            CancellationToken cancellationToken)
        {
            var maybeEntity = await _tableClient.GetEntityIfExistsAsync<PackageEntity>(
                id.ToLowerInvariant(),
                version.ToNormalizedString().ToLowerInvariant());

            if (!maybeEntity.HasValue)
                return null;

            // Filter out the package if it's unlisted.
            if (!includeUnlisted && !maybeEntity.Value.Listed)
                return null;

            return maybeEntity.Value.AsPackage();
        }
    }
}
