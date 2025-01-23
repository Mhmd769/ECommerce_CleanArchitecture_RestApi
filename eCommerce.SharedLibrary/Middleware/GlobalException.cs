using eCommerce.SharedLibrary.Logs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace eCommerce.SharedLibrary.Middleware
{
    public class GlobalException(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            //Declare the defaults variables
            string message = "sorry, internal server error occurred. please try again";
            int statuscode=(int)HttpStatusCode.InternalServerError;
            string title = "Error";

            try
            {
                await next(context);
                // check if Response is too many request // 429 status code.
                if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
                {
                    title = "Warning";
                    message = "Too Many Request made";
                    statuscode = (int)StatusCodes.Status429TooManyRequests;
                    await ModifyHeader(context, title, message, statuscode);
                }
                // check if response is UnAuthorized // 401 status code
                if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
                {
                    title = "Alert";
                    message = "You are not Authorized to access";
                    await ModifyHeader(context, title, message, statuscode);

                }

                //if response is forbidden //403 status code
                if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
                {
                    title = "Out of Access";
                    message = " You are not allowed to access here";
                    statuscode = StatusCodes.Status403Forbidden;
                    await ModifyHeader(context, title, message, statuscode);

                }
            }
            catch (Exception ex) {
                //log orginal exception /file , debugger , console 
                LogException.LogExceptions(ex);
                //check if the exception is timeout //408 request timed out 
                if (ex is TaskCanceledException || ex is TimeoutException) {
                    title = "Out of time";
                    message = "Request TimedOut.... Try again";
                    statuscode = StatusCodes.Status408RequestTimeout;
                }

                // if exception is caught .
                // if none of the exceptions then run the default

                await ModifyHeader(context, title, message, statuscode);
            }
        }

        private static async Task ModifyHeader(HttpContext context, string title, string message, int statuscode)
        {
            //display scary-free message to client 
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new ProblemDetails()
            {
                Detail = message,
                Status = statuscode,
                Title = title

            }),CancellationToken.None);
            return;
        }
    }
}
