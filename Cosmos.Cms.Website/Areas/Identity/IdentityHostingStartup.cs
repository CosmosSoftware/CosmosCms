using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(CosmosCMS.Publisher.Iden.Areas.Identity.IdentityHostingStartup))]
namespace CosmosCMS.Publisher.Iden.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {

                //
                // Note, the ApplicationDbContext is already defined in the Startup.cs file
                //
                //services.AddDbContext<ApplicationDbContext>(options =>
                //    options.UseSqlServer(
                //        context.Configuration.GetConnectionString("ApplicationDbContextConnection")));

                //services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                //    .AddEntityFrameworkStores<ApplicationDbContext>();
            });
        }
    }
}