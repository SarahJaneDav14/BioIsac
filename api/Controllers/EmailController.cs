using Microsoft.AspNetCore.Mvc;
using BioIsac.Models;
using BioIsac.Services;

namespace BioIsac.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly EmailService _emailService;
    private readonly AuthService _authService;

    public EmailController(EmailService emailService, AuthService authService)
    {
        _emailService = emailService;
        _authService = authService;
    }

    private bool IsAuthenticated()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return false;

        var token = authHeader.Substring(7);
        return _authService.ValidateSession(token);
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
    {
        if (!IsAuthenticated()) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body))
        {
            return BadRequest(new { message = "Subject and Body are required" });
        }

        try
        {
            var result = await _emailService.SendEmailAsync(request);
            if (result)
            {
                return Ok(new { message = "Email sent successfully" });
            }
            return BadRequest(new { message = "Failed to send email. Check recipients and email configuration." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}

