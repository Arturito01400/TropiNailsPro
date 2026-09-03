
using TropiNailsPro.Models;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.AspNetCore.SignalR;
using TropiNailsPro.Hubs;
using TropiNailsPro.Data;
using Microsoft.EntityFrameworkCore;
using WebPush;

namespace TropiNailsPro.Services
{
    public class NotificacionService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public NotificacionService(
            IHubContext<NotificationHub> hubContext,
            AppDbContext context,
            IConfiguration configuration)
        {
            _hubContext = hubContext;
            _context = context;
            _configuration = configuration;
        }

        // ======================================================
        // 🔔 NOTIFICACIÓN GENERAL
        // ======================================================
        // Esta función mantiene las notificaciones SignalR
        // que ya utiliza TropiNailsPro.
        // ======================================================

        public async Task EnviarNotificacionTiempoReal(
            int manicuristaId,
            string mensaje)
        {
            await _hubContext.Clients
                .Group($"manicurista-{manicuristaId}")
                .SendAsync(
                    "RecibirNotificacion",
                    mensaje);

            // 🔥 PUSH REAL
            await EnviarPushAUsuario(
                manicuristaId,
                mensaje);
        }


        // ======================================================
        // 🔔 NOTIFICACIÓN POR STRING
        // ======================================================
        // Se mantiene para no romper los controladores actuales.
        // ======================================================

        public async Task EnviarNotificacionTiempoReal(
            string usuario,
            string mensaje)
        {
            await _hubContext.Clients
                .Group($"manicurista-{usuario}")
                .SendAsync(
                    "RecibirNotificacion",
                    mensaje);

            // Intentamos localizar el usuario para enviar Push.
            var usuarioDb = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.Email == usuario ||
                    u.Nombre == usuario);

