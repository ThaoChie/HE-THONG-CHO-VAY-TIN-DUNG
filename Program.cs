using EasyCredit.API.Data;
using EasyCredit.API.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using FluentValidation.AspNetCore;
using FluentValidation;
using EasyCredit.API.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// 1. Đăng ký License QuestPDF
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// 2. Cấu hình Database (Hỗ trợ MySQL với tính năng tự động kết nối lại)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString, 
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure() // Quan trọng khi dùng AWS RDS
    )
);

// 3. Đăng ký các dịch vụ hệ thống
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoanApplicationValidator>();

// 4. Đăng ký Business Services
builder.Services.AddSingleton<EasyCredit.API.Services.LoanRecommendationService>();
builder.Services.AddScoped<CreditScoringService>();

// 5. Cấu hình Authentication (JWT)
var jwtSection = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key is missing"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();

// 6. Cấu hình CORS linh hoạt từ appsettings.json
var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(",") ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Cần thiết nếu bạn dùng Cookie hoặc các loại Auth phức tạp
        });
});

var app = builder.Build();

// 7. Cấu hình Pipeline (Thứ tự các dòng dưới đây là cực kỳ quan trọng)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()  // Cho phép mọi tên miền (Frontend nào cũng vào được)
               .AllowAnyMethod()  // Cho phép mọi method (GET, POST, PUT, DELETE...)
               .AllowAnyHeader(); // Cho phép mọi Header
    });
});
// Chạy CORS trước khi check Auth
app.UseCors("AllowAll");

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

app.Run();