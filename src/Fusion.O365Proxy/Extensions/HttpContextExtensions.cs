using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Fusion.O365Proxy
{
    public static class HttpContextExtensions
    {
        public static async Task<TResponse> DispatchCommandAsync<TRequest, TResponse>(this HttpContext httpContext, TRequest command)
            where TRequest : IRequest<TResponse>
        {
            var mediator = httpContext.RequestServices.GetRequiredService<IMediator>();
            var response = await mediator.Send(command);
            return response;
        }

        public static async Task<TResponse> DispatchQueryAsync<TResponse>(this HttpContext httpContext, IRequest<TResponse> query)
        {
            var mediator = httpContext.RequestServices.GetRequiredService<IMediator>();
            var response = await mediator.Send(query);
            return response;
        }

        public static async Task DispatchCommandAsync<TRequest>(this HttpContext httpContext, TRequest command)
            where TRequest : IRequest
        {
            var mediator = httpContext.RequestServices.GetRequiredService<IMediator>();
            await mediator.Send(command);
        }

        public static async Task<AuthorizationResult> AuthorizeAsync(this HttpContext httpContext, IAuthorizationRequirement requirement, object resource)
        {
            var authorizationService = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();

            var result = await authorizationService.AuthorizeAsync(httpContext.User, resource, requirement);
            return result;
        }

        public static async Task<bool> EnsureAuthenticatedAsync(this HttpContext httpContext)
        {
            var authResult = await httpContext.AuthenticateAsync();

            if (!authResult.Succeeded)
            {
                httpContext.Response.StatusCode = 401;
                return false;
            }

            return true;
        }
    }

}
