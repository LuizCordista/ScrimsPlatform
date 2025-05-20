using System.Net;
using System.Text.Json;
using UserService.CustomException;

namespace UserService.Handler;

public class ExceptionHandlerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var message = ex switch
            {
                ArgumentException => ex.Message,
                UserAlreadyExistsException => ex.Message,
                _ => "An Internal Error Occurred."
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = ex switch
            {
                ArgumentException => (int)HttpStatusCode.BadRequest,
                UserAlreadyExistsException => (int)HttpStatusCode.Conflict,
                _ => (int)HttpStatusCode.InternalServerError
            };

            var result = JsonSerializer.Serialize(new
            {
                context.Response.StatusCode, message
            });

            await context.Response.WriteAsync(result);
        }
    }
}