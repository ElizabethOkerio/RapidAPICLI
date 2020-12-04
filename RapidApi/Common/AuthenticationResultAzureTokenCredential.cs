using Azure.Core;
using Microsoft.Identity.Client;
using System.Threading;
using System.Threading.Tasks;

namespace RapidApi.Cli.Common
{
    class AuthenticationResultAzureTokenCredential: TokenCredential
    {
        AuthenticationResult authResult;

        public AuthenticationResultAzureTokenCredential(AuthenticationResult auth)
        {
            authResult = auth;
        }
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken(authResult.AccessToken, authResult.ExpiresOn);
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
        }
    }
}
