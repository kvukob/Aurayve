using System.Net;
using System.Net.Mail;
using System.Text;

namespace Server.Services.Email;

public class EmailService(IConfiguration configuration)
{
    public async Task SendEmailAsync(string email, string subject, string bodyInsert)
    {
        var serverEmail = configuration.GetValue<string>("EmailService:Address");
        if (serverEmail is null)
            throw new Exception("Email service not configured.");

        var serverPassword = configuration.GetValue<string>("EmailService:Password");

        var fromAddress = new MailAddress(serverEmail, "Aurayve");
        var toAddress = new MailAddress(email, email);


        var smtp = new SmtpClient
        {
            Host = "mail.privateemail.com",
            Port = 587,
            EnableSsl = true,
            //DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential(serverEmail, serverPassword)
        };
        using var message = new MailMessage(fromAddress, toAddress);

        message.Subject = subject;

        var htmlBuilder = new StringBuilder();
        htmlBuilder.AppendLine("<html>");
        htmlBuilder.AppendLine("<head></head>");
        htmlBuilder.AppendLine("<body'>");
        htmlBuilder.AppendLine("<div style='margin:auto;'>");
        htmlBuilder.AppendLine($"<p style='font-decoration: none;'>Hi, {email}.</p>");
        htmlBuilder.AppendLine(bodyInsert);
        htmlBuilder.AppendLine("<p>Best regards,</p>");
        htmlBuilder.AppendLine("<p>Aurayve</p>");
        htmlBuilder.AppendLine("</div>");
        htmlBuilder.AppendLine("</body>");
        htmlBuilder.AppendLine("</html>");

        var fullBody = htmlBuilder.ToString();
        message.Body = fullBody;

        message.IsBodyHtml = true;

        await smtp.SendMailAsync(message);
    }
}