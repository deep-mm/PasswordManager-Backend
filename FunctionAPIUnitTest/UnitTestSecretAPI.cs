using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PasswordManagerFunctionApp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FunctionAPIUnitTest
{
    [TestClass]
    public class UnitTestSecretAPI
    {
        private Mock<IConfiguration> configuration;
        private MockRepository mockRepository;
        private SecretAPI secretAPI;
        private Mock<ILogger> log;
        private Mock<IHelper> helper;
        private Mock<IKeyVaultClient> keyVaultClient;

        [TestInitialize]
        public void BeforeEveryTest()
        {
            this.mockRepository = new MockRepository(MockBehavior.Default);
            this.helper = this.mockRepository.Create<IHelper>();
            this.keyVaultClient = this.mockRepository.Create<IKeyVaultClient>();
            this.configuration = this.mockRepository.Create<IConfiguration>();
            this.secretAPI = new SecretAPI(configuration.Object);
            this.log = this.mockRepository.Create<ILogger>();
        }

        [TestCleanup]
        public void CleanUpEnd()
        {
            //this.mockRepository.VerifyAll();
        }

        public HttpRequest HttpRequestSetup(Dictionary<String, StringValues> query, string body)
        {
            var reqMock = new Mock<HttpRequest>();

            reqMock.Setup(req => req.Query).Returns(new QueryCollection(query));
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(body);
            writer.Flush();
            stream.Position = 0;
            reqMock.Setup(req => req.Body).Returns(stream);
            return reqMock.Object;
        }

        [TestMethod]
        public void CreateSecretSuccessFlow()
        {
            var query = new Dictionary<String, StringValues>();
            var body = "{\"secretName\":\"test\", \"secretValue\":\"test\"}";

            //keyVaultClient.Setup(x => x.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, null, null, default)).Returns(ExecuteSetSecretSuccess());
            helper.Setup(x => x.DecryptSecret(It.IsAny<string>())).Returns(ExecuteDecryptSuccess());

            var result = secretAPI.CreateSecret(req: HttpRequestSetup(query, body), log: log.Object);
            var resultObject = (ObjectResult)result.Result;

            Assert.IsNotNull(resultObject);
            Assert.AreEqual(StatusCodes.Status400BadRequest, resultObject.StatusCode);
        }

        private async Task<string> ExecuteDecryptSuccess()
        {
            return await Task.FromResult(It.IsAny<string>());
        }

        private async Task<SecretBundle> ExecuteSetSecretSuccess()
        {
            return await Task.FromResult(It.IsAny<SecretBundle>());
        }
    }
}
