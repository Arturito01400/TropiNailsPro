using Microsoft.AspNetCore.Mvc;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System;

namespace TropiNailsPro.Controllers
{
    public class RedSocialController : Controller
    {
        private readonly AppDbContext _context;

        public RedSocialController(AppDbContext context)
        {
            _context = context;
        }

        // ======================================================
        // LISTAR PUBLICACIONES (FEED) + Historias + Catalogos
        // ======================================================
        public async Task<IActionResult> Index()
        {
            var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioIdStr)) return RedirectToAction("Login", "Auth");
            var usuarioId = int.Parse(usuarioIdStr);

            var publicaciones = await _context.Publicaciones
                .Include(p => p.Usuario)
                .Include(p => p.Comentarios)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.Likes.Count + p.Comentarios.Count)
                .ThenByDescending(p => p.Id)
                .ToListAsync();

            var historias = await _context.Historias
                .Include(h => h.Manicurista)
                .Where(h => h.FechaExpira > DateTime.Now)
                .OrderByDescending(h => h.FechaSubida)
                .Take(20)
                .ToListAsync();

            var catalogos = await _context.Catalogos
                .Include(c => c.Manicurista)
                .OrderByDescending(c => c.FechaSubida)
                .Take(20)
                .ToListAsync();

            ViewBag.UsuarioId = usuarioId;
            ViewBag.Historias = historias;
            ViewBag.Catalogos = catalogos;

            return View(publicaciones);
        }

        // ======================================================
        // CREAR PUBLICACIÓN
        // ======================================================
        [HttpPost]
        public async Task<IActionResult> CrearPublicacion(string texto, string mediaUrl, string tipoMedia = "imagen")
        {
            if (string.IsNullOrWhiteSpace(texto) && string.IsNullOrWhiteSpace(mediaUrl))
                return RedirectToAction("Index");

            var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioIdStr)) return RedirectToAction("Login", "Auth");
            var usuarioId = int.Parse(usuarioIdStr);

            var publicacion = new Publicacion
            {
                Texto = texto ?? "",
                MediaUrl = mediaUrl ?? "",
                TipoMedia = tipoMedia,
                UsuarioId = usuarioId
            };

            _context.Publicaciones.Add(publicacion);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // ======================================================
        // COMENTAR PUBLICACIÓN
        // ======================================================
        [HttpPost]
        public async Task<IActionResult> Comentar(int publicacionId, string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return RedirectToAction("Index");

            var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioIdStr)) return RedirectToAction("Login", "Auth");
            var usuarioId = int.Parse(usuarioIdStr);

            var comentario = new Comentario
            {
                PublicacionId = publicacionId,
                UsuarioId = usuarioId,
                Texto = texto
            };

            _context.Comentarios.Add(comentario);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // ======================================================
        // DAR O QUITAR LIKE
        // ======================================================
        [HttpPost]
        public async Task<IActionResult> ToggleLike(int publicacionId)
        {
            var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioIdStr)) return RedirectToAction("Login", "Auth");
            var usuarioId = int.Parse(usuarioIdStr);

            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.PublicacionId == publicacionId && l.UsuarioId == usuarioId);

            if (like != null)
                _context.Likes.Remove(like);
            else
                _context.Likes.Add(new Like { PublicacionId = publicacionId, UsuarioId = usuarioId });

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // ======================================================
        // SEGUIR / DEJAR DE SEGUIR
        // ======================================================
        [HttpPost]
        public async Task<IActionResult> ToggleSeguir(int seguidoId)
        {
            var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioIdStr)) return RedirectToAction("Login", "Auth");
            var usuarioId = int.Parse(usuarioIdStr);

            var seguimiento = await _context.Seguidores
                .FirstOrDefaultAsync(s => s.SeguidorId == usuarioId && s.SeguidoId == seguidoId);

            if (seguimiento != null)
                _context.Seguidores.Remove(seguimiento);
            else
                _context.Seguidores.Add(new Seguidor { SeguidorId = usuarioId, SeguidoId = seguidoId });

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // ======================================================
        // COMPARTIR PUBLICACIÓN
        // ======================================================
        [HttpPost]
        public async Task<IActionResult> CompartirPublicacion(int publicacionId)
        {
            var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(usuarioIdStr)) return RedirectToAction("Login", "Auth");
            var usuarioId = int.Parse(usuarioIdStr);

            var publicacionOriginal = await _context.Publicaciones
                .FirstOrDefaultAsync(p => p.Id == publicacionId);

            if (publicacionOriginal != null)
            {
                var copia = new Publicacion
                {
                    Texto = publicacionOriginal.Texto,
                    MediaUrl = publicacionOriginal.MediaUrl,
                    TipoMedia = publicacionOriginal.TipoMedia,
                    UsuarioId = usuarioId
                };

                _context.Publicaciones.Add(copia);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}