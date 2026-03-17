using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order.Application.Features.Commands.CheckoutOrder;
public record CheckoutCommand(Guid CustomerId, AddressDto ShippingAddress, string OrderName)
    : ICommand<Result<CheckoutResponse>>;

public record CheckoutResponse(Guid OrderId, string PaymentUrl);

