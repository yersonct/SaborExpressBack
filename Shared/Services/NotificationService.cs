using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SaborExpress.Shared.Interfaces;

namespace SaborExpress.Shared.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpFactory;

        private static readonly TimeZoneInfo ColombiaTimeZone = GetColombiaTimeZone();

        public NotificationService(
            ILogger<NotificationService> logger,
            IConfiguration config,
            IHttpClientFactory httpFactory)
        {
            _logger = logger;
            _config = config;
            _httpFactory = httpFactory;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var apiKey = _config["Brevo:ApiKey"]
                    ?? throw new InvalidOperationException("Falta Brevo:ApiKey");
                var senderEmail = _config["EmailSettings:Username"]
                    ?? throw new InvalidOperationException("Falta EmailSettings:Username");

                var payload = new
                {
                    sender = new { name = "SaborExpress", email = senderEmail },
                    to = new[] { new { email = toEmail } },
                    subject,
                    htmlContent = BuildEmailTemplate(subject, body)
                };

                var client = _httpFactory.CreateClient("email");
                client.Timeout = TimeSpan.FromSeconds(20);

                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
                {
                    Content = JsonContent.Create(payload)
                };
                request.Headers.Add("api-key", apiKey);

                using var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var detail = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException(
                        $"Brevo respondió {(int)response.StatusCode}: {detail}");
                }

                _logger.LogInformation("Correo enviado exitosamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error al enviar correo ({ErrorType}).", ex.GetType().Name);
                throw;
            }
        }

        private static TimeZoneInfo GetColombiaTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); }
            catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota"); }
        }

        private static string BuildEmailTemplate(string subject, string body)
        {
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ColombiaTimeZone);
            var year = localTime.Year;

            return $@"
<!DOCTYPE html>
<html lang=""es"">
<head><meta charset=""UTF-8""></head>
<body style=""margin:0; padding:0; background-color:#F1F5F9; font-family: 'Segoe UI', Arial, sans-serif;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
    <tr>
      <td align=""center"" style=""padding:24px 0;"">
        <table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#FFFFFF; border-radius:16px; overflow:hidden;"">
          <tr>
            <td style=""background-color:#E61C24; padding:32px 40px; text-align:center;"">
              <p style=""margin:0; font-size:22px; font-weight:700; color:#FFFFFF;"">SaborExpress</p>
              <p style=""margin:6px 0 0; font-size:13px; color:#FFFFFF; text-transform:uppercase; letter-spacing:1px;"">Notificación del sistema</p>
            </td>
          </tr>
          <tr>
            <td style=""padding:40px;"">
              <p style=""margin:0 0 8px; font-size:12px; font-weight:600; color:#E61C24; text-transform:uppercase; letter-spacing:1px;"">{subject}</p>
              <div style=""width:40px; height:3px; background-color:#E61C24; margin-bottom:20px;""></div>
              <p style=""margin:0; font-size:15px; line-height:1.6; color:#0F172A;"">{body}</p>
              <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top:28px; background-color:#F8FAFC; border:1px solid #E2E8F0;"">
                <tr><td style=""padding:16px 20px; font-size:13px; color:#64748B;"">{localTime:dd/MM/yyyy HH:mm}</td></tr>
              </table>
            </td>
          </tr>
          <tr>
            <td style=""padding:24px 40px; background-color:#F8FAFC; text-align:center; border-top:1px solid #E2E8F0;"">
              <p style=""margin:0; font-size:12px; color:#94A3B8;"">Este es un mensaje automático, por favor no respondas a este correo.</p>
              <p style=""margin:6px 0 0; font-size:12px; color:#94A3B8;"">&copy; {year} SaborExpress. Todos los derechos reservados.</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }
    }
}
