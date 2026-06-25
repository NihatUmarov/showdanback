using System.Net;
using System.Net.Mail;
using System.Text;

namespace ShowDanWebApi.API.Service;

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string otpCode);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private const int MinutesToExpire = 3;
    public EmailService(IConfiguration config) => _config = config;
    public async Task SendOtpEmailAsync(string toEmail, string otpCode)
    {
        using var message = new MailMessage(smtpSection["From"]!, toEmail, "Код входа ShowDan", body) { IsBodyHtml = true };
        await smtp.SendMailAsync(message);
    }
    {}
}e