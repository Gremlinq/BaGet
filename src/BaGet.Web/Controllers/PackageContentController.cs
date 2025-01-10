using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core;
using BaGet.Protocol.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using NuGet.Packaging;
using NuGet.Versioning;

namespace BaGet.Web
{
    /// <summary>
    /// The Package Content resource, used to download content from packages.
    /// See: https://docs.microsoft.com/en-us/nuget/api/package-base-address-resource
    /// </summary>
    public class PackageContentController : Controller
    {
        private readonly IPackageContentService _content;
        private readonly IOptionsSnapshot<BaGetOptions> _options;

        public PackageContentController(IPackageContentService content, IOptionsSnapshot<BaGetOptions> options)
        {
            _options = options;
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public async Task<ActionResult<PackageVersionsResponse>> GetPackageVersionsAsync(string id, CancellationToken cancellationToken)
        {
            if (!_options.Value.ServerMode.HasFlag(ServerMode.Read))
                return Unauthorized();

            return await _content.GetPackageVersionsOrNullAsync(id, cancellationToken) is { } versions
                ? versions
                : NotFound();
        }

        public async Task<IActionResult> DownloadPackageAsync(string id, string version, CancellationToken cancellationToken)
        {
            if (!_options.Value.ServerMode.HasFlag(ServerMode.Read))
                return Unauthorized();

            if (!NuGetVersion.TryParse(version, out var nugetVersion))
                return NotFound();

            return await _content.GetPackageContentStreamOrNullAsync(id, nugetVersion, cancellationToken) is { } packageStream
                ? File(packageStream, "application/octet-stream")
                : NotFound();
        }

        public async Task<IActionResult> DownloadNuspecAsync(string id, string version, CancellationToken cancellationToken)
        {
            if (!_options.Value.ServerMode.HasFlag(ServerMode.Read))
                return Unauthorized();

            if (!NuGetVersion.TryParse(version, out var nugetVersion))
                return NotFound();

            return await _content.GetPackageManifestStreamOrNullAsync(id, nugetVersion, cancellationToken) is { } nuspecStream
                ? File(nuspecStream, "text/xml")
                : NotFound();
        }

        public async Task<IActionResult> DownloadReadmeAsync(string id, string version, CancellationToken cancellationToken)
        {
            if (!_options.Value.ServerMode.HasFlag(ServerMode.Read))
                return Unauthorized();

            if (!NuGetVersion.TryParse(version, out var nugetVersion))
                return NotFound();

            return await _content.GetPackageReadmeStreamOrNullAsync(id, nugetVersion, cancellationToken) is { } readmeStream
                ? File(readmeStream, "text/markdown")
                : NotFound();
        }

        public async Task<IActionResult> DownloadIconAsync(string id, string version, CancellationToken cancellationToken)
        {
            if (!_options.Value.ServerMode.HasFlag(ServerMode.Read))
                return Unauthorized();

            if (!NuGetVersion.TryParse(version, out var nugetVersion))
                return NotFound();

            return await _content.GetPackageIconStreamOrNullAsync(id, nugetVersion, cancellationToken) is { } iconStream
                ? File(iconStream, "image/xyz")
                : NotFound();
        }

        public async Task DownloadEulaAsync(string id, string version, CancellationToken cancellationToken)
        {
            if (_options.Value.ServerMode.HasFlag(ServerMode.Read))
            {
                if (NuGetVersion.TryParse(version, out var nugetVersion))
                {
                    if (await _content.GetPackageContentStreamOrNullAsync(id, nugetVersion, cancellationToken) is { } packageStream)
                    {
                        using (packageStream)
                        {
                            Response.ContentType = "text/plain; charset=utf-8";

                            using (var tempPackageStream = new MemoryStream())
                            {
                                await packageStream.CopyToAsync(tempPackageStream, cancellationToken);

                                tempPackageStream.Position = 0;

                                using (var packageReader = new PackageArchiveReader(tempPackageStream))
                                {
                                    using (var eulaStream = await packageReader.GetStreamAsync("EULA.txt", cancellationToken))
                                    {
                                        await eulaStream.CopyToAsync(Response.BodyWriter.AsStream(), cancellationToken);

                                        return;
                                    }
                                }
                            }
                        }
                    }
                }

                Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            else
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
    }
}
