using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Versioning;

namespace BaGet.Azure
{
    /// <summary>
    /// Stores the metadata of packages using Azure Table Storage.
    /// </summary>
    public class TablePackageDatabase : IPackageDatabase
    {
        private const int MaxPreconditionFailures = 5;
        private static List<string> MinimalColumnSet => new List<string> { "PartitionKey" };

        private readonly TableOperationBuilder _operationBuilder;
        private readonly CloudTable _table;
        private readonly ILogger<TablePackageDatabase> _logger;

        public TablePackageDatabase(
            TableOperationBuilder operationBuilder,
            CloudTableClient client,
            IOptionsSnapshot<AzureTableOptions> options,
            ILogger<TablePackageDatabase> logger)
        {
            _operationBuilder = operationBuilder ?? throw new ArgumentNullException(nameof(operationBuilder));
            _table = client?.GetTableReference(options.Value.TableName) ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PackageAddResult> AddAsync(Package package, CancellationToken cancellationToken)
        {
            try
            {
                var operation = _operationBuilder.AddPackage(package);

                await _table.ExecuteAsync(operation, cancellationToken);
            }
            catch (StorageException e) when (e.IsAlreadyExistsException())
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
            var operation = TableOperation.Retrieve<PackageEntity>(
                id.ToLowerInvariant(),
                version.ToNormalizedString().ToLowerInvariant(),
                MinimalColumnSet);

            var execution = await _table.ExecuteAsync(operation, cancellationToken);

            return execution.Result is PackageEntity;
        }

        public async Task<IReadOnlyList<Package>> FindAsync(string id, bool includeUnlisted, CancellationToken cancellationToken)
        {
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, id.ToLowerInvariant());
            if (!includeUnlisted)
            {
                filter = TableQuery.CombineFilters(
                    filter,
                    TableOperators.And,
                    TableQuery.GenerateFilterConditionForBool(nameof(PackageEntity.Listed), QueryComparisons.Equal, true));
            }

            var query = new TableQuery<PackageEntity>().Where(filter);
            var results = new List<Package>();

            // Request 500 results at a time from the server.
            TableContinuationToken token = null;
            query.TakeCount = 500;

            do
            {
                var segment = await _table.ExecuteQuerySegmentedAsync(query, token, cancellationToken);

                token = segment.ContinuationToken;

                // Write out the properties for each entity returned.
                results.AddRange(segment.Results.Select(r => r.AsPackage()));
            }
            while (token != null);

            return results.OrderBy(p => p.Version).ToList();
        }

        public async Task<Package> FindOrNullAsync(
            string id,
            NuGetVersion version,
            bool includeUnlisted,
            CancellationToken cancellationToken)
        {
            var operation = TableOperation.Retrieve<PackageEntity>(
                id.ToLowerInvariant(),
                version.ToNormalizedString().ToLowerInvariant());

            var result = await _table.ExecuteAsync(operation, cancellationToken);
            var entity = result.Result as PackageEntity;

            if (entity == null)
            {
                return null;
            }

            // Filter out the package if it's unlisted.
            if (!includeUnlisted && !entity.Listed)
            {
                return null;
            }

            return entity.AsPackage();
        }
    }
}
