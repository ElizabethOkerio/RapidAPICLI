using Azure.Core;
using System.Threading;
using System.Threading.Tasks;

namespace RapidApi.Cli.Common
{
    class ExistingTokenTokenCredential : TokenCredential
    {
        AccessToken token;

        public ExistingTokenTokenCredential(AccessToken token)
        {
            this.token = token;
        }
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return token;
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(token);
        }
    }
}