            if (usuarioDb != null)
            {
                await EnviarPushAUsuario(
                    usuarioDb.Id,
                    mensaje);
            }
        }


        // ======================================================
        // 🔥 PUSH REAL
        // ======================================================
        // Envía la notificación a todos los dispositivos activos
        // registrados para ese usuario.
        // ======================================================

        public async Task EnviarPushAUsuario(
            int usuarioId,
            string mensaje)
        {
            try
            {
                var suscripciones =
                    await _context.PushSubscriptions
                        .Where(p =>
                            p.UsuarioId == usuarioId &&
                            p.Activa)
                        .ToListAsync();

                if (!suscripciones.Any())
                {
                    return;
                }


                // ==================================================
                // 🔐 CONFIGURACIÓN VAPID
                // ==================================================

                var publicKey =
                    _configuration["Vapid:PublicKey"];

                var privateKey =
                    _configuration["Vapid:PrivateKey"];

                var subject =
                    _configuration["Vapid:Subject"];


                if (string.IsNullOrWhiteSpace(publicKey) ||
                    string.IsNullOrWhiteSpace(privateKey) ||
                    string.IsNullOrWhiteSpace(subject))
                {
                    Console.WriteLine(
                        "[PUSH] Faltan las claves VAPID.");

                    return;
                }


                var vapidDetails =
                    new VapidDetails(
                        subject,
                        publicKey,
                        privateKey);


                var pushClient =
                    new WebPushClient();


                var payload = System.Text.Json.JsonSerializer.Serialize(
                    new
                    {
                        title = "TropiNailsPro",
                        body = mensaje,
                        icon = "/images/logo-tropinails.png",
                        badge = "/images/logo-tropinails.png",
                        url = "/"
                    });


                foreach (var suscripcion in suscripciones)
                {
                    try
                    {
                        var pushSubscription =
                            new WebPush.PushSubscription(
                                suscripcion.Endpoint,
                                suscripcion.P256dh,
                                suscripcion.Auth);


                        await pushClient.SendNotificationAsync(
                            pushSubscription,
                            payload,
                            vapidDetails);


                        // ==========================================
                        // 🕒 ÚLTIMO USO
                        // ==========================================

                        suscripcion.UltimoUso =
                            DateTime.UtcNow;
                    }
                    catch (WebPushException ex)
                    {
                        Console.WriteLine(
                            $"[PUSH] Error enviando a dispositivo: {ex.Message}");


                        // ==========================================
                        // ❌ SUSCRIPCIÓN EXPIRADA O INVÁLIDA
                        // ==========================================

                        if (ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                            ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            suscripcion.Activa = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"[PUSH] Error inesperado: {ex.Message}");
                    }
                }


                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[PUSH] Error general: {ex.Message}");
            }
        }


        // ======================================================
        // 🔥 PUSH DIRECTO
        // ======================================================
        // Útil cuando ya conocemos el usuario.
        // ======================================================

        public async Task EnviarPush(
            int usuarioId,
            string titulo,
            string mensaje,
            string? url = null)
        {
            try
            {
                var suscripciones =
                    await _context.PushSubscriptions
                        .Where(p =>
                            p.UsuarioId == usuarioId &&
                            p.Activa)
                        .ToListAsync();

                if (!suscripciones.Any())
                {
                    return;
                }


                var publicKey =
                    _configuration["Vapid:PublicKey"];

                var privateKey =
                    _configuration["Vapid:PrivateKey"];

                var subject =
                    _configuration["Vapid:Subject"];


                if (string.IsNullOrWhiteSpace(publicKey) ||
                    string.IsNullOrWhiteSpace(privateKey) ||
                    string.IsNullOrWhiteSpace(subject))
                {
                    Console.WriteLine(
                        "[PUSH] Configuración VAPID incompleta.");

                    return;
                }


                var vapidDetails =
                    new VapidDetails(
                        subject,
                        publicKey,
                        privateKey);


                var pushClient =
                    new WebPushClient();


                var payload =
                    System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            title = titulo,
                            body = mensaje,
                            icon = "/images/logo-tropinails.png",
                            badge = "/images/logo-tropinails.png",
                            url = url ?? "/"
                        });


                foreach (var suscripcion in suscripciones)
                {
                    try
                    {
                        var pushSubscription =
                            new WebPush.PushSubscription(
                                suscripcion.Endpoint,
                                suscripcion.P256dh,
                                suscripcion.Auth);


                        await pushClient.SendNotificationAsync(
                            pushSubscription,
                            payload,
                            vapidDetails);


                        suscripcion.UltimoUso =
                            DateTime.UtcNow;
                    }
                    catch (WebPushException ex)
                    {
                        Console.WriteLine(
                            $"[PUSH] Error: {ex.Message}");


                        if (ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                            ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            suscripcion.Activa = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"[PUSH] Error inesperado: {ex.Message}");
                    }
                }


                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[PUSH] Error general: {ex.Message}");
            }
        }


        // ======================================================
        // 🔢 CONTADOR POR ID
        // ======================================================

        public async Task ActualizarContador(
            int manicuristaId,
            int cantidad)
        {
            await _hubContext.Clients
                .Group($"manicurista-{manicuristaId}")
                .SendAsync(
                    "ActualizarContador",
                    cantidad);
        }


        // ======================================================
        // 🔢 CONTADOR POR STRING
        // ======================================================

        public async Task ActualizarContador(
            string usuario,
            int cantidad)
        {
            await _hubContext.Clients
                .Group($"manicurista-{usuario}")
                .SendAsync(
                    "ActualizarContador",
                    cantidad);
        }


        // ======================================================
        // 📧 ENVÍO EMAIL AUTOMÁTICO
        // ======================================================

        public async Task EnviarCorreoAsync(
            Manicurista manicurista)
        {
            var mensaje =
                new MimeMessage();


            mensaje.From.Add(
                new MailboxAddress(
                    "TropiNailsPro",
                    "tropinailspro@gmail.com"));


            mensaje.To.Add(
                MailboxAddress.Parse(
                    manicurista.Email));


            mensaje.Subject =
                "Renovación de suscripción confirmada";


            mensaje.Body =
                new TextPart("html")
                {
                    Text = $@"
                        <h2>Hola {manicurista.NombreNegocio}</h2>

                        <p>
                            Tu suscripción ha sido renovada correctamente.
                        </p>

                        <p>
                            Fecha de vencimiento:
                            <strong>
                                {manicurista.FechaVencimiento:dd/MM/yyyy}
                            </strong>
                        </p>

                        <p>
                            Gracias por confiar en TropiNailsPro.
                        </p>"
                };


            using var client =
                new SmtpClient();


            await client.ConnectAsync(
                "smtp.gmail.com",
                587,
                false);


            await client.AuthenticateAsync(
                "tropinailspro@gmail.com",
                "TU_PASSWORD_GMAIL");


            await client.SendAsync(
                mensaje);


            await client.DisconnectAsync(
                true);
        }


        // ======================================================
        // 📱 ENVÍO SMS AUTOMÁTICO
        // ======================================================

        public Task EnviarSmsAsync(
            Manicurista manicurista,
            string numeroTelefono)
        {
            Console.WriteLine(
                $"[SMS] Hola {manicurista.NombreNegocio}, " +
                $"tu suscripción se renovó. " +
                $"Vence: {manicurista.FechaVencimiento:dd/MM/yyyy}");

            return Task.CompletedTask;
        }
    }
}

