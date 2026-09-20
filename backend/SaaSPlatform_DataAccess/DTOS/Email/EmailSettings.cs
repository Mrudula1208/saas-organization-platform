namespace SaaSPlatform.Application.DTOS.Email
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = "noreply@saasplatform.com";
        public string FromName { get; set; } = "SaaS Platform";
        public string ClientAppUrl { get; set; } = "http://localhost:4200";
    }
}
