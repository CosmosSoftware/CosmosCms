using AspNetCore.Identity.CosmosDb.Extensions;
using AspNetCore.Identity.Services.SendGrid;
using AspNetCore.Identity.Services.SendGrid.Extensions;
using Cosmos.BlobService;
using Cosmos.Cms.Common.Data;
using Cosmos.Cms.Common.Services;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Cms.Data.Logic;
using Cosmos.Cms.Hubs;
using Cosmos.Cms.Services;
using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using System;
using System.Threading.Tasks;

namespace Cosmos.Cms
{
    /// <summary>
    ///     Startup class for the website.
    /// </summary>
    public class Startup
    {

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        ///     Configuration for the website.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        ///     Method configures services for the website.
        /// </summary>
        /// <param name="services"></param>
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // The following line enables Application Insights telemetry collection.
            services.AddApplicationInsightsTelemetry();

            // The Cosmos connection string
            var connectionString = Configuration.GetConnectionString("ApplicationDbContextConnection");

            // Name of the Cosmos database to use
            var cosmosIdentityDbName = Configuration.GetValue<string>("CosmosIdentityDbName");

            // If this is set, the Cosmos identity provider will:
            // 1. Create the database if it does not already exist.
            // 2. Create the required containers if they do not already exist.
            // IMPORTANT: Remove this variable if after first run. It will improve startup performance.
            var setupCosmosDb = Configuration.GetValue<string>("SetupCosmosDb");

            // If the following is set, it will create the Cosmos database and
            //  required containers.
            if (bool.TryParse(setupCosmosDb, out var setup) && setup)
            {
                var builder1 = new DbContextOptionsBuilder<ApplicationDbContext>();
                builder1.UseCosmos(connectionString, cosmosIdentityDbName);

                using (var dbContext = new ApplicationDbContext(builder1.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            }

            //
            // Add the Cosmos database context here
            //
            services.AddDbContext<ApplicationDbContext>(options =>
              options.UseCosmos(connectionString: connectionString, databaseName: cosmosIdentityDbName));

            //
            // Add Cosmos Identity here
            //
            services.AddCosmosIdentity<ApplicationDbContext, IdentityUser, IdentityRole>(
                  options => options.SignIn.RequireConfirmedAccount = true
                )
                .AddDefaultUI() // Use this if Identity Scaffolding added
                .AddDefaultTokenProviders();

            // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-3.1&tabs=visual-studio
            services.ConfigureApplicationCookie(o =>
            {
                o.Cookie.Name = "CosmosAuthCookie";
                o.ExpireTimeSpan = TimeSpan.FromDays(5);
                o.SlidingExpiration = true;
            });

            //
            // Get the boot variables loaded, and
            // do some validation to make sure Cosmos can boot up
            // based on the values given.
            //
            var cosmosStartup = new CosmosStartup(Configuration);

            // Add Cosmos Options
            var option = cosmosStartup.Build();
            services.AddSingleton(option);

            //
            // Add services
            //

            //
            // Must have an Email sender when using Identity Framework.
            // You will need an IEmailProvider. Below uses a SendGrid EmailProvider. You can use another.
            // Below users NuGet package: AspNetCore.Identity.Services.SendGrid
            var sendGridApiKey = Configuration.GetValue<string>("CosmosSendGridApiKey");
            var sendGridOptions = new SendGridEmailProviderOptions(sendGridApiKey, "eric@moonrise.net");
            services.AddSendGridEmailProvider(sendGridOptions);
            // End add SendGrid

            services.AddTransient<StorageContext>(); // Blob storage
            services.AddTransient<TranslationServices>();
            services.AddTransient<ArticleEditLogic>();

            // This is used by the ViewRenderingService 
            // to export web pages for external editing.
            services.AddScoped<IViewRenderService, ViewRenderService>();

            services.AddCors(options =>
            {
                options.AddPolicy("AllCors",
                    policy =>
                    {
                        policy.AllowAnyOrigin().AllowAnyMethod();
                    });
            });

            // Add this before identity
            services.AddControllersWithViews();
            services.AddRazorPages();

            // End before identity

            // Add Kendo services (required for Editor)
            services.AddKendo();

            // Need to add this for Telerik
            // https://docs.telerik.com/aspnet-core/getting-started/prerequisites/environment-support#json-serialization
            services.AddMvc()
                .AddNewtonsoftJson(options =>
                    options.SerializerSettings.ContractResolver =
                        new DefaultContractResolver())
                .AddRazorPagesOptions(options =>
                {
                    // This section docs are here: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/scaffold-identity?view=aspnetcore-3.1&tabs=visual-studio#full
                    //options.AllowAreas = true;
                    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
                    options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
                });

            // https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-2.1&tabs=visual-studio#http-strict-transport-security-protocol-hsts
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
                //options.ExcludedHosts.Add("example.com");
                //options.ExcludedHosts.Add("www.example.com");
            });

            services.ConfigureApplicationCookie(options =>
            {
                // This section docs are here: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/scaffold-identity?view=aspnetcore-3.1&tabs=visual-studio#full
                // The following is when using Docker container with a proxy like
                // Azure front door. It ensures relative paths for redirects
                // which is necessary when the public DNS at Front door is www.mycompany.com 
                // and the DNS of the App Service is something like myappservice.azurewebsites.net.
                options.Events.OnRedirectToLogin = x =>
                {
                    x.Response.Redirect("/Identity/Account/Login");
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToLogout = x =>
                {
                    x.Response.Redirect("/Identity/Account/Logout");
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = x =>
                {
                    x.Response.Redirect("/Identity/Account/AccessDenied");
                    return Task.CompletedTask;
                };
            });

            // BEGIN
            // When deploying to a Docker container, the OAuth redirect_url
            // parameter may have http instead of https.
            // Providers often do not allow http because it is not secure.
            // So authentication will fail.
            // Article below shows instructions for fixing this.
            //
            // NOTE: There is a companion secton below in the Configure method. Must have this
            // app.UseForwardedHeaders();
            //
            // https://seankilleen.com/2020/06/solved-net-core-azure-ad-in-docker-container-incorrectly-uses-an-non-https-redirect-uri/
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                           ForwardedHeaders.XForwardedProto;
                // Only loopback proxies are allowed by default.
                // Clear that restriction because forwarders are enabled by explicit
                // configuration.
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
            // END

            // https://docs.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/5.0/middleware-database-error-page-obsolete
            //services.AddDatabaseDeveloperPageExceptionFilter();

            // Add the SignalR service.
            // If there is a DB connection, then use SQL backplane.
            // See: https://github.com/IntelliTect/IntelliTect.AspNetCore.SignalR.SqlServer
            services.AddSignalR();

            // Add Node Services
            services.AddNodeJS();

        }

        /// <summary>
        ///     This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // BEGIN
            // https://seankilleen.com/2020/06/solved-net-core-azure-ad-in-docker-container-incorrectly-uses-an-non-https-redirect-uri/
            app.UseForwardedHeaders();
            // END

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseCors();

            app.UseResponseCaching(); //https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-3.1

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // Point to the route that will return the SignalR Hub.
                endpoints.MapHub<ChatHub>("/chat");


                endpoints.MapControllerRoute(
                    "MsValidation",
                    ".well-known/microsoft-identity-association.json",
                    new { controller = "Home", action = "GetMicrosoftIdentityAssociation" });

                endpoints.MapControllerRoute(
                    "MyArea",
                    "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                        "default",
                        "{controller=Home}/{action=Index}/{id?}");

                // Deep path
                endpoints.MapFallbackToController("Index", "Home");

                endpoints.MapRazorPages();

            });
        }

    }
}