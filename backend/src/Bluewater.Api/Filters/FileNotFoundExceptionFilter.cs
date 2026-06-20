using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bluewater.Api.Filters;

public class FileNotFoundExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is FileNotFoundException ex)
        {
            context.Result = new NotFoundObjectResult(new { message = ex.Message });
            context.ExceptionHandled = true;
        }
    }
}
