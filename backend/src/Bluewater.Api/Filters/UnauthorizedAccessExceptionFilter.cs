using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bluewater.Api.Filters;

public class UnauthorizedAccessExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is UnauthorizedAccessException ex)
        {
            context.Result = new UnauthorizedObjectResult(new { message = ex.Message });
            context.ExceptionHandled = true;
        }
    }
}