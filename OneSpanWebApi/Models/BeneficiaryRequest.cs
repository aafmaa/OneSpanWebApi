namespace OneSpanWebApi.Models
{
    public class BeneficiaryRequest
    {
        public string SignerEmail { get; set; } = string.Empty;
        public string SignerFirstName { get; set; } = string.Empty;
        public string SignerLastName { get; set; } = string.Empty;
        public string SignerDateOfBirth { get; set; } = string.Empty; // MM/DD/YYYY
        public string SignerLast4SSN { get; set; } = string.Empty;
        public string DesignationId { get; set; } = string.Empty;
        public string CN { get; set; } = string.Empty;

        public Dictionary<string, string> PdfFieldValues { get; set; } = new();
    }
}
