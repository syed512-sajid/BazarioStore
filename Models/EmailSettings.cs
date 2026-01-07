namespace EcommerceStore.Models
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 465;
        public string SmtpUser { get; set; } = "";
        public string SmtpPass { get; set; } = "";
        public string FromName { get; set; } = "BAZARIO Store";
        public string FromEmail { get; set; } = "";
    }
}
