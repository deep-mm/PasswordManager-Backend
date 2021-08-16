using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.WebKey;
using Microsoft.Azure.Services.AppAuthentication;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManagerFunctionApp
{
    public static class Helper
    {
        public static AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
        public static KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
        public static string encryptionKeyUri = "https://passwordmanagervault.vault.azure.net/keys/PasswordManagerEncryptionKey/51b36c3d1de14c399b733ec57b25cd3c";

        public static async Task<string> DecryptSecret(string encryptedSecret)
        {
            try
            {
                string decryptedValue = System.Text.Encoding.UTF8.GetString(keyVaultClient.DecryptAsync(encryptionKeyUri, JsonWebKeyEncryptionAlgorithm.RSAOAEP, System.Text.Encoding.UTF8.GetBytes(encryptedSecret)).GetAwaiter().GetResult().Result);
                return decryptedValue;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to Decrypt Secret. Exception Occured: " + e);
                return null;
            }
        }

        public static async Task<string> EncryptSecret(string decryptedSecret)
        {
            try
            {
                string encryptedValue = System.Text.Encoding.UTF8.GetString(keyVaultClient.EncryptAsync(encryptionKeyUri, JsonWebKeyEncryptionAlgorithm.RSAOAEP, System.Text.Encoding.UTF8.GetBytes(decryptedSecret)).GetAwaiter().GetResult().Result);
                return encryptedValue;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to Encrypt Secret. Exception Occured: " + e);
                return null;
            }
        }
    }
}
