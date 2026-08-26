using System.Threading.Tasks;

namespace SaborExpress.Shared.Interfaces
{
    public interface INotificationService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}