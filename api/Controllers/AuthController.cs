using Microsoft.AspNetCore.Mvc;
using BioIsac.Models;
using BioIsac.Services;
using OtpNet;

namespace BioIsac.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!_authService.ValidateUser(request.Username, request.Password))
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var secret = _authService.GetTwoFactorSecret(request.Username);
            
            if (string.IsNullOrEmpty(secret))
            {
                // First time login - generate and return secret
                secret = _authService.GenerateTwoFactorSecret();
                _authService.SetTwoFactorSecret(request.Username, secret);
                
                var totp = new Totp(Base32Encoding.ToBytes(secret));
                var qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString($"otpauth://totp/Admin?secret={secret}&issuer=BioIsac")}";
                
                return Ok(new { 
                    requiresTwoFactor = true, 
                    secret = secret,
                    qrCodeUrl = qrCodeUrl,
                    message = "Please set up 2FA and enter the code"
                });
            }

            // 2FA is for show only - accept any code or no code
            if (string.IsNullOrEmpty(request.TwoFactorCode))
            {
                return Ok(new { 
                    requiresTwoFactor = true, 
                    message = "Please enter your 2FA code (any code will work)" 
                });
            }

            // Accept any 2FA code (for show only) - just proceed with login
            var token = _authService.GenerateSessionToken();
            _authService.SaveSession(request.Username, token);

            return Ok(new { token = token, message = "Login successful" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
    }

    [HttpGet("verify")]
    public IActionResult VerifyToken()
    {
        try
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized();
            }

            var token = authHeader.Substring(7);
            if (_authService.ValidateSession(token))
            {
                return Ok(new { valid = true });
            }

            return Unauthorized();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
    }
}

