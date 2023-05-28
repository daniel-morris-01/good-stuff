using Microsoft.EntityFrameworkCore;
using $safeprojectname$.EF;

namespace $safeprojectname$.Services
{
    public class $safeprojectname$Main
    {
		private readonly ILogger<$safeprojectname$Main> logger;
		private readonly IDbContextFactory<$safeprojectname$Context> dbContextFactory;
        private readonly IConfiguration configuration;
		
		public $safeprojectname$Main(ILogger<$safeprojectname$Main> logger,IDbContextFactory<$safeprojectname$Context> dbContextFactory
            ,IConfiguration configuration)
        {
            this.logger=logger;
            this.dbContextFactory = dbContextFactory;
            this.configuration = configuration;
        }
    }
}
