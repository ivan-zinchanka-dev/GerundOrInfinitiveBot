using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace GerundOrInfinitiveBot;

public class ReportService
{
    private const string EmailConfigKey = "EmailConfig";
    private const string SmtpAddressKey = "SmtpAddress";
    private const string PortKey = "Port";
    private const string EnableSslKey = "EnableSSL";
    private const string BotAddressKey = "BotAddress";
    private const string BotAppPasswordKey = "BotAppPassword";
    private const string AdminAddressKey = "AdminAddress";
    
    private readonly IConfigurationRoot _configurationRoot; 
    
    public ReportService(IConfigurationRoot configurationRoot)
    {
        _configurationRoot = configurationRoot;
    }

    public async Task<bool> ReportException(Exception exception)
    {
        IConfigurationSection emailConfig = _configurationRoot.GetSection(EmailConfigKey);
        
        using (MailMessage mailMessage = new MailMessage())
        {
            mailMessage.From = new MailAddress(emailConfig[BotAddressKey]);
            mailMessage.To.Add(emailConfig[AdminAddressKey]);
            mailMessage.Subject = "Internal exception occured";
            mailMessage.Body = exception.ToString();
            mailMessage.IsBodyHtml = false;
            
            using (SmtpClient smtp = new SmtpClient(emailConfig[SmtpAddressKey], Convert.ToInt32(emailConfig[PortKey])))
            {
                smtp.Credentials = new NetworkCredential(emailConfig[BotAddressKey], emailConfig[BotAppPasswordKey]);
                smtp.EnableSsl = Convert.ToBoolean(emailConfig[EnableSslKey]);
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