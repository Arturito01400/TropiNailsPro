using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using System.Security.Claims;

namespace TropiNailsPro.Controllers
{
    public class ComentariosController : Controller
    {
        private readonly AppDbContext _context;

        public ComentariosController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 🔥 VER PUBLICACIÓN (VERSIÓN 100% ESTABLE)
        // ==========================================
        public async Task<IActionResult> Ver(int id)
        {
            // 🔥 1. Traer publicación limpia (SIN TRACKING)
            var publicacion = await _context.Publicaciones
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new Publicacion
                {
                    Id = p.Id,
                    Texto = p.Texto,
                    MediaUrl = p.MediaUrl,
                    TipoMedia = p.TipoMedia,
                    Fecha = p.Fecha,
                    UsuarioId = p.UsuarioId,

                    Usuario = new Usuario
                    {
                        Id = p.Usuario.Id,
                        Nombre = p.Usuario.Nombre,
                        FotoPerfil = p.Usuario.FotoPerfil
                    }
                })
                .FirstOrDefaultAsync();

            if (publicacion == null)
                return NotFound();

            // 🔥 2. Traer comentarios limpios separados (SIN mezcla EF)
            var comentarios = await _context.Comentarios
                .AsNoTracking()
                .Where(c => c.PublicacionId == id)
                .Select(c => new Comentario
                {
                    Id = c.Id,
                    Texto = c.Texto,
                    Fecha = c.Fecha,
                    PublicacionId = c.PublicacionId,
                    UsuarioId = c.UsuarioId,
                    ComentarioPadreId = c.ComentarioPadreId,

                    Usuario = new Usuario
                    {
                        Id = c.Usuario.Id,
                        Nombre = c.Usuario.Nombre,
                        FotoPerfil = c.Usuario.FotoPerfil
                    }
                })
                .ToListAsync();

            // 🔥 3. Separar respuestas SIN EF tracking
            var respuestas = comentarios
                .Where(x => x.ComentarioPadreId != null)
                .ToList();

            foreach (var comentario in comentarios.Where(x => x.ComentarioPadreId == null))
            {
                comentario.Respuestas = respuestas
                    .Where(r => r.ComentarioPadreId == comentario.Id)
                    .ToList();
            }

            // 🔥 4. Asignación final (YA LIMPIA)
            publicacion.Comentarios = comentarios;

            return View(publicacion);
        }

        // ==========================================
        // 💬 CREAR COMENTARIO
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> Comentar(int publicacionId, string texto, int? comentarioPadreId)
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(texto))
                return RedirectToAction("Ver", new { id = publicacionId });

            var comentario = new Comentario
            {
                PublicacionId = publicacionId,
                UsuarioId = int.Parse(usuarioId),
                Texto = texto,
                Fecha = DateTime.Now,
                ComentarioPadreId = comentarioPadreId
            };

            _context.Comentarios.Add(comentario);
            await _context.SaveChangesAsync();

            return RedirectToAction("Ver", new { id = publicacionId });
        }

        // ==========================================
        // 🗑 ELIMINAR COMENTARIO
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> EliminarComentario(int id)
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var comentario = await _context.Comentarios
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comentario == null)
                return NotFound();

            if (comentario.UsuarioId.ToString() != usuarioId)
                return Forbid();

            _context.Comentarios.Remove(comentario);
            await _context.SaveChangesAsync();

            return RedirectToAction("Ver", new { id = comentario.PublicacionId });
        }
    }
}