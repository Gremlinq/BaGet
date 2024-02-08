using System;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaGet.Web
{
    public class SymbolController : Controller
    {
        private readonly IAuthenticationService _authentication;
        private readonly ISymbolIndexingService _indexer;
        private readonly ISymbolStorageService _storage;
        private readonly IOptionsSnapshot<BaGetOptions> _options;
        private readonly ILogger<SymbolController> _logger;

        public SymbolController(
            IAuthenticationService authentication,
            ISymbolIndexingService indexer,
            ISymbolStorageService storage,
            IOptionsSnapshot<BaGetOptions> options,
            ILogger<SymbolController> logger)
        {
            _authentication = authentication ?? throw new ArgumentNullException(nameof(authentication));
            _indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // See: https://docs.microsoft.com/en-us/nuget/api/package-publish-resource#push-a-package
        public async Task<IActionResult> Upload(CancellationToken cancellationToken)
        {
            if (!_options.Value.ServerMode.HasFlag(ServerMode.Write) || !await _authentication.AuthenticateAsync(Request.GetApiKey(), cancellationToken))
                return Unauthorized();

            try
            {
                using (var uploadStream = await Request.GetUploadStreamOrNullAsync(cancellationToken))
                {
                    if (uploadStream == null)
                        return StatusCode(400);

                    var result = await _indexer.IndexAsync(uploadStream, cancellationToken);

                    switch (result)
                    {
                        case SymbolIndexingResult.InvalidSymbolPackage:
                            return StatusCode(400);

                        case SymbolIndexingResult.PackageNotFound:
                            return StatusCode(404);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception thrown during symbol upload");

                return StatusCode(500);
            }

            return StatusCode(201);
        }

        public async Task<IActionResult> Get(string file, string key)
        {
            if (!_options.Value.ServerMode.HasFlag(ServerMode.Read))
                return Unauthorized();

            var pdbStream = await _storage.GetPortablePdbContentStreamOrNullAsync(file, key);
            if (pdbStream == null)
            {
                return NotFound();
            }

            return File(pdbStream, "application/octet-stream");
        }
    }
}
