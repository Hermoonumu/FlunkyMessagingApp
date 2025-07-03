
using System.Data;
using System.Text;
using MessagingApp;
using MessagingApp.Services;
using MessagingApp.Services.Implementation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IMessageService, MessageService>();
builder.Services.AddSingleton<ILoggerService, LoggerService>();

builder.Services.AddControllers();
builder.Services.AddDbContext<DataContext>(option => { option.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")); });

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
                builder.Configuration.GetSection("SecConfig").GetValue<String>("PrivateKey")
            )),
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration.GetSection("SecConfig").GetValue<String>("Issuer"),
            ValidateAudience = true,
            ValidAudience = builder.Configuration.GetSection("SecConfig").GetValue<String>("Audience"),
            ValidateLifetime = true,
        };
        x.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<DataContext>();
                var jti = context.SecurityToken.Id;
                var isRevoked = await db.revokedJWTs.FirstOrDefaultAsync(x => x.JTI == jti);
                if (isRevoked!=null){ context.Fail("Token has been revoked"); }
            }
        };
    });



var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();
app.MapOpenApi();
app.MapScalarApiReference();



app.Run();