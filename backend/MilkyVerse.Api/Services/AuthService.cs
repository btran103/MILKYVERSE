using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Services;

public interface IAuthService
{
    Task<ApiResponse<SendOtpResultDto>> SendEmailOtpAsync(string email);
    Task<ApiResponse<bool>> VerifyEmailOtpAsync(string email, string code);
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<UserProfileDto>> GetProfileAsync(int userId);
    Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(int userId, RegisterRequest request);
    string GenerateJwtToken(User user);
}

public class AuthService : IAuthService
{
    private readonly MilkyVerseDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(MilkyVerseDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<ApiResponse<SendOtpResultDto>> SendEmailOtpAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return ApiResponse<SendOtpResultDto>.Fail("Địa chỉ email không hợp lệ.");
        }

        var normalizedEmail = email.Trim().ToLower();

        // Generate 6-digit OTP code
        var otpCode = Random.Shared.Next(100000, 999999).ToString();

        // Expire in 10 minutes
        var verification = new EmailVerification
        {
            Email = normalizedEmail,
            Code = otpCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailVerifications.Add(verification);
        await _context.SaveChangesAsync();

        // Attempt SMTP sending if configured, otherwise log clearly to console for development
        var smtpHost = _config["Email:SmtpHost"];
        bool sentViaSmtp = false;

        if (!string.IsNullOrWhiteSpace(smtpHost) && !string.IsNullOrWhiteSpace(_config["Email:Username"]))
        {
            try
            {
                using var client = new System.Net.Mail.SmtpClient(smtpHost)
                {
                    Port = int.Parse(_config["Email:SmtpPort"] ?? "587"),
                    Credentials = new System.Net.NetworkCredential(
                        _config["Email:Username"],
                        _config["Email:Password"]
                    ),
                    EnableSsl = true
                };

                var mail = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress(_config["Email:FromEmail"] ?? "no-reply@milkyverse.com", "MilkyVerse Tea"),
                    Subject = $"[{otpCode}] Mã xác thực tài khoản MilkyVerse",
                    Body = $"Chào bạn,\n\nMã xác nhận tài khoản MilkyVerse của bạn là: {otpCode}\n\nMã có hiệu lực trong 10 phút. Tuyệt đối không chia sẻ mã này cho người khác.\n\nTrân trọng,\nĐội ngũ MilkyVerse.",
                    IsBodyHtml = false
                };
                mail.To.Add(normalizedEmail);

                await client.SendMailAsync(mail);
                sentViaSmtp = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MilkyVerse SMTP Info] Không gửi được qua SMTP: {ex.Message}. Mã OTP: {otpCode}");
            }
        }

        Console.WriteLine($"=================================================");
        Console.WriteLine($"[MILKYVERSE EMAIL VERIFICATION OTP]");
        Console.WriteLine($"Email: {normalizedEmail}");
        Console.WriteLine($"MÃ XÁC THỰC 6 SỐ: {otpCode}");
        Console.WriteLine($"Hiệu lực đến: {verification.ExpiresAt:HH:mm:ss} UTC");
        Console.WriteLine($"=================================================");

        string message = sentViaSmtp
            ? $"Đã gửi mã xác nhận 6 số đến email {normalizedEmail}. Vui lòng kiểm tra hộp thư."
            : $"Mã xác thực tài khoản của bạn là: {otpCode}";

        return ApiResponse<SendOtpResultDto>.Ok(new SendOtpResultDto
        {
            Email = normalizedEmail,
            Code = otpCode,
            Message = message,
            SentViaSmtp = sentViaSmtp
        }, message);
    }

    public async Task<ApiResponse<bool>> VerifyEmailOtpAsync(string email, string code)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
        {
            return ApiResponse<bool>.Fail("Vui lòng cung cấp email và mã xác thực.");
        }

        var normalizedEmail = email.Trim().ToLower();
        var record = await _context.EmailVerifications
            .Where(v => v.Email == normalizedEmail && v.Code == code.Trim() && !v.IsUsed)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync();

        if (record == null)
        {
            return ApiResponse<bool>.Fail("Mã xác thực không chính xác hoặc đã được sử dụng.");
        }

        if (DateTime.UtcNow > record.ExpiresAt)
        {
            return ApiResponse<bool>.Fail("Mã xác thực đã hết hạn (quá 10 phút). Vui lòng yêu cầu mã mới.");
        }

        record.IsUsed = true;
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "Xác thực email thành công!");
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResponse<AuthResponse>.Fail("Email và mật khẩu không được để trống.");
        }

        var normalizedEmail = request.Email.Trim().ToLower();
        var exists = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
        if (exists)
        {
            return ApiResponse<AuthResponse>.Fail("Email này đã được đăng ký trên hệ thống.");
        }

        // Validate password complexity
        if (!IsPasswordStrong(request.Password, out var passError))
        {
            return ApiResponse<AuthResponse>.Fail(passError);
        }

        // Verify OTP if code is provided
        if (!string.IsNullOrWhiteSpace(request.VerificationCode))
        {
            var cleanCode = request.VerificationCode.Trim();
            var record = await _context.EmailVerifications
                .Where(v => v.Email == normalizedEmail && v.Code == cleanCode)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (record == null)
            {
                return ApiResponse<AuthResponse>.Fail("Mã xác thực OTP không chính xác.");
            }

            if (DateTime.UtcNow > record.ExpiresAt)
            {
                return ApiResponse<AuthResponse>.Fail("Mã xác thực OTP đã hết hạn. Vui lòng yêu cầu mã mới.");
            }

            record.IsUsed = true;
            await _context.SaveChangesAsync();
        }
        else
        {
            return ApiResponse<AuthResponse>.Fail("Vui lòng nhập mã xác thực OTP gửi đến email của bạn.");
        }

        var user = new User
        {
            FullName = request.FullName?.Trim() ?? string.Empty,
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty,
            Address = request.Address?.Trim() ?? string.Empty,
            Role = "Customer",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return ApiResponse<AuthResponse>.Ok(new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address
        }, "Đăng ký và xác thực tài khoản thành công!");
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return ApiResponse<AuthResponse>.Fail("Email hoặc mật khẩu không chính xác.");
        }

        var token = GenerateJwtToken(user);

        return ApiResponse<AuthResponse>.Ok(new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address
        }, "Đăng nhập thành công!");
    }

    public async Task<ApiResponse<UserProfileDto>> GetProfileAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return ApiResponse<UserProfileDto>.Fail("Không tìm thấy người dùng.");
        }

        return ApiResponse<UserProfileDto>.Ok(new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        });
    }

    public async Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(int userId, RegisterRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return ApiResponse<UserProfileDto>.Fail("Không tìm thấy người dùng.");
        }

        user.FullName = request.FullName ?? user.FullName;
        user.PhoneNumber = request.PhoneNumber ?? "";
        user.Address = request.Address ?? "";
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        await _context.SaveChangesAsync();

        return ApiResponse<UserProfileDto>.Ok(new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber ?? "",
            Address = user.Address ?? "",
            Role = user.Role,
            CreatedAt = user.CreatedAt
        }, "Cập nhật hồ sơ thành công!");
    }

    public string GenerateJwtToken(User user)
    {
        var jwtSecret = _config["Jwt:Key"] ?? "MilkyVerseSuperSecretKey2026MilkyWayGalaxySecurityBeverageApi!";
        var issuer = _config["Jwt:Issuer"] ?? "MilkyVerse";
        var audience = _config["Jwt:Audience"] ?? "MilkyVerseUsers";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool IsPasswordStrong(string password, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            errorMessage = "Mật khẩu phải có độ dài tối thiểu từ 8 ký tự.";
            return false;
        }

        if (!password.Any(char.IsUpper))
        {
            errorMessage = "Mật khẩu phải chứa ít nhất một chữ cái in hoa (A-Z).";
            return false;
        }

        if (!password.Any(char.IsLower))
        {
            errorMessage = "Mật khẩu phải chứa ít nhất một chữ cái thường (a-z).";
            return false;
        }

        if (!password.Any(char.IsDigit))
        {
            errorMessage = "Mật khẩu phải chứa ít nhất một chữ số (0-9).";
            return false;
        }

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            errorMessage = "Mật khẩu phải chứa ít nhất một ký tự đặc biệt (ví dụ: !@#$%^&*...).";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
