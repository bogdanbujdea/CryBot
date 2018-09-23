using Microsoft.WindowsAzure.Storage.Table;

using System;

namespace CryBot.Functions.Utils
{
    public class Signal : TableEntity
    {
        private DateTime _time;
        private string _market;

        public Signal()
        {
            PartitionKey = "bitmex";
        }

        public string SignalType { get; set; }

        public string Market
        {
            get => _market;
            set
            {
                _market = value;
                RowKey = DateTime.Now.Ticks.ToString();
            }
        }
    }
}