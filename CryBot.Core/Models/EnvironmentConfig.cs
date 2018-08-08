namespace CryBot.Core.Models
{
    public class EnvironmentConfig
    {
        public string BittrexApiKey { get; set; }
        
        public string BittrexApiSecret { get; set; }
        public string PushPublicKey { get; set; }
        public string PushPrivateKey { get; set; }
        public bool TestMode { get; set; }
    }
}
