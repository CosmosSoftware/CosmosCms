using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Cms.Common.Services.Configurations.BootUp;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace Cosmos.Tests
{
    [TestClass]
    public class CORE_H01_DiagnosticTests
    {
        private static Utilities utils;

        private static DiagnosticTests GetTests(IOptions<CosmosConfig> config)
        {
            return new DiagnosticTests(config);
        }

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            utils = new Utilities();
        }

        [TestMethod]
        public async Task Fail_ConfigurationIsNull()
        {

            var options = Options.Create<CosmosConfig>(null);
            var diag = GetTests(options);

            var result = await diag.Run();

            Assert.IsFalse(diag.PublisherIsConfigured);
            Assert.IsFalse(diag.EditorIsConfigured);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Cosmos options value is null.", result[0].Message);

        }

        [TestMethod]
        public async Task SQL_Fail_ConfigExistsMissingEverything()
        {

            var options = utils.GetCosmosConfigOptions();

            options.Value.AuthenticationConfig = null;
            options.Value.CdnConfig.AkamaiContextConfig = null;
            options.Value.CdnConfig.AzureCdnConfig = null;
            options.Value.EditorUrls.Clear();
            options.Value.SecretKey = null;
            options.Value.SendGridConfig = null;
            options.Value.SiteSettings = null;
            options.Value.StorageConfig.AzureConfigs.Clear();
            options.Value.StorageConfig.AmazonConfigs.Clear();
            options.Value.SqlConnectionStrings.Clear();

            var diag = GetTests(options);

            var result = await diag.Run();

            Assert.IsFalse(diag.PublisherIsConfigured);
            Assert.IsFalse(diag.EditorIsConfigured);

            Assert.IsFalse(result.Where(w => w.ServiceType == DiagnosticTests.DBSERVICETYPENAME).All(a => a.Success));
        }

        public async Task SQL_Success_PublisherOk()
        {

            var options = utils.GetCosmosConfigOptions();

            options.Value.AuthenticationConfig = null;
            options.Value.CdnConfig.AkamaiContextConfig = null;
            options.Value.CdnConfig.AzureCdnConfig = null;
            options.Value.EditorUrls.Clear();
            options.Value.SecretKey = null;
            options.Value.SendGridConfig = null;
            options.Value.SiteSettings = null;
            options.Value.StorageConfig.AzureConfigs.Clear();
            options.Value.StorageConfig.AmazonConfigs.Clear();
            // options.Value.SqlConnectionStrings.Clear();

            var diag = GetTests(options);

            var result = await diag.Run();

            Assert.IsTrue(diag.PublisherIsConfigured);
            Assert.IsFalse(diag.EditorIsConfigured);

            Assert.IsFalse(result.Where(w => w.ServiceType == DiagnosticTests.DBSERVICETYPENAME).All(a => a.Success));
        }
    }
}
