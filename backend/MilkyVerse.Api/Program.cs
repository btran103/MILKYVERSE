using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.Services;
using MilkyVerse.Api.Services.AI;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();
builder.WebHost.UseUrls("http://localhost:5000");

// 1. Controller, MVC Razor Views & JSON serialization
builder.Services.AddControllersWithViews()
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
builder.Services.AddSingleton<IToolRegistry, ToolRegistry>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IGeminiService, GeminiService>();
builder.Services.AddScoped<IToolExecutionService, ToolExecutionService>();
builder.Services.AddScoped<IMilkyAIOrchestrator, MilkyAIOrchestrator>();
builder.Services.AddScoped<IAgentTool, SearchMenuTool>();
builder.Services.AddScoped<IAgentTool, GetProductDetailTool>();
builder.Services.AddScoped<IAgentTool, ListToppingsTool>();
builder.Services.AddScoped<IAgentTool, QuoteDiyDrinkTool>();
builder.Services.AddScoped<IAgentTool, ValidateCouponTool>();
builder.Services.AddScoped<IAgentTool, GetMyOrdersTool>();
builder.Services.AddScoped<IAgentTool, GetOrderStatusTool>();
builder.Services.AddScoped<IAgentTool, AddToCartTool>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddScoped<IMomoPaymentService, MomoPaymentService>();
builder.Services.AddScoped<ISePayService, SePayService>();
builder.Services.AddHostedService<OnlinePaymentExpirationService>();
builder.Services.AddHttpClient();

// 4. JWT Authentication (Read from Environment Variable JWT_SECRET_KEY / user-secrets / appsettings.Local.json)
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
             ?? builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    jwtKey = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production"
        ? throw new InvalidOperationException("JWT_SECRET_KEY environment variable or Jwt:Key user-secret is required in Production.")
        : "MilkyVerseDevFallbackSecretKey2026ForLocalDevelopmentOnly!";
}
builder.Configuration["Jwt:Key"] = jwtKey;
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

// 4b. Rate Limiting for POST /api/ai/chat (20 requests / minute / userId or IP)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"success\":false,\"message\":\"Bạn đã gửi quá 20 tin nhắn/phút tới AI Barista. Vui lòng đợi ít giây rồi thử lại nhé!\"}",
            cancellationToken);
    };

    options.AddPolicy("AiChatLimit", httpContext =>
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var partitionKey = !string.IsNullOrWhiteSpace(userId)
            ? $"user_{userId}"
            : $"ip_{httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous"}";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
});

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
app.UseRateLimiter();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

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
