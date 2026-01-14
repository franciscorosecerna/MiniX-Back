namespace MiniX.Emailer;
using MiniX.Emailer.Gen;

public class OtpEmailSender : EmailSender
{
    public void Send(string To, string email, string pin)
    {
        var mail = new EmailBuilder().To(To).Body(email, pin).Subject("Mail de Recuperacion").Build();
        base.Send(mail);

    }
}
