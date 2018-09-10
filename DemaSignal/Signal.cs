using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace DemaSignal
{
    public class Signal : TableEntity
    {
        private DateTime _time;

        public Signal()
        {
            PartitionKey = "bittrex";
        }

        public string SignalType { get; set; }

        public DateTime Time
        {
            get => _time;
            set
            {
                _time = value;
                RowKey = _time.ToString("F");
            }
        }
    }
}