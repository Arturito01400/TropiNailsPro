using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using TropiNailsPro.Hubs;
using System;
using System.Linq;
using System.Threading.Tasks;
using TropiNailsPro.Models.ViewModels;

namespace TropiNailsPro.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<OnlineHub> _hub;

        public DashboardController(
            AppDbContext context,
            IHubContext<OnlineHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        public async Task<IActionResult> Index()
{
    var nombre = HttpContext.Session.GetString("UsuarioNombre");
    var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
    var rol = HttpContext.Session.GetString("UsuarioRol");

    Console.WriteLine("================================");
Console.WriteLine("UsuarioId Session: " + usuarioId);
Console.WriteLine("Rol Session: " + rol);
Console.WriteLine("Nombre Session: " + nombre);
Console.WriteLine("================================");


    if (string.IsNullOrEmpty(nombre) || usuarioId == null)
    {
        TempData["Error"] =
            "Debes iniciar sesión para acceder al sistema.";

        return RedirectToAction("Login", "Auth");
    }

    int? manicuristaIdSession =
    HttpContext.Session.GetInt32("ManicuristaId");

if (manicuristaIdSession == null)
{
    return RedirectToAction("Login", "Auth");
}

int manicuristaId =
    manicuristaIdSession.Value;
    
Console.WriteLine("======== DASHBOARD ========");
    Console.WriteLine("UsuarioId: " + usuarioId);
    Console.WriteLine("ManicuristaId Session: " + manicuristaId);
    Console.WriteLine("Rol: " + rol);
    Console.WriteLine("===========================");
            // ==============================
            // 🔥 BLOQUE CLIENTA (NO TOCADO)
            // ==============================
            // ==============================
// 🔥 BLOQUE CLIENTA (CORREGIDO)
// ==============================
if (rol == "Clienta")
{
    Console.WriteLine("===== CLIENTA =====");
    Console.WriteLine("ClienteId: " + usuarioId);
    Console.WriteLine("ManicuristaId Session: " +
        HttpContext.Session.GetInt32("ManicuristaId"));
    Console.WriteLine("Nombre: " + nombre);
    Console.WriteLine("===================");

    ViewBag.EsClienta = true;
    ViewBag.UsuarioNombre = nombre;

    DateTime hoyClienta = DateTime.Today;

    if (!usuarioId.HasValue)
    {
        return RedirectToAction("Login", "Auth");
    }

    int clienteId = usuarioId.Value;
    int idManicurista = manicuristaId;

    ViewBag.ManicuristaId = idManicurista;

    // =====================
    // 📅 CITAS BASE
    // =====================
    var citasBase = await _context.Citas
        .Where(c =>
            c.ManicuristaId == idManicurista &&
            c.ClienteId == clienteId)
        .ToListAsync();

    // =====================
    // 📅 FILTROS
    // =====================
    var citasHoy = citasBase
        .Where(c => c.Fecha.Date == hoyClienta)
        .OrderBy(c => c.Fecha)
        .ThenBy(c => c.Hora)
        .ToList();

    var citasManana = citasBase
        .Where(c => c.Fecha.Date == hoyClienta.AddDays(1))
        .OrderBy(c => c.Fecha)
        .ThenBy(c => c.Hora)
        .ToList();

    var citasFuturas = citasBase
        .Where(c => c.Fecha.Date > hoyClienta.AddDays(1))
        .OrderBy(c => c.Fecha)
        .ThenBy(c => c.Hora)
        .ToList();

    // =====================
    // VIEWBAG CITAS
    // =====================
    ViewBag.CitasHoy = citasHoy;
    ViewBag.CitasManana = citasManana;
    ViewBag.CitasFuturas = citasFuturas;

    // =====================================================
    // 💅 MODELOS DE UÑAS
    // =====================================================
    ViewBag.ModelosUnas = await _context.ModelosUnas
        .Where(m => m.ManicuristaId == idManicurista)
        .OrderByDescending(m => m.Id)
        .ToListAsync();

    // ======================================
    // 💳 CUENTAS BANCARIAS
    // ======================================
    var manicuristaCuenta = await _context.Manicuristas
        .FirstOrDefaultAsync(m => m.Id == idManicurista);

    if (manicuristaCuenta != null)
    {
        ViewBag.CuentasBancarias =
            await _context.CuentasBancarias
            .Where(c => c.ManicuristaId == idManicurista)
            .ToListAsync();

        Console.WriteLine(
            "💳 Cuentas encontradas: " +
            ((List<CuentaBancaria>)ViewBag.CuentasBancarias).Count);

        Console.WriteLine(
            "💳 Manicurista real encontrada: " +
            manicuristaCuenta.Id);
    }
    else
    {
        ViewBag.CuentasBancarias = new List<CuentaBancaria>();

        Console.WriteLine(
            "❌ No se encontró la manicurista para las cuentas bancarias");
    }

    // =====================================================
    // 💬 CHAT
    // =====================================================
    ViewBag.Chats = await _context.Chats
        .Where(c =>
            (c.UsuarioId == clienteId && c.ReceptorId == idManicurista) ||
            (c.UsuarioId == idManicurista && c.ReceptorId == clienteId))
        .OrderBy(c => c.Fecha)
        .ToListAsync();

    // =====================================================
    // 💖 FEED
    // =====================================================
    var manicuristaReal = await _context.Manicuristas
        .FirstOrDefaultAsync(m =>
            m.Id == idManicurista);

    if (manicuristaReal != null)
    {
        ViewBag.Feed = await _context.Publicaciones
            .Where(p => p.ManicuristaId == manicuristaReal.Id)
            .OrderByDescending(p => p.Fecha)
            .ToListAsync();
    }
    else
    {
        ViewBag.Feed = new List<Publicacion>();
    }

    return View("ClientaDashboard");
}

            // ======================
            // 🔥 DASHBOARD MANICURISTA
            // ======================

            ViewBag.Plan =
                HttpContext.Session.GetString("UsuarioPlan");

            ViewBag.UsuarioNombre = nombre;

            ViewBag.MensajeBienvenida =
                $"👋 Bienvenida, {nombre}!";

            // 🔥 LINK PRO REAL (CORREGIDO)

           // 🔥 LINK PRO REAL (FIX DEFINITIVO)

var manicurista = await _context.Manicuristas
    .FirstOrDefaultAsync(m =>
        m.Id == manicuristaId);

string registroUnico;

// 🔥 SI NO EXISTE -> CREAR AUTOMATICAMENTE
if (manicurista == null)
{
    manicurista = new Manicurista
    {
        Id = manicuristaId,

        NombreNegocio =
            string.IsNullOrWhiteSpace(nombre)
            ? "Mi Negocio"
            : nombre,

        CodigoPublico =
            Guid.NewGuid()
            .ToString("N")
            .Substring(0, 10),

        FechaInicioPrueba =
            DateTime.Now
    };

    _context.Manicuristas.Add(manicurista);

    await _context.SaveChangesAsync();
}

// 🔥 SI EL CODIGO ESTA VACIO -> GENERARLO
if (string.IsNullOrWhiteSpace(
    manicurista.CodigoPublico))
{
    manicurista.CodigoPublico =
        Guid.NewGuid()
        .ToString("N")
        .Substring(0, 10);

    await _context.SaveChangesAsync();
}

// 🔥 LINK FINAL REAL
registroUnico =
    $"https://tropinailspro.com/registro?codigo={manicurista.CodigoPublico}";

ViewBag.LinkRegistro = registroUnico;

            // 🔥 CLIENTAS (SIN TOCAR)
            var manicuristaActual =
    await _context.Manicuristas
    .FirstOrDefaultAsync(m =>
        m.Id == manicuristaId);

Console.WriteLine("======== CLIENTAS ========");
Console.WriteLine("UsuarioId: " + usuarioId);
Console.WriteLine("ManicuristaId Session: " + manicuristaId);
Console.WriteLine("==========================");

var clientas = await _context.Usuarios
    .Where(u =>
        u.Rol == "Clienta" &&
        u.ManicuristaId == manicuristaId)
    .OrderByDescending(u => u.FechaRegistro)
    .ToListAsync();

Console.WriteLine(
    "Cantidad Clientas: " +
    clientas.Count);

            ViewBag.Clientas = clientas;

            // ======================
            // INVENTARIO (SIN TOCAR)
            // ======================

            var productos =
                await _context.Productos.ToListAsync();

            ViewBag.TotalProductos =
                productos.Count;

            ViewBag.StockBajo =
                productos.Count(p => p.Cantidad <= 5);

            ViewBag.ProductosAgotados =
                productos.Count(p => p.Cantidad == 0);

            // ======================
            // 🔥 SUSCRIPCIÓN (FIX REAL)
            // ======================

            var usuario =
                await _context.Manicuristas
                .FirstOrDefaultAsync(u => u.Id == manicuristaId);

            if (usuario != null)
            {
                bool periodoPruebaActivo =
                    usuario.FechaInicioPrueba.AddDays(15) > DateTime.Now;

                if (periodoPruebaActivo)
                {
                    ViewBag.InventarioDesbloqueado = true;
                    ViewBag.PagosDesbloqueados = true;
                    ViewBag.EstadisticasDesbloqueadas = true;
                    ViewBag.MostrarSuscripcion = false;

                    HttpContext.Session.SetString("InventarioDesbloqueado", "true");
                    HttpContext.Session.SetString("PagosDesbloqueados", "true");
                    HttpContext.Session.SetString("EstadisticasDesbloqueadas", "true");
                }
                else
                {
                    var suscripcion =
                        await _context.Suscripciones
                        .Where(s => s.ManicuristaId == manicuristaId)
                        .OrderByDescending(s => s.FechaInicio)
                        .FirstOrDefaultAsync();

                    bool suscripcionValida =
                        suscripcion != null &&
                        suscripcion.Activa &&
                        !suscripcion.Cancelada &&
                        suscripcion.FechaVencimiento > DateTime.Now;

                    if (!suscripcionValida)
                    {
                        TempData["Info"] =
                            "Tu prueba gratis terminó. Debes suscribirte para continuar.";

                        return RedirectToAction("Vencida", "Suscripcion");
                    }

                    if (suscripcion.Plan != null &&
                        suscripcion.Plan.ToLower() == "premium")
                    {
                        ViewBag.InventarioDesbloqueado = true;
                        ViewBag.PagosDesbloqueados = true;
                    }
                    else
                    {
                        ViewBag.InventarioDesbloqueado = false;
                        ViewBag.PagosDesbloqueados = false;
                    }

                    ViewBag.EstadisticasDesbloqueadas = true;
                    ViewBag.MostrarSuscripcion = false;

                    HttpContext.Session.SetString(
                        "InventarioDesbloqueado",
                        ViewBag.InventarioDesbloqueado ? "true" : "false");

                    HttpContext.Session.SetString(
                        "PagosDesbloqueados",
                        ViewBag.PagosDesbloqueados ? "true" : "false");

                    HttpContext.Session.SetString(
                        "EstadisticasDesbloqueadas",
                        "true");
                }
            }

            // ======================
            // ESTADÍSTICAS (SIN TOCAR)
            // ======================

            var hoy = DateTime.Today;

            var ingresosHoy =
                await _context.Pagos
                .Where(p =>
                    p.UsuarioId == usuarioId.Value &&
                    p.FechaPago.Date == hoy)
                .SumAsync(p => (decimal?)p.Monto) ?? 0;

            var ingresosMes =
                await _context.Pagos
                .Where(p =>
                    p.UsuarioId == usuarioId.Value &&
                    p.FechaPago.Month == hoy.Month &&
                    p.FechaPago.Year == hoy.Year)
                .SumAsync(p => (decimal?)p.Monto) ?? 0;

            var ingresosAnio =
                await _context.Pagos
                .Where(p =>
                    p.UsuarioId == usuarioId.Value &&
                    p.FechaPago.Year == hoy.Year)
                .SumAsync(p => (decimal?)p.Monto) ?? 0;

            var clientasDeudoras =
                await _context.Pagos
                .Where(p =>
                    p.UsuarioId == usuarioId.Value &&
                    p.Pagado == false)
                .OrderByDescending(p => p.FechaPago)
                .ToListAsync();

            ViewBag.IngresosHoy = ingresosHoy;
            ViewBag.IngresosMes = ingresosMes;
            ViewBag.IngresosAnio = ingresosAnio;
            ViewBag.ClientasDeudoras = clientasDeudoras;

            // ======================
            // GRÁFICA (SIN TOCAR)
            // ======================

            var datosGrafica =
                await _context.Pagos
                .Where(p =>
                    p.UsuarioId == usuarioId.Value &&
                    p.FechaPago.Year == hoy.Year)
                .GroupBy(p => p.FechaPago.Month)
                .Select(g => new
                {
                    Mes = g.Key,
                    Total = g.Sum(x => x.Monto)
                })
                .ToListAsync();

            ViewBag.GraficaIngresos = datosGrafica;

            // ======================
            // CITAS (SIN TOCAR)
            // ======================

            // ======================
// CITAS DE HOY
// ======================

var citasHoyManicurista =
    await _context.Citas
    .Where(c =>
        c.Fecha.Date == hoy &&
        c.ManicuristaId == manicuristaId)
    .ToListAsync();

// 🔥 ENVIAR A LA VISTA
ViewBag.CitasHoy = citasHoyManicurista;

// 🔥 DEBUG
Console.WriteLine("======== CITAS ========");
Console.WriteLine("UsuarioId: " + usuarioId);
Console.WriteLine("ManicuristaId Session: " + manicuristaId);
Console.WriteLine("Cantidad citas hoy: " + citasHoyManicurista.Count);

foreach (var cita in citasHoyManicurista)
{
    Console.WriteLine(
        $"Cita ID: {cita.Id} | Fecha: {cita.Fecha} | ManicuristaId: {cita.ManicuristaId}");
}

Console.WriteLine("=======================");

return View();

}
        // TODO LO DEMÁS IGUAL

        public IActionResult PremiumInventario()
        {
            ViewBag.Titulo = "Inventario Premium";
            ViewBag.Mensaje =
                "Este módulo de Inventario solo está disponible en Premium.";
            return View();
        }

        public IActionResult PremiumPagos()
        {
            ViewBag.Titulo = "Pagos Premium";
            ViewBag.Mensaje =
                "El módulo Registro de Pagos está disponible solo en Premium.";
            return View();
        }

        public IActionResult PremiumRequired()
        {
            ViewBag.Titulo = "Módulo Premium";
            ViewBag.Mensaje =
                "Este módulo está disponible en plan Premium.";
            return View();
        }

        public IActionResult Salir()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        [HttpPost]
        public async Task<IActionResult> AgregarClienta(Usuario model)
        {
            var manicuristaId =
                HttpContext.Session.GetInt32("UsuarioId");

            if (manicuristaId == null)
                return Unauthorized();

            model.ManicuristaId = manicuristaId.Value;
            model.FechaRegistro = DateTime.Now;

            _context.Usuarios.Add(model);
            await _context.SaveChangesAsync();

            await _hub.Clients
                .Group($"manicurista-{manicuristaId.Value}")
                .SendAsync("NuevaClientaDashboard", new
                {
                    model.Nombre,
                    model.Email,
                    model.Telefono,
                    model.FechaRegistro
                });

            return Ok(new
            {
                mensaje = "Clienta agregada correctamente ✅"
            });
        }
    }
    
}