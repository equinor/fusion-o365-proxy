using Azure.Core;
using Fusion.O365Proxy.Authentication;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.O365Proxy.Health
{
    public class ConfigurationHealthCheck : IHealthCheck
    {
        private readonly ILogger<ConfigurationHealthCheck> logger;
        private readonly GraphCredentialsProvider graphCredentialsProvider;

        public ConfigurationHealthCheck(ILogger<ConfigurationHealthCheck> logger, GraphCredentialsProvider graphCredentialsProvider)
        {
            this.logger = logger;
            this.graphCredentialsProvider = graphCredentialsProvider;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var healthCheckResultHealthy = false;

            try
            {
                var credentials = graphCredentialsProvider.GetCredentials();
                var token = await credentials.GetTokenAsync(new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }), CancellationToken.None);
                
                healthCheckResultHealthy = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could generate token to graph api: {Message}", ex.Message);
                healthCheckResultHealthy = false;
            }

            if (healthCheckResultHealthy)
            {
                return HealthCheckResult.Healthy("A healthy result.");
            }

            return HealthCheckResult.Unhealthy("An unhealthy result.");
        }
    }
}
