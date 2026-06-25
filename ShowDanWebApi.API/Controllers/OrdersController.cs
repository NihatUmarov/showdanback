using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowDanWebApi.API.Service;
using ShowDanWebApi.Core.DTO;
using ShowDanWebApi.Core.Entities.Users;
using ShowDanWebApi.Data;
using static ShowDanWebApi.API.Controllers.AuthController;

namespace ShowDanWebApi.API.Controllers;

[Authorize]
[Route("api/orders")]
public class OrdersController : BaseController
{
TimeUtc.ToUniversalTime();

        if (startUtc >= endUtc || startUtc < DateTime.UtcNow)
            return BadRequest(new { error = "Невалидный интервал времени заказа." });

        dto.StartTimeUtc = startUtc;
        dto.EndTimeUtc = endUtc;

        var strategy = _context.Database.CreateExecutionStrategy();
        object orderResult = null!;
        await strategy.ExecuteAsync(async () =>
        {
            .Skip(dto.Offset)
            .Take(limit)
            .Select(o => new OrderShortResponseDto
            {
                OrderId = o.OrderId,
                DisplayName = isClient
                    ? (o.Service.StageName ?? "Артист")
                    : $"{o.Client.FirstName} {o.Client.LastName}",

                DisplayAvatar = isClient
                    ? o.Performer.User.PhotoThumbUrl
                    : o.Client.PhotoThumbUrl,

                StartTimeUtc = o.StartTimeUtc,
                EndTimeUtc = o.EndTimeUtc,
                CityId = o.CityId,

                TotalPrice = o.PerformancePrice + o.TravelPrice,
                CurrencyCode = o.CurrencyCode,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        return Ok(orders);
    }
d
    [HttpGet("{id:int}")]

}