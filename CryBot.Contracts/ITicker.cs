namespace CryBot.Contracts
{
    public interface ITicker
    {
        decimal Last { get; set; }
        
        decimal Bid { get; set; }
        
        decimal Ask { get; set; }
        
        decimal BaseVolume { get; set; }
        
        string Market { get; set; }
    }
}