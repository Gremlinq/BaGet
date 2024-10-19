using System;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core;
using BaGet.Protocol.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BaGet.Web
{
    public class SearchController : Controller
    {
        private readonly ISearchService _searchService;
        private readonly IOptionsSnapshot<BaGetOptions> _options;

        public SearchController(ISearchService searchService, IOptionsSnapshot<BaGetOptions> options)
        {
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _options = options;
        }

        public async Task<ActionResult<SearchResponse>> SearchAsync(
            [FromQuery(Name = "q")] string query = null,
            [FromQuery]int skip = 0,
            [FromQuery]int take = 20,
            [FromQuery]bool prerelease = false,
            [FromQuery]string semVerLevel = null,

            // These are unofficial parameters
            [FromQuery]string packageType = null,
            [FromQuery]string framework = null,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Value.ServerMode.HasFlag(ServerMode.Read))
                return Unauthorized();

            var request = new SearchRequest
            {
                Skip = skip,
                Take = take,
                IncludePrerelease = prerelease,
                IncludeSemVer2 = semVerLevel == "2.0.0",
                PackageType = packageType,
                Framework = framework,
                Query = query ?? string.Empty,
            };

            return await _searchService.SearchAsync(request, cancellationToken);
        }
    }
}
