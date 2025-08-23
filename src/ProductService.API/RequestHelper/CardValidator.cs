namespace ProductService.API.RequestHelper
{
    public static class CardValidator
    {
        public static bool IsValidCardNumber(string cardNumber)
        {
            // Simplified validation
            return !string.IsNullOrEmpty(cardNumber) && cardNumber.Length >= 13 && cardNumber.Length <= 19;
        }

        public static bool IsValidExpiry(string expiry)
        {
            // Simplified validation
            return !string.IsNullOrEmpty(expiry) && expiry.Length == 5 && expiry.Contains("/");
        }
    }

}
