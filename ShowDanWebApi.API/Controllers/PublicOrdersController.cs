using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowDanWebApi.API.Service;
using ShowDanWebApi.Core.DTO;
using ShowDanWebApi.Core.Entities.Users;
using ShowDanWebApi.Data;

namespace ShowDanWebApi.API.Controllers;

[Authorize]
[ApiController]
[Route("api/pub-orders")]
public class PublicOrdersController : BaseController
{
    private readonly AppDbContext _context;
    private readonly IPublicOrderService _publicOrderService;

    public PublicOrdersController(AppDbContext context, IPublicOrderService publicOrderService)
    {
        _context = context;
        _publicOrderService = publicOrderService;
                Latitude = o.Location != null ? o.Location.Y : (double?)null,
                Longitude = o.Location != null ? o.Location.X : (double?)null,
                CustomerBudget = o.CustomerBudget,
                Comment = o.Comment,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt
            })
            .ToListAsync()
};

        return Ok(feed);
        
    }
    [HttpPost("apply")]
    public async Task<IActionResult> ApplyToOrder([FromBody] ApplyToPublicOrderDto dto)
    {
        return Ok(await _publicOrderService.ApplyToOrderAsync(CurrentUserId, dto));
    }

    #endregion
}