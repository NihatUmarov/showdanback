using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShowDanWebApi.Core.Entities.Users;

public class Orders
{
    [Key] public int OrderId { get; set; }public enum CancelledByType
{
    Client = 1,
    Performer = 2,
    System = 3,  // Авто-отмена, если не ответил вовремя
    Admin = 4    // Диспут / Поддержка
}

public enum OrderStatuse
{
    Created = 1,      // Заказ создан клиентом, ожидает подтверждения от артиста
    Confirmed = 2,    // Артист подтвердил, что готов выступить (ожидаем оплату от клиента)
    Paid = 3,         // Клиент оплатил (деньги захолдированы системой)
    InProgress = 4,   // Мероприятие идет прямо сейчас
    Completed = 5,    // Успешно завершено (деньги уходят артисту)
    Cancelled = 6     // Отменен (клиентом, артистом или администрацией)
}