using Cosmos.Cms.Controllers;
using Cosmos.Cms.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Cosmos.Tests
{
    [TestClass]
    public class CORE_G02_EditorSignalTests
    {
        private static Utilities utils;

        private static EditorController Get_SignalSend_Controller()
        {
            var options = utils.GetCosmosConfigOptions();
            options.Value.PrimaryCloud = "azure";

            using var controller =
                utils.GetEditorController(utils.GetPrincipal(TestUsers.Foo).Result);
            return controller;
        }
        private static EditorController Get_SignalRecieve_Controller()
        {
            var options = utils.GetCosmosConfigOptions();
            options.Value.PrimaryCloud = "amazon";

            using var controller =
                utils.GetEditorController(utils.GetPrincipal(TestUsers.Foo).Result);
            return controller;
        }

        private static HomeController Get_AwsHomeControllerInstance()
        {

            var options = utils.GetCosmosConfigOptions();
            options.Value.PrimaryCloud = "amazon";
            options.Value.SiteSettings.IsEditor = false;

            var controller = utils.GetHomeController(utils.GetPrincipal(TestUsers.Foo).Result, false, options);

            return controller;
        }

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            utils = new Utilities();
        }

        [TestMethod]
        public async Task A00_EncryptAndDecrypt_Success()
        {
            using var controller =
                utils.GetEditorController(await utils.GetPrincipal(TestUsers.Foo));

            var testValue = Guid.NewGuid().ToString();

            var encrypted = controller.EncryptString(testValue);

            var decrypted = controller.DecryptString(encrypted);

            Assert.AreEqual(testValue, decrypted);
        }

        [TestMethod]
        public void A01_TestVerify_Fail()
        {
            string encryptedSendMessage = "";
            string encryptedRecievedMessage = "";
            string decryptedRecievedMessage = "";

            using (var sender = Get_SignalSend_Controller())
            {
                encryptedSendMessage = sender.Signal_PrepareMessage("I WILL FAIL!");
            }

            using (var receiver = Get_SignalRecieve_Controller())
            {
                encryptedRecievedMessage = receiver.Signal(encryptedSendMessage);
                decryptedRecievedMessage = receiver.DecryptString(encryptedRecievedMessage);
            }

            var result = JsonConvert.DeserializeObject<SignalResult>(decryptedRecievedMessage);

            Assert.IsTrue(result.HasErrors);
            Assert.AreEqual(1, result.Exceptions.Count);

            using (var sender = Get_SignalSend_Controller())
            {
                Assert.ThrowsException<Exception>(() => sender.Signal_PostProcess<SignalVerifyResult>(encryptedRecievedMessage), "Signal I WILL FAIL! not supported.");
            }
        }


        [TestMethod]
        public void A02_TestSignal_VerifySuccessValid()
        {
            string encryptedSendMessage = "";
            string encryptedRecievedMessage = "";
            string decryptedRecievedMessage;

            using (var sender = Get_SignalSend_Controller())
            {
                encryptedSendMessage = sender.Signal_PrepareMessage("VERIFY|HELLO WORLD!");
            }

            using (var receiver = Get_SignalRecieve_Controller())
            {
                encryptedRecievedMessage = receiver.Signal(encryptedSendMessage);
                decryptedRecievedMessage = receiver.DecryptString(encryptedRecievedMessage);
                Assert.IsTrue(!string.IsNullOrEmpty(decryptedRecievedMessage));
                var signalResult = JsonConvert.DeserializeObject<SignalResult>(decryptedRecievedMessage);

                Assert.IsFalse(signalResult.HasErrors);
                Assert.AreEqual(0, signalResult.Exceptions.Count);
                Assert.IsTrue(!string.IsNullOrEmpty(signalResult.JsonValue));
            }

            using (var sender = Get_SignalSend_Controller())
            {
                var veryfyResult = sender.Signal_PostProcess<SignalVerifyResult>(encryptedRecievedMessage);
                Assert.AreEqual("HELLO WORLD!", veryfyResult.Echo);
            }

        }

    }
}