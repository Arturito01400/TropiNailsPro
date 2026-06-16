using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace TropiNailsPro.Controllers
{
    [Authorize]
    public class HistoriasController : Controller
    {
        private readonly AppDbContext _context;

        public HistoriasController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================
        // HISTORIAS DE LA MANICURISTA
        // ==============================

        public async Task<IActionResult> Index()
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(claimId))
            {
                return Unauthorized(); // Cambiado para no usar /Cuenta
            }

            int manicuristaId = int.Parse(claimId);

            // eliminar historias expiradas automáticamente
            var expiradas = await _context.Historias
                .Where(h => h.FechaExpira < DateTime.Now)
                .ToListAsync();

            if (expiradas.Any())
            {
                _context.Historias.RemoveRange(expiradas);
                await _context.SaveChangesAsync();
            }

            var historias = await _context.Historias
                .Where(h => h.ManicuristaId == manicuristaId)
                .OrderByDescending(h => h.FechaSubida)
                .ToListAsync();

            return View(historias);
        }

        // ==============================
        // FORMULARIO SUBIR HISTORIA
        // ==============================

        public IActionResult Create()
        {
            return View();
        }

        // ==============================
        // SUBIR HISTORIA
        // ==============================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Historia historia, IFormFile Archivo)
        {
            if (Archivo == null || Archivo.Length == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar un archivo.");
                return View(historia);
            }

            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(claimId))
            {
                return Unauthorized(); // Cambiado para no usar /Cuenta
            }

            int manicuristaId = int.Parse(claimId);

            // ==============================
            // VALIDAR TAMAÑO
            // ==============================

            if (Archivo.Length > 20 * 1024 * 1024)
            {
                ModelState.AddModelError("", "El archivo es demasiado grande.");
                return View(historia);
            }

            // ==============================
            // VALIDAR TIPO
            // ==============================

            string[] allowedTypes =
            {
                "image/jpeg",
                "image/png",
                "image/gif",
                "video/mp4",
                "video/webm"
            };

            if (!allowedTypes.Contains(Archivo.ContentType))
            {
                ModelState.AddModelError("", "Solo se permiten imágenes o videos.");
                return View(historia);
            }

            // ==============================
            // CARPETA SEGURA
            // ==============================

            string uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot/uploads/historias"
            );

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // ==============================
            // NOMBRE ÚNICO
            // ==============================

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(Archivo.FileName);

            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await Archivo.CopyToAsync(stream);
            }

            // ==============================
            // GUARDAR HISTORIA
            // ==============================

            historia.RutaArchivo = "/uploads/historias/" + fileName;
            historia.ManicuristaId = manicuristaId;
            historia.FechaSubida = DateTime.Now;
            historia.FechaExpira = DateTime.Now.AddHours(24);

            _context.Historias.Add(historia);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ==============================
        // API PARA PERFIL
        // ==============================

        [HttpGet]
        public async Task<IActionResult> ObtenerHistorias(int manicuristaId)
        {
            var historias = await _context.Historias
                .Where(h =>
                    h.ManicuristaId == manicuristaId &&
                    h.FechaExpira > DateTime.Now
                )
                .OrderBy(h => h.FechaSubida)
                .Select(h => new
                {
                    h.Id,
                    h.RutaArchivo,
                    Tipo = h.Tipo.ToString()
                })
                .ToListAsync();

            return Json(historias);
        }
    }
}