using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TropiNailsPro.Controllers
{
    public class FollowRequest
    {
        public int SeguidoId { get; set; }
    }

    public class SeguidoresController : Controller
    {
        private readonly AppDbContext _context;

        public SeguidoresController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================
        // VISTA SIGUIENDO
        // ============================================
        public async Task<IActionResult> VistaSiguiendo()
{
    var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

    if (usuarioId == null)
        return RedirectToAction("Login", "Auth");

    var siguiendo = await _context.Seguidores
        .Where(s => s.SeguidorId == usuarioId.Value)
        .Select(s => s.SeguidoUsuario)
        .GroupBy(u => u.Id)
        .Select(g => g.First())
        .ToListAsync();

    return View("Siguiendo", siguiendo);
}

        // ============================================
        // RESUMEN PERFIL
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Resumen(int usuarioId)
        {
            var seguidores = await _context.Seguidores
                .Where(s => s.SeguidoId == usuarioId)
                .Select(s => s.SeguidorId)
                .Distinct()
                .CountAsync();

            var siguiendo = await _context.Seguidores
                .Where(s => s.SeguidorId == usuarioId)
                .Select(s => s.SeguidoId)
                .Distinct()
                .CountAsync();

            return Json(new { seguidores, siguiendo });
        }

        // ============================================
        // TOGGLE SEGUIR
        // ============================================
        [HttpPost]
        [Route("Seguidores/ToggleSeguir")]
        public async Task<IActionResult> ToggleSeguir([FromQuery] int seguidoId)
        {
            try
            {
                var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

                if (usuarioId == null)
                    return Json(new { ok = false, error = "sin sesion" });

                if (usuarioId.Value == seguidoId)
                    return Json(new { ok = false, error = "no puedes seguirte" });

                var relacion = await _context.Seguidores
                    .FirstOrDefaultAsync(x =>
                        x.SeguidorId == usuarioId.Value &&
                        x.SeguidoId == seguidoId);

                bool siguiendo;

                if (relacion == null)
                {
                    _context.Seguidores.Add(new Seguidor
                    {
                        SeguidorId = usuarioId.Value,
                        SeguidoId = seguidoId,
                        Fecha = DateTime.Now
                    });

                    siguiendo = true;
                }
                else
                {
                    _context.Seguidores.Remove(relacion);
                    siguiendo = false;
                }

                await _context.SaveChangesAsync();

                var total = await _context.Seguidores
                    .Where(x => x.SeguidoId == seguidoId)
                    .Select(x => x.SeguidorId)
                    .Distinct()
                    .CountAsync();

                return Json(new { ok = true, siguiendo, seguidores = total });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        // ============================================
        // SEGUIR
        // ============================================
        [HttpPost]
        public async Task<IActionResult> Seguir(int seguidoId)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
                return Unauthorized();

            if (usuarioId.Value == seguidoId)
                return BadRequest("No puedes seguirte a ti mismo.");

            var existe = await _context.Seguidores
                .AnyAsync(s =>
                    s.SeguidorId == usuarioId.Value &&
                    s.SeguidoId == seguidoId);

            if (!existe)
            {
                _context.Seguidores.Add(new Seguidor
                {
                    SeguidorId = usuarioId.Value,
                    SeguidoId = seguidoId,
                    Fecha = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }

            var total = await _context.Seguidores
                .Where(s => s.SeguidoId == seguidoId)
                .Select(s => s.SeguidorId)
                .Distinct()
                .CountAsync();

            return Json(new { siguiendo = true, seguidores = total });
        }

        // ============================================
        // DEJAR DE SEGUIR
        // ============================================
        [HttpPost]
        public async Task<IActionResult> DejarSeguir(int seguidoId)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
                return Unauthorized();

            var relaciones = await _context.Seguidores
                .Where(s =>
                    s.SeguidorId == usuarioId.Value &&
                    s.SeguidoId == seguidoId)
                .ToListAsync();

            if (relaciones.Any())
            {
                _context.Seguidores.RemoveRange(relaciones);
                await _context.SaveChangesAsync();
            }

            var total = await _context.Seguidores
                .Where(s => s.SeguidoId == seguidoId)
                .Select(s => s.SeguidorId)
                .Distinct()
                .CountAsync();

            return Json(new { siguiendo = false, seguidores = total });
        }

        // ============================================
        // ESTA SIGUIENDO
        // ============================================
        [HttpGet]
        public async Task<IActionResult> EstaSiguiendo(int seguidoId)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
                return Json(false);

            var sigue = await _context.Seguidores
                .AnyAsync(s =>
                    s.SeguidorId == usuarioId.Value &&
                    s.SeguidoId == seguidoId);

            return Json(sigue);
        }

        // ============================================
        // CONTAR SEGUIDORES
        // ============================================
        [HttpGet]
        public async Task<IActionResult> ContarSeguidores(int usuarioId)
        {
            var total = await _context.Seguidores
                .Where(s => s.SeguidoId == usuarioId)
                .Select(s => s.SeguidorId)
                .Distinct()
                .CountAsync();

            return Json(total);
        }

        // ============================================
        // LISTA VIEW
        // ============================================
        [HttpGet("Seguidores/Lista/{id}")]
        public async Task<IActionResult> Lista(int id)
        {
            var seguidores = await _context.Seguidores
                .Include(s => s.SeguidorUsuario)
                .Where(s => s.SeguidoId == id)
                .GroupBy(s => s.SeguidorUsuario.Id)
                .Select(g => g.First().SeguidorUsuario)
                .ToListAsync();

            return View(seguidores);
        }

        // ============================================
        // JSON SEGUIDORES
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Seguidores(int usuarioId)
        {
            var seguidores = await _context.Seguidores
                .Include(s => s.SeguidorUsuario)
                .Where(s => s.SeguidoId == usuarioId)
                .GroupBy(s => s.SeguidorUsuario.Id)
                .Select(g => new
                {
                    id = g.Key,
                    nombre = g.First().SeguidorUsuario.Nombre,
                    foto = string.IsNullOrEmpty(g.First().SeguidorUsuario.FotoPerfil)
                        ? "/img/user-default.png"
                        : g.First().SeguidorUsuario.FotoPerfil
                })
                .ToListAsync();

            return Json(seguidores);
        }

        // ============================================
        // SIGUIENDO
        // ============================================
        [HttpGet("Seguidores/Siguiendo/{usuarioId}")]
        public async Task<IActionResult> Siguiendo(int usuarioId)
        {
            var siguiendo = await _context.Seguidores
                .Include(s => s.SeguidoUsuario)
                .Where(s => s.SeguidorId == usuarioId)
                .GroupBy(s => s.SeguidoUsuario.Id)
                .Select(g => new
                {
                    id = g.Key,
                    nombre = g.First().SeguidoUsuario.Nombre,
                    foto = string.IsNullOrEmpty(g.First().SeguidoUsuario.FotoPerfil)
                        ? "/img/user-default.png"
                        : g.First().SeguidoUsuario.FotoPerfil
                })
                .ToListAsync();

            return Json(siguiendo);
        }

        // ============================================
        // SIGUIENDO (SESION)
        // ============================================
       [HttpGet]
public async Task<IActionResult> ObtenerSiguiendo()
{
    var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

    if (usuarioId == null)
        return Json(new object[] { });

    var siguiendo = await _context.Seguidores
        .Where(s => s.SeguidorId == usuarioId.Value)
        .Include(s => s.SeguidoUsuario) // 🔥 CLAVE
        .Select(s => new
        {
            id = s.SeguidoUsuario.Id,
            nombre = s.SeguidoUsuario.Nombre,
            foto = s.SeguidoUsuario.FotoPerfil ?? "/img/user-default.png"
        })
        .ToListAsync();

    return Json(siguiendo);


}

        // ============================================
        // SUGERENCIAS
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Sugerencias()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
                return Json(new object[] { });

            var sugerencias = await _context.Usuarios
                .Where(u =>
                    u.Id != usuarioId &&
                    !_context.Seguidores.Any(s =>
                        s.SeguidorId == usuarioId &&
                        s.SeguidoId == u.Id))
                .OrderByDescending(u => u.FechaRegistro)
                .Take(5)
                .Select(u => new
                {
                    id = u.Id,
                    nombre = u.Nombre,
                    foto = string.IsNullOrEmpty(u.FotoPerfil)
                        ? "/img/user-default.png"
                        : u.FotoPerfil
                })
                .ToListAsync();

            return Json(sugerencias);
        }
    }
}