using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Services;

public interface IAuthService
{
    Task<ApiResponse<SendOtpResultDto>> SendEmailOtpAsync(string email, string? purpose = null);
    Task<ApiResponse<bool>> VerifyEmailOtpAsync(string email, string code, bool consumeNow = true);
    Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request);
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
    private readonly PasswordHasher<User> _identityHasher = new();

    // Bảo mật chống brute-force OTP (tối đa 5 lần nhập sai cho mỗi mã OTP)
    private static readonly ConcurrentDictionary<string, int> _failedAttemptsByEmail = new(StringComparer.OrdinalIgnoreCase);
    // Đánh dấu email đã xác thực bước 2 (Verify OTP) thành công trong luồng Quên mật khẩu
    private static readonly ConcurrentDictionary<string, (string Code, DateTime VerifiedAt)> _verifiedResetSessions = new(StringComparer.OrdinalIgnoreCase);

    public AuthService(MilkyVerseDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<ApiResponse<SendOtpResultDto>> SendEmailOtpAsync(string email, string? purpose = null)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || !email.Contains('.'))
        {
            return ApiResponse<SendOtpResultDto>.Fail("Địa chỉ email không hợp lệ. Vui lòng nhập đúng định dạng email (ví dụ: ten@gmail.com).");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var cleanPurpose = (purpose ?? "general").Trim().ToLowerInvariant();

        // 1. Kiểm tra điều kiện nghiệp vụ theo luồng Đăng ký vs Quên mật khẩu
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        if (cleanPurpose == "register" && existingUser != null && existingUser.IsEmailVerified)
        {
            return ApiResponse<SendOtpResultDto>.Fail("Email này đã được đăng ký và kích hoạt trên hệ thống. Vui lòng đăng nhập hoặc dùng chức năng Quên mật khẩu.");
        }

        if (cleanPurpose == "forgot-password" && existingUser == null)
        {
            return ApiResponse<SendOtpResultDto>.Fail("Email này chưa được đăng ký tài khoản trên hệ thống MilkyVerse.");
        }

        // 2. Chống spam email & giới hạn tần suất gửi lại mã (Rate Limiting)
        int maxSendPer15Min = int.TryParse(_config["Email:MaxSendPer15Minutes"], out var m15) ? m15 : 5;
        int resendCooldownSec = int.TryParse(_config["Email:ResendCooldownSeconds"], out var cd) ? cd : 60;
        int expiryMinutes = int.TryParse(_config["Email:OtpExpiryMinutes"], out var exp) ? exp : 5;

        var fifteenMinsAgo = DateTime.UtcNow.AddMinutes(-15);
        var recentOtps = await _context.EmailVerifications
            .Where(v => v.Email == normalizedEmail && v.CreatedAt >= fifteenMinsAgo)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();

        if (recentOtps.Count >= maxSendPer15Min)
        {
            return ApiResponse<SendOtpResultDto>.Fail($"Bạn đã yêu cầu gửi OTP quá {maxSendPer15Min} lần trong 15 phút. Vui lòng đợi ít phút trước khi thử lại để tránh spam.");
        }

        var latestOtp = recentOtps.FirstOrDefault();
        if (latestOtp != null)
        {
            var elapsedSec = (DateTime.UtcNow - latestOtp.CreatedAt).TotalSeconds;
            // Cho phép khoảng đệm 45s để đồng bộ đồng hồ client-server
            if (elapsedSec < Math.Min(45, resendCooldownSec))
            {
                int waitRemain = (int)Math.Ceiling(resendCooldownSec - elapsedSec);
                return ApiResponse<SendOtpResultDto>.Fail($"Vui lòng đợi {waitRemain} giây nữa trước khi yêu cầu gửi lại mã OTP mới.");
            }
        }

        // 3. Vô hiệu hóa toàn bộ các mã OTP cũ chưa sử dụng của email này (chỉ mã mới nhất có hiệu lực)
        var activeOldOtps = await _context.EmailVerifications
            .Where(v => v.Email == normalizedEmail && !v.IsUsed)
            .ToListAsync();
        foreach (var old in activeOldOtps)
        {
            old.IsUsed = true;
        }

        // Reset bộ đếm nhập sai khi cấp mã OTP mới
        _failedAttemptsByEmail.TryRemove(normalizedEmail, out _);

        // 4. Sinh mã OTP 6 chữ số ngẫu nhiên an toàn mật mã học, hiệu lực đúng 5 phút
        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var verification = new EmailVerification
        {
            Email = normalizedEmail,
            Code = otpCode,
            ExpiresAt = expiresAt,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailVerifications.Add(verification);
        await _context.SaveChangesAsync();

        // 5. Gửi email thật 100% qua Gmail SMTP từ milkyverse103@gmail.com (Đã bỏ hoàn toàn chế độ giả lập OTP)
        var smtpHost = Environment.GetEnvironmentVariable("MILKYVERSE_SMTP_HOST") ?? _config["Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPortStr = Environment.GetEnvironmentVariable("MILKYVERSE_SMTP_PORT") ?? _config["Email:SmtpPort"] ?? "587";
        var smtpUser = Environment.GetEnvironmentVariable("MILKYVERSE_SMTP_EMAIL")
                       ?? Environment.GetEnvironmentVariable("MILKYVERSE_SMTP_USERNAME")
                       ?? (!string.IsNullOrWhiteSpace(_config["Email:Username"]) ? _config["Email:Username"] : "milkyverse103@gmail.com");
        var smtpPass = Environment.GetEnvironmentVariable("MILKYVERSE_SMTP_APP_PASSWORD")
                       ?? Environment.GetEnvironmentVariable("MILKYVERSE_SMTP_PASSWORD")
                       ?? _config["Email:Password"];
        var fromEmail = Environment.GetEnvironmentVariable("MILKYVERSE_SMTP_FROM")
                        ?? (!string.IsNullOrWhiteSpace(_config["Email:FromEmail"]) ? _config["Email:FromEmail"] : smtpUser)
                        ?? "milkyverse103@gmail.com";
        var fromName = _config["Email:FromName"] ?? "MILKYVERSE Tea & Food";

        if (string.IsNullOrWhiteSpace(smtpPass))
        {
            return ApiResponse<SendOtpResultDto>.Fail(
                "Chưa cấu hình Mật khẩu ứng dụng (App Password 16 ký tự) cho Gmail 'milkyverse103@gmail.com'. Vui lòng điền App Password vào mục Email:Password trong appsettings.json (hoặc biến môi trường MILKYVERSE_SMTP_APP_PASSWORD).");
        }

        try
        {
            using var client = new SmtpClient(smtpHost)
            {
                Port = int.TryParse(smtpPortStr, out var port) ? port : 587,
                Credentials = new NetworkCredential(smtpUser!.Trim(), smtpPass.Replace(" ", "").Trim()),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout = 20000
            };

            string actionTitle = cleanPurpose == "forgot-password"
                ? "Khôi Phục Mật Khẩu Tài Khoản"
                : "Kích Hoạt Đăng Ký Tài Khoản";

            string htmlBody = $@"
<!DOCTYPE html>
<html lang='vi'>
<head><meta charset='UTF-8'></head>
<body style='margin:0;padding:24px;background-color:#FAF7F2;font-family:Arial,sans-serif;color:#2D2522;'>
  <div style='max-width:520px;margin:0 auto;background:#FFFFFF;border:1px solid #EFE6DC;border-radius:24px;overflow:hidden;box-shadow:0 8px 24px rgba(111,78,55,0.08);'>
    <div style='background:linear-gradient(135deg,#4A3324,#6F4E37);padding:24px;text-align:center;color:#FAF7F2;'>
      <h1 style='margin:0;font-size:22px;letter-spacing:1.5px;'>🧋 MILKYVERSE</h1>
      <p style='margin:6px 0 0;font-size:12px;color:#F7D6D0;'>Một ly trà sữa, ngàn cách tận hưởng</p>
    </div>
    <div style='padding:28px 24px;text-align:center;'>
      <h2 style='margin:0 0 12px;font-size:18px;color:#4A3324;'>{actionTitle}</h2>
      <p style='font-size:13px;color:#786C65;line-height:1.6;margin:0 0 20px;'>
        Xin chào <strong>{normalizedEmail}</strong>,<br/>
        Sử dụng mã OTP gồm 6 chữ số dưới đây để hoàn tất xác thực trên hệ thống MilkyVerse:
      </p>
      <div style='display:inline-block;background:#FAF7F2;border:2px dashed #6F4E37;border-radius:16px;padding:16px 32px;margin:8px 0 20px;'>
        <span style='font-size:32px;font-weight:900;letter-spacing:10px;color:#6F4E37;font-family:monospace;'>{otpCode}</span>
      </div>
      <p style='font-size:12px;color:#B91C1C;font-weight:bold;margin:0 0 16px;'>
        ⏳ Mã OTP có hiệu lực trong vòng {expiryMinutes} phút và chỉ sử dụng 1 lần duy nhất.
      </p>
      <p style='font-size:11px;color:#786C65;line-height:1.5;margin:0;border-top:1px solid #EFE6DC;padding-top:16px;'>
        Vì lý do bảo mật, tuyệt đối không chia sẻ mã này cho bất kỳ ai. Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.
      </p>
    </div>
  </div>
</body>
</html>";

            using var mail = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName, Encoding.UTF8),
                Subject = $"[{otpCode}] Mã OTP {actionTitle} - MILKYVERSE",
                Body = htmlBody,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            mail.To.Add(normalizedEmail);

            await client.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            // Hủy bản ghi OTP vừa tạo nếu gửi mail thất bại để người dùng gửi lại ngay mà không bị vướng cooldown
            _context.EmailVerifications.Remove(verification);
            await _context.SaveChangesAsync();
            return ApiResponse<SendOtpResultDto>.Fail($"Không thể gửi email OTP từ '{smtpUser}' qua Gmail SMTP: {ex.Message}. Vui lòng kiểm tra lại App Password 16 ký tự của Gmail.");
        }

        string message = $"Đã gửi mã xác thực OTP 6 chữ số từ {fromEmail} đến hộp thư {normalizedEmail} (hiệu lực {expiryMinutes} phút). Vui lòng kiểm tra Inbox hoặc mục Thư rác (Spam).";

        return ApiResponse<SendOtpResultDto>.Ok(new SendOtpResultDto
        {
            Email = normalizedEmail,
            Code = string.Empty, // Bảo mật tuyệt đối: Không bao giờ trả mã OTP về trình duyệt
            Message = message,
            SentViaSmtp = true,
            ExpiresInSeconds = expiryMinutes * 60,
            ResendCooldownSeconds = resendCooldownSec
        }, message);
    }

    public async Task<ApiResponse<bool>> VerifyEmailOtpAsync(string email, string code, bool consumeNow = true)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
        {
            return ApiResponse<bool>.Fail("Vui lòng nhập đầy đủ địa chỉ email và mã OTP 6 chữ số.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var cleanCode = code.Trim();

        if (cleanCode.Length != 6 || !cleanCode.All(char.IsDigit))
        {
            return ApiResponse<bool>.Fail("Mã OTP phải bao gồm đúng 6 chữ số.");
        }

        int maxFailed = int.TryParse(_config["Email:MaxFailedAttempts"], out var mf) ? mf : 5;

        // Lấy bản ghi OTP mới nhất chưa sử dụng của email này
        var latestRecord = await _context.EmailVerifications
            .Where(v => v.Email == normalizedEmail && !v.IsUsed)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestRecord == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy mã OTP hợp lệ hoặc mã đã được sử dụng. Vui lòng bấm 'Gửi lại mã OTP'.");
        }

        if (DateTime.UtcNow > latestRecord.ExpiresAt)
        {
            latestRecord.IsUsed = true;
            await _context.SaveChangesAsync();
            return ApiResponse<bool>.Fail("Mã xác thực OTP đã hết hạn (quá 5 phút). Vui lòng bấm 'Gửi lại mã OTP' để nhận mã mới.");
        }

        if (!string.Equals(latestRecord.Code, cleanCode, StringComparison.Ordinal))
        {
            int fails = _failedAttemptsByEmail.AddOrUpdate(normalizedEmail, 1, (_, count) => count + 1);
            int remaining = Math.Max(0, maxFailed - fails);

            if (fails >= maxFailed)
            {
                latestRecord.IsUsed = true;
                await _context.SaveChangesAsync();
                _failedAttemptsByEmail.TryRemove(normalizedEmail, out _);
                return ApiResponse<bool>.Fail($"Bạn đã nhập sai mã OTP quá {maxFailed} lần cho phép. Mã OTP này đã bị khóa, vui lòng yêu cầu gửi lại mã mới.");
            }

            return ApiResponse<bool>.Fail($"Mã xác thực OTP không chính xác. Bạn còn {remaining} lần thử trước khi mã bị vô hiệu hóa.");
        }

        // Xác thực khớp thành công -> Xóa bộ đếm lỗi
        _failedAttemptsByEmail.TryRemove(normalizedEmail, out _);
        _verifiedResetSessions[normalizedEmail] = (cleanCode, DateTime.UtcNow);

        if (consumeNow)
        {
            latestRecord.IsUsed = true;
            await _context.SaveChangesAsync();
        }

        return ApiResponse<bool>.Ok(true, "Xác thực mã OTP thành công!");
    }

    public async Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return ApiResponse<bool>.Fail("Vui lòng cung cấp đầy đủ email, mã OTP đã xác thực và mật khẩu mới.");
        }

        if (!string.IsNullOrEmpty(request.ConfirmPassword) && request.NewPassword != request.ConfirmPassword)
        {
            return ApiResponse<bool>.Fail("Mật khẩu xác nhận không trùng khớp với mật khẩu mới.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        if (user == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy tài khoản đã đăng ký với địa chỉ email này.");
        }

        if (!IsPasswordStrong(request.NewPassword, out var passError))
        {
            return ApiResponse<bool>.Fail(passError);
        }

        var cleanCode = request.Code.Trim();

        // Kiểm tra OTP trong DB (hoặc phiên vừa verify ở Bước 2 trong vòng 5 phút)
        var verifyResult = await VerifyEmailOtpAsync(normalizedEmail, cleanCode, consumeNow: true);
        if (!verifyResult.Success)
        {
            // Kiểm tra xem mã vừa được consume ở bước VerifyOtp cách đây < 5 phút không
            if (!_verifiedResetSessions.TryGetValue(normalizedEmail, out var session) ||
                session.Code != cleanCode ||
                (DateTime.UtcNow - session.VerifiedAt).TotalMinutes > 5)
            {
                return verifyResult;
            }
        }

        // Vô hiệu hóa toàn bộ OTP cũ của email này trong Database
        var allUnused = await _context.EmailVerifications
            .Where(v => v.Email == normalizedEmail && !v.IsUsed)
            .ToListAsync();
        foreach (var item in allUnused)
        {
            item.IsUsed = true;
        }
        _verifiedResetSessions.TryRemove(normalizedEmail, out _);

        // Hash mật khẩu mới và cập nhật Database
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.IsEmailVerified = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "Đổi mật khẩu mới thành công! Mã OTP cũ đã được vô hiệu hóa. Bạn có thể đăng nhập ngay bằng mật khẩu mới.");
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResponse<AuthResponse>.Fail("Email và mật khẩu không được để trống.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        if (existingUser != null && existingUser.IsEmailVerified)
        {
            return ApiResponse<AuthResponse>.Fail("Email này đã được đăng ký và kích hoạt trên hệ thống.");
        }

        if (!IsPasswordStrong(request.Password, out var passError))
        {
            return ApiResponse<AuthResponse>.Fail(passError);
        }

        if (string.IsNullOrWhiteSpace(request.VerificationCode))
        {
            return ApiResponse<AuthResponse>.Fail("Bắt buộc phải nhập mã xác thực OTP 6 chữ số gửi tới email để kích hoạt tài khoản.");
        }

        // Xác thực OTP nghiêm ngặt (kiểm tra 5 phút, giới hạn 5 lần sai, đánh dấu IsUsed = true ngay sau khi dùng)
        var otpCheck = await VerifyEmailOtpAsync(normalizedEmail, request.VerificationCode.Trim(), consumeNow: true);
        if (!otpCheck.Success)
        {
            return ApiResponse<AuthResponse>.Fail(otpCheck.Message);
        }

        // Vô hiệu hóa tất cả OTP còn lại của email này
        var remainingOtps = await _context.EmailVerifications
            .Where(v => v.Email == normalizedEmail && !v.IsUsed)
            .ToListAsync();
        foreach (var r in remainingOtps)
        {
            r.IsUsed = true;
        }

        User user;
        if (existingUser != null)
        {
            user = existingUser;
            user.FullName = request.FullName?.Trim() ?? user.FullName;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            user.PhoneNumber = request.PhoneNumber?.Trim() ?? user.PhoneNumber;
            user.Address = request.Address?.Trim() ?? user.Address;
            user.IsEmailVerified = true;
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            user = new User
            {
                FullName = request.FullName?.Trim() ?? string.Empty,
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty,
                Address = request.Address?.Trim() ?? string.Empty,
                Role = "Customer",
                IsActive = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
        }

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
            Address = user.Address,
            AvatarUrl = user.AvatarUrl
        }, "Xác thực OTP thành công! Tài khoản của bạn đã được kích hoạt và đăng nhập.");
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResponse<AuthResponse>.Fail("Vui lòng nhập đầy đủ email và mật khẩu.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user == null || !VerifyPasswordHash(user, request.Password))
        {
            return ApiResponse<AuthResponse>.Fail("Email hoặc mật khẩu không chính xác.");
        }

        if (!user.IsEmailVerified)
        {
            return ApiResponse<AuthResponse>.Fail("Tài khoản chưa được kích hoạt xác thực OTP qua email. Vui lòng hoàn tất xác thực OTP trước khi đăng nhập.");
        }

        if (!user.IsActive)
        {
            return ApiResponse<AuthResponse>.Fail("Tài khoản của bạn hiện đang bị khóa. Vui lòng liên hệ Quản trị viên MilkyVerse.");
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
            Address = user.Address,
            AvatarUrl = user.AvatarUrl
        }, "Đăng nhập thành công!");
    }

    private bool VerifyPasswordHash(User user, string plainPassword)
    {
        if (string.IsNullOrEmpty(user.PasswordHash)) return false;

        // Hỗ trợ cả BCrypt ($2a$, $2b$, $2y$) và ASP.NET Core Identity PasswordHasher
        if (user.PasswordHash.StartsWith("$2"))
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, user.PasswordHash);
        }

        var res = _identityHasher.VerifyHashedPassword(user, user.PasswordHash, plainPassword);
        return res != PasswordVerificationResult.Failed;
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
            AvatarUrl = user.AvatarUrl,
            Role = user.Role,
            IsActive = user.IsActive,
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
        if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
        {
            user.AvatarUrl = request.AvatarUrl;
        }
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
            AvatarUrl = user.AvatarUrl,
            Role = user.Role,
            IsActive = user.IsActive,
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
