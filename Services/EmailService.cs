using System.Net;
using System.Net.Mail;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public void Send(string to, string subject, string body)
    {
        var smtp = new SmtpClient
        {
            Host = _config["Email:Smtp"],
            Port = int.Parse(_config["Email:Port"]),
            EnableSsl = true,
            Credentials = new NetworkCredential(
                _config["Email:Username"],
                _config["Email:Password"]
            )
        };

        var mail = new MailMessage
        {
            From = new MailAddress(_config["Email:From"], "No Reply"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mail.To.Add(to);
        smtp.Send(mail);
    }
}
