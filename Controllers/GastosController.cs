using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using TropiNailsPro.Services;

namespace TropiNailsPro.Controllers
{
    public class GastosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TimeService _timeService;

        public GastosController(
            AppDbContext context,
            TimeService timeService)
        {
            _context = context;
            _timeService = timeService;
        }


        // ===============================
        // LISTADO DE GASTOS
        // ===============================
        public async Task<IActionResult> Index()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }


            var manicuristaId = await _context.Manicuristas
                .Where(m => m.UsuarioId == usuarioId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync();


            if (manicuristaId == 0)
            {
                TempData["Error"] = "No se encontró la información de la manicurista.";
                return RedirectToAction("Index", "Home");
            }


            var gastos = await _context.Gastos
                .Where(g => g.ManicuristaId == manicuristaId)
                .OrderByDescending(g => g.FechaGasto)
                .ToListAsync();


            return View(gastos);
        }



        // ===============================
        // CREAR GASTO - GET
        // ===============================
        [HttpGet]
        public IActionResult Create()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }


            return View();
        }

        // ===============================
// CREAR GASTO - POST
// ===============================
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Gasto gasto)
{
    var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

    if (usuarioId == null)
    {
        return RedirectToAction(
            "Login",
            "Auth");
    }


    var manicuristaId = await _context.Manicuristas
        .Where(m => m.UsuarioId == usuarioId)
        .Select(m => m.Id)
        .FirstOrDefaultAsync();


    if (manicuristaId == 0)
    {
        TempData["Error"] = "No se encontró la información de la manicurista.";
        return RedirectToAction("Index");
    }


    if (!ModelState.IsValid)
    {
        return View(gasto);
    }


    gasto.ManicuristaId = manicuristaId;

    // Fecha controlada por el servidor
    gasto.FechaGasto = _timeService.ObtenerHoraLocal();


    _context.Gastos.Add(gasto);

    await _context.SaveChangesAsync();


    TempData["Exito"] = "Gasto registrado correctamente.";


    return RedirectToAction(nameof(Index));


}

// ===============================
// EDITAR GASTO - GET
// ===============================
[HttpGet]
public async Task<IActionResult> Edit(int? id)
{
    var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

    if (usuarioId == null)
    {
        return RedirectToAction(
            "Login",
            "Auth");
    }


    if (id == null)
    {
        return NotFound();
    }


    var manicuristaId = await _context.Manicuristas
        .Where(m => m.UsuarioId == usuarioId)
        .Select(m => m.Id)
        .FirstOrDefaultAsync();


    var gasto = await _context.Gastos
        .FirstOrDefaultAsync(g =>
            g.Id == id &&
            g.ManicuristaId == manicuristaId);


    if (gasto == null)
    {
        return NotFound();
    }


    return View(gasto);
}



// ===============================
// EDITAR GASTO - POST
// ===============================
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(
    int id,
    Gasto gasto)
{
    var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

    if (usuarioId == null)
    {
        return RedirectToAction(
            "Login",
            "Auth");
    }


    var manicuristaId = await _context.Manicuristas
        .Where(m => m.UsuarioId == usuarioId)
        .Select(m => m.Id)
        .FirstOrDefaultAsync();


    if (id != gasto.Id)
    {
        return NotFound();
    }


    var gastoExistente = await _context.Gastos
        .FirstOrDefaultAsync(g =>
            g.Id == id &&
            g.ManicuristaId == manicuristaId);


    if (gastoExistente == null)
    {
        return NotFound();
    }


    if (!ModelState.IsValid)
    {
        return View(gasto);
    }


    gastoExistente.Descripcion = gasto.Descripcion;
    gastoExistente.Monto = gasto.Monto;
    gastoExistente.Categoria = gasto.Categoria;
    gastoExistente.Notas = gasto.Notas;
    gastoExistente.FechaGasto = gasto.FechaGasto;


    await _context.SaveChangesAsync();


    TempData["Exito"] = "Gasto actualizado correctamente.";


    return RedirectToAction(nameof(Index));
}

// ===============================
// ELIMINAR GASTO - GET
// ===============================
[HttpGet]
public async Task<IActionResult> Delete(int? id)
{
    var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

    if (usuarioId == null)
    {
        return RedirectToAction(
            "Login",
            "Auth");
    }


    if (id == null)
    {
        return NotFound();
    }


    var manicuristaId = await _context.Manicuristas
        .Where(m => m.UsuarioId == usuarioId)
        .Select(m => m.Id)
        .FirstOrDefaultAsync();


    var gasto = await _context.Gastos
        .FirstOrDefaultAsync(g =>
            g.Id == id &&
            g.ManicuristaId == manicuristaId);


    if (gasto == null)
    {
        return NotFound();
    }


    return View(gasto);
}



// ===============================
// ELIMINAR GASTO - POST
// ===============================
[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

    if (usuarioId == null)
    {
        return RedirectToAction(
            "Login",
            "Auth");
    }


    var manicuristaId = await _context.Manicuristas
        .Where(m => m.UsuarioId == usuarioId)
        .Select(m => m.Id)
        .FirstOrDefaultAsync();


    var gasto = await _context.Gastos
        .FirstOrDefaultAsync(g =>
            g.Id == id &&
            g.ManicuristaId == manicuristaId);


    if (gasto == null)
    {
        return NotFound();
    }


    _context.Gastos.Remove(gasto);

    await _context.SaveChangesAsync();


    TempData["Exito"] = "Gasto eliminado correctamente.";


    return RedirectToAction(nameof(Index));
}
    
}


}