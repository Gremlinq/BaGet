using BaGet.Azure;
using BaGet.Core;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Blob;
using CloudStorageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount;
using StorageCredentials = Microsoft.WindowsAzure.Storage.Auth.StorageCredentials;
using TableStorageAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount;

namespace BaGet
{
    public static class AzureApplicationExtensions
    {
        public static BaGetApplication AddAzureTableDatabase(this BaGetApplication app)
        {
            app.Services.AddBaGetOptions<AzureTableOptions>("Database");

            app.Services.AddTransient<IPackageDatabase, TablePackageDatabase>();
            app.Services.AddTransient<TableOperationBuilder>();
            app.Services.AddTransient<ISearchService, TableSearchService>();
            app.Services.TryAddTransient<IPackageDatabase, TablePackageDatabase>();
            app.Services.TryAddTransient<ISearchService, TableSearchService>();
            app.Services.TryAddTransient<ISearchIndexer, NullSearchIndexer>();

            app.Services.AddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<AzureTableOptions>>().Value;

                return TableStorageAccount.Parse(options.ConnectionString);
            });

            app.Services.AddTransient(provider =>
            {
                var account = provider.GetRequiredService<TableStorageAccount>();

                return account.CreateCloudTableClient();
            });

            return app;
        }

        public static BaGetApplication AddAzureBlobStorage(this BaGetApplication app)
        {
            app.Services.AddBaGetOptions<AzureBlobStorageOptions>("Storage");
            app.Services.AddTransient<IStorageService, BlobStorageService>();
            app.Services.TryAddTransient<IStorageService>(provider => provider.GetRequiredService<BlobStorageService>());

            app.Services.AddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<AzureBlobStorageOptions>>().Value;

                if (!string.IsNullOrEmpty(options.ConnectionString))
                {
                    return CloudStorageAccount.Parse(options.ConnectionString);
                }

                return new CloudStorageAccount(
                    new StorageCredentials(
                        options.AccountName,
                        options.AccessKey),
                    useHttps: true);
            });

            app.Services.AddTransient(provider =>
            {
                var options = provider.GetRequiredService<IOptionsSnapshot<AzureBlobStorageOptions>>().Value;
                var account = provider.GetRequiredService<CloudStorageAccount>();

                var client = account.CreateCloudBlobClient();

                return client.GetContainerReference(options.Container);
            });

            return app;
        }
    }
}
