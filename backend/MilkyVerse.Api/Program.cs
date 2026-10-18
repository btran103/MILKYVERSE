using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

// 1. Controller & JSON serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// 2. Database Context setup (MySQL with auto-fallback for local testing)
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "Auto";
var mySqlConnStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
var sqliteConnStr = builder.Configuration.GetConnectionString("FallbackSqlite") ?? "Data Source=milkyverse_local.db";

bool useMySql = false;

if (dbProvider.Equals("MySQL", StringComparison.OrdinalIgnoreCase) || dbProvider.Equals("Auto", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        // Test if MySQL server is reachable
        using var testConn = new MySqlConnector.MySqlConnection(mySqlConnStr);
        testConn.Open();
        useMySql = true;
    }
    catch
    {
        useMySql = false;
        Console.WriteLine("[MilkyVerse Info] Chưa phát hiện dịch vụ MySQL đang chạy tại localhost:3306. Hệ thống chuyển sang cơ sở dữ liệu dự phòng SQLite để bạn có thể chạy thử nghiệm ngay lập tức!");
    }
}

builder.Services.AddDbContext<MilkyVerseDbContext>(options =>
{
    if (useMySql)
    {
        Console.WriteLine("[MilkyVerse Info] Đang kết nối tới MySQL database (milkyverse_db)...");
        options.UseMySql(mySqlConnStr, ServerVersion.AutoDetect(mySqlConnStr));
    }
    else
    {
        Console.WriteLine("[MilkyVerse Info] Đang sử dụng cơ sở dữ liệu SQLite cục bộ (milkyverse_local.db)...");
        options.UseSqlite(sqliteConnStr);
    }
});

// 3. Register Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IDIYService, DIYService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IAIBaristaService, AIBaristaService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddHttpClient();

// 4. JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "MilkyVerseSuperSecretKey2026MilkyWayGalaxySecurityBeverageApi!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MilkyVerse";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MilkyVerseUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// 5. CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 6. Swagger & OpenAPI with Bearer Authorization
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MILKYVERSE Web API",
        Version = "v1",
        Description = "Hệ thống REST API cho thương mại điện tử trà sữa MilkyVerse tích hợp AI Barista."
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCors("AllowAll");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MILKYVERSE API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 7. Seed sample data on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<MilkyVerseDbContext>();
        await DbInitializer.SeedAsync(context);
        Console.WriteLine("[MilkyVerse Info] Dữ liệu mẫu khởi tạo thành công!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[MilkyVerse Error] Lỗi khởi tạo database: {ex.Message}");
    }
}

app.Run();
