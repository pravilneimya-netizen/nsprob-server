using System.Net;
using System.Net.Mail;

namespace NSprob.Server.Services
{
    public class EmailService
    {
        private readonly IConfiguration _cfg;
        public EmailService(IConfiguration cfg) => _cfg = cfg;

        public async Task SendVerificationAsync(string toEmail, string username, string code)
        {
            // Читаємо з Environment Variables (Railway) або appsettings.json
            var host = Environment.GetEnvironmentVariable("EMAIL_HOST")
                       ?? _cfg["Email:Host"] ?? "smtp.gmail.com";
            var port = int.Parse(Environment.GetEnvironmentVariable("EMAIL_PORT")
                       ?? _cfg["Email:Port"] ?? "587");
            var user = Environment.GetEnvironmentVariable("EMAIL_USER")
                       ?? _cfg["Email:Username"] ?? "";
            var pass = Environment.GetEnvironmentVariable("EMAIL_PASS")
                       ?? _cfg["Email:Password"] ?? "";

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
                throw new Exception("Email credentials not configured");

            var body = $@"
<div style='font-family:Segoe UI,sans-serif;background:#0A0D14;padding:40px;'>
  <div style='max-width:460px;margin:0 auto;background:#10141F;border-radius:16px;border:1px solid #1E2840;overflow:hidden;'>
    <div style='padding:28px;text-align:center;background:linear-gradient(135deg,#6C63FF,#00D4FF);'>
      <div style='font-size:30px;font-weight:900;color:#fff;'>NSprob</div>
      <div style='color:rgba(255,255,255,0.8);font-size:12px;margin-top:4px;'>Encrypted Messenger</div>
    </div>
    <div style='padding:32px;color:#D0D8E8;font-size:14px;'>
      <p>Привіт, <strong style='color:#F0F4FF;'>{username}</strong> 👋</p>
      <p>Твій код верифікації для NSprob:</p>
      <div style='font-size:38px;font-weight:bold;letter-spacing:14px;color:#F0F4FF;
                  text-align:center;background:#1C2235;padding:22px;border-radius:12px;
                  margin:24px 0;border:1px solid #1E2840;'>
        {code}
      </div>
      <p>Код дійсний <strong>15 хвилин</strong>.</p>
      <p style='color:#4A5568;font-size:12px;'>Якщо ти не реєструвався — просто ігноруй.</p>
    </div>
  </div>
</div>";

            using var smtp = new SmtpClient(host, port)
            {
                EnableSsl   = true,
                Credentials = new NetworkCredential(user, pass)
            };
            var mail = new MailMessage(user, toEmail)
            {
                Subject    = $"NSprob · Код: {code}",
                Body       = body,
                IsBodyHtml = true
            };
            await smtp.SendMailAsync(mail);
        }
    }
}
