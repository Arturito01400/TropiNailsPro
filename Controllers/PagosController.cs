using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using TropiNailsPro.Services;

namespace TropiNailsPro.Controllers
{
    public class PagosController : Controller
    {
        private readonly AppDbContext _context;

        private readonly TimeService _timeService;

public PagosController(AppDbContext context, TimeService timeService)
{
    _context = context;
    _timeService = timeService;
}

        // LISTA DE PAGOS
        public async Task<IActionResult> Lista()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            var pagos = await _context.Pagos
    .Where(p => p.UsuarioId == usuarioId)
    .OrderByDescending(p => p.FechaPago)
    .ToListAsync();

var hoy = _timeService.ObtenerHoraLocal().Date;

var sumatorias = new
{
    Diario = pagos
        .Where(p => p.FechaPago.Date == hoy)
        .Sum(p => p.Monto),

    Mensual = pagos
        .Where(p =>
            p.FechaPago.Month == hoy.Month &&
            p.FechaPago.Year == hoy.Year)
        .Sum(p => p.Monto),

    Anual = pagos
        .Where(p =>
            p.FechaPago.Year == hoy.Year)
        .Sum(p => p.Monto)
};

            ViewBag.Sumatorias = sumatorias;

            // Clientes con deuda
            ViewBag.Deudoras = pagos.Where(p => !p.Pagado).ToList();

            return View(pagos);
        }


        // ==============================
        // CREAR PAGO
        // ==============================

        // Mostrar formulario
        public IActionResult Crear()
        {
            return View();
        }

        // Guardar pago
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Pago pago)
        {
            if (!ModelState.IsValid)
            {
                return View(pago);
            }

            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

           pago.FechaPago = _timeService.ObtenerHoraLocal();

            if (usuarioId.HasValue)
            {
                pago.UsuarioId = usuarioId.Value;
            }

            _context.Pagos.Add(pago);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Lista));
        }


        // ==============================
        // COBRAR DEUDA
        // ==============================

        public async Task<IActionResult> CobrarDeuda(int id)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            var deuda = await _context.Pagos
                .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == usuarioId);

            if (deuda != null)
            {
                deuda.Pagado = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Lista));
        }


        // ==============================
        // EDITAR PAGO
        // ==============================

        public async Task<IActionResult> Editar(int id)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            var pago = await _context.Pagos
                .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == usuarioId);

            if (pago == null)
                return NotFound();

            return View(pago);
        }

        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Editar(int id, Pago pago)
{
    if (id != pago.Id)
        return NotFound();

    if (!ModelState.IsValid)
        return View(pago);

    var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

    if (!usuarioId.HasValue)
        return RedirectToAction("Login", "Auth");

    try
    {
        var pagoDB = await _context.Pagos
            .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == usuarioId.Value);

        if (pagoDB == null)
            return NotFound();

        // 👇 SOLO CAMPOS EDITABLES
        pagoDB.ClienteNombre = pago.ClienteNombre;
        pagoDB.ModeloUnas = pago.ModeloUnas;
        pagoDB.Monto = pago.Monto;
        pagoDB.TransaccionId = pago.TransaccionId;

        // ❌ NO TOCAR FECHA PARA EVITAR PROBLEMAS DE HORA
        // pagoDB.FechaPago = _timeService.ObtenerHoraLocal();

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Lista));
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return View(pago);
    }
}


        // ==============================
        // ELIMINAR
        // ==============================

        public async Task<IActionResult> Eliminar(int id)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            var pago = await _context.Pagos
                .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == usuarioId);

            if (pago != null)
            {
                _context.Pagos.Remove(pago);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Lista));
        }
    }
}