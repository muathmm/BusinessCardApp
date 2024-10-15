namespace BusinessCardManager.Models.DTO
{
    public class BusinessCardReqDto
    {
        public string? Name { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? PhotoBase64 { get; set; } // encoded Base64
        public string? Address { get; set; }
  
       
        public IFormFile? CsvFile { get; set; } // File CSV
        public IFormFile? XmlFile { get; set; } // File XML
        public string? QrCodeData { get; set; }
    }
}
