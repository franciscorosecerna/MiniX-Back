using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace MiniX.Emailer;

public class EmailSender
{
    protected static SmtpClient? smtp = null;
    protected void configSmtp(MailMessage mail)
    {
        var jsonContent = File.ReadAllText("appsettings.json");
        var appSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
        if (appSettings == null || !appSettings.ContainsKey("EmailConfiguration")) return;

        var options = JsonSerializer.Deserialize<Dictionary<string, string>>(appSettings["EmailConfiguration"].ToString() ?? "");
        if (options == null) return;

        if (!options.ContainsKey("smtpHost") ||
            !options.ContainsKey("smtpPort") ||
            !options.ContainsKey("emailAddr") ||
            !options.ContainsKey("emailPass"))
        {
            return;
        }

        mail.Sender = new MailAddress(options["emailAddr"]);
        mail.From = new MailAddress(options["emailAddr"]);

        if (null != smtp) return;
        smtp = new();
        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
        smtp.EnableSsl = true;
        smtp.Host = options["smtpHost"];
        smtp.Port = int.Parse(options["smtpPort"].ToString());
        smtp.Credentials = new NetworkCredential(options["emailAddr"], options["emailPass"]);
    }

    public virtual void Send(MailMessage message)
    {
        configSmtp(message);
        if (smtp == null) return;
        try
        {
            smtp.Send(message);
            message.Dispose();
        }
        catch (Exception)
        {
            throw;
        }
    }

}
