using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace $safeprojectname$.EF
{
    public partial class $safeprojectname$Context : DbContext
    {
		private readonly IConfiguration configuration;
		
		public $safeprojectname$Context(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
		
		//todo:
		//	1) add Shared\AimsSharedModels to solution and add reference to it from service project
		//	2) add ConnectionStrings__$safeprojectname$=Data Source=localhost;Database=$safeprojectname$;Trusted_Connection=True; to environment variables and restart
		//	3) scaffold: dotnet ef dbcontext scaffold "Data Source=localhost;Database=$safeprojectname$;Trusted_Connection=True;" Microsoft.EntityFrameworkCore.SqlServer -t <TABLE1> -t <TABLE2> -t <TABLE3...> -o EF -c $safeprojectname$ContextTemp
		//	4) copy dbset,OnModelCreating,OnModelCreatingPartial to original dbset then delete the temp one
		//	5) move models to shared project
		//	6) dotnet ef migrations add InitialCreate
		//	7) dotnet ef migrations script
		//	8) run the create migrations table and insert statements only (no need to create the tables that already exists)
		//	later and adding/changing models:
		//	9) dotnet ef migrations add <whatever>
		//	10) dotnet ef database update
		
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = configuration.GetConnectionString("$safeprojectname$");
            optionsBuilder.UseSqlServer(connectionString, x => x.MigrationsHistoryTable("__$safeprojectname$_MigrationHistory"));
            base.OnConfiguring(optionsBuilder);
        }
    }
}
