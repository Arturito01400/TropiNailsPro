using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using TropiNailsPro.Services;

namespace TropiNailsPro.Controllers
{
    public class GastosPersonalesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TimeService _timeService;

        public GastosPersonalesController(
            AppDbContext context,
            TimeService timeService)
        {
            _context = context;
            _timeService = timeService;
        }


        // ===============================
        // LISTADO
        // ===============================
        public async Task<IActionResult> Index()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
                return RedirectToAction("Login", "Auth");


            var manicuristaId = await _context.Manicuristas
                .Where(m => m.UsuarioId == usuarioId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync();


            if (manicuristaId == 0)
            {
                TempData["Error"] = "No se encontró la información de la manicurista.";
                return RedirectToAction("Index", "Home");
            }


            var gastos = await _context.GastosPersonales
                .Where(g => g.ManicuristaId == manicuristaId)
                .OrderByDescending(g => g.FechaGasto)
                .ToListAsync();


            return View(gastos);
        }



        // ===============================
        // CREAR GET
        // ===============================
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }



        // ===============================
        // CREAR POST
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GastoPersonal gasto)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
                return RedirectToAction("Login", "Auth");


            var manicuristaId = await _context.Manicuristas
                .Where(m => m.UsuarioId == usuarioId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync();


            if (manicuristaId == 0)
            {
                TempData["Error"] = "No se encontró la información de la manicurista.";
                return RedirectToAction(nameof(Index));
            }


            if (!ModelState.IsValid)
                return View(gasto);



            gasto.ManicuristaId = manicuristaId;

            // Hora República Dominicana controlada por servidor
            gasto.FechaGasto = _timeService.ObtenerHoraLocal();


            _context.GastosPersonales.Add(gasto);

            await _context.SaveChangesAsync();


            TempData["Exito"] = "Gasto personal registrado correctamente.";


            return RedirectToAction(nameof(Index));
        }



        // ===============================
        // EDITAR GET
        // ===============================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {

            if (id == null)
                return NotFound();


            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
                return RedirectToAction("Login", "Auth");



            var manicuristaId = await _context.Manicuristas
                .Where(m => m.UsuarioId == usuarioId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync();



            var gasto = await _context.GastosPersonales
                .FirstOrDefaultAsync(g =>
                    g.Id == id &&
                    g.ManicuristaId == manicuristaId);



            if (gasto == null)
                return NotFound();



            return View(gasto);
        }




        // ===============================
        // EDITAR POST
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            GastoPersonal gasto)
        {

            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
                return RedirectToAction("Login", "Auth");



            var manicuristaId = await _context.Manicuristas
                .Where(m => m.UsuarioId == usuarioId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync();



            if (id != gasto.Id)
                return NotFound();



            var gastoExistente = await _context.GastosPersonales
                .FirstOrDefaultAsync(g =>
                    g.Id == id &&
                    g.ManicuristaId == manicuristaId);



            if (gastoExistente == null)
                return NotFound();



            if (!ModelState.IsValid)
                return View(gasto);



            gastoExistente.Descripcion = gasto.Descripcion;
            gastoExistente.Monto = gasto.Monto;
            gastoExistente.Categoria = gasto.Categoria;
            gastoExistente.Notas = gasto.Notas;
            


            await _context.SaveChangesAsync();



            TempData["Exito"] = "Gasto personal actualizado correctamente.";


            return RedirectToAction(nameof(Index));
        }





        // ===============================
        // DELETE GET
        // ===============================
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {

            if(id == null)
                return NotFound();



            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
                return RedirectToAction("Login", "Auth");



            var manicuristaId = await _context.Manicuristas
                .Where(m => m.UsuarioId == usuarioId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync();




            var gasto = await _context.GastosPersonales
                .FirstOrDefaultAsync(g =>
                    g.Id == id &&
                    g.ManicuristaId == manicuristaId);



            if(gasto == null)
                return NotFound();



            return View(gasto);
        }




        // ===============================
        // DELETE POST
        // ===============================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {

            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
                return RedirectToAction("Login", "Auth");



            var manicuristaId = await _context.Manicuristas
                .Where(m => m.UsuarioId == usuarioId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync();




            var gasto = await _context.GastosPersonales
                .FirstOrDefaultAsync(g =>
                    g.Id == id &&
                    g.ManicuristaId == manicuristaId);




            if(gasto != null)
            {
                _context.GastosPersonales.Remove(gasto);

                await _context.SaveChangesAsync();
            }



            TempData["Exito"] = "Gasto personal eliminado correctamente.";


            return RedirectToAction(nameof(Index));
        }

    }
}