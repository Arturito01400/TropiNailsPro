using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TropiNailsPro.Models;

namespace TropiNailsPro.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly EmailSettings _emailSettings;

        public EmailService(
            IConfiguration config,
            IOptions<EmailSettings> emailSettings)
        {
            _config = config;
            _emailSettings = emailSettings.Value;
        }

        // ======================================================
        // 📧 MÉTODO GENERAL
        // ======================================================
        public async System.Threading.Tasks.Task EnviarCorreoAsync(
            string destino,
            string asunto,
            string cuerpoHtml)
        {
            var apiKey = _config["Brevo:ApiKey"];

            var senderName = _emailSettings.SenderName;

            var senderEmail = _emailSettings.SenderEmail;

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("Brevo ApiKey no configurada");

            Configuration.Default.ApiKey.Clear();
            Configuration.Default.ApiKey.Add("api-key", apiKey);

            var apiInstance = new TransactionalEmailsApi();

            var email = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender(senderName, senderEmail),

                To = new List<SendSmtpEmailTo>
                {
                    new SendSmtpEmailTo(destino)
                },

                Subject = asunto,
                HtmlContent = cuerpoHtml
            };

            await apiInstance.SendTransacEmailAsync(email);
        }

        // ======================================================
        // 🔐 RECUPERACIÓN
        // ======================================================
        public async System.Threading.Tasks.Task EnviarRecuperacionAsync(
            string destino,
            string enlaceReset)
        {
            string asunto = "Recupera tu contraseña - TropiNails Pro";

            string cuerpo = $@"
                <div style='font-family:Arial;padding:20px'>
                    <h2>Hola 👋</h2>
                    <p>Recibimos una solicitud para cambiar tu contraseña.</p>

                    <a href='{enlaceReset}'
                       style='background:#ff4da6;color:white;padding:10px 20px;border-radius:8px'>
                       Cambiar contraseña
                    </a>

                    <p>Si no fuiste tú, ignora este correo.</p>
                </div>";

            await EnviarCorreoAsync(destino, asunto, cuerpo);
        }
    }
}