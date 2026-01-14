using System.Net.Mail;
namespace MiniX.Emailer.Gen;

public class EmailBuilder
{
    private MailMessage _message = new();

    public EmailBuilder To(string to)
    {
        _message.To.Add(to);
        return this;
    }

    public EmailBuilder Subject(string subject)
    {
        _message.Subject = subject;
        return this;
    }

    public EmailBuilder Body(string email, string pin, string modo = "2fa")
    {
        _message.IsBodyHtml = true;
        switch (modo)
        {
            case "2fa":
                _message.Body = new EmailGenerator().GenerarMail2fa(email, pin);
                break;
            default:
                break;
        }
        return this;
    }

    public MailMessage Build() => _message;
}
