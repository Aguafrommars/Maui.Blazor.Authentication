using Duende.IdentityServer.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Serilog;

namespace OidcAndApiServer;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        services.AddRazorPages();
        services.AddControllers();

        var isBuilder = services.AddIdentityServer(options =>
        {
            options.UserInteraction = new UserInteractionOptions()
            {
                LogoutUrl = "/account/logout",
                LoginUrl = "/account/login",
                LoginReturnUrlParameter = "returnUrl"
            };
        }).AddTestUsers(TestUsers.Users);

        // in-memory, code config
        isBuilder.AddInMemoryIdentityResources(Config.IdentityResources)
            .AddInMemoryApiScopes(Config.ApiScopes)
            .AddInMemoryClients(Config.Clients)
            .AddInMemoryApiResources(Config.Apis);

        services.AddAuthorization(options =>
            {
                options.AddPolicy("scope1", builder => builder.RequireClaim("scope", "scope1"));
            })
            .AddAuthentication()
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = "https://localhost:5001";
                options.Audience = "api";
            });

        return builder.Build();
    }
    
    public static WebApplication ConfigurePipeline(this WebApplication app)
    { 
        app.UseSerilogRequestLogging();
    
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles()
            .UseCors(builder =>
            {
                builder.AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithOrigins("https://localhost:7043");
            })
            .UseRouting()
            .UseIdentityServer()
            .UseAuthorization();

        app.MapRazorPages()
            .RequireAuthorization();
        app.MapControllers();

        return app;
    }
}