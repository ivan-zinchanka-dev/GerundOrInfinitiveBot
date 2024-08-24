using System.Net;
using System.Net.Mail;
using GerundOrInfinitiveBot.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GerundOrInfinitiveBot.Services.Reporting;

public class ReportService
{
    private readonly IOptions<EmailSettings> _options;
    private readonly ILogger<ReportService> _logger;
    
    public ReportService(IOptions<EmailSettings> options, ILogger<ReportService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<bool> ReportExceptionAsync(Exception exception)
    {
        try
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
                        _logger.LogInformation("Email sent successfully.");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation($"Error sending email: {ex.Message}");
                        return false;
                    }
                }

            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Report service exception:\n{ex}");
            return false;
        }
    }
}