using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using TropiNailsPro.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace TropiNailsPro.Controllers
{
    public class NotificacionesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificacionesController(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // ==========================================
        // LISTAR NOTIFICACIONES
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var manicuristaId = HttpContext.Session.GetInt32("ManicuristaId");

            // 🔥 FIX: NO TE SACA DEL SISTEMA
            if (manicuristaId == null)
                return View(new List<Notificacion>());

            var notificaciones = await _context.Notificaciones
                .Where(n => n.ManicuristaId == manicuristaId)
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
            var manicuristaId = HttpContext.Session.GetInt32("ManicuristaId");

            if (manicuristaId == null)
                return Json(0);

            var cantidad = await _context.Notificaciones
                .Where(n => n.ManicuristaId == manicuristaId && !n.Leida)
                .CountAsync();

            return Json(cantidad);
        }

        // ==========================================
        // 🔥 ÚLTIMAS NOTIFICACIONES (PANEL)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Ultimas()
        {
            var manicuristaId = HttpContext.Session.GetInt32("ManicuristaId");

            if (manicuristaId == null)
                return Json(new List<object>());

            var notificaciones = await _context.Notificaciones
                .Where(n => n.ManicuristaId == manicuristaId)
                .OrderByDescending(n => n.Fecha)
                .Take(10)
                .Select(n => new
                {
                    mensaje = n.Mensaje,
                    fecha = n.Fecha.ToString("HH:mm")
                })
                .ToListAsync();

            return Json(notificaciones);
        }

        // ==========================================
        // MARCAR COMO LEÍDA
        // ==========================================
        public async Task<IActionResult> MarcarLeida(int id)
        {
            var notificacion = await _context.Notificaciones.FindAsync(id);

            if (notificacion != null)
            {
                notificacion.Leida = true;
                await _context.SaveChangesAsync();

                // 🔥 ACTUALIZAR CONTADOR EN TIEMPO REAL
                var cantidad = await _context.Notificaciones
                    .Where(n => n.ManicuristaId == notificacion.ManicuristaId && !n.Leida)
                    .CountAsync();

                await _hubContext.Clients.Group($"manicurista-{notificacion.ManicuristaId}")
                    .SendAsync("ActualizarContador", cantidad);

                if (!string.IsNullOrEmpty(notificacion.Url))
                    return Redirect(notificacion.Url);
            }

            return RedirectToAction("Index");
        }

        // ==========================================
        // MARCAR TODAS COMO LEÍDAS
        // ==========================================
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            var manicuristaId = HttpContext.Session.GetInt32("ManicuristaId");

            if (manicuristaId == null)
                return RedirectToAction("Index");

            var notificaciones = await _context.Notificaciones
                .Where(n => n.ManicuristaId == manicuristaId && !n.Leida)
                .ToListAsync();

            foreach (var n in notificaciones)
            {
                n.Leida = true;
            }

            await _context.SaveChangesAsync();

            // 🔥 ACTUALIZAR CONTADOR EN TIEMPO REAL
            await _hubContext.Clients.Group($"manicurista-{manicuristaId}")
                .SendAsync("ActualizarContador", 0);

            return RedirectToAction("Index");
        }

        // ==========================================
        // CREAR Y ENVIAR NOTIFICACIÓN EN TIEMPO REAL
        // ==========================================
        public async Task<IActionResult> CrearNotificacion(int manicuristaId, string mensaje, string url = null)
        {
            // Guardar en BD
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

            // 🔥 ENVIAR NOTIFICACIÓN
            await _hubContext.Clients.Group($"manicurista-{manicuristaId}")
                .SendAsync("RecibirNotificacion", mensaje, url);

            // 🔥 NUEVO: ACTUALIZAR CONTADOR
            var cantidad = await _context.Notificaciones
                .Where(n => n.ManicuristaId == manicuristaId && !n.Leida)
                .CountAsync();

            await _hubContext.Clients.Group($"manicurista-{manicuristaId}")
                .SendAsync("ActualizarContador", cantidad);

            return Ok(new { success = true });
        }
    }
}