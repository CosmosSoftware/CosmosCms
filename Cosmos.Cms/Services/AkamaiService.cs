using CDT.Akamai.Cdn;
using Cosmos.BlobService.Config;
using Cosmos.Cms.Common.Services.Configurations;
using Microsoft.Extensions.Options;

namespace Cosmos.Cms.Services
{
    public class AkamaiService
    {
        private readonly AkamaiCdnClient _client;
        private readonly IOptions<CosmosConfig> _options;

        public AkamaiService(IOptions<CosmosConfig> options)
        {
            _options = options;
            _client = new AkamaiCdnClient(_options.Value.CdnConfig.AkamaiContextConfig.ClientToken,
                _options.Value.CdnConfig.AkamaiContextConfig.AccessToken,
                _options.Value.CdnConfig.AkamaiContextConfig.Secret,
                _options.Value.CdnConfig.AkamaiContextConfig.AkamaiHost);
        }
        /// <summary>
        /// Purge an entire end point using CP Code
        /// </summary>
        /// <returns></returns>
        public string PurgeCdnByCpCode()
        {
            var purgeObjects = new AkamaiPurgeObjects
            { Objects = new[] { _options.Value.CdnConfig.AkamaiContextConfig.CpCode } };
            return _client.Purge(purgeObjects,
                PurgeEndPoints.CpCodeProductionEndpoint);
        }
        /// <summary>
        /// Purge an array of absolute Uris
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="paths"></param>
        /// <returns></returns>
        public string PurgeCdnByUrls(string hostName, string[] paths)
        {
            return _client.PurgeUrls(hostName, paths);
        }
    }
}