using Microsoft.AspNetCore.Mvc;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using TropiNailsPro.Services;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;

namespace TropiNailsPro.Controllers
{
    [Authorize]
    public class SuscripcionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PayPalService _paypal;

        // Plan oficial mensual TropiNails Pro
// USD $10.20 ≈ RD$600
private const decimal PRECIO_MENSUAL = 10.20m;
        private const string PLAN_OFICIAL = "Premium";

        private bool ModoPrueba =>
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        public SuscripcionController(AppDbContext context, PayPalService paypal)
        {
            _context = context;
            _paypal = paypal;
        }

        [HttpGet]
[AllowAnonymous]
public async Task<IActionResult> Vencida()
{
    var email = User.FindFirstValue(ClaimTypes.Email);

    if (string.IsNullOrEmpty(email))
        return View(null);

    var manicurista = await _context.Manicuristas
        .Include(m => m.Usuario)
        .FirstOrDefaultAsync(m => m.Usuario.Email == email);

    return View(manicurista);
}

        [HttpGet]
        public async Task<IActionResult> Renovar()
        {
            var email = User.FindFirstValue(
    ClaimTypes.Email
);

            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login", "Auth");

            var manicurista = await _context.Manicuristas
                .Include(m => m.Usuario)
                .Include(m => m.Suscripciones)
                .FirstOrDefaultAsync(m => m.Usuario.Email == email);

            if (manicurista == null)
                return RedirectToAction("Login", "Auth");

            if (manicurista.Suscripciones == null || !manicurista.Suscripciones.Any())
            {
                var suscripcionPrueba = new Suscripcion
                {
                    ManicuristaId = manicurista.Id,
                    FechaInicio = DateTime.UtcNow,
                    FechaVencimiento = DateTime.UtcNow.AddDays(15),
                    Plan = "Prueba Gratis",
                    Activa = true,
                    Cancelada = false,
                    MetodoPago = "Trial",
                    EstadoPago = "TRIAL",
                    Monto = 0,
                    Moneda = "USD"
                };

                _context.Suscripciones.Add(suscripcionPrueba);
                await _context.SaveChangesAsync();
            }

            return View(manicurista);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenovarPago()
        {
Console.WriteLine(">>>>>>>>>> ENTRE A RENOVARPAGO <<<<<<<<<<");

            var email = User.FindFirstValue(
    ClaimTypes.Email
);

            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login", "Auth");

                Console.WriteLine("==============");
Console.WriteLine("RENOVAR PAGO");
Console.WriteLine("AUTH: " + User.Identity?.IsAuthenticated);
Console.WriteLine("NAME: " + User.Identity?.Name);
Console.WriteLine("EMAIL CLAIM: " +
    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value);
Console.WriteLine("==============");

            HttpContext.Session.SetString("email", email);

            Console.WriteLine("================================");
Console.WriteLine("GUARDANDO EMAIL EN SESSION");
Console.WriteLine(email);
Console.WriteLine("SESSION ID: " + HttpContext.Session.Id);
Console.WriteLine("================================");

           Response.Cookies.Append(
    "PagoEmail",
    email,
    new CookieOptions
    {
        Expires = DateTimeOffset.UtcNow.AddMinutes(30),
        HttpOnly = true,
        IsEssential = true,
        SameSite = SameSiteMode.Lax,
        Secure = false // localhost
    });

    var manicurista = await _context.Manicuristas
    .Include(m => m.Usuario)
    .FirstOrDefaultAsync(m => m.Usuario.Email == email);

if (manicurista == null)
{
    TempData["Error"] =
        "No se encontró la manicurista.";

    return RedirectToAction("Vencida");
}

            var descripcion = "Suscripción Premium TropiNails Pro";

            var urlPago = await _paypal.CrearOrden(
                PRECIO_MENSUAL,
                descripcion
            );

            Console.WriteLine("==============");
Console.WriteLine("URL PAYPAL");
Console.WriteLine(urlPago);
Console.WriteLine("==============");

            var uri = new Uri(urlPago);

var orderId =
    System.Web.HttpUtility
    .ParseQueryString(uri.Query)
    .Get("token");

    Console.WriteLine("==============");
Console.WriteLine("ORDER ID EXTRAIDO");
Console.WriteLine(orderId);
Console.WriteLine("==============");

   

    if (!string.IsNullOrEmpty(orderId))
{
    var pendiente =
        new Suscripcion
        {
            ManicuristaId = manicurista.Id,
            Plan = PLAN_OFICIAL,
            FechaInicio = DateTime.UtcNow,
            FechaVencimiento = DateTime.UtcNow,
            FechaRenovacion = DateTime.UtcNow,
            Activa = false,
            Cancelada = false,
            MetodoPago = "PayPal",
            PayPalOrderId = orderId,
            EstadoPago = "PENDIENTE",
            Monto = PRECIO_MENSUAL,
            Moneda = "USD"
        };

        Console.WriteLine("==============");
Console.WriteLine("GUARDANDO SUSCRIPCION PENDIENTE");
Console.WriteLine("ORDER ID: " + orderId);
Console.WriteLine("MANICURISTA ID: " + manicurista.Id);
Console.WriteLine("==============");

Console.WriteLine("==============");
Console.WriteLine("ANTES DEL ADD");
Console.WriteLine("MANICURISTA: " + manicurista.Id);
Console.WriteLine("ORDER: " + orderId);
Console.WriteLine("==============");

    _context.Suscripciones.Add(pendiente);

    await _context.SaveChangesAsync();

    Console.WriteLine("==============");
Console.WriteLine("SAVECHANGES EJECUTADO");
Console.WriteLine("ID GENERADO: " + pendiente.Id);
Console.WriteLine("==============");

    Console.WriteLine("==============");
Console.WriteLine("SUSCRIPCION PENDIENTE GUARDADA");
Console.WriteLine("ID BD: " + pendiente.Id);
Console.WriteLine("==============");

    Console.WriteLine("==============");
Console.WriteLine("SUSCRIPCION PENDIENTE GUARDADA");
Console.WriteLine("MANICURISTA: " + manicurista.Id);
Console.WriteLine("ORDERID: " + orderId);
Console.WriteLine("==============");
}

            if (string.IsNullOrEmpty(urlPago))
            {
                TempData["Error"] = "No se pudo iniciar el pago.";
                return RedirectToAction("Vencida");
            }

            return Redirect(urlPago);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Exito(string token)
        {
            Console.WriteLine("================================");
Console.WriteLine("SESSION ID EN EXITO");
Console.WriteLine(HttpContext.Session.Id);
Console.WriteLine("================================");
            
            Console.WriteLine("ENTRO A EXITO");
            Console.WriteLine("TOKEN: " + token);

            try
            {
                var orderId = token;

if (string.IsNullOrEmpty(orderId))
    orderId = Request.Query["token"];

if (string.IsNullOrEmpty(orderId))
    return RedirectToAction("Vencida");

var todas = await _context.Suscripciones
    .OrderByDescending(x => x.Id)
    .Take(10)
    .ToListAsync();

Console.WriteLine("==============");
Console.WriteLine("ULTIMAS SUSCRIPCIONES");

foreach (var s in todas)
{
    Console.WriteLine(
        $"ID={s.Id} ORDER={s.PayPalOrderId}"
    );
}

Console.WriteLine("==============");

var suscripcionExistente = await _context.Suscripciones
    .FirstOrDefaultAsync(x =>
        x.PayPalOrderId == orderId);

if (suscripcionExistente != null &&
    suscripcionExistente.EstadoPago == "COMPLETED")
{
    return RedirectToAction("Index", "Dashboard");
}

                var pagado = await _paypal.CapturarOrden(orderId);

                if (!pagado)
                    return RedirectToAction("Vencida");

                    var suscripcion = await _context.Suscripciones
    .FirstOrDefaultAsync(x =>
        x.PayPalOrderId == orderId);

if (suscripcion == null)
{
    Console.WriteLine("SUSCRIPCION NO ENCONTRADA");
    return RedirectToAction("Vencida");
}

var manicurista = await _context.Manicuristas
    .Include(m => m.Usuario)
    .Include(m => m.Suscripciones)
    .FirstOrDefaultAsync(m =>
        m.Id == suscripcion.ManicuristaId);

if (manicurista == null)
{
    Console.WriteLine("MANICURISTA NO ENCONTRADA");
    return RedirectToAction("Vencida");
}

Console.WriteLine("================================");
Console.WriteLine("MANICURISTA RECUPERADA POR ORDERID");
Console.WriteLine("ID: " + manicurista.Id);
Console.WriteLine("NEGOCIO: " + manicurista.NombreNegocio);
Console.WriteLine("================================");

                HttpContext.Session.SetInt32(
    "UsuarioId",
    manicurista.UsuarioId);

HttpContext.Session.SetString(
    "UsuarioRol",
    "Manicurista");

HttpContext.Session.SetString(
    "Rol",
    "Manicurista");

HttpContext.Session.SetInt32(
    "ManicuristaId",
    manicurista.Id);

                // Ya fue obtenida arriba usando el OrderID

                

                var baseDate =
                    suscripcion.FechaVencimiento > DateTime.UtcNow
                    ? suscripcion.FechaVencimiento
                    : DateTime.UtcNow;

                suscripcion.Plan = PLAN_OFICIAL;
suscripcion.FechaInicio = DateTime.UtcNow;
suscripcion.FechaVencimiento = baseDate.AddMonths(1);

                suscripcion.FechaRenovacion = DateTime.UtcNow;
                suscripcion.Activa = true;
                suscripcion.Cancelada = false;
                suscripcion.MetodoPago = "PayPal";
                suscripcion.PayPalOrderId = orderId;
                suscripcion.EstadoPago = "COMPLETED";
                suscripcion.Monto = PRECIO_MENSUAL;
                suscripcion.Moneda = "USD";

                Console.WriteLine("================================");
Console.WriteLine("GUARDANDO SUSCRIPCION...");
Console.WriteLine("PLAN: " + suscripcion.Plan);
Console.WriteLine("MONTO: " + suscripcion.Monto);
Console.WriteLine("ORDER ID: " + suscripcion.PayPalOrderId);
Console.WriteLine("VENCE: " + suscripcion.FechaVencimiento);
Console.WriteLine("================================");

                await _context.SaveChangesAsync();

                Console.WriteLine("================================");
Console.WriteLine("SUSCRIPCION GUARDADA");
Console.WriteLine("ID: " + suscripcion.Id);
Console.WriteLine("MANICURISTA ID: " + suscripcion.ManicuristaId);
Console.WriteLine("VENCE: " + suscripcion.FechaVencimiento);
Console.WriteLine("================================");

                HttpContext.Session.Remove("email");

                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR PAYPAL EXITO: " + ex.Message);
                return RedirectToAction("Vencida");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Cancelado()
        {
            TempData["Error"] = "Pago cancelado por el usuario.";
            return RedirectToAction("Vencida");
        }
    }
}