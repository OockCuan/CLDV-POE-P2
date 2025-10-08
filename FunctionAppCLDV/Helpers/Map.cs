using FunctionAppCLDV.Models;
using FunctionAppCLDV.Entities;

namespace FunctionAppCLDV.Helpers
{
    public static class Map
    {
        public static CustomerDto ToDto(CustomerEntity e)
            => new CustomerDto(
                Id: e.RowKey,
                Name: e.Name,
                Surname: e.Surname,
                Username: e.Username,
                Email: e.Email,
                ShippingAddress: e.ShippingAddress
            );

        public static ProductDto ToDto(ProductEntity e)
            => new ProductDto(
                Id: e.RowKey,
                ProductName: e.ProductName,
                Description: e.Description,
                Price: (decimal)e.Price,           
                StockAvailable: e.StockAvailable,
                ImageUrl: e.ImageUrl
            );

        public static OrderDto ToDto(OrderEntity e)
        {
            var unitPrice = (decimal)e.UnitPrice;   // double -> decimal
            var total = unitPrice * e.Quantity;

            return new OrderDto(
                Id: e.RowKey,
                CustomerId: e.CustomerId,
                ProductId: e.ProductId,
                ProductName: e.ProductName,
                Quantity: e.Quantity,
                UnitPrice: unitPrice,
                TotalAmount: total,
                OrderDateUtc: e.OrderDateUtc,
                Status: e.Status
            );
        }
    }
}
