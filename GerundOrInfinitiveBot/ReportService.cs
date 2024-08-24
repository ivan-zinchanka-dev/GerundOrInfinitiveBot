using System.Net;
using System.Net.Mail;
using GerundOrInfinitiveBot.Settings;
using Microsoft.Extensions.Options;

namespace GerundOrInfinitiveBot;

public class ReportService
{
    private readonly IOptions<EmailSettings> _options; 
    
    public ReportService(IOptions<EmailSettings> options)
    {
        _options = options;
    }

    public async Task<bool> ReportExceptionAsync(Exception exception)
    {
        EmailSettings emailSettings = _options.Value;
        
        using (MailMessage mailMessage = new MailMessage())
        {
            mailMessage.From = new MailAddress(emailSettings.BotAddress);
            mailMessage.To.Add(emailSettings.AdminAddress);
            mailMessage.Subject = "Internal exception occured";
            mailMessage.Body = exception.ToString();
            mailMessage.IsBodyHtml = false;
            
            using (SmtpClient smtp = new SmtpClient(emailSettings.SmtpAddress, Convert.ToInt32(emailSettings.Port)))
            {
                smtp.Credentials = new NetworkCredential(emailSettings.BotAddress, emailSettings.BotAppPassword);
                smtp.EnableSsl = Convert.ToBoolean(emailSettings.EnableSsl);
                
                try
                {
                    await smtp.SendMailAsync(mailMessage);
                    Console.WriteLine("Email sent successfully.");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending email: {ex.Message}");
                    return false;
                }
            }

        }
    }
}