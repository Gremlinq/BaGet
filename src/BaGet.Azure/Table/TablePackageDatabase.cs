using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core;
using Azure.Data.Tables;
using NuGet.Versioning;
using Azure;
using Newtonsoft.Json;

namespace BaGet.Azure
{
    /// <summary>
    /// Stores the metadata of packages using Azure Table Storage.
    /// </summary>
    public class TablePackageDatabase : IPackageDatabase
    {
        private static List<string> MinimalColumnSet => new List<string> { "PartitionKey" };

        private readonly TableClient _tableClient;

        public TablePackageDatabase(TableClient tableClient)
        {
            _tableClient = tableClient;
        }

        public async Task<PackageAddResult> AddAsync(Package package, CancellationToken cancellationToken)
        {
            try
            {
                var version = package.Version;
                var normalizedVersion = version.ToNormalizedString();

                var packageEntity = new PackageEntity
                {
                    PartitionKey = package.Id.ToLowerInvariant(),
                    RowKey = normalizedVersion.ToLowerInvariant(),

                    Identifier = package.Id,
                    NormalizedVersion = normalizedVersion,
                    OriginalVersion = version.ToFullString(),
                    Authors = JsonConvert.SerializeObject(package.Authors),
                    Description = package.Description,
                    Downloads = package.Downloads,
                    HasReadme = package.HasReadme,
                    HasEmbeddedIcon = package.HasEmbeddedIcon,
                    IsPrerelease = package.IsPrerelease,
                    Language = package.Language,
                    Listed = package.Listed,
                    MinClientVersion = package.MinClientVersion,
                    Published = package.Published,
                    RequireLicenseAcceptance = package.RequireLicenseAcceptance,
                    SemVerLevel = (int)package.SemVerLevel,
                    Summary = package.Summary,
                    Title = package.Title,
                    IconUrl = package.IconUrlString,
                    LicenseUrl = package.LicenseUrlString,
                    ReleaseNotes = package.ReleaseNotes,
                    ProjectUrl = package.ProjectUrlString,
                    RepositoryUrl = package.RepositoryUrlString,
                    RepositoryType = package.RepositoryType,
                    Tags = JsonConvert.SerializeObject(package.Tags),
                    Dependencies = SerializeList(
                        package.Dependencies,
                        dependency =>
                        {
                            return new DependencyModel
                            {
                                Id = dependency.Id,
                                VersionRange = dependency.VersionRange,
                                TargetFramework = dependency.TargetFramework
                            };
                        }),
                    PackageTypes = SerializeList(
                        package.PackageTypes,
                        packageType => new PackageTypeModel
                        {
                            Name = packageType.Name,
                            Version = packageType.Version
                        }),
                    TargetFrameworks = SerializeList(package.TargetFrameworks, f => f.Moniker)
                };

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
                version.ToNormalizedString().ToLowerInvariant(),
                cancellationToken: cancellationToken);

            if (!maybeEntity.HasValue)
                return null;

            // Filter out the package if it's unlisted.
            if (!includeUnlisted && !maybeEntity.Value.Listed)
                return null;

            return maybeEntity.Value.AsPackage();
        }

        private static string SerializeList<TIn, TOut>(IReadOnlyList<TIn> objects, Func<TIn, TOut> map)
        {
            var data = objects.Select(map).ToList();

            return JsonConvert.SerializeObject(data);
        }
    }
}
