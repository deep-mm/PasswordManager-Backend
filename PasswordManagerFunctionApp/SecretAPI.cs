using DataAccessLayer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.WebKey;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;

namespace PasswordManagerFunctionApp
{
    public class SecretAPI
    {
        public static AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
        public KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
        public string keyVaultUrl = "";

        public IConfiguration configuration { get; }
        public Helper helper { get; }

        public SecretAPI(IConfiguration configuration)
        {
            this.configuration = configuration;
            keyVaultUrl = configuration["KeyVaultUri"];
            helper = new Helper(configuration);
        }

        [FunctionName("CreateSecret")]
        public async Task<IActionResult> CreateSecret(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "secret")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Creating a new secret in keyvault");
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var input = JsonConvert.DeserializeObject<Secret>(requestBody);

                input.secretValue = await helper.DecryptSecret(input.secretValue);

                var result = await keyVaultClient.SetSecretAsync(keyVaultUrl, input.secretName, input.secretValue).ConfigureAwait(false);

                if (result.Id != null)
                {
                    return new OkObjectResult("Secret Created Succcessfully!");
                }
                else
                {
                    return new BadRequestObjectResult(result);
                }
            }
            catch
            {
                return new BadRequestObjectResult("Error Occured: Failed to add secret to keyvault");
            }
        }

        [FunctionName("GetAllSecrets")]
        public async Task<IActionResult> GetAllSecrets(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "secret")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Getting all secrets from keyvault");

            try
            {
                var response = keyVaultClient.GetSecretsAsync(keyVaultUrl);

                string secretId;
                ArrayList secretList = new ArrayList();

                foreach (Microsoft.Azure.KeyVault.Models.SecretItem secretItem in response.Result)
                {
                    secretId = secretItem.Id;
                    var secretValue = await keyVaultClient.GetSecretAsync(secretId).ConfigureAwait(false);

                    var encyrptedSecretValue = await helper.EncryptSecret(secretValue.Value);

                    Secret s = new Secret() { secretName = secretValue.SecretIdentifier.Name, secretValue = encyrptedSecretValue };
                    secretList.Add(s);
                }

                return new OkObjectResult(secretList);
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult("Error Occured: Failed to get secrets from keyvault. " + e);
            }
        }

        [FunctionName("GetSecretByName")]
        public async Task<IActionResult> GetSecretByName(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "secret/{name}")] HttpRequest req,
            ILogger log, string name)
        {
            log.LogInformation("Getting secret: " + name + " from KeyVault");

            try
            {
                //Get secret value from keyvault
                var response = await keyVaultClient.GetSecretAsync(keyVaultUrl + "secrets/" + name).ConfigureAwait(false);

                if (response.Value != null)
                {
                    var encyrptedSecretValue = await helper.EncryptSecret(response.Value);
                    Secret s = new Secret() { secretName = response.SecretIdentifier.Name, secretValue = encyrptedSecretValue };

                    return new OkObjectResult(s);
                }
                else
                {
                    return new NotFoundObjectResult("Secret :" + name + " not found.");
                }
            }
            catch
            {
                return new BadRequestObjectResult("Error Occured: Failed to get secret from keyvault");
            }
        }

        [FunctionName("UpdateSecret")]
        public async Task<IActionResult> UpdateSecret(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "secret/{name}")] HttpRequest req,
            ILogger log, string name)
        {
            log.LogInformation("Updating secret: " + name + " in KeyVault");

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var secret = JsonConvert.DeserializeObject<Secret>(requestBody);

                secret.secretValue = await helper.DecryptSecret(secret.secretValue);

                var result = await keyVaultClient.SetSecretAsync("https://passwordmanagervault.vault.azure.net/", name, secret.secretValue).ConfigureAwait(false);

                if (result.Id != null)
                {
                    return new OkObjectResult("Secret Updated Succcessfully");
                }
                else
                {
                    return new NotFoundObjectResult("Secret :" + name + " not found.");
                }
            }
            catch
            {
                return new BadRequestObjectResult("Error Occured: Failed to update secret in keyvault");
            }
        }

        [FunctionName("DeleteSecret")]
        public async Task<IActionResult> DeleteSecret(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "secret/{name}")] HttpRequest req,
            ILogger log, string name)
        {
            log.LogInformation("Deleting secret: " + name + " in KeyVault");

            try
            {
                var result = await keyVaultClient.DeleteSecretAsync("https://passwordmanagervault.vault.azure.net/", name).ConfigureAwait(false);

                if (result.Id != null)
                {
                    return new OkObjectResult("Secret Deleted Succcessfully");
                }
                else
                {
                    return new NotFoundObjectResult("Secret :" + name + " not found.");
                }
            }
            catch
            {
                return new BadRequestObjectResult("Error Occured: Failed to delete secret in keyvault");
            }
        }
    }
}
