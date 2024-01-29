using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core;
using BaGet.Protocol.Models;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;

namespace BaGet.Azure
{
    public class TableSearchService : ISearchService
    {
        private readonly TableClient _tableClient;
        private readonly ISearchResponseBuilder _responseBuilder;

        public TableSearchService(
            TableServiceClient client,
            ISearchResponseBuilder responseBuilder,
            IOptionsSnapshot<AzureTableOptions> options)
        {
            _tableClient = client?.GetTableClient(options.Value.TableName) ?? throw new ArgumentNullException(nameof(client));
            _responseBuilder = responseBuilder ?? throw new ArgumentNullException(nameof(responseBuilder));
        }

        public async Task<SearchResponse> SearchAsync(
            SearchRequest request,
            CancellationToken cancellationToken)
        {
            var results = await SearchAsync(
                request.Query,
                request.Skip,
                request.Take,
                request.IncludePrerelease,
                request.IncludeSemVer2,
                cancellationToken);

            return _responseBuilder.BuildSearch(results);
        }

        public async Task<AutocompleteResponse> AutocompleteAsync(
            AutocompleteRequest request,
            CancellationToken cancellationToken)
        {
            var results = await SearchAsync(
                request.Query,
                request.Skip,
                request.Take,
                request.IncludePrerelease,
                request.IncludeSemVer2,
                cancellationToken);

            var packageIds = results.Select(p => p.PackageId).ToList();

            return _responseBuilder.BuildAutocomplete(packageIds);
        }

        public Task<AutocompleteResponse> ListPackageVersionsAsync(
            VersionsRequest request,
            CancellationToken cancellationToken)
        {
            // TODO: Support versions autocomplete.
            // See: https://github.com/loic-sharma/BaGet/issues/291
            var response = _responseBuilder.BuildAutocomplete(new List<string>());

            return Task.FromResult(response);
        }

        public Task<DependentsResponse> FindDependentsAsync(string packageId, CancellationToken cancellationToken)
        {
            var response = _responseBuilder.BuildDependents(new List<PackageDependent>());

            return Task.FromResult(response);
        }

        private async Task<List<PackageRegistration>> SearchAsync(
            string searchText,
            int skip,
            int take,
            bool includePrerelease,
            bool includeSemVer2,
            CancellationToken cancellationToken)
        {
            var results = await _tableClient
                .QueryAsync<PackageEntity>(GenerateSearchFilter(searchText, includePrerelease, includeSemVer2), cancellationToken: cancellationToken)
                .OrderByDescending(x => x.Timestamp)
                .Take(500)
                .ToArrayAsync(cancellationToken);

            return results
                .GroupBy(p => p.Identifier, StringComparer.OrdinalIgnoreCase)
                .Select(group => new PackageRegistration(group.Key, group.Select(x => x.AsPackage()).ToList()))
                .Skip(skip)
                .Take(take)
                .ToList();
        }

        private string GenerateSearchFilter(string searchText, bool includePrerelease, bool includeSemVer2)
        {
            var result = "";

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                // Filter to rows where the "searchText" prefix matches on the partition key.
                var prefix = searchText
                    .TrimEnd()
                    .Split(separator: null)
                    .Last()
                    .ToLowerInvariant();

                var prefixLower = prefix;
                var prefixUpper = prefix + "~";

                var partitionLowerFilter = TableClient.CreateQueryFilter<PackageEntity>(x => x.PartitionKey.CompareTo(prefixLower) >= 0);

                var partitionUpperFilter = TableClient.CreateQueryFilter<PackageEntity>(x => x.PartitionKey.CompareTo(prefixUpper) <= 0);

                result = GenerateAnd(partitionLowerFilter, partitionUpperFilter);
            }

            // Filter to rows that are listed.
            result = GenerateAnd(
                result,
                TableClient.CreateQueryFilter<PackageEntity>(x => x.Listed == true));

            if (!includePrerelease)
            {
                result = GenerateAnd(
                    result,
                    TableClient.CreateQueryFilter<PackageEntity>(x => x.IsPrerelease == false));
            }

            if (!includeSemVer2)
            {
                result = GenerateAnd(
                    result,
                    TableClient.CreateQueryFilter<PackageEntity>(x => x.SemVerLevel == 0));
            }

            return result;

            string GenerateAnd(string left, string right)
            {
                if (string.IsNullOrEmpty(left))
                    return right;

                if (string.IsNullOrEmpty(right))
                    return left;

                return TableClient.CreateQueryFilter($"({left}) and ({right})");
            }
        }
    }
}
