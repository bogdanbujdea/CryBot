using System;

namespace CryBot.Core.Models
{
    public class CryptoOrder
    {
        public string Market { get; set; }
        
        public CryptoOrderType OrderType { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal CommissionPaid { get; set; }
        public bool Canceled { get; set; }
        public Guid Uuid { get; set; }
        public DateTime Opened { get; set; }
        public decimal Limit { get; set; }
        public decimal QuantityRemaining { get; set; }
        public DateTime Closed { get; set; }
        public bool IsClosed { get; set; }
    }

    public enum CryptoOrderType
    {
        None,
        LimitBuy,
        LimitSell
    }
}