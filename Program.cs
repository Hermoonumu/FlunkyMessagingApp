using System.Text;
using MessagingApp.Services;
using MessagingApp.Services.Implementation;
using MessagingApp.Validator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(),
    builder.Configuration.GetValue<string>("LoggerPath") ?? "./Logs/Log-.json",
    rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();


builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IMessageService, MessageService>();
builder.Services.AddTransient<IChatService, ChatService>();
builder.Services.AddScoped<IValidationService, ValidationService>();

builder.Services.AddControllers();
builder.Services.AddDbContext<DataContext>(option => { option.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")); });


builder.Services.AddSignalR();

builder.Services.AddOpenApi();

builder.Services
    .AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(x =>
    {
        x.RequireHttpsMetadata = false;
        x.SaveToken = true;
        x.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
                builder.Configuration.GetSection("SecConfig").GetValue<String>("PrivateKey")!
            )),
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration.GetSection("SecConfig").GetValue<String>("Issuer"),
            ValidateAudience = true,
            ValidAudience = builder.Configuration.GetSection("SecConfig").GetValue<String>("Audience"),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(0)
        };
        x.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<DataContext>();
                var jti = context.SecurityToken.Id;
                var isRevoked = await db.revokedJWTs.FirstOrDefaultAsync(x => x.JTI == jti);
                if (isRevoked != null) { context.Fail("Token has been revoked"); }
            }
        };
    });



var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    db.Database.Migrate();
}


app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception e)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { Message = "Error has occurred " + e.GetType() });
    }
});

app.Urls.Add("http://0.0.0.0:4200");

app.UseHttpsRedirection();
app.UseRouting();


app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapOpenApi();
app.MapScalarApiReference();



app.Run();

public partial class Program { }