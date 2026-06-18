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
        var smtpSection = _config.GetSection("Smtp");

        using var smtp = new SmtpClient
        {
            Host = smtpSection["Host"]!,
            Port = int.Parse(smtpSection["Port"]!),
            EnableSsl = bool.Parse(smtpSection["EnableSsl"] ?? "true"),
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(smtpSection["User"], smtpSection["Pass"])
        };

        var codeBoxesHtml = new StringBuilder();
        foreach (char digit in otpCode)
        {
            codeBoxesHtml.Append($@"
                <div style='display: inline-block; width: 38px; height: 48px; line-height: 48px; text-align: center; 
                            background-color: #ffffff; color: #ff6b6b; font-size: 26px; font-weight: bold; 
                            border-radius: 10px; margin: 0 3px; box-shadow: 0 4px 10px rgba(0, 0, 0, 0.15);'>
                    {digit}
                </div>");
        }

        string body = $@"
        <div style='background: linear-gradient(180deg, #F864EF 0%, #FD758D 50%, #FF6B6B 100%); padding: 40px 10px; font-family: sans-serif; text-align: center;'>
            <div style='max-width: 400px; margin: 0 auto; background-color: rgba(255, 255, 255, 0.15); padding: 30px 15px; border-radius: 20px; border: 1px solid rgba(255, 255, 255, 0.25); box-shadow: 0 12px 30px rgba(0,0,0,0.15);'>
                <h2 style='color: #ffffff; margin: 0 0 10px 0; font-size: 24px;'>Код подтверждения</h2>
                <p style='color: #ffffff; font-size: 15px; margin: 0 0 25px 0; opacity: 0.95;'>Используйте этот код для входа в <strong>ShowDan</strong></p>
                <div style='margin-bottom: 25px; white-space: nowrap;'>{codeBoxesHtml}</div>
                <hr style='border: none; border-top: 1px solid rgba(255, 255, 255, 0.2); margin: 20px 0;'>
                <p style='color: #ffffff; font-size: 13px; margin: 0; opacity: 0.85; font-style: italic;'>Код действителен в течение {MinutesToExpire} минут.</p>
            </div>
        </div>";

        using var message = new MailMessage(smtpSection["From"]!, toEmail, "Код входа ShowDan", body) { IsBodyHtml = true };
        await smtp.SendMailAsync(message);
    }
}