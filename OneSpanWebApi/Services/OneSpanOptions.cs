namespace OneSpanWebApi.Services
{
    public class OneSpanOptions
    {
        public required string BaseApiUrl { get; set; }
        public required string ApiKey { get; set; }
        public required string CallbackKey { get; set; }
        public required string DocPath { get; set; }
        public required string SenderEmail { get; set; }
    }
}
