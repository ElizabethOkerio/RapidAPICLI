using Azure.Security.KeyVault.Secrets;
using Microsoft.Identity.Client;
using RapidApi.Cli.Common.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RapidApi.Cli.Common
{
    class KeyVaultImageCredentialsProvider: IImageCredentialsProvider
    {
        string vaultUrl;
        string nameKey;
        string serverKey;
        string usernameKey;
        string passwordKey;
        IPublicClientApplication app;

        private const string DEFAULT_CLIENT_ID = "ae2f7323-9035-4e8d-81c1-3bdcbed2ce19";
        private const string DEFAULT_TENANT_ID = "936c057b-93ca-45bb-91c7-32ac0363257f";
        private const string DEFAULT_VAULT_URL = "https://habbesrapidkv.vault.azure.net/";
        private const string DEFAULT_SERVER_KEY = "ImageServer";
        private const string DEFAULT_NAME_KEY = "ImageName";
        private const string DEFAULT_USERNAME_KEY = "ImageUsername";
        private const string DEFAULT_PASSWORD_KEY = "ImagePassword";

        private readonly string redirectUrl = "http://localhost";
        private readonly string keyVaultScope = "https://vault.azure.net/user_impersonation";

        public KeyVaultImageCredentialsProvider() : this
            (clientId: DEFAULT_CLIENT_ID,
            tenantId: DEFAULT_TENANT_ID,
            vaultUrl: DEFAULT_VAULT_URL,
            nameKey: DEFAULT_NAME_KEY,
            serverKey: DEFAULT_SERVER_KEY,
            usernameKey: DEFAULT_USERNAME_KEY,
            passwordKey: DEFAULT_PASSWORD_KEY)
        { }

        public KeyVaultImageCredentialsProvider(string clientId, string tenantId, string vaultUrl) : this
            (clientId,
            tenantId,
            vaultUrl,
            nameKey: DEFAULT_NAME_KEY,
            serverKey: DEFAULT_SERVER_KEY,
            usernameKey: DEFAULT_USERNAME_KEY,
            passwordKey: DEFAULT_PASSWORD_KEY)
        { }

        public KeyVaultImageCredentialsProvider(
            string clientId,
            string tenantId,
            string vaultUrl,
            string nameKey,
            string serverKey,
            string usernameKey,
            string passwordKey)
        {
            this.vaultUrl = vaultUrl;
            this.nameKey = nameKey;
            this.serverKey = serverKey;
            this.usernameKey = usernameKey;
            this.passwordKey = passwordKey;

            app = PublicClientApplicationBuilder.Create(clientId)
                .WithRedirectUri(redirectUrl)
                .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                .Build();
        }

        public async Task<ImageCredentials> GetCredentials()
        {
            var authResult = await Authenticate();
            var credential = new AuthenticationResultAzureTokenCredential(authResult);
            var client = new SecretClient(new Uri(vaultUrl), credential);

            var secrets = await Task.WhenAll(
                client.GetSecretAsync(nameKey),
                client.GetSecretAsync(serverKey),
                client.GetSecretAsync(usernameKey),
                client.GetSecretAsync(passwordKey));

            return new ImageCredentials()
            {
                Name = secrets[0].Value.Value,
                Server = secrets[1].Value.Value,
                Username = secrets[2].Value.Value,
                Password = secrets[3].Value.Value
            };
        }

        private async Task<AuthenticationResult> Authenticate()
        {
            var accounts = await app.GetAccountsAsync();
            var account = accounts.FirstOrDefault();
            var scopes = new[] { keyVaultScope };

            try
            {
                if (account == null)
                {
                    var result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
                    return result;
                }
                else
                {
                    var result = await app.AcquireTokenSilent(scopes, account).ExecuteAsync();
                    return result;
                }
            }
            catch (Exception e)
            {
                throw new RapidApiException($"Authentication error: {e.Message}");
            }
            
        }
    }
}
