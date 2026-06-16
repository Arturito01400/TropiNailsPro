using TropiNailsPro.Models;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.AspNetCore.SignalR;
using TropiNailsPro.Hubs;

namespace TropiNailsPro.Services
{
    public class NotificacionService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificacionService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        // ===============================
        // 🔹 ENVÍO EMAIL AUTOMÁTICO
        // ===============================
        public async Task EnviarCorreoAsync(Manicurista manicurista)
        {
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress("TropiNailsPro", "tropinailspro@gmail.com"));
            mensaje.To.Add(MailboxAddress.Parse(manicurista.Email));
            mensaje.Subject = "Renovación de suscripción confirmada";

            mensaje.Body = new TextPart("html")
            {
                Text = $@"
                    <h2>Hola {manicurista.NombreNegocio}</h2>
                    <p>Tu suscripción ha sido renovada correctamente.</p>
                    <p>Fecha de vencimiento: <strong>{manicurista.FechaVencimiento:dd/MM/yyyy}</strong></p>
                    <p>Gracias por confiar en TropiNailsPro.</p>"
            };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, false);
            await client.AuthenticateAsync("tropinailspro@gmail.com", "TU_PASSWORD_GMAIL");
            await client.SendAsync(mensaje);
            await client.DisconnectAsync(true);
        }

        // ===============================
        // 🔹 ENVÍO SMS AUTOMÁTICO
        // ===============================
        public Task EnviarSmsAsync(Manicurista manicurista, string numeroTelefono)
        {
            Console.WriteLine($"[SMS] Hola {manicurista.NombreNegocio}, tu suscripción se renovó. Vence: {manicurista.FechaVencimiento:dd/MM/yyyy}");
            return Task.CompletedTask;
        }

        // ===============================
        // 🔥 NOTIFICACIÓN POR ID (CORRECTO)
        // ===============================
        public async Task EnviarNotificacionTiempoReal(int manicuristaId, string mensaje)
        {
            await _hubContext.Clients.Group($"manicurista-{manicuristaId}")
                .SendAsync("RecibirNotificacion", mensaje);
        }

        // ===============================
        // 🔥 NOTIFICACIÓN POR STRING (COMPATIBLE)
        // ===============================
        public async Task EnviarNotificacionTiempoReal(string usuario, string mensaje)
        {
            await _hubContext.Clients.Group($"manicurista-{usuario}")
                .SendAsync("RecibirNotificacion", mensaje);
        }

        // ===============================
        // 🔥 CONTADOR POR ID
        // ===============================
        public async Task ActualizarContador(int manicuristaId, int cantidad)
        {
            await _hubContext.Clients.Group($"manicurista-{manicuristaId}")
                .SendAsync("ActualizarContador", cantidad);
        }

        // ===============================
        // 🔥 CONTADOR POR STRING (COMPATIBLE)
        // ===============================
        public async Task ActualizarContador(string usuario, int cantidad)
        {
            await _hubContext.Clients.Group($"manicurista-{usuario}")
                .SendAsync("ActualizarContador", cantidad);
        }
    }
}