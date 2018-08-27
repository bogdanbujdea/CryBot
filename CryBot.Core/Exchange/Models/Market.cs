using Microsoft.WindowsAzure.Storage.Table;

namespace CryBot.Core.Exchange.Models
{
    public class Market: TableEntity
    {
        private string _name;

        public Market()
        {
            PartitionKey = "bittrex";
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                RowKey = _name;
            }
        }
    }
}
