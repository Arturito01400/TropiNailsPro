using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using TropiNailsPro.Hubs;
using TropiNailsPro.Services;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace TropiNailsPro.Controllers
{
    public class NotificacionesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly PushNotificationService _pushNotificationService;
        private readonly IConfiguration _configuration;

        public NotificacionesController(
            AppDbContext context,
            IHubContext<NotificationHub> hubContext,
            PushNotificationService pushNotificationService,
            IConfiguration configuration)
        {
            _context = context;
            _hubContext = hubContext;
            _pushNotificationService = pushNotificationService;
            _configuration = configuration;
        }

        // ==========================================
        // LISTAR NOTIFICACIONES
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var manicuristaId =
                HttpContext.Session.GetInt32("ManicuristaId");

            // NO SACAR AL USUARIO DEL SISTEMA
            if (manicuristaId == null)
                return View(new List<Notificacion>());

            var notificaciones = await _context.Notificaciones
                .Where(n => n.ManicuristaId == manicuristaId.Value)
                .OrderByDescending(n => n.Fecha)
                .ToListAsync();

            return View(notificaciones);
        }

        // ==========================================
        // CONTADOR DE NOTIFICACIONES
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Contador()
        {
            var manicuristaId =
                HttpContext.Session.GetInt32("ManicuristaId");

            if (manicuristaId == null)
                return Json(0);

            var cantidad = await _context.Notificaciones
                .Where(n =>
                    n.ManicuristaId == manicuristaId.Value &&
                    !n.Leida)
                .CountAsync();

            return Json(cantidad);
        }

        // ==========================================
        // ÚLTIMAS NOTIFICACIONES
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Ultimas()
        {
            var manicuristaId =
                HttpContext.Session.GetInt32("ManicuristaId");

            if (manicuristaId == null)
                return Json(new List<object>());

            var notificaciones = await _context.Notificaciones
                .Where(n => n.ManicuristaId == manicuristaId.Value)
                .OrderByDescending(n => n.Fecha)
                .Take(10)
                .Select(n => new
                {
                    mensaje = n.Mensaje,
                    fecha = n.Fecha.ToString("HH:mm"),
                    leida = n.Leida,
                    url = n.Url
                })
                .ToListAsync();

            return Json(notificaciones);
        }

        // ==========================================
        // MARCAR COMO LEÍDA
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> MarcarLeida(int id)
        {
            var manicuristaId =
                HttpContext.Session.GetInt32("ManicuristaId");

            if (manicuristaId == null)
                return RedirectToAction("Index");

            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n =>
                    n.Id == id &&
                    n.ManicuristaId == manicuristaId.Value);

            if (notificacion == null)
                return RedirectToAction("Index");

            notificacion.Leida = true;

            await _context.SaveChangesAsync();

            // ==========================================
            // ACTUALIZAR CONTADOR EN TIEMPO REAL
            // ==========================================

            var cantidad = await _context.Notificaciones
                .Where(n =>
                    n.ManicuristaId == manicuristaId.Value &&
                    !n.Leida)
                .CountAsync();

            await _hubContext.Clients
                .Group($"manicurista-{manicuristaId.Value}")
                .SendAsync(
                    "ActualizarContador",
                    cantidad);

            // ==========================================
            // REDIRECCIÓN DE LA NOTIFICACIÓN
            // ==========================================

            if (!string.IsNullOrWhiteSpace(notificacion.Url))
                return Redirect(notificacion.Url);

            return RedirectToAction("Index");
        }

        // ==========================================
        // MARCAR TODAS COMO LEÍDAS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            var manicuristaId =
                HttpContext.Session.GetInt32("ManicuristaId");

            if (manicuristaId == null)
                return RedirectToAction("Index");

            var notificaciones = await _context.Notificaciones
                .Where(n =>
                    n.ManicuristaId == manicuristaId.Value &&
                    !n.Leida)
                .ToListAsync();

            foreach (var notificacion in notificaciones)
            {
                notificacion.Leida = true;
            }

            await _context.SaveChangesAsync();

            // ==========================================
            // CONTADOR EN TIEMPO REAL
            // ==========================================

            await _hubContext.Clients
                .Group($"manicurista-{manicuristaId.Value}")
                .SendAsync(
                    "ActualizarContador",
                    0);

            return RedirectToAction("Index");
        }

        // ==========================================
        // CREAR Y ENVIAR NOTIFICACIÓN
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> CrearNotificacion(
            int manicuristaId,
            string mensaje,
            string? url = null)
        {
            if (manicuristaId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "El ID de la manicurista no es válido."
                });
            }

            if (string.IsNullOrWhiteSpace(mensaje))
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "El mensaje de la notificación es obligatorio."
                });
            }

            // ==========================================
            // BUSCAR MANICURISTA Y USUARIO
            // ==========================================

            var manicurista = await _context.Manicuristas
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == manicuristaId);

            if (manicurista == null)
            {
                return NotFound(new
                {
                    success = false,
                    mensaje = "No se encontró la manicurista."
                });
            }

            // ==========================================
            // GUARDAR NOTIFICACIÓN EN BASE DE DATOS
            // ==========================================

            var notificacion = new Notificacion
            {
                ManicuristaId = manicuristaId,
                Mensaje = mensaje,
                Url = url,
                Leida = false,
                Fecha = DateTime.Now
            };

            _context.Notificaciones.Add(notificacion);

            await _context.SaveChangesAsync();

            // ==========================================
            // SIGNALR — NOTIFICACIÓN EN TIEMPO REAL
            // ==========================================

            var grupo =
                $"manicurista-{manicuristaId}";

            await _hubContext.Clients
                .Group(grupo)
                .SendAsync(
                    "RecibirNotificacion",
                    mensaje,
                    url);

            // ==========================================
            // ACTUALIZAR CONTADOR
            // ==========================================

            var cantidad = await _context.Notificaciones
                .Where(n =>
                    n.ManicuristaId == manicuristaId &&
                    !n.Leida)
                .CountAsync();

            await _hubContext.Clients
                .Group(grupo)
                .SendAsync(
                    "ActualizarContador",
                    cantidad);

            // ==========================================
            // WEB PUSH
            // ==========================================
            //
            // PushNotificationService trabaja con UsuarioId,
            // por eso usamos el UsuarioId de la manicurista.
            //
            // ==========================================

            if (manicurista.UsuarioId > 0)
            {
                try
                {
                    await _pushNotificationService.EnviarAsync(
                        manicurista.UsuarioId,
                        "TropiNailsPro",
                        mensaje,
                        url ?? "/");
                }
                catch (Exception ex)
                {
                    // El Push no debe impedir que la
                    // notificación quede guardada en BD
                    // ni que SignalR funcione.

                    Console.WriteLine(
                        $"⚠️ Error enviando Web Push: {ex.Message}");
                }
            }

            // ==========================================
            // RESPUESTA
            // ==========================================

            return Ok(new
            {
                success = true,
                mensaje = "Notificación creada y enviada correctamente.",
                manicuristaId = manicuristaId,
                usuarioId = manicurista.UsuarioId
            });
        }

        // ==========================================
        // OBTENER VAPID PUBLIC KEY
        // ==========================================
        //
        // IMPORTANTE:
        // Solamente entregamos la PUBLIC KEY al navegador.
        //
        // La PrivateKey NUNCA sale del servidor.
        //
        // La clave se obtiene desde:
        //
        // Azure App Service
        // Configuration
        // Environment variables
        //
        // VAPID:PublicKey
        //
        // ==========================================

        [HttpGet]
        public IActionResult ObtenerVapidPublicKey()
        {
            var publicKey =
                _configuration["VAPID:PublicKey"];

            if (string.IsNullOrWhiteSpace(publicKey))
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        mensaje = "La VAPID PublicKey no está configurada en el servidor."
                    });
            }

            return Ok(new
            {
                success = true,
                publicKey = publicKey
            });
        }

        // ==========================================
        // REGISTRAR SUSCRIPCIÓN WEB PUSH
        // ==========================================

        [HttpPost]
        public async Task<IActionResult> RegistrarPush(
            [FromBody] PushSubscriptionRequest request)
        {
            // ==========================================
            // VERIFICAR USUARIO AUTENTICADO
            // ==========================================

            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized(new
                {
                    success = false,
                    mensaje = "El usuario no está autenticado."
                });
            }

            // ==========================================
            // OBTENER USUARIO ACTUAL
            // ==========================================

            var usuarioIdClaim =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;

            if (!int.TryParse(
                    usuarioIdClaim,
                    out int usuarioId))
            {
                return Unauthorized(new
                {
                    success = false,
                    mensaje = "No se pudo identificar al usuario."
                });
            }

            // ==========================================
            // VALIDAR DATOS
            // ==========================================

            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "No se recibió la suscripción Push."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Endpoint))
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "El endpoint Push es obligatorio."
                });
            }

            if (string.IsNullOrWhiteSpace(request.P256dh))
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "La clave p256dh es obligatoria."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Auth))
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "La clave auth es obligatoria."
                });
            }

            // ==========================================
            // REGISTRAR EN PUSHNOTIFICATIONSERVICE
            // ==========================================

            try
            {
                var registrada =
                    await _pushNotificationService
                        .RegistrarSuscripcionAsync(
                            usuarioId,
                            request.Endpoint,
                            request.P256dh,
                            request.Auth,
                            request.Plataforma,
                            request.Navegador,
                            request.UserAgent
                        );

                if (!registrada)
                {
                    return BadRequest(new
                    {
                        success = false,
                        mensaje = "No se pudo registrar la suscripción Push."
                    });
                }

                // ======================================
                // RESPUESTA EXITOSA
                // ======================================

                return Ok(new
                {
                    success = true,
                    mensaje = "Suscripción Push registrada correctamente.",
                    usuarioId = usuarioId
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"⚠️ Error registrando suscripción Push: {ex.Message}"
                );

                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        mensaje = "Ocurrió un error registrando la suscripción Push."
                    }
                );
            }
        }

        // ==========================================
        // DESACTIVAR SUSCRIPCIÓN WEB PUSH
        // ==========================================

        [HttpPost]
        public async Task<IActionResult> DesactivarPush(
            [FromBody] DesactivarPushRequest request)
        {
            // ==========================================
            // VERIFICAR AUTENTICACIÓN
            // ==========================================

            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized(new
                {
                    success = false,
                    mensaje = "El usuario no está autenticado."
                });
            }

            // ==========================================
            // OBTENER USUARIO ACTUAL
            // ==========================================

            var usuarioIdClaim =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;

            if (!int.TryParse(
                    usuarioIdClaim,
                    out int usuarioId))
            {
                return Unauthorized(new
                {
                    success = false,
                    mensaje = "No se pudo identificar al usuario."
                });
            }

            // ==========================================
            // VALIDAR ENDPOINT
            // ==========================================

            if (request == null ||
                string.IsNullOrWhiteSpace(request.Endpoint))
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "El endpoint Push es obligatorio."
                });
            }

            try
            {
                var desactivada =
                    await _pushNotificationService
                        .DesactivarSuscripcionAsync(
                            usuarioId,
                            request.Endpoint
                        );

                if (!desactivada)
                {
                    return NotFound(new
                    {
                        success = false,
                        mensaje = "No se encontró la suscripción Push."
                    });
                }

                return Ok(new
                {
                    success = true,
                    mensaje = "Suscripción Push desactivada correctamente."
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"⚠️ Error desactivando suscripción Push: {ex.Message}"
                );

                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        mensaje = "Ocurrió un error desactivando la suscripción Push."
                    }
                );
            }
        }
    }

    // ============================================================
    // MODELO PARA RECIBIR LA SUSCRIPCIÓN WEB PUSH
    // ============================================================

    public class PushSubscriptionRequest
    {
        public string? Endpoint { get; set; }

        public string? P256dh { get; set; }

        public string? Auth { get; set; }

        public string? Plataforma { get; set; }

        public string? Navegador { get; set; }

        public string? UserAgent { get; set; }
    }

    // ============================================================
    // MODELO PARA DESACTIVAR UNA SUSCRIPCIÓN WEB PUSH
    // ============================================================

    public class DesactivarPushRequest
    {
        public string? Endpoint { get; set; }
    }
}