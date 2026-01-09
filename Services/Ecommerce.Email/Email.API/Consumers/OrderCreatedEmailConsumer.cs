using BuildingBlocks.Messaging.Events;
using Email.API.Dtos;
using Email.API.Services;
using MassTransit;

namespace Email.API.Consumers
{
    public class OrderCreatedEmailConsumer(IEmailService email) : IConsumer<CreatedEvent>
    {
        public async Task Consume(ConsumeContext<CreatedEvent> context)
        {
            var m = context.Message;
            await email.SendOrderCreatedEmailAsync(toEmail: "nguyenbaminhduc2019@gmail.com", m.FullName, m.OrderId, m.Price, m.TotalItem);
        }
    }
}
