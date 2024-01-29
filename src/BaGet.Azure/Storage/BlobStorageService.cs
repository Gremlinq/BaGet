using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

using BaGet.Core;
using Microsoft.Extensions.Logging;

namespace BaGet.Azure
{
    // See: https://github.com/NuGet/NuGetGallery/blob/master/src/NuGetGallery.Core/Services/CloudBlobCoreFileStorageService.cs
    public class BlobStorageService : IStorageService
    {
        private readonly BlobContainerClient _container;
        private readonly ILogger<BlobStorageService> _logger;

        public BlobStorageService(BlobContainerClient container, ILogger<BlobStorageService> logger)
        {
            _logger = logger;
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public async Task<Stream> GetAsync(string path, CancellationToken cancellationToken)
        {
            return await _container
                .GetBlockBlobClient(path)
                .OpenReadAsync(new BlobOpenReadOptions(true), cancellationToken);
        }

        public async Task<StoragePutResult> PutAsync(
            string path,
            Stream content,
            string contentType,
            CancellationToken cancellationToken)
        {
            var blob = _container.GetBlockBlobClient(path);

            try
            {
                await blob.UploadAsync(
                    content,
                    new BlobUploadOptions()
                    {
                        Conditions = new BlobRequestConditions
                        {
                            IfNoneMatch = ETag.All
                        },
                        HttpHeaders = new BlobHttpHeaders()
                        {
                            ContentType = contentType
                        }
                    },
                    cancellationToken: cancellationToken);

                return StoragePutResult.Success;
            }
            catch (RequestFailedException e) when (e.IsAlreadyExistsException())
            {
                _logger.LogWarning(e, "Uploading a blob failed");

                return StoragePutResult.Conflict;
            }
        }

        public async Task DeleteAsync(string path, CancellationToken cancellationToken)
        {
            await _container
                .GetBlockBlobClient(path)
                .DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
    }
}
