using Azure.Core;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RapidApi.Cli.Common
{
    class PublicAppTokenCredential : TokenCredential
    {
        IPublicClientApplication app;

        public PublicAppTokenCredential(IPublicClientApplication app)
        {
            this.app = app;
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            var result = app.AcquireTokenInteractive(requestContext.Scopes).ExecuteAsync(cancellationToken).Result;
            return new AccessToken(result.AccessToken, result.ExpiresOn);
        }

        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            var result = await app.AcquireTokenInteractive(requestContext.Scopes).ExecuteAsync(cancellationToken);
            return new AccessToken(result.AccessToken, result.ExpiresOn);
        }
    }
}
