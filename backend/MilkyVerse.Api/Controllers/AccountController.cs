using Microsoft.AspNetCore.Mvc;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Services;

namespace MilkyVerse.Api.Controllers;

/// <summary>
/// ASP.NET Core MVC Controller phục vụ trực tiếp các Razor Views (.cshtml) cho Đăng ký OTP, Quên mật khẩu OTP & Đăng nhập
/// </summary>
[Route("Account")]
public class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("Register")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpGet("ForgotPassword")]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpGet("Login")]
    public IActionResult Login()
    {
        return View();
    }
}
