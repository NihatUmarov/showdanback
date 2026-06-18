using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowDanWebApi.API.Service;
using ShowDanWebApi.Core.Entities.Users;
using ShowDanWebApi.Data;
using System.Text.Json.Serialization;

namespace ShowDanWebApi.API.Controllers;

[Route("api/auth")]
public class AuthController : BaseController
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private const int MinutesToExpire = 3;
    private const int RefreshTokenExpiryDays = 90;

    public AuthController(AppDbContext context, IJwtService jwtService, IEmailService emailService) =>
        (_context, _jwtService, _emailService) = (context, jwtService, emailService);

    [HttpPost("email_send_otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { msg = "Email не заполнен" });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == req.Email);

        if (user == null)
        {
            user = new Users { Email = req.Email, FirstName = req.Email.Split('@')[0], CreatedAt = DateTime.UtcNow, UserPublicId = Guid.NewGuid() };
            _context.Users.Add(user);
        }
        user.OtpCode = Random.Shared.Next(100000, 999999).ToString();
        user.OtpExpires = DateTime.UtcNow.AddMinutes(MinutesToExpire);

        await _context.SaveChangesAsync();

        await _emailService.SendOtpEmailAsync(user.Email, user.OtpCode);
        return Ok(new { msg = "Код отправлен" });
    }

    [HttpPost("email_verify_otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest req)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == req.Email && u.OtpCode == req.Otp && u.OtpExpires > DateTime.UtcNow);
        if (user == null) return Unauthorized(new { msg = "Неверный или просроченный код" });

        (user.OtpCode, user.OtpExpires) = (null, null);
        string accessToken = _jwtService.GenerateJwt(user, UserRoles.Client);
        string refreshToken = _jwtService.GenerateRefreshToken();

        var newSession = new UserRefreshTokens
        {
            UserId = user.UserId,
            RefreshToken = refreshToken,
            ExpiryTime = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
            DeviceInfo = Request.Headers["User-Agent"].ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        _context.UserRefreshTokens.Add(newSession);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var realErrorMessage = ex.InnerException?.Message;
            return StatusCode(500, new { error = realErrorMessage });
        }
        return Ok(new { msg = "Успешный вход", tok = accessToken, rf_tok = refreshToken, role = UserRoles.Client });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest req)
    {
        if (req == null || string.IsNullOrEmpty(req.AccessToken) || string.IsNullOrEmpty(req.RefreshToken))
            return BadRequest(new { msg = "Неверный запрос" });

        var principal = _jwtService.GetPrincipalFromExpiredToken(req.AccessToken);
        if (principal == null) return Unauthorized(new { msg = "Невалидный токен" });

        var userId = principal.GetUserId();
        if (userId <= 0) return Unauthorized(new { msg = "Невалидный идентификатор пользователя" });

        var session = await _context.UserRefreshTokens
            .FirstOrDefaultAsync(s => s.RefreshToken == req.RefreshToken && s.UserId == userId);

        if (session == null || session.ExpiryTime <= DateTime.UtcNow)
            return Unauthorized(new { msg = "Сессия просрочена или не существует" });

        var user = await _context.Users
            .Include(u => u.PerformerProfile)
            .ThenInclude(p => p!.PerformerServices)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null) return Unauthorized(new { msg = "Пользователь не найден" });

        var requestedRoleStr = principal.FindFirst("r")?.Value;
        int requestedRoleId = string.IsNullOrEmpty(requestedRoleStr) ? UserRoles.Client : int.Parse(requestedRoleStr);

        var requestedServiceIdStr = principal.FindFirst("sid")?.Value;
        int? requestedServiceId = string.IsNullOrEmpty(requestedServiceIdStr) ? null : int.Parse(requestedServiceIdStr);

        if (requestedRoleId != UserRoles.Client)
        {
            if (user.PerformerProfile == null || !user.PerformerProfile.PerformerServices.Any(s => s.ServiceId == requestedServiceId && s.CategoryId == requestedRoleId))
            {
                requestedRoleId = UserRoles.Client;
                requestedServiceId = null;
            }
        }

        var newAccessToken = _jwtService.GenerateJwt(user, requestedRoleId, requestedServiceId);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        session.RefreshToken = newRefreshToken;
        session.ExpiryTime = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);
        session.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        await _context.SaveChangesAsync();

        return Ok(new { tok = newAccessToken, rf_tok = newRefreshToken, role = requestedRoleId });
    }

    [Authorize]
    [HttpPost("switch_profile")]
    public async Task<IActionResult> SwitchProfile([FromBody] SwitchProfileRequest req)
    {
        var user = await _context.Users
            .Include(u => u.PerformerProfile)
            .ThenInclude(p => p!.PerformerServices)
            .FirstOrDefaultAsync(u => u.UserId == CurrentUserId);

        if (user == null) return NotFound(new { msg = "Пользователь не найден" });

        int finalRoleId = UserRoles.Client;
        int? finalServiceId = null;
        if (req.TargetRole != UserRoles.Client)
        {
            if (user.PerformerProfile == null)
                return BadRequest(new { msg = "У вас нет профиля исполнителя" });

            var selectedService = user.PerformerProfile.PerformerServices
                .FirstOrDefault(s => s.ServiceId == req.TargetServiceId);

            if (selectedService == null)
                return BadRequest(new { msg = "У вас нет такой услуги" });
            finalRoleId = selectedService.CategoryId;
            finalServiceId = req.TargetServiceId;
        }

        string newAccessToken = _jwtService.GenerateJwt(user, finalRoleId, finalServiceId);
        return Ok(new
        {
            msg = $"Успешно: {(finalRoleId == UserRoles.Client ? "Клиент" : "Исполнитель")}",
            tok = newAccessToken,
            role = finalRoleId
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest req)
    {
        var session = await _context.UserRefreshTokens
            .FirstOrDefaultAsync(s => s.RefreshToken == req.RefreshToken && s.UserId == CurrentUserId);

        if (session != null)
        {
            _context.UserRefreshTokens.Remove(session);
            await _context.SaveChangesAsync();
        }

        return Ok(new { msg = "Успешный выход из устройства" });
    }
    public static class UserRoles
    {
        public const int Client = 0;
    }

    public record SendOtpRequest([property: JsonPropertyName("em")] string Email);
    public record VerifyOtpRequest([property: JsonPropertyName("em")] string Email, [property: JsonPropertyName("otp")] string Otp);
    public record RefreshTokenRequest([property: JsonPropertyName("t_tok")] string AccessToken, [property: JsonPropertyName("rf_tok")] string RefreshToken);
    public record SwitchProfileRequest([property: JsonPropertyName("role")] int TargetRole, [property: JsonPropertyName("sid")] int? TargetServiceId);
    public record LogoutRequest([property: JsonPropertyName("rf_tok")] string RefreshToken);
}