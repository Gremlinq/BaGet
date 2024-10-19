using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core;
using BaGet.Protocol.Models;
using Azure.Data.Tables;

namespace BaGet.Azure
{
    public class TableSearchService : ISearchService
    {
        private readonly TableClient _tableClient;
        private readonly ISearchResponseBuilder _responseBuilder;

        public TableSearchService(TableClient tableClient, ISearchResponseBuilder responseBuilder)
        {
            _tableClient = tableClient;
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

        public Task<AutocompleteResponse> ListPackageVersionsAsync(
            VersionsRequest request,
            CancellationToken cancellationToken)
        {
            // TODO: Support versions autocomplete.
            // See: https://github.com/loic-sharma/BaGet/issues/291
            var response = _responseBuilder.BuildAutocomplete(new List<string>());

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

        private static string GenerateSearchFilter(string searchText, bool includePrerelease, bool includeSemVer2)
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

                result = TableClient.CreateQueryFilter<PackageEntity>(x => x.PartitionKey.CompareTo(prefixLower) >= 0 && x.PartitionKey.CompareTo(prefixUpper) <= 0);
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

                return $"({left}) and ({right})";
            }
        }
    }
}
