using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace BaGet.Core
{
    /// <summary>
    /// The "source of truth" for packages' state. Packages' content
    /// are stored by the <see cref="IPackageStorageService"/>.
    /// </summary>
    public interface IPackageDatabase
    {
        /// <summary>
        /// Attempt to add a new package to the database.
        /// </summary>
        /// <param name="package">The package to add to the database.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>The result of attempting to add the package to the database.</returns>
        Task<PackageAddResult> AddAsync(Package package, CancellationToken cancellationToken);

        /// <summary>
        /// Attempt to find a package with the given id and version.
        /// </summary>
        /// <param name="id">The package's id.</param>
        /// <param name="version">The package's version.</param>
        /// <param name="includeUnlisted">Whether unlisted results should be included.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>The package found, or null.</returns>
        Task<Package> FindOrNullAsync(
            string id,
            NuGetVersion version,
            bool includeUnlisted,
            CancellationToken cancellationToken);

        /// <summary>
        /// Attempt to find all packages with a given id.
        /// </summary>
        /// <param name="id">The packages' id.</param>
        /// <param name="includeUnlisted">Whether unlisted results should be included.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>The packages found. Always non-null.</returns>
        Task<IReadOnlyList<Package>> FindAsync(string id, bool includeUnlisted, CancellationToken cancellationToken);

        /// <summary>
        /// Determine whether a package exists in the database (even if the package is unlisted).
        /// </summary>
        /// <param name="id">The package id to search.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>Whether the package exists in the database.</returns>
        Task<bool> ExistsAsync(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Determine whether a package exists in the database (even if the package is unlisted).
        /// </summary>
        /// <param name="id">The package id to search.</param>
        /// <param name="version">The package version to search.</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        /// <returns>Whether the package exists in the database.</returns>
        Task<bool> ExistsAsync(string id, NuGetVersion version, CancellationToken cancellationToken);
    }

    /// <summary>
    /// The result of attempting to add the package to the database.
    /// See <see cref="IPackageDatabase.AddAsync(Package, CancellationToken)"/>
    /// </summary>
    public enum PackageAddResult
    {
        /// <summary>
        /// Failed to add the package as it already exists.
        /// </summary>
        PackageAlreadyExists,

        /// <summary>
        /// The package was added successfully.
        /// </summary>
        Success
    }
}
