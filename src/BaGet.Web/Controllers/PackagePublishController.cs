using System;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaGet.Web
{
    public class PackagePublishController : Controller
    {
        private readonly IAuthenticationService _authentication;
        private readonly IPackageIndexingService _indexer;
        private readonly IOptionsSnapshot<BaGetOptions> _options;
        private readonly ILogger<PackagePublishController> _logger;

        public PackagePublishController(
            IAuthenticationService authentication,
            IPackageIndexingService indexer,
            IOptionsSnapshot<BaGetOptions> options,
            ILogger<PackagePublishController> logger)
        {
            _authentication = authentication ?? throw new ArgumentNullException(nameof(authentication));
            _indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // See: https://docs.microsoft.com/en-us/nuget/api/package-publish-resource#push-a-package
        public async Task Upload(CancellationToken cancellationToken)
        {
            if (!_options.Value.ServerMode.HasFlag(ServerMode.Write))
            {
                _logger.LogWarning("Attempted upload in read-only mode.");

                HttpContext.Response.StatusCode = 401;
                return;
            }

            if (Request.GetApiKey() is { Length: > 30 } apiKey)
            {
                if (!await _authentication.AuthenticateAsync(apiKey, cancellationToken))
                {
                    _logger.LogWarning($"Authenticating api key {apiKey.Substring(0, 3)}... failed");

                    HttpContext.Response.StatusCode = 401;
                    return;
                }

                try
                {
                    using (var uploadStream = await Request.GetUploadStreamOrNullAsync(cancellationToken))
                    {
                        if (uploadStream == null)
                        {
                            HttpContext.Response.StatusCode = 400;
                            return;
                        }

                        var result = await _indexer.IndexAsync(uploadStream, cancellationToken);

                        switch (result)
                        {
                            case PackageIndexingResult.InvalidPackage:
                                HttpContext.Response.StatusCode = 400;
                                break;

                            case PackageIndexingResult.PackageAlreadyExists:
                                HttpContext.Response.StatusCode = 409;
                                break;

                            case PackageIndexingResult.Success:
                                HttpContext.Response.StatusCode = 201;
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception thrown during package upload");

                    HttpContext.Response.StatusCode = 500;
                }
            }
            else
                _logger.LogWarning("No api key for upload provided.");
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(string id, string version, CancellationToken cancellationToken)
        {
            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> Relist(string id, string version, CancellationToken cancellationToken)
        {
            return Unauthorized();
        }
    }
}
