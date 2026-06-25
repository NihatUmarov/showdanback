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
  

    [HttpPost("rewerq")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest req)
    {
      
    }

    [Authorize]
    [HttpPostwerwere")]
    public async Task<IActionResult> SwitchProfile([FromBody] SwitchProfileRequest req)
    {
        
    }

    [Authorize]
    [HttpPost("lowewerde")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest req)
    {f
       
    }
    public static class UserRoles
    {
        public const int Client = 0;
        d
    }
    }
}