namespace CryBot.Core.Models
{
    public class WebSubscription
    {
        public int Id { get; set; }

        public string Key { get; set; }

        public string AuthSecret { get; set; }

        public string Endpoint { get; set; }

    }
}