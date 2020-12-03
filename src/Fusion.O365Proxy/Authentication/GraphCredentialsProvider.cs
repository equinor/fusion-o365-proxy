using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace Fusion.O365Proxy.Authentication
{

    public class GraphCredentialsProvider
    {
        private readonly ClientSecretCredential credential;

        public GraphCredentialsProvider(IConfiguration configuration)
        {
            //this.credential = new ClientSecretCredential(configuration["AzureAd:TenantId"], configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"]);

            // In the poc, we want to authenticate against another tenant
            this.credential = new ClientSecretCredential(configuration["GraphCredentials:TenantId"], configuration["GraphCredentials:ClientId"], configuration["GraphCredentials:ClientSecret"]);
        }

        public TokenCredential GetCredentials()
        {
            return credential;
        }
    }
}
