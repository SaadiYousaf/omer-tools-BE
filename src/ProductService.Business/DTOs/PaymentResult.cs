namespace ProductService.Business.DTOs
{
    public enum PaymentStatus
    {
        Success,
        Failed,
        TransientError,
        RequiresAction
    }

    public class PaymentResult
    {
        public PaymentStatus Status { get; set; }
        public bool IsSuccess => Status == PaymentStatus.Success;
        public bool RequiresAction => Status == PaymentStatus.RequiresAction;
        public string ClientSecret { get; set; }
        public string TransactionId { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string Last4Digits { get; set; }
        public string Brand { get; set; }
        public int? ExpiryMonth { get; set; }
        public int? ExpiryYear { get; set; }

        public static PaymentResult CreateSuccess(
            string transactionId,
            string last4 = null,
            string brand = null,
            int? expMonth = null,
            int? expYear = null)
        {
            return new PaymentResult
            {
                Status = PaymentStatus.Success,
                TransactionId = transactionId,
                Last4Digits = last4,
                Brand = brand,
                ExpiryMonth = expMonth,
                ExpiryYear = expYear
            };
        }

        public static PaymentResult CreateFailed(string errorCode, string errorMessage)
        {
            return new PaymentResult
            {
                Status = PaymentStatus.Failed,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }

        public static PaymentResult CreateTransientError(string errorCode, string errorMessage)
        {
            return new PaymentResult
            {
                Status = PaymentStatus.TransientError,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }

        public static PaymentResult CreateRequiresAction(string clientSecret)
        {
            return new PaymentResult
            {
                Status = PaymentStatus.RequiresAction,
                ClientSecret = clientSecret
            };
        }
    }
}