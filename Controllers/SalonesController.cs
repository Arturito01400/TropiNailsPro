using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using System.Linq;

namespace TropiNailsPro.Controllers
{
    public class SalonesController : Controller
    {
        private readonly AppDbContext _context;

        public SalonesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string slug)
        {
            if (string.IsNullOrEmpty(slug))
                return NotFound();

            var manicurista = await _context.Manicuristas
                .FirstOrDefaultAsync(m => m.Slug == slug);

            if (manicurista == null)
                return NotFound();

            var citas = await _context.Citas
                .Where(c => c.ManicuristaId == manicurista.Id)
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            var modelos = await _context.ModelosUnas
                .Where(m => m.ManicuristaId == manicurista.Id)
                .ToListAsync();

            var feed = await _context.Publicaciones
                .Where(p => p.ManicuristaId == manicurista.Id)
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            ViewBag.Manicurista = manicurista;
            ViewBag.Citas = citas;
            ViewBag.Modelos = modelos;
            ViewBag.Feed = feed;

            return View();
        }
    }
}