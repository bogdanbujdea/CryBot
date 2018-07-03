namespace CryBot.Contracts
{
    public interface ITrade
    {
        bool IsActive { get; set; }
        
        ICryptoOrder BuyOrder { get; set; } 
        
        ICryptoOrder SellOrder { get; set; }
        
        decimal MaxPricePerUnit { get; set; }
    }
}