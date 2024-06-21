using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace _sever.Controllers.Filter.Exception_Filter
{
    public class GeneralExceptionFilter : IAsyncExceptionFilter
    {
        public Task OnExceptionAsync(ExceptionContext context)
        {
            ObjectResult result = new ObjectResult(new { Code = 500, Msg = "服务器异常" });
            context.Result = result;
            context.ExceptionHandled = true;
            return Task.CompletedTask;
        }
    }
}
