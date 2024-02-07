using KEOPBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<UsersDbContext>(
    o => o.UseSqlServer(builder.Configuration.GetConnectionString("UsersDbContext")));
builder.Services.AddDbContext<NotesDbContext>(
    o => o.UseSqlServer(builder.Configuration.GetConnectionString("NotesDbContext")));
builder.Services.AddDbContext<AlertsDbContext>(
    o => o.UseSqlServer(builder.Configuration.GetConnectionString("AlertsDbContext")));
builder.Services.AddDbContext<SpentAnalysisDbContext>(
    o => o.UseSqlServer(builder.Configuration.GetConnectionString("SpentAnalysisDbContext")));

// Add JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

var app = builder.Build();

// Enable CORS
app.UseCors(policy =>
{
    policy.AllowAnyOrigin();
    policy.AllowAnyHeader();
    policy.AllowAnyMethod();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // Add this line to enable JWT authentication
app.UseAuthorization();

app.MapControllers();

app.Run();
