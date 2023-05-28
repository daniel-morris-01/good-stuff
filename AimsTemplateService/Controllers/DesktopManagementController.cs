using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace $safeprojectname$.Controllers
{
    [ApiController]
    [Authorize]
    public class $safeprojectname$Controller : ControllerBase
    {

        private readonly ILogger<$safeprojectname$Controller> logger;
		private readonly $safeprojectname$.Services.$safeprojectname$Main mainService;

        public $safeprojectname$Controller(ILogger<$safeprojectname$Controller> logger,$safeprojectname$.Services.$safeprojectname$Main mainService)
        {
            this.logger = logger;
			this.mainService = mainService;
        }

		[HttpGet]
		[Route("")]
        public IActionResult Get()
        {
            return Ok("Hello");
        }
		
		[HttpGet]
		[Route("[action]")]
        public IActionResult GetSomethingElse()
        {
            return Ok("Hello something else");
        }
        
    }
}