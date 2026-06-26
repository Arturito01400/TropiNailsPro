using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Models;
using TropiNailsPro.Data;
using Microsoft.AspNetCore.SignalR;
using TropiNailsPro.Hubs;
using Microsoft.AspNetCore.Http;
using TropiNailsPro.Services;

namespace TropiNailsPro.Controllers
{
    public class CitasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<OnlineHub> _hub;
        private readonly NotificacionService _notificacionService;
private readonly TimeService _timeService;

        public CitasController(
    AppDbContext context,
    IHubContext<OnlineHub> hub,
    NotificacionService notificacionService,
    TimeService timeService)
        {
            _context = context;
            _hub = hub;
            _notificacionService = notificacionService;
            _timeService = timeService;
        }

        // ======================================================
        // INDEX
        // ======================================================
        public async Task<IActionResult> Index()
        {
           var manicuristaId =
    HttpContext.Session.GetInt32("ManicuristaId");

            if (!manicuristaId.HasValue)
                return RedirectToAction("Login","Auth");


            // =====================================================
            // AJUSTE:
            // 1- FECHA MAS CERCANA PRIMERO
            // 2- HORA MAS TEMPRANA SIEMPRE NUMERO 1
            // 3- SI HAY MISMA HORA USA ID SOLO COMO DESEMPATE
            // =====================================================
            var citas = await _context.Citas
                .Where(c =>
                    c.ManicuristaId == manicuristaId.Value)
                .OrderBy(c => c.Fecha.Date)   // primero fecha
                .ThenBy(c => c.Hora)          // luego hora temprana
                .ThenBy(c => c.Id)            // desempate
                .ToListAsync();


            ViewBag.EsManicurista = true;

            // para tu sección "Próximos días"
            ViewBag.ProximosDias = citas
    .Where(c => c.Fecha.Date >= _timeService.ObtenerHoraLocal().Date)
                .ToList();

            return View(citas);
        }

        // ======================================================
        // CREATE GET
        // ======================================================
        public IActionResult Create()
{
    ViewBag.HoraLocal = _timeService.ObtenerHoraLocal().ToString("HH:mm");
    return View();
}

        // ======================================================
        // CREATE POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cita cita)
        {
            var manicuristaId =
    HttpContext.Session.GetInt32("ManicuristaId");

            if (!manicuristaId.HasValue)
                return RedirectToAction("Login", "Auth");


            if (cita.Fecha.Date < _timeService.ObtenerHoraLocal().Date)
            {
                ModelState.AddModelError("",
                "⚠️ No se pueden registrar citas en fechas pasadas.");
                return View(cita);
            }

            cita.ManicuristaId = manicuristaId.Value;
cita.FechaRegistro = _timeService.ObtenerHoraLocal();
cita.Estado = "Pendiente";
cita.CreadaPorManicurista = true;

// 🔥 ASOCIAR CITA A LA CLIENTA
var clienta = await _context.Usuarios
    .FirstOrDefaultAsync(u =>
        u.Rol == "Clienta" &&
        u.ManicuristaId == manicuristaId.Value &&
        u.Nombre.Trim().ToLower() ==
        cita.NombreClienta.Trim().ToLower());

if (clienta != null)
{
    cita.ClienteId = clienta.Id;
}
            if (cita.DuracionMinutos <= 0)
                cita.DuracionMinutos = 60;

            cita.HoraFin =
                cita.Hora +
                TimeSpan.FromMinutes(cita.DuracionMinutos);


            var citasDelDia = await _context.Citas
                .Where(c =>
                    c.ManicuristaId == manicuristaId.Value &&
                    c.Fecha.Date == cita.Fecha.Date)
                .ToListAsync();


            bool existeChoque = citasDelDia.Any(c =>
            {
                var horaFinExistente =
                    c.HoraFin == TimeSpan.Zero
                    ? c.Hora + TimeSpan.FromMinutes(
                        c.DuracionMinutos <= 0 ? 60 : c.DuracionMinutos)
                    : c.HoraFin;

                return cita.Hora < horaFinExistente &&
                       cita.HoraFin > c.Hora;
            });


            if (existeChoque)
            {
                ModelState.AddModelError("",
                "⚠️ Ya existe una cita en ese horario.");
                return View(cita);
            }


            _context.Citas.Add(cita);
            await _context.SaveChangesAsync();


            await EnviarDatosTiempoReal(
                manicuristaId.Value);


            var usuario =
                User.Identity?.Name ?? "Sistema";


            await _notificacionService
            .EnviarNotificacionTiempoReal(
                usuario,
                $"Nueva cita con {cita.NombreClienta} 🗓️");


            await _notificacionService
            .ActualizarContador(usuario,1);

var horaTexto =
    DateTime.Today
    .Add(cita.Hora)
    .ToString("hh:mm tt");



            await _notificacionService
    .EnviarNotificacionTiempoReal(
        cita.NombreClienta,
        $"Tu manicurista te agendó para " +
        $"{cita.Fecha:dd/MM/yyyy} " +
        $"a las {horaTexto} 💅"
    );


            return RedirectToAction(nameof(Index));
        }


        // ======================================================
        // EDIT GET
        // ======================================================
        public async Task<IActionResult> Edit(int id)
        {
            var cita = await _context.Citas.FindAsync(id);

            if (cita == null)
                return RedirectToAction(nameof(Index));

            return View(cita);
        }


