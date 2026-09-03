
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using WebPush;
using System.Text.Json;

namespace TropiNailsPro.Services
{
    public class PushNotificationService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PushNotificationService> _logger;

        public PushNotificationService(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<PushNotificationService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // ============================================================
        // ENVIAR NOTIFICACIÓN PUSH A UN USUARIO
        // ============================================================

        public async Task EnviarAsync(
            int usuarioId,
            string titulo,
            string mensaje,
            string url = "/")
        {
            var publicKey = _configuration["VAPID:PublicKey"];
            var privateKey = _configuration["VAPID:PrivateKey"];
            var subject = _configuration["VAPID:Subject"];

            if (string.IsNullOrWhiteSpace(publicKey) ||
                string.IsNullOrWhiteSpace(privateKey) ||
                string.IsNullOrWhiteSpace(subject))
            {
                _logger.LogError(
                    "Las claves VAPID no están configuradas correctamente."
                );

                return;
            }

            var suscripciones = await _context.PushSubscriptions
                .Where(p =>
                    p.UsuarioId == usuarioId &&
                    p.Activa)
                .ToListAsync();

            if (!suscripciones.Any())
            {
                _logger.LogInformation(
                    "El usuario {UsuarioId} no tiene suscripciones Push activas.",
                    usuarioId
                );

                return;
            }

            var vapidDetails = new VapidDetails(
                subject,
                publicKey,
                privateKey
            );

            var payload = JsonSerializer.Serialize(new
            {
                title = titulo,
                body = mensaje,
                icon = "/images/logo-tropinails.png",
                badge = "/images/logo-tropinails.png",
                url = url
            });

            var webPushClient = new WebPushClient();

            foreach (var suscripcion in suscripciones)
            {
                try
                {
                    var pushSubscription = new WebPush.PushSubscription(
                        suscripcion.Endpoint,
                        suscripcion.P256dh,
                        suscripcion.Auth
                    );

                    await webPushClient.SendNotificationAsync(
                        pushSubscription,
                        payload,
                        vapidDetails
                    );

                    suscripcion.UltimoUso = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Push enviado correctamente al usuario {UsuarioId}.",
                        usuarioId
                    );
                }
                catch (WebPushException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Error enviando Push al usuario {UsuarioId}. Código HTTP: {StatusCode}",
                        usuarioId,
                        ex.StatusCode
                    );

                    // ====================================================
                    // SUSCRIPCIÓN EXPIRADA O INVÁLIDA
                    // ====================================================

                    if (ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                        ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        suscripcion.Activa = false;

                        _logger.LogInformation(
                            "Suscripción Push desactivada por estar inválida."
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error inesperado enviando Push al usuario {UsuarioId}.",
                        usuarioId
                    );
                }
            }

            await _context.SaveChangesAsync();
        }

        // ============================================================
        // GUARDAR SUSCRIPCIÓN DEL NAVEGADOR
        // ============================================================

        public async Task<bool> RegistrarSuscripcionAsync(
            int usuarioId,
            string endpoint,
            string p256dh,
            string auth,
            string? plataforma,
            string? navegador,
            string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(endpoint) ||
                string.IsNullOrWhiteSpace(p256dh) ||
                string.IsNullOrWhiteSpace(auth))
            {
                return false;
            }

            var existente = await _context.PushSubscriptions
                .FirstOrDefaultAsync(p => p.Endpoint == endpoint);

            if (existente != null)
            {
                existente.UsuarioId = usuarioId;
                existente.P256dh = p256dh;
                existente.Auth = auth;
                existente.Plataforma = plataforma;
                existente.Navegador = navegador;
                existente.UserAgent = userAgent;
                existente.Activa = true;
                existente.UltimoUso = DateTime.UtcNow;
            }
            else
            {
                var nuevaSuscripcion =
                    new TropiNailsPro.Models.PushSubscription
                    {
                        UsuarioId = usuarioId,
                        Endpoint = endpoint,
                        P256dh = p256dh,
                        Auth = auth,
                        Plataforma = plataforma,
                        Navegador = navegador,
                        UserAgent = userAgent,
                        Activa = true,
                        FechaRegistro = DateTime.UtcNow
                    };

                _context.PushSubscriptions.Add(nuevaSuscripcion);
            }

            await _context.SaveChangesAsync();

            return true;
        }

        // ============================================================
        // DESACTIVAR SUSCRIPCIÓN
        // ============================================================

        public async Task<bool> DesactivarSuscripcionAsync(
            int usuarioId,
            string endpoint)
        {
            var suscripcion = await _context.PushSubscriptions
                .FirstOrDefaultAsync(p =>
                    p.UsuarioId == usuarioId &&
                    p.Endpoint == endpoint);

            if (suscripcion == null)
                return false;

            suscripcion.Activa = false;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}

