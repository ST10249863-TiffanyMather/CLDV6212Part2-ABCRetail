using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ABCRetail_Part1.Services;
using ABCRetail_Part1.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ABCRetail_Part1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddSingleton<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/Login";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                });

            builder.Services.AddAuthorization();

            var configuration = builder.Configuration;


            builder.Services.AddSingleton(sp =>
            {
                var connectionString = configuration.GetConnectionString("AzureStorage");
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                return new BlobService(connectionString, httpClientFactory.CreateClient());
            });

            builder.Services.AddSingleton(new TableStorageService(configuration.GetConnectionString("AzureStorage")));

            builder.Services.AddSingleton<QueueService>(sp =>
            {
                var connectionString = configuration.GetConnectionString("AzureStorage");
                return new QueueService(connectionString, "orders");
            });

            builder.Services.AddSingleton<AzureFileShareService>(sp =>
            {
                var connectionString = configuration.GetConnectionString("AzureStorage");
                return new AzureFileShareService(connectionString, "documents");
            });

            builder.Services.AddHttpClient();



            var app = builder.Build();

            //configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                //default HSTS value is 30 days.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}