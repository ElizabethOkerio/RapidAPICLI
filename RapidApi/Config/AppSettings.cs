using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace RapidApi.Cli.Config
{
    class AppSettings
    {
        readonly IConfigurationRoot config;

        public AppSettings(string settingsFile)
        {
            config = new ConfigurationBuilder()
                .AddJsonFile(settingsFile)
                .Build();
        }

        public KeyVaultOptions KeyVault
        {
            get => config.GetSection("KeyVault").Get<KeyVaultOptions>();
        }
    }
}
