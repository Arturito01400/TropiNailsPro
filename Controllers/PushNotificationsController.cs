using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TropiNailsPro.Services;

namespace TropiNailsPro.Controllers
{
[Authorize]
public class PushNotificationsController : Controller
{
private readonly PushNotificationService _pushNotificationService;
private readonly ILogger<PushNotificationsController> _logger;


    public PushNotificationsController(
        PushNotificationService pushNotificationService,
        ILogger<PushNotificationsController> logger)
    {
        _pushNotificationService = pushNotificationService;
        _logger = logger;
    }

    // ============================================================
    // REGISTRAR SUSCRIPCIÓN DEL NAVEGADOR
    // ============================================================

    [HttpPost]
    public async Task<IActionResult> Registrar(
        [FromBody] PushSubscriptionRequest request)
    {
        try
        {
            var usuarioId = ObtenerUsuarioId();

            if (usuarioId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Usuario no autenticado."
                });
            }

            if (request == null ||
                string.IsNullOrWhiteSpace(request.Endpoint) ||
                string.IsNullOrWhiteSpace(request.P256dh) ||
                string.IsNullOrWhiteSpace(request.Auth))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Los datos de la suscripción están incompletos."
                });
            }

            var resultado = await _pushNotificationService
                .RegistrarSuscripcionAsync(
                    usuarioId.Value,
                    request.Endpoint,
                    request.P256dh,
                    request.Auth,
                    request.Plataforma,
                    request.Navegador,
                    request.UserAgent
                );

            if (!resultado)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No fue posible registrar la suscripción."
                });
            }

            _logger.LogInformation(
                "Suscripción Push registrada correctamente para el usuario {UsuarioId}.",
                usuarioId.Value
            );

            return Ok(new
            {
                success = true,
                message = "Notificaciones Push activadas correctamente."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error registrando la suscripción Push."
            );

            return StatusCode(500, new
            {
                success = false,
                message = "Ocurrió un error al registrar las notificaciones."
            });
        }
    }

    // ============================================================
    // DESACTIVAR SUSCRIPCIÓN DEL NAVEGADOR
    // ============================================================

    [HttpPost]
    public async Task<IActionResult> Desactivar(
        [FromBody] DesactivarPushRequest request)
    {
        try
        {
            var usuarioId = ObtenerUsuarioId();

            if (usuarioId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Usuario no autenticado."
                });
            }

            if (request == null ||
                string.IsNullOrWhiteSpace(request.Endpoint))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "El Endpoint es obligatorio."
                });
            }

            var resultado = await _pushNotificationService
                .DesactivarSuscripcionAsync(
                    usuarioId.Value,
                    request.Endpoint
                );

            return Ok(new
            {
                success = resultado,
                message = resultado
                    ? "Notificaciones Push desactivadas correctamente."
                    : "No se encontró la suscripción."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error desactivando la suscripción Push."
            );

            return StatusCode(500, new
            {
                success = false,
                message = "Ocurrió un error al desactivar las notificaciones."
            });
        }
    }

    // ============================================================
    // PROBAR NOTIFICACIÓN PUSH
    // ============================================================

    [HttpPost]
    public async Task<IActionResult> Probar()
    {
        try
        {
            var usuarioId = ObtenerUsuarioId();

            if (usuarioId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Usuario no autenticado."
                });
            }

            await _pushNotificationService.EnviarAsync(
                usuarioId.Value,
                "TropiNails Pro 💅",
                "¡Las notificaciones Push están funcionando correctamente!",
                "/Notificaciones"
            );

            return Ok(new
            {
                success = true,
                message = "Notificación de prueba enviada."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error enviando notificación Push de prueba."
            );

            return StatusCode(500, new
            {
                success = false,
                message = "No fue posible enviar la notificación de prueba."
            });
        }
    }

    // ============================================================
    // OBTENER ID DEL USUARIO AUTENTICADO
    // ============================================================

    private int? ObtenerUsuarioId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(claim, out var usuarioId))
        {
            return usuarioId;
        }

        return null;
    }
}

// ============================================================
// DTO PARA REGISTRAR PUSH
// ============================================================

public class PushSubscriptionRequest
{
    public string Endpoint { get; set; } = string.Empty;

    public string P256dh { get; set; } = string.Empty;

    public string Auth { get; set; } = string.Empty;

    public string? Plataforma { get; set; }

    public string? Navegador { get; set; }

    public string? UserAgent { get; set; }
}

// ============================================================
// DTO PARA DESACTIVAR PUSH
// ============================================================

public class DesactivarPushRequest
{
    public string Endpoint { get; set; } = string.Empty;
}


}
