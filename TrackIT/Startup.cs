using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.EntityFrameworkCore;
using TrackIT.Data;
using TrackIT.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IdentityServer4.Services;
using IdentityServer4.Models;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TrackIT.Settings;

namespace TrackIT
{
    public class Startup
    {
        // see https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-3.0&tabs=windows
        // method 1?
        //private string _trackITApiKey = null; // added
        //private string _trackITApiSecret = null; // added
        //// method 2?
        //private string _trackITApiKey2 = null; // added
        //private string _trackITApiSecret2 = null; // added
        //// method 2?
        //private string _trackITApiKey3 = null; // added
        //private string _trackITApiSecret3 = null; // added

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //// method 1 for reading secrets here
            //_trackITApiKey = Configuration["TrackIT:ApiKey"]; // added
            //_trackITApiSecret = Configuration["TrackIT:ApiSecret"]; // added
            //// method 2 for reading secrets here (mapped to POCO)
            //var trackITConfig2 = Configuration.GetSection("TrackIT").Get<TrackITSettings>(); // added
            //_trackITApiKey2 = trackITConfig2.ApiKey; // added
            //_trackITApiSecret2 = trackITConfig2.ApiSecret; // added
            //var trackITConfig3 = Configuration.GetSection("CloudinarySettings").Get<CloudinarySettings>(); // added
            //_trackITApiKey3 = trackITConfig3.ApiKey; // added
            //_trackITApiSecret3 = trackITConfig3.ApiSecret; // added

            // https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-3.0&tabs=windows
            // makes secrets available in rest of the app through dependency injection
            // use "IOptions<CloudinarySettings> secrets" in the constructor and then read secrets.Value
            // see https://www.twilio.com/blog/2018/05/user-secrets-in-a-net-core-web-app.html
            services.Configure<TrackITSettings>(Configuration.GetSection("TrackIT")); //added
            services.Configure<CloudinarySettings>(Configuration.GetSection("CloudinarySettings")); //added

            // Added to connect to Azure mySQL in App database dynamically (or local dev database)
            string mysql = System.Environment.GetEnvironmentVariable("MYSQLCONNSTR_localdb");
            System.Text.StringBuilder c = new System.Text.StringBuilder(mysql)
                            .Replace("Data Source=127.0.0.1:", "server=localhost;Port=")
                            .Replace("User Id", "uid")
                            .Append(";");
            string connectionString = mysql != null ? c.ToString() : Configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(
                    connectionString));
                    //Configuration.GetConnectionString("DefaultConnection")));

            // add .AddRoles<IdentityRole>()? - not sure if it works yet - doesn't seem to but leave in
            services.AddDefaultIdentity<ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();


            // replace above with below (see https://github.com/aspnet/Identity/issues/1813) to get roles to work. (Otherwise new way is claims). This may cause other problems
            // the AddDefaultIdentity is the preferred way and using Claims
            //services.AddIdentity<ApplicationUser, IdentityRole>()
            //    .AddEntityFrameworkStores<ApplicationDbContext>()
            //    .AddDefaultUI()
            //    .AddDefaultTokenProviders();


            services.AddIdentityServer()
                .AddApiAuthorization<ApplicationUser, ApplicationDbContext>()
                .AddProfileService<ProfileService>();  // added
            

            services.AddAuthentication()
                .AddIdentityServerJwt();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireEmployeeRole", policy => policy.RequireRole("employee"));  // this doesn't seem to work (even with .AddRoles<IdentityRole>() )
                options.AddPolicy("RequireEmployeeRoleClaim", policy => policy.RequireClaim(ClaimTypes.Role, "employee"));  // this works
                options.AddPolicy("RequireManagerRoleClaim", policy => policy.RequireClaim(ClaimTypes.Role, "manager"));
                options.AddPolicy("RequireAdminRoleClaim", policy => policy.RequireClaim(ClaimTypes.Role, "admin"));
            });

            services.AddControllersWithViews();
            services.AddRazorPages();

            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            if (!env.IsDevelopment())
            {
                app.UseSpaStaticFiles();
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseIdentityServer();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseAngularCliServer(npmScript: "start");
                }
            });
        }
    }

    public class ProfileService : IProfileService
    {
        protected UserManager<ApplicationUser> _userManager;

        public ProfileService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var user = await _userManager.GetUserAsync(context.Subject);
            var userRoles = await _userManager.GetRolesAsync(user);  // not saving roles - can't figure out how yet
            var userClaims = await _userManager.GetClaimsAsync(user);
            // Identity4 is being used

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FirstName ?? ""), // was UserName
                new Claim("name", user.FirstName ?? "")  // change this to FirstName to show on navbar
            
            };

            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));  // not saving roles - can't figure out how yet
                claims.Add(new Claim("role", role));           // not saving roles - can't figure out how yet
            }

            foreach (var roleClaim in userClaims)
            {
                claims.Add(new Claim(ClaimTypes.Role, roleClaim.ToString()));
                claims.Add(new Claim("role", roleClaim.ToString()));
            }

            context.IssuedClaims.AddRange(claims);  // save to token
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var user = await _userManager.GetUserAsync(context.Subject);

            context.IsActive = (user != null);
        }
    }


    public class MyCustomClaimsFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        protected UserManager<ApplicationUser> _userManager;

        public MyCustomClaimsFactory(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IOptions<IdentityOptions> optionsAccessor) : base(userManager, roleManager, optionsAccessor)
        {
            _userManager = userManager;
        }

        protected async override Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var identity = await base.GenerateClaimsAsync(user);

            identity.AddClaim(new Claim("FirstName", user.FirstName ?? ""));

            foreach (var role in userRoles)
            {           
                identity.AddClaim(new Claim("role", role));
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }

            return identity;
        }
    }
}
