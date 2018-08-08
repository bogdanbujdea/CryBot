using System;
using System.Collections.Generic;
using System.Text;
using Bittrex.Net.Objects;

namespace CryBot.Core.Models
{
    public class PushMessage
    {
        public string Timestamp { get; set; }
        public string Message { get; set; }

        public string Currency { get; set; }
        
        public OrderBookType OrderType { get; set; }

        public PushMessage(string message, string currency = "", OrderBookType orderType = OrderBookType.Both)
        {
            Message = message;
            Currency = currency;
            OrderType = orderType;
            Timestamp = DateTime.UtcNow.ToString("F");
        }

        public static PushMessage FromMessage(string message)
        {
            return new PushMessage(message);
        }
    }
}
