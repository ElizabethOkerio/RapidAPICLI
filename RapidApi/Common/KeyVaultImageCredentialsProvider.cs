using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using RapidApi.Cli.Common.Models;
using System;
using System.Threading.Tasks;

namespace RapidApi.Cli.Common
{
    class KeyVaultImageCredentialsProvider: IImageCredentialsProvider
    {
        readonly string tenantId;
        readonly string vaultUrl;
        readonly string nameKey;
        readonly string serverKey;
        readonly string usernameKey;
        readonly string passwordKey;

        private const string DEFAULT_SERVER_KEY = "ImageServer";
        private const string DEFAULT_NAME_KEY = "ImageName";
        private const string DEFAULT_USERNAME_KEY = "ImageUsername";
        private const string DEFAULT_PASSWORD_KEY = "ImagePassword";

        private readonly string redirectUrl = "http://localhost";

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
            this.tenantId = tenantId;
            this.vaultUrl = vaultUrl;
            this.nameKey = nameKey;
            this.serverKey = serverKey;
            this.usernameKey = usernameKey;
            this.passwordKey = passwordKey;
        }

        public async Task<ImageCredentials> GetCredentials()
        {
            var options = new InteractiveBrowserCredentialOptions()
            {
                TenantId = tenantId,
            };

            var credential = new InteractiveBrowserCredential(options);
            var client = new SecretClient(new Uri(vaultUrl), credential);

            // wait for the first request to finish so that
            // we the user signs in before the other requests
            // that way the token will be cached and reused for subsequent
            // requests without requiring signin
            var name = await client.GetSecretAsync(nameKey);
            var secrets = await Task.WhenAll(
                client.GetSecretAsync(serverKey),
                client.GetSecretAsync(usernameKey),
                client.GetSecretAsync(passwordKey));

            return new ImageCredentials()
            {
                Name = name.Value.Value,
                Server = secrets[0].Value.Value,
                Password = secrets[1].Value.Value,
                Username = secrets[2].Value.Value
            };
        }
    }
}
