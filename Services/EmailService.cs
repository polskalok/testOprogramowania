using System.Net;
using System.Net.Mail;

namespace przychodnia.Services
{
    public static class EmailService
    {
        private const string SenderEmail = "ttestowski578@gmail.com";
        private const string SenderPassword = "rdti esxs kezm wjdm";
        private const string SmtpServer = "smtp.gmail.com";
        private const int SmtpPort = 587;

        public static async Task<bool> SendPasswordRecoveryEmail(string recipientEmail, string newPassword)
        {
            try
            {
                using (var smtpClient = new SmtpClient(SmtpServer, SmtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential(SenderEmail, SenderPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(SenderEmail, "Przychodnia"),
                        Subject = "Resetowanie hasła - Przychodnia",
                        Body = GenerateEmailBody(newPassword),
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(recipientEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas wysyłania e-maila: {ex.Message}");
                return false;
            }
        }

        private static string GenerateEmailBody(string newPassword)
        {
            return $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; background-color: #f9f9f9; padding: 20px; border-radius: 10px; }}
                        .header {{ background-color: #1B263B; color: white; padding: 20px; text-align: center; border-radius: 5px; margin-bottom: 20px; }}
                        .content {{ background-color: white; padding: 20px; border-radius: 5px; }}
                        .password-box {{ background-color: #B2DAFA; padding: 15px; border-left: 4px solid #1B263B; margin: 20px 0; border-radius: 5px; font-family: monospace; font-size: 18px; font-weight: bold; text-align: center; }}
                        .footer {{ text-align: center; color: #666; margin-top: 20px; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h2>Resetowanie Hasła</h2>
                        </div>
                        <div class=""content"">
                            <p>Cześć,</p>
                            <p>Poprosiłeś o zmianę hasła w systemie Przychodnia. Poniżej znajduje się Twoje nowe tymczasowe hasło:</p>
                            <div class=""password-box"">
                                {newPassword}
                            </div>
                            <p><strong>Ważne:</strong> Po zalogowaniu się tym hasłem zalecaną jest zmiana hasła na własne.</p>
                            <p>Jeśli nie prosiłeś o reset hasła, zignoruj tę wiadomość.</p>
                        </div>
                        <div class=""footer"">
                            <p>Wiadomość została wygenerowana automatycznie. Prosimy nie odpowiadać na tę wiadomość.</p>
                            <p>&copy; 2026 Przychodnia. Wszystkie prawa zastrzeżone.</p>
                        </div>
                    </div>
                </body>
                </html>";
        }
    }
}
