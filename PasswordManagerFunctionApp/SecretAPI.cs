using DataAccessLayer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.WebKey;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;

namespace PasswordManagerFunctionApp
{
    public static class SecretAPI
    {
        public static AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
        public static KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
        public static string keyVaultUrl = "https://passwordmanagervault.vault.azure.net/";

        [FunctionName("CreateSecret")]
        public static async Task<IActionResult> CreateSecret(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "secret")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Creating a new secret in keyvault");
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var input = JsonConvert.DeserializeObject<Secret>(requestBody);

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
        public static async Task<IActionResult> GetAllSecrets(
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

                    var encyrptedSecretValue = keyVaultClient.EncryptAsync("https://passwordmanagervault.vault.azure.net/keys/PasswordManagerEncryptionKey/51b36c3d1de14c399b733ec57b25cd3c", JsonWebKeyEncryptionAlgorithm.RSAOAEP, System.Text.Encoding.UTF8.GetBytes(secretValue.Value)).GetAwaiter().GetResult().Result;

                    Secret s = new Secret() { secretName = secretValue.SecretIdentifier.Name, secretValue = System.Text.Encoding.UTF8.GetString(encyrptedSecretValue) };
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
        public static async Task<IActionResult> GetSecretByName(
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
                    var encyrptedSecretValue = keyVaultClient.EncryptAsync(keyVaultUrl, "PasswordManagerEncryptionKey", "51b36c3d1de14c399b733ec57b25cd3c", JsonWebKeyEncryptionAlgorithm.RSAOAEP, System.Text.Encoding.UTF8.GetBytes(response.Value)).GetAwaiter().GetResult().Result;
                    Secret s = new Secret() { secretName = response.SecretIdentifier.Name, secretValue = System.Text.Encoding.UTF8.GetString(encyrptedSecretValue) };

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
        public static async Task<IActionResult> UpdateSecret(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "secret/{name}")] HttpRequest req,
            ILogger log, string name)
        {
            log.LogInformation("Updating secret: " + name + " in KeyVault");

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var secret = JsonConvert.DeserializeObject<Secret>(requestBody);

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
        public static async Task<IActionResult> DeleteSecret(
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
