using System;

namespace CryBot.Core.Models
{
    public interface ICryptoOrder
    {
        string Market { get; set; }
        CryptoOrderType OrderType { get; set; }
        decimal Price { get; set; }
        decimal Quantity { get; set; }
        decimal PricePerUnit { get; set; }
        decimal CommissionPaid { get; set; }
        bool Canceled { get; set; }
        string Uuid { get; set; }
        DateTime Opened { get; set; }
        decimal Limit { get; set; }
        decimal QuantityRemaining { get; set; }
        DateTime Closed { get; set; }
        bool IsClosed { get; set; }
    }
}