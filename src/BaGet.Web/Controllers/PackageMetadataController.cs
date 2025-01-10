using System;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core;
using BaGet.Protocol.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using NuGet.Versioning;

namespace BaGet.Web
{
    /// <summary>
    /// The Package Metadata resource, used to fetch packages' information.
    /// See: https://docs.microsoft.com/en-us/nuget/api/registration-base-url-resource
    /// </summary>
    public class PackageMetadataController : Controller
    {
        private readonly IPackageMetadataService _metadata;
        private readonly IOptionsSnapshot<BaGetOptions> _options;

        public PackageMetadataController(IPackageMetadataService metadata, IOptionsSnapshot<BaGetOptions> options)
        {
            _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            _options = options;
        }

        // GET v3/registration/{id}.json
        [HttpGet]
        public async Task<ActionResult<BaGetRegistrationIndexResponse>> RegistrationIndexAsync(string id, CancellationToken cancellationToken)
        {
            if (!_options.Value.ServerMode.HasFlag(ServerMode.Read))
                return Forbid();

            return await _metadata.GetRegistrationIndexOrNullAsync(id, cancellationToken) is { } index
                ? index
                : NotFound();
        }

        // GET v3/registration/{id}/{version}.json
        [HttpGet]
        public async Task<ActionResult<RegistrationLeafResponse>> RegistrationLeafAsync(string id, string version, CancellationToken cancellationToken)
        {
            if (!_options.Value.ServerMode.HasFlag(ServerMode.Read))
                return Forbid();

            if (NuGetVersion.TryParse(version, out var nugetVersion) && await _metadata.GetRegistrationLeafOrNullAsync(id, nugetVersion, cancellationToken) is { } leaf)
                return leaf;

            return NotFound();
        }
    }
}
