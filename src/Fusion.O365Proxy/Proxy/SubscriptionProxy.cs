using Fusion.O365Proxy.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ReverseProxy.Service.Proxy;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fusion.O365Proxy.Proxy
{
    public class SubscriptionProxy : ProxyOperation
    {
        public SubscriptionProxy(HttpContext httpContext, HttpMessageInvoker messageInvoker) 
            : base(httpContext, messageInvoker)
        {
        }

        public override async Task HandleAsync()
        {
            #region Authorization

            if (!await httpContext.EnsureAuthenticatedAsync())
                return;

            if (!await AuthorizeAsync())
                return;
            
            #endregion

            var httpProxy = httpContext.RequestServices.GetRequiredService<IHttpProxy>();


            var proxyOptions = await CreateProxyOptionsAsync();

            await httpProxy.ProxyAsync(httpContext, "https://graph.microsoft.com", messageInvoker, proxyOptions);

            var errorFeature = httpContext.Features.Get<IProxyErrorFeature>();
            if (errorFeature != null)
            {
                var error = errorFeature.Error;
                var exception = errorFeature.Exception;

                await httpContext.Response.WriteErrorAsync("ProxyError", $"Proxy operation ended with '{error}' error", exception);
            }
        }

        private async Task<bool> AuthorizeAsync()
        {
            // Check if app is allowed to access functionality
            if (!httpContext.User.IsInRole("Subscriptions.ReadWrite"))
            {
                await httpContext.Response.WriteForbiddenErrorAsync("The 'Subscriptions.ReadWrite' role is required to manage mailbox outlook entities");
                return false;
            }


            // Must process the body to verify the resource
            try
            {
                var mailbox = await GetResourceUserAsync();

                var authorizationResult = await httpContext.AuthorizeAsync(Operations.Edit, new MailboxIdentifier(mailbox));
                if (!authorizationResult.Succeeded)
                {
                    await httpContext.Response.WriteForbiddenErrorAsync($"The app must be granted access in the proxy api, to the mailbox '{mailbox}'");
                    return false;
                }
            }
            catch (ArgumentException ex)
            {
                await httpContext.Response.WriteBadRequestAsync("InvalidInput", ex.Message);
                return false;
            }

            return true;
        }

        private async Task<string> GetResourceUserAsync()
        {
            httpContext.Request.EnableBuffering();
            var body = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
            httpContext.Request.Body.Seek(0, SeekOrigin.Begin); // Reset stream

            var subscriptionDetails = JsonSerializer.Deserialize<GraphSubscriptionRequest>(body);

            if (subscriptionDetails is null)
                throw new ArgumentException("Could not locate the resource.");


            var match = Regex.Match(subscriptionDetails.Resource, "/users/([^/]+)/.*");
            if (!match.Success)
                throw new ArgumentException($"Only resources starting with /users/ is allowed. Found resource '{subscriptionDetails.Resource}'");

            // Process the mailbox authorization
            var user = match.Groups[1].Value;

            if (string.IsNullOrEmpty(user))
                throw new ArgumentException($"User identifier not found in resource path '{subscriptionDetails.Resource}'");

            return user;
        }


        private class GraphSubscriptionRequest
        {
            [JsonPropertyName("resource")]
            public string Resource { get; set; } = null!;
        }
    }

}
