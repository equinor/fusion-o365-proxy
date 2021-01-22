using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Fusion.O365Proxy.Resolvers
{

    public class ApplicationMailboxResolver : IApplicationMailboxResolver
    {
        private readonly IMemoryCache cache;

        public ApplicationMailboxResolver(IMemoryCache cache)
        {
            this.cache = cache;
        }

        public Task<IEnumerable<string>> ResolveOwnedMailboxesAsync(Guid appId)
        {
            var config = GetApplicationConfigs();

            if (config.TryGetValue(appId, out List<string>? allowedMailboxes))
                return Task.FromResult(allowedMailboxes.AsEnumerable());

            return Task.FromResult(Array.Empty<string>().AsEnumerable());
        }

        private Dictionary<Guid, List<string>> GetApplicationConfigs()
        {
            return cache.GetOrCreate("mailboxAccess", i =>
            {
                i.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                var configData = File.ReadAllText("mailboxAccess.json");
                var parsedData = JsonSerializer.Deserialize<MailboxAccessConfig>(configData);

                if (parsedData?.Applications == null)
                    return new();

                var config = parsedData.Applications.ToDictionary(a => a.AppId, a => a.Mailboxes.ToList());
                return config;
            });
        }

        /// <summary>
        /// Json config object
        /// </summary>
        private class MailboxAccessConfig
        {
            [JsonPropertyName("applications")]
            public AppMailboxAccess[]? Applications { get; set; }

            public class AppMailboxAccess
            {
                [JsonPropertyName("appId")]
                public Guid AppId { get; set; }

                [JsonPropertyName("mailboxes")]
                public string[] Mailboxes { get; set; } = null!;
            }
        }
    }

    
}
