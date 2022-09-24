using Cosmos.BlobService.Config;
using Cosmos.Cms.Common.Data;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Cms.Data.Logic;
using Cosmos.Cms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Rest.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cosmos.Cms.Services
{
    /// <summary>
    /// Azure CDN Service
    /// </summary>
    public class AzureCdnService
    {
        private readonly ArticleEditLogic _articleLogic;
        private readonly CdnManagement _cdnManagement;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="dbContext"></param>
        /// <param name="articleLogic"></param>
        /// <param name="logger"></param>
        public AzureCdnService(IOptions<CosmosConfig> options, ApplicationDbContext dbContext,
            ArticleEditLogic articleLogic, ILogger logger = null)
        {
            _dbContext = dbContext;
            _cdnManagement = new CdnManagement(options.Value.CdnConfig.AzureCdnConfig);
            _articleLogic = articleLogic;
            _logger = logger;
        }

        /// <summary>
        /// CDN Provider Property
        /// </summary>
        public CdnProvider CdnProvider => _cdnManagement.CdnProvider;

        /// <summary>
        ///     Purges one or more end paths on a CDN.
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public async Task<CdnPurgeViewModel> Purge(params string[] paths)
        {
            // Standard Akamai from Azure does not recognize wildcard.  Must purge every published page.
            var profile = await _cdnManagement.GetProfile();
            if (paths.Any(a => a.Equals("/*")) && profile.Sku.Name.Equals("standard_akamai", StringComparison.CurrentCultureIgnoreCase))
            {
                paths = await _dbContext.Articles.Where(a => a.Published != null).Select(s => s.UrlPath).ToArrayAsync();
            }

            var purgePaths = new List<string>();

            if (paths.Any())
            {
                foreach (var url in paths)
                {
                    var trimmed = url.TrimStart('/');
                    if (trimmed.Equals("root", StringComparison.CurrentCultureIgnoreCase)) trimmed = "";

                    purgePaths.Add($"/{trimmed}");
                    purgePaths.Add($"/{trimmed.ToLower()}");
                }
            }

            // Run this asynchronously so we can continue.
            //task.Start();
            var result = SubmitPurge(purgePaths.OrderBy(o => o).ToList());

            var pathString = string.Join(',', paths.ToList()).Replace(",", ", ");

            var model = new CdnPurgeViewModel()
            {
                Detail = $"Paths purged: { pathString }.",
                HttpStatus = System.Net.HttpStatusCode.OK.ToString(),
                PurgeId = result.Id.ToString(),
                SupportId = DateTimeOffset.UtcNow.ToString()
            };
            switch (profile.Sku.Name)
            {
                // Akamai standard does not support wildcard
                case "Standard_Akamai":
                    model.EstimatedSeconds = 20;
                    break;
                case "Standard_Verizon":
                case "Premium_Verizon":
                    model.EstimatedSeconds = 120;
                    break;
                case "Standard_Microsoft":
                    model.EstimatedSeconds = 600;
                    break;
                case "Premium_AzureFrontDoor":
                case "Standard_AzureFrontDoor":
                    model.EstimatedSeconds = 120;
                    break;
                default:
                    return null;
            }
            return model;
        }

        private async Task<AzureOperationResponse> SubmitPurge(List<string> paths)
        {
            try
            {
                return await _cdnManagement.PurgeEndpoint(paths.ToArray());
            }
            catch (Exception e)
            {
                _logger?.LogError("Error purging CDN.", e);
                throw;
            }
        }

    }
}