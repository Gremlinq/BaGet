using BaGet.Azure;
using BaGet.Core;
using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Azure.Storage.Blobs;

namespace BaGet
{
    public static class AzureApplicationExtensions
    {
        public static BaGetApplication AddAzureTableDatabase(this BaGetApplication app)
        {
            app.Services.AddBaGetOptions<AzureTableOptions>("Database");

            app.Services.AddTransient<IPackageDatabase, TablePackageDatabase>();
            app.Services.AddTransient<ISearchService, TableSearchService>();

            app.Services.AddTransient(provider =>
            {
                var options = provider.GetRequiredService<IOptions<AzureTableOptions>>().Value;

                return new TableServiceClient(options.ConnectionString);
            });

            return app;
        }

        public static BaGetApplication AddAzureBlobStorage(this BaGetApplication app)
        {
            app.Services.AddBaGetOptions<AzureBlobStorageOptions>("Storage");
            app.Services.AddTransient<IStorageService, BlobStorageService>();
            app.Services.TryAddTransient<IStorageService>(provider => provider.GetRequiredService<BlobStorageService>());

            app.Services.AddTransient(provider =>
            {
                var options = provider.GetRequiredService<IOptionsSnapshot<AzureBlobStorageOptions>>().Value;

                var client = new BlobServiceClient(options.ConnectionString);

                return client.GetBlobContainerClient(options.Container);
            });

            return app;
        }
    }
}
