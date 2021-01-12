using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace Fusion.O365Proxy.Authentication
{

    public class GraphCredentialsProvider
    {
        // The tokens are cached in the credentials object.
        private readonly ClientSecretCredential credential;

        public GraphCredentialsProvider(IConfiguration configuration)
        {
            this.credential = new ClientSecretCredential(configuration["AzureAd:TenantId"], configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"]);
        }

        public TokenCredential GetCredentials()
        {
            return credential;
        }
    }
}
