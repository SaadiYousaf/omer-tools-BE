namespace ProductService.API.RequestHelper
{
    public class CustomValidationResult
    {
        public bool IsValid { get; private set; } = true;
        public List<string> Errors { get; } = new List<string>();

        public void AddError(string error)
        {
            IsValid = false;
            Errors.Add(error);
        }
    }
}
