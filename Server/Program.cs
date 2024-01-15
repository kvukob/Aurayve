using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Database;
using Server.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
const string myAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)
).AddCors(options =>
{
    options.AddPolicy(myAllowSpecificOrigins,
        policyBuilder =>
        {
            policyBuilder.WithOrigins("http://localhost:9000").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            //builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        });
});

builder.Services.AddAuthorization();

builder.Services.AddAuthentication(options =>
{
    // Identity made Cookie authentication the default.
    // However, we want JWT Bearer Auth to be the default.
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    // jwt addition
    // get key from settings
    var appSettings = builder.Configuration.GetSection("AppSettings").GetValue<string>("Secret");
    var key = Encoding.ASCII.GetBytes(appSettings);

    //options.Authority = "Authority URL"; // TODO: Update URL
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // If the request is for our hub...
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hub"))
                // Read the token out of the query string
                context.Token = accessToken;

            return Task.CompletedTask;
        }
    };
});


builder.Services.AddSignalR();

var app = builder.Build();


app.UseCors(myAllowSpecificOrigins);

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.MapHub<AppHub>("/hub");
app.Run();