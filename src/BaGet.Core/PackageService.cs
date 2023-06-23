using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace BaGet.Core
{
    public class PackageService : IPackageService
    {
        private readonly IPackageDatabase _db;

        public PackageService(IPackageDatabase db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<IReadOnlyList<NuGetVersion>> FindPackageVersionsAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var localPackages = await _db.FindAsync(id, includeUnlisted: true, cancellationToken);
            var localVersions = localPackages.Select(p => p.Version);

            return localVersions.ToList();
        }

        public async Task<IReadOnlyList<Package>> FindPackagesAsync(string id, CancellationToken cancellationToken)
        {
            return await _db.FindAsync(id, includeUnlisted: true, cancellationToken);
        }

        public async Task<Package> FindPackageOrNullAsync(
            string id,
            NuGetVersion version,
            CancellationToken cancellationToken)
        {
            if (!await MirrorAsync(id, version, cancellationToken))
            {
                return null;
            }

            return await _db.FindOrNullAsync(id, version, includeUnlisted: true, cancellationToken);
        }

        public async Task<bool> ExistsAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        {
            return await MirrorAsync(id, version, cancellationToken);
        }

        public async Task AddDownloadAsync(string packageId, NuGetVersion version, CancellationToken cancellationToken)
        {
            await _db.AddDownloadAsync(packageId, version, cancellationToken);
        }

        /// <summary>
        /// Index the package from an upstream if it does not exist locally.
        /// </summary>
        /// <param name="id">The package ID to index from an upstream.</param>
        /// <param name="version">The package version to index from an upstream.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>True if the package exists locally or was indexed from an upstream source.</returns>
        private async Task<bool> MirrorAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        {
            return await _db.ExistsAsync(id, version, cancellationToken);
        }
    }
}
