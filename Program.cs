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

// =========================================================================
// PHẦN 1: ĐĂNG KÝ DỊCH VỤ (BEFORE BUILD)
// =========================================================================

// 2. Cấu hình Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString, 
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure() // Quan trọng cho AWS RDS
    )
);

// 3. Đăng ký CORS (QUAN TRỌNG: Phải đăng ký trước khi Build)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()   // Chấp nhận mọi nguồn (Frontend, Mobile...)
              .AllowAnyMethod()   // Chấp nhận GET, POST, PUT, DELETE...
              .AllowAnyHeader();  // Chấp nhận mọi Header
    });
});

// 4. Đăng ký Authentication (JWT)
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];

// Kiểm tra Key để tránh lỗi null khi khởi động
if (!string.IsNullOrEmpty(jwtKey))
{
    var key = Encoding.UTF8.GetBytes(jwtKey);
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
}

builder.Services.AddAuthorization();

// 5. Các dịch vụ khác
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoanApplicationValidator>();

// Business Services
builder.Services.AddSingleton<EasyCredit.API.Services.LoanRecommendationService>();
builder.Services.AddScoped<CreditScoringService>();

// =========================================================================
// PHẦN 2: BUILD APP
// =========================================================================
var app = builder.Build();

// =========================================================================
// PHẦN 3: MIDDLEWARE PIPELINE (Thứ tự cực kỳ quan trọng)
// =========================================================================

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ⚠️ QUAN TRỌNG: Đã XÓA dòng app.UseHttpsRedirection(); 
// Lý do: CloudFront gọi Backend qua HTTP. Nếu redirect sang HTTPS sẽ gây lỗi CORS.

app.UseRouting();

// ✅ Kích hoạt CORS (Phải nằm giữa UseRouting và UseAuthentication)
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();