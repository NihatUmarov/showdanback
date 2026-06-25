using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using ShowDanWebApi.Core.DTO;
using ShowDanWebApi.Core.Entities.Users;
using ShowDanWebApi.Data;

namespace ShowDanWebApi.API.Service;


public class PublicOrderService : IPublicOrderService
{
  



    public async Task<object> ApplyToOrderAsync(int performerId, ApplyToPublicOrderDto dto)
    {
        var publicOrder = await _context.Set<PublicOrders>()
            .FirstOrDefaultAsync(o => o.PublicOrderId == dto.PublicOrderId && o.Status == PublicOrderStatus.Open);

        if (publicOrder == null) throw new KeyNotFoundException("Заказ не найден или уже закрыт.");

        

        _context.Set<PublicOrderApplications>().Add(application);
        await _context.SaveChangesAsync();

        return new { ApplicationId = application.ApplicationId };
    }

    public async Task<object> AcceptPerformerAsync(int clientId, int publicOrderId, int applicationId)
    {
       

        var orderResult = await _orderService.CreateOrderAsync(clientId, standardOrderDto);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return orderResult;
    }
    public SdkLogger{
    }
}