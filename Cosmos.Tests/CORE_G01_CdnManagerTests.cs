using System.Threading.Tasks;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Cms.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cosmos.Tests
{
    [TestClass]
    public class CORE_G01_CdnManagerTests
    {
        private static Utilities utils;
        private static AzureCdnConfig _azureCdnConfig;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            utils = new Utilities();
            var cosmosConfig = utils.GetCosmosConfigOptions();
            _azureCdnConfig = cosmosConfig.Value.CdnConfig.AzureCdnConfig;
        }

        [TestMethod]
        public void A01_CdnManagerConstruct()
        {
            var manager = new CdnManagement(_azureCdnConfig);
            Assert.IsNotNull(manager);
        }

        [TestMethod]
        public async Task A02_GetCdnProfile()
        {
            var manager = new CdnManagement(_azureCdnConfig);

            var result = await manager.GetProfile();

            Assert.IsTrue(result.Type == "Microsoft.Cdn/profiles");

            Assert.AreEqual("Succeeded", result.ProvisioningState);
            Assert.AreEqual("Active", result.ResourceState );

        }

        [TestMethod]
        public async Task A03_GetEndPoint()
        {
            var manager = new CdnManagement(_azureCdnConfig);

            var result = await manager.GetEndPoint();

            Assert.IsTrue(result.Type == "Microsoft.Cdn/profiles/endpoints");

            Assert.AreEqual("Succeeded", result.ProvisioningState);
            Assert.AreEqual("Running", result.ResourceState );

        }
        
        [TestMethod]
        public async Task A04_CdnManagerPurgeSuccess()
        {
            var manager = new CdnManagement(_azureCdnConfig);

            await manager.PurgeEndpoint(new string[] { "/", "/Cosmos_CMS_Training"});

        }

        // This can take a long time, so comment this out when not needing to test this.
        //[TestMethod]
        //public async Task PurgeCdn()
        //{
        //    var manager = new Management(_azureCdnConfig.TenantId, _azureCdnConfig.TenantDomainName,
        //        _azureCdnConfig.ClientId, _azureCdnConfig.ClientSecret, _azureCdnConfig.SubscriptionId,
        //        _azureCdnConfig.CdnProvider);

        //    var paths = new[] {"/", "/Cosmos_CMS_Training"};

        //    await manager.PurgeEndpoints(_azureCdnConfig.ResourceGroup, _azureCdnConfig.CdnProfileName,
        //        _azureCdnConfig.EndPointName, paths);
        //}
    }
}