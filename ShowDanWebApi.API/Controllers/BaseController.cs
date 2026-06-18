using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace ShowDanWebApi.API.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected int CurrentUserId => User.GetUserId();
    protected int? CurrentServiceId => User.GetServiceId();
    protected int CurrentUserRole => User.GetUserRole();
    protected string CurrentLang => Request.Headers.TryGetValue("a_lang", out var lang) ? lang.ToString().ToLower() : "en";
    protected bool IsAuthorized => CurrentUserId > 0;

    [NonAction]
    protected (int Skip, int Take) GetPagination(int start, int end, int maxPageSize = 20, int defaultPageSize = 10)
    {
        int skip = start < 0 ? 0 : start;
        int take = (end - start) is var t && (t <= 0 || t > maxPageSize) ? defaultPageSize : t;
        return (skip, take);
    }
}

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal? principal)
    {
        if (principal == null) return 0;
        var claim = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    public static int? GetServiceId(this ClaimsPrincipal? principal)
    {
        if (principal == null) return null;
        return int.TryParse(principal.FindFirst("sid")?.Value, out var id) ? id : null;
    }
    public static int GetUserRole(this ClaimsPrincipal? principal)
    {
        if (principal == null) return 0;
        var claimValue = principal.FindFirst("r")?.Value;
        return int.TryParse(claimValue, out var roleId) ? roleId : 0;
    }
}
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BadHttpRequestException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { msg = ex.Message }));
        }
        catch (Exception)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { msg = "Внутренняя ошибка сервера" }));
        }
    }
}