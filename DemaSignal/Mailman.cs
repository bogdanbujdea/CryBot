using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace DemaSignal
{
    public class Mailman : IMailman
    {
        public async Task SendMailAsync(string text)
        {
            MailMessage mail = new MailMessage();
            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(Environment.GetEnvironmentVariable("gmailUser"), Environment.GetEnvironmentVariable("password"))
            };
            mail.From = new MailAddress("bujdeabogdan@gmail.com");
            mail.To.Add("bogdan@thewindev.net");

            mail.Subject = "Signal";

            StringBuilder sbBody = new StringBuilder();
            sbBody.AppendLine(text);
            mail.Body = sbBody.ToString();

            await smtpClient.SendMailAsync(mail);
        }
    }

    public interface IMailman
    {
    }
}
