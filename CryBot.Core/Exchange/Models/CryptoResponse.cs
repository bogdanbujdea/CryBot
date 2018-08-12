namespace CryBot.Core.Exchange.Models
{
    public class CryptoResponse<T>
    {
        public CryptoResponse(T content)
        {
            Content = content;
            IsSuccessful = true;
        }

        public CryptoResponse(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public bool IsSuccessful { get; }

        public string ErrorMessage { get; }

        public T Content { get; }
    }
}
