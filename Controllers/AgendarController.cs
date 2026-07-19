using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using TropiNailsPro.Models.ViewModels;
using TropiNailsPro.Hubs;
using TropiNailsPro.Services;

namespace TropiNailsPro.Controllers
{
    public class AgendarController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TimeService _timeService;
        private readonly NotificacionService _notificacionService;
        private readonly IHubContext<OnlineHub> _hub;

        public AgendarController(
            AppDbContext context,
            TimeService timeService,
            NotificacionService notificacionService,
            IHubContext<OnlineHub> hub)
        {
            _context = context;
            _timeService = timeService;
            _notificacionService = notificacionService;
            _hub = hub;
        }

        // ======================================================
        // MOSTRAR HORARIOS DISPONIBLES
        // ======================================================

        public async Task<IActionResult> Index(int manicuristaId)
        {
            var horarios = await _context.Disponibilidades
                .Include(d => d.Manicurista)
                .ThenInclude(m => m.Usuario)
                .Where(d =>
                    d.ManicuristaId == manicuristaId &&
                    d.Disponible &&
                    d.Fecha.Date >= _timeService.ObtenerHoraLocal().Date)
                .OrderBy(d => d.Fecha)
                .ThenBy(d => d.Hora)
                .ToListAsync();

            if (!horarios.Any())
            {
                ViewBag.Mensaje =
                    "No hay horarios disponibles actualmente.";
            }

            ViewBag.ManicuristaId = manicuristaId;

            return View(horarios);
        }

        // ======================================================
        // CONFIRMAR RESERVA DE CLIENTA
        // ======================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmar(
            AgendarCitaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var disponibilidad =
                await _context.Disponibilidades
                .Include(d => d.Manicurista)
                .ThenInclude(m => m.Usuario)
                .FirstOrDefaultAsync(d =>
                    d.Id == model.DisponibilidadId);

            if (disponibilidad == null)
            {
                return NotFound();
            }

            if (!disponibilidad.Disponible)
            {
                TempData["Error"] =
                    "Este horario ya fue reservado.";

                return RedirectToAction(
                    "Index",
                    new
                    {
                        manicuristaId = model.ManicuristaId
                    });
            }

            // ==================================================
            // CREAR CITA
            // ==================================================

            var cita = new Cita
            {
                NombreClienta = model.NombreClienta,

                TelefonoCliente = model.TelefonoCliente,

                Fecha = disponibilidad.Fecha,

                Hora = disponibilidad.Hora,

                Servicio = model.Servicio,

                NotasAdicionales =
                    model.NotasAdicionales,

                ManicuristaId =
                    disponibilidad.ManicuristaId,

                Estado = "Pendiente",

                FechaRegistro =
                    _timeService.ObtenerHoraLocal(),

                CreadaPorManicurista = false
            };

            // Calculamos duración estándar
            cita.DuracionMinutos = 60;

            cita.HoraFin =
                cita.Hora +
                TimeSpan.FromMinutes(
                    cita.DuracionMinutos);

            _context.Citas.Add(cita);

            // Bloquear horario
            disponibilidad.Disponible = false;

            await _context.SaveChangesAsync();

            // ==================================================
            // NOTIFICACIÓN TIEMPO REAL
            // ==================================================

            await _hub.Clients
                .Group(
                    $"manicurista-{disponibilidad.ManicuristaId}")
                .SendAsync(
                    "NuevaCita",
                    cita);

            await _notificacionService
                .EnviarNotificacionTiempoReal(
                    disponibilidad.Manicurista.Usuario.Nombre,
                    $"Nueva solicitud de cita de {cita.NombreClienta} 💅");

            // ==================================================
            // CREAR LINK WHATSAPP
            // ==================================================

            string telefono = "";

            if (!string.IsNullOrWhiteSpace(
                disponibilidad.Manicurista.TelefonoNegocio))
            {
                telefono =
                    disponibilidad.Manicurista.TelefonoNegocio;
            }
            else if (!string.IsNullOrWhiteSpace(
                disponibilidad.Manicurista.Usuario.WhatsApp))
            {
                telefono =
                    disponibilidad.Manicurista.Usuario.WhatsApp;
            }
            else
            {
                telefono =
                    disponibilidad.Manicurista.Usuario.Telefono
                    ?? "";
            }

            var mensaje =
$@"💅 Nueva solicitud de cita - TropiNails Pro

Hola {disponibilidad.Manicurista.Nombre} 👋

Una clienta acaba de solicitar una cita.

👩 Clienta:
{cita.NombreClienta}

📅 Fecha:
{cita.Fecha:dd/MM/yyyy}

⏰ Hora:
{cita.Hora}

💖 Diseño:
{cita.Servicio}

La clienta está esperando confirmación.

Gracias por usar TropiNails Pro 💅✨";

            var whatsapp =
                "https://wa.me/" +
                telefono +
                "?text=" +
                Uri.EscapeDataString(mensaje);

            TempData["WhatsApp"] = whatsapp;

            return RedirectToAction(
                "Confirmacion");
        }

        // ======================================================
        // PANTALLA FINAL
        // ======================================================

        public IActionResult Confirmacion()
        {
            return View();
        }
    }
}