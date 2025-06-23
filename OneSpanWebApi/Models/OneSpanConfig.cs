namespace OneSpanWebApi.Models
{
    public class OneSpanConfig
    {
        public required string BaseApiUrl { get; set; }
        public required string ApiKey { get; set; }
        public required string CallbackKey { get; set; }
        public required string DocPath { get; set; }
        public required int DocExperationDays { get; set; } = 7; // Default to 30 days
        public required string SenderEmail { get; set; }

        public required string GemBoxDocumentLicense { get; set; }
        public required string GemBoxPdfLicense { get; set; }
    }
}
