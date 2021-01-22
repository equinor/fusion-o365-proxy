using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Fusion.O365Proxy.Authorization
{
    public static class Operations
    {
        public static OperationAuthorizationRequirement Edit = new OperationAuthorizationRequirement() { Name = "EDIT" };
    }
}