        // ======================================================
        // EDIT POST
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Cita cita)
        {
            ModelState.Remove(nameof(Cita.HoraFin));

            if (!ModelState.IsValid)
                return View(cita);


            var citaDb =
                await _context.Citas.FindAsync(id);

            if (citaDb == null)
                return RedirectToAction(nameof(Index));


            if (cita.Fecha.Date < _timeService.ObtenerHoraLocal().Date)
            {
                ModelState.AddModelError("",
                "⚠️ No se pueden registrar citas en fechas pasadas.");
                return View(cita);
            }


            var nuevaHoraFin =
                cita.Hora +
                TimeSpan.FromMinutes(
                    cita.DuracionMinutos);


            var citasDelDia = await _context.Citas
                .Where(c =>
                    c.Id != id &&
                    c.ManicuristaId == citaDb.ManicuristaId &&
                    c.Fecha.Date == cita.Fecha.Date)
                .ToListAsync();


            bool existeChoque = citasDelDia.Any(c =>
            {
                var horaFinExistente =
                    c.HoraFin == TimeSpan.Zero
                    ? c.Hora +
                      TimeSpan.FromMinutes(
                      c.DuracionMinutos <= 0
                      ? 60
                      : c.DuracionMinutos)
                    : c.HoraFin;

                return cita.Hora < horaFinExistente &&
                       nuevaHoraFin > c.Hora;
            });


            if (existeChoque)
            {
                ModelState.AddModelError("",
                "⚠️ Ya existe una cita en ese horario.");

                return View(cita);
            }


            citaDb.NombreClienta = cita.NombreClienta;
            citaDb.Fecha = cita.Fecha;
            citaDb.Hora = cita.Hora;
            citaDb.Servicio = cita.Servicio;
            citaDb.NotasAdicionales = cita.NotasAdicionales;
            citaDb.DuracionMinutos = cita.DuracionMinutos;
            citaDb.HoraFin = nuevaHoraFin;


            await _context.SaveChangesAsync();


            await EnviarDatosTiempoReal(
                citaDb.ManicuristaId);


            var usuario =
                User.Identity?.Name ?? "Sistema";


            await _notificacionService
                .EnviarNotificacionTiempoReal(
                    usuario,
                    $"Cita actualizada: {citaDb.NombreClienta} ✏️");


            var horaTexto = DateTime.Today
    .Add(citaDb.Hora)
    .ToString("hh:mm tt");

await _notificacionService
    .EnviarNotificacionTiempoReal(
        citaDb.NombreClienta,
        $"Tu cita fue actualizada para {citaDb.Fecha:dd/MM/yyyy} a las {horaTexto} ✨"
    );


            return RedirectToAction(nameof(Index));
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var cita =
                    await _context.Citas.FindAsync(id);

                if (cita == null)
                    return RedirectToAction(nameof(Index));

                var manicuristaId =
                    cita.ManicuristaId;

                _context.Citas.Remove(cita);

                await _context.SaveChangesAsync();

                await EnviarDatosTiempoReal(
                    manicuristaId);

                var usuario =
                    User.Identity?.Name ?? "Sistema";

                await _notificacionService
                .EnviarNotificacionTiempoReal(
                    usuario,
                    $"Cita eliminada: {cita.NombreClienta} ❌");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "ERROR AL ELIMINAR: "
                    + ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SiguienteCliente(int id)
        {
            var manicuristaId =
    HttpContext.Session.GetInt32("ManicuristaId");

            if (!manicuristaId.HasValue)
                return RedirectToAction(
                    "Login",
                    "Auth");


            var hoy = _timeService.ObtenerHoraLocal().Date;

            var actual =
                await _context.Citas
                .FirstOrDefaultAsync(c =>
                    c.ManicuristaId == manicuristaId &&
                    c.Fecha.Date == hoy &&
                    c.Estado == "En curso");

            if (actual != null)
                actual.Estado = "Finalizado";


            var siguiente =
                await _context.Citas
                .Where(c =>
                    c.ManicuristaId == manicuristaId &&
                    c.Fecha.Date == hoy &&
                    c.Estado == "Pendiente")
                .OrderBy(c => c.Hora)
                .FirstOrDefaultAsync();

            if (siguiente != null)
                siguiente.Estado = "En curso";

            await _context.SaveChangesAsync();

            await EnviarDatosTiempoReal(
                manicuristaId.Value);

            var usuario =
                User.Identity?.Name ?? "Sistema";

            await _notificacionService
                .EnviarNotificacionTiempoReal(
                    usuario,
                    "Pasaste al siguiente cliente ⏭️");

            return RedirectToAction(nameof(Index));
        }



        private async Task EnviarDatosTiempoReal(
            int manicuristaId)
        {
            var citas =
                await _context.Citas
                .Where(c =>
                    c.ManicuristaId == manicuristaId)
                .OrderBy(c=>c.Fecha.Date)
                .ThenBy(c=>c.Hora)
                .ThenBy(c=>c.Id)
                .ToListAsync();

            await _hub.Clients
                .Group($"manicurista-{manicuristaId}")
                .SendAsync(
                    "ActualizarCitas",
                    citas);
        }
  
  
  
  
    }
}