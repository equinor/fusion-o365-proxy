using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Fusion.O365Proxy
{
    public static class HttpResponseExtensions
    {
        public static bool IsSuccessfulResponse(this HttpResponse response)
        {
            return response.StatusCode >= 200 && response.StatusCode < 300;
        }

        public static async Task WriteErrorAsync(this HttpResponse response, string code, string message, Exception ex)
        {
            response.Clear();
            response.ContentType = "application/json";
            response.StatusCode = 500;

            //var problem = new ProblemDetails()
            //{
            //    Detail = message,
            //    Status = 500,
            //    Title = code                
            //};

            await response.WriteAsync(JsonSerializer.Serialize(new
            {

                error = new
                {
                    code = code,
                    message = message,
                    innerError = new
                    {
                        type = ex.GetType().Name,
                        message = ex.Message
                    }
                }
            }));
        }

        public static async Task WriteBadRequestAsync(this HttpResponse response, string code, string message)
        {
            response.Clear();
            response.ContentType = "application/json";
            response.StatusCode = 400;

            //var problem = new ProblemDetails()
            //{
            //    Detail = message,
            //    Status = 500,
            //    Title = code                
            //};

            await response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = new
                {
                    code = code,
                    message = message
                }
            }));
        }

        public static async Task WriteForbiddenErrorAsync(this HttpResponse response, string message)
        {
            response.Clear();
            response.ContentType = "application/json";
            response.StatusCode = 403;

            //var problem = new ProblemDetails()
            //{
            //    Detail = message,
            //    Status = 500,
            //    Title = code                
            //};

            await response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = new
                {
                    code = "Forbidden",
                    message = message
                }
            }));
        }
    }

}
