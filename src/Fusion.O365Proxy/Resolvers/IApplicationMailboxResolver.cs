using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fusion.O365Proxy
{
    public interface IApplicationMailboxResolver
    {
        Task<IEnumerable<string>> ResolveOwnedMailboxesAsync(Guid appId);
    }
}
