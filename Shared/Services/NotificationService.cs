using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SaborExpress.Shared.Interfaces;
using System.Net;
using System.Net.Mail;

namespace SaborExpress.Shared.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly IConfiguration _config;

        private static readonly TimeZoneInfo ColombiaTimeZone = GetColombiaTimeZone();

        public NotificationService(ILogger<NotificationService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using var client = ConstruirClienteSmtp();
                using var mailMessage = ConstruirMensaje(toEmail, subject, body);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Correo enviado exitosamente a {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo a {Email}: {Message}", toEmail, ex.Message);
                throw;
            }
        }

        private SmtpClient ConstruirClienteSmtp()
        {
            var smtpHost = _config["EmailSettings:Host"];
            var smtpPort = int.Parse(_config["EmailSettings:Port"] ?? "587");
            var smtpUser = _config["EmailSettings:Username"];
            var smtpPass = _config["EmailSettings:Password"];

            return new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };
        }

        private MailMessage ConstruirMensaje(string toEmail, string subject, string body)
        {
            var smtpUser = _config["EmailSettings:Username"];
            var htmlBody = BuildEmailTemplate(subject, body);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpUser!, "SaborExpress"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);
            return mailMessage;
        }

        private static TimeZoneInfo GetColombiaTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
            }
        }

        private static string BuildEmailTemplate(string subject, string body)
        {
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ColombiaTimeZone);
            var year = localTime.Year;

            return $@"
<!DOCTYPE html>
<html lang=""es"">
<body style=""margin:0; padding:0; background-color:#F1F5F9; font-family: 'Segoe UI', Arial, sans-serif;"">
  
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#FFFFFF; border-radius:16px; overflow:hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08);"">
          
          <!-- Header -->
          <tr>
            <td style=""background: linear-gradient(135deg, #E61C24 0%, #B3151B 100%); padding: 32px 40px; text-align:center;"">
              <p style=""margin:0; font-size: 22px; font-weight: 700; color:#FFFFFF; letter-spacing: 0.5px;"">
                SaborExpress
              </p>
              <p style=""margin:6px 0 0; font-size: 13px; color:rgba(255,255,255,0.85); text-transform: uppercase; letter-spacing: 1px;"">
                Notificación del sistema
              </p>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style=""padding: 40px;"">
              <p style=""margin:0 0 8px; font-size: 12px; font-weight:600; color:#E61C24; text-transform:uppercase; letter-spacing: 1px;"">
                {subject}
              </p>
              <div style=""width:40px; height:3px; background-color:#E61C24; border-radius:2px; margin-bottom:20px;""></div>

              <p style=""margin:0; font-size: 15px; line-height: 1.6; color:#0F172A;"">
                {body}
              </p>

              <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top:28px; background-color:#F8FAFC; border-radius:10px; border: 1px solid #E2E8F0;"">
                <tr>
                  <td style=""padding: 16px 20px; font-size: 13px; color:#64748B;"">
                    {localTime:dd/MM/yyyy HH:mm}
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""padding: 24px 40px; background-color:#F8FAFC; text-align:center; border-top: 1px solid #E2E8F0;"">
              <p style=""margin:0; font-size: 12px; color:#94A3B8;"">
                Este es un mensaje automatico, por favor no respondas a este correo.
              </p>
              <p style=""margin:6px 0 0; font-size: 12px; color:#CBD5E1;"">
                � {year} SaborExpress. Todos los derechos reservados.
              </p>
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