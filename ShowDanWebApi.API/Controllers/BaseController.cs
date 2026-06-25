using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace ShowDanWebApi.API.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected int CurrentUserId => User.GetUserId();
   
    [NonAction]
    protected (int Skip, int Take) GetPagination(int start, int end, int maxPageSize = 20, int defaultPageSize = 10)
    {
    }
}

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal? principal)
    {
        
    }
}