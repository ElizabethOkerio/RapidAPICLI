using System;
using System.Collections.Generic;
using System.Text;

namespace RapidApi.Cli.Config
{
    class KeyVaultOptions
    {
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string VaultUrl { get; set; }
    }
}
