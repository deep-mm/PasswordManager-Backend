using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.WebKey;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace PasswordManagerFunctionApp
{
    public class Helper
    {
        public static AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
        public static KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
        public static string encryptionKeyUri = "";
        public IConfiguration configuration { get; }
        public Helper(IConfiguration configuration)
        {
            this.configuration = configuration;
            encryptionKeyUri = configuration["KeyVaultEncryptionKeyUrl"];
        }

        public async Task<string> DecryptSecret(string encryptedSecret)
        {
            try
            {
                string decryptedValue = System.Text.Encoding.UTF8.GetString(keyVaultClient.DecryptAsync(encryptionKeyUri, JsonWebKeyEncryptionAlgorithm.RSAOAEP, Convert.FromBase64String(encryptedSecret)).GetAwaiter().GetResult().Result);
                return decryptedValue;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to Decrypt Secret. Exception Occured: " + e);
                throw new Exception("Failed to Decrypt Secret. Exception Occured: " + e);
            }
        }

        public async Task<string> EncryptSecret(string decryptedSecret)
        {
            try
            {
                var encryptedValue = Convert.ToBase64String(keyVaultClient.EncryptAsync(encryptionKeyUri, JsonWebKeyEncryptionAlgorithm.RSAOAEP, System.Text.Encoding.UTF8.GetBytes(decryptedSecret)).GetAwaiter().GetResult().Result);
                return encryptedValue;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to Encrypt Secret. Exception Occured: " + e);
                throw new Exception("Failed to Encrypt Secret. Exception Occured: " + e);
            }
        }
    }
}
