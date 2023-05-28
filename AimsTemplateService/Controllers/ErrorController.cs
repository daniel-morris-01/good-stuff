using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace $safeprojectname$.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorController : ControllerBase
    {
        [Route("/error-local-development")]
        public IActionResult ErrorLocalDevelopment([FromServices] IWebHostEnvironment webHostEnvironment, 
                                                   [FromServices] ILogger<ErrorController> logger)
        {
            if (webHostEnvironment.EnvironmentName != "Development")
            {
                throw new InvalidOperationException(
                    "This shouldn't be invoked in non-development environments.");
            }

            var context = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            logger.LogError("Exception in {action} {exception}", context?.Path, context?.Error);

            return Problem(
                detail: context.Error.StackTrace,
                title: context.Error.Message,
                type: context.Error.GetType().Name);
        }

        [Route("/error")]
        public IActionResult Error([FromServices] ILogger<ErrorController> logger)
        {
            var context = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            logger.LogError("Exception in {action} {exception}", context?.Path, context?.Error);
            return Problem();
        }
    }
}
