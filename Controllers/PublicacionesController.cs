using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Xabe.FFmpeg;
using TropiNailsPro.Services;

namespace TropiNailsPro.Controllers
{
    public class PublicacionesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TimeService _timeService;

        public PublicacionesController(
    AppDbContext context,
    TimeService timeService)
{
    _context = context;
    _timeService = timeService;
}

        public IActionResult Crear()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
                return RedirectToAction("Login", "Auth");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
            Publicacion model,
            IFormFile mediaArchivo)
        {
            var usuarioSessionId =
                HttpContext.Session.GetInt32("UsuarioId");

            var manicuristaSessionId =
                HttpContext.Session.GetInt32("ManicuristaId");

            if (usuarioSessionId == null
                || manicuristaSessionId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }

            try
            {
                if (string.IsNullOrWhiteSpace(model.Texto)
                    && mediaArchivo == null
                    && string.IsNullOrWhiteSpace(model.ImagenUrl))
                {
                    ModelState.AddModelError(
                        "",
                        "Debes escribir algo o subir un archivo."
                    );

                    return View(model);
                }

                var usuarioReal =
                    await _context.Usuarios
                    .FirstOrDefaultAsync(u =>
                        u.Id == usuarioSessionId.Value);

                if (usuarioReal == null)
                    return Content("Usuario no encontrado");

                string tipoMedia = "imagen";
                string mediaUrl = "";
                int? duracionVideo = null;

                if (mediaArchivo != null
                    && mediaArchivo.Length > 0)
                {
                    var extension =
                        Path.GetExtension(
                            mediaArchivo.FileName
                        ).ToLower();

                    string[] videos =
                    {
                        ".mp4",
                        ".mov",
                        ".webm"
                    };

                    var carpeta =
                        Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot/uploads"
                        );

                    if (!Directory.Exists(carpeta))
                    {
                        Directory.CreateDirectory(carpeta);
                    }

                    var nombreArchivo =
                        Guid.NewGuid().ToString()
                        + extension;

                    var rutaCompleta =
                        Path.Combine(
                            carpeta,
                            nombreArchivo
                        );

                    using (var stream =
                        new FileStream(
                            rutaCompleta,
                            FileMode.Create
                        ))
                    {
                        await mediaArchivo.CopyToAsync(stream);
                    }

                    mediaUrl =
                        "/uploads/" + nombreArchivo;

                    tipoMedia =
                        videos.Contains(extension)
                        ? "video"
                        : "imagen";

                    if (tipoMedia == "video")
                    {
                        var mediaInfo =
                            await FFmpeg.GetMediaInfo(
                                rutaCompleta
                            );

                        duracionVideo =
                            (int)mediaInfo
                            .Duration
                            .TotalSeconds;
                    }
                }

                model.MediaUrl =
                    mediaUrl ?? "";

                model.TipoMedia =
                    tipoMedia;

                model.DuracionVideo =
                    duracionVideo;

                model.UsuarioId =
                    usuarioReal.Id;

                model.Fecha = _timeService.ObtenerHoraLocal();

                model.ManicuristaId =
                    manicuristaSessionId.Value;

                _context.Publicaciones.Add(model);

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    "Index",
                    "Feed"
                );
            }
            catch (Exception ex)
            {
                return Content(
                    "Error: " + ex.Message
                );
            }
        }

        [HttpGet]
        public async Task<IActionResult> Ver(int id)
        {
            var publicacion =
                await _context.Publicaciones
                .Include(p => p.Comentarios)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p =>
                    p.Id == id);

            if (publicacion == null)
                return NotFound();

            foreach (var comentario
                in publicacion.Comentarios)
            {
                comentario.Usuario =
                    await _context.Usuarios
                    .FirstOrDefaultAsync(u =>
                        u.Id == comentario.UsuarioId);
            }

            return View(
                "Ver",
                publicacion
            );
        }

        [HttpPost]
        public async Task<IActionResult> Comentar(
            int publicacionId,
            string texto,
            int? comentarioPadreId)
        {
            var usuarioId =
                HttpContext.Session.GetInt32(
                    "UsuarioId"
                );

            if (usuarioId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }

            if (string.IsNullOrWhiteSpace(texto))
            {
                return RedirectToAction(
                    "Ver",
                    new { id = publicacionId }
                );
            }

            var comentario = new Comentario
            {
                Texto = texto,
                Fecha = _timeService.ObtenerHoraLocal(),
                UsuarioId = usuarioId.Value,
                PublicacionId = publicacionId,
                ComentarioPadreId = comentarioPadreId
            };

            _context.Comentarios.Add(comentario);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Ver",
                new { id = publicacionId }
            );
        }

        [HttpGet]
public async Task<IActionResult> ObtenerFeed(
    int pagina = 1,
    string modo = "tendencias",
    string tag = null)
{
    int tam = 10;

    var usuarioActual =
        HttpContext.Session.GetInt32(
            "UsuarioId"
        );

    var query =
        _context.Publicaciones
        .Include(p => p.Likes)
        .Include(p => p.Comentarios)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(tag))
    {
        query = query.Where(p =>
            p.Texto.Contains("#" + tag));
    }

    switch (modo?.ToLower())
    {
        case "recientes":

            query = query
                .OrderByDescending(p => p.Fecha);

            break;

        case "tendencias":

            query = query
                .OrderByDescending(p =>
                    (p.Likes.Count * 3)
                    +
                    (p.Comentarios.Count * 5));

            break;

        default:

            query = query
                .OrderByDescending(p => p.Fecha);

            break;
    }

    var publicaciones =
        await query
        .Skip((pagina - 1) * tam)
        .Take(tam)
        .ToListAsync();

   var data =
    publicaciones.Select(p =>
    {
        var usuario =
            _context.Usuarios
            .FirstOrDefault(x =>
                x.Id == p.UsuarioId);

        return new
        {
            id = p.Id,

            usuarioId =
                usuario != null
                ? usuario.Id
                : 0,

            usuario =
                usuario != null
                ? usuario.Nombre
                : "Usuario",

            foto =
                usuario != null
                ? NormalizePath(usuario.FotoPerfilUrl)
                : "/img/user-default.png",

            mediaUrl =
                NormalizePath(p.MediaUrl),

            tipoMedia =
                string.IsNullOrWhiteSpace(p.TipoMedia)
                ? "imagen"
                : p.TipoMedia.ToLower(),

            fecha = p.Fecha,

            likes =
                p.Likes?.Count ?? 0,

            comentarios =
                p.Comentarios?.Count ?? 0,

            esMia =
                usuarioActual != null
                && p.UsuarioId == usuarioActual.Value,

            siguiendo =
                usuarioActual != null
                && _context.Seguidores.Any(s =>
                    s.SeguidorId == usuarioActual.Value
                    && s.SeguidoId == p.UsuarioId
                )
        };
    })
    .ToList();

return Json(data);

}

        [HttpPost]
        public async Task<IActionResult> Like(int id)
        {
            var usuarioId =
                HttpContext.Session.GetInt32(
                    "UsuarioId"
                );

            if (usuarioId == null)
                return Unauthorized();

            var usuario =
                await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.Id == usuarioId.Value);

            if (usuario == null)
                return BadRequest();

            var existeLike =
                await _context.Likes
                .FirstOrDefaultAsync(l =>
                    l.PublicacionId == id
                    &&
                    l.UsuarioId == usuario.Id);

            if (existeLike != null)
            {
                _context.Likes.Remove(existeLike);
            }
            else
            {
                _context.Likes.Add(new Like
                {
                    PublicacionId = id,
                    UsuarioId = usuario.Id,
                    Fecha = _timeService.ObtenerHoraLocal()
                });
            }

            await _context.SaveChangesAsync();

            var totalLikes =
                await _context.Likes
                .CountAsync(l =>
                    l.PublicacionId == id);

            return Json(new
            {
                likes = totalLikes
            });
        }

        // ==========================================
        // STATS PUBLICACION
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Stats(int id)
        {
            var publicacion =
                await _context.Publicaciones
                .Include(p => p.Likes)
                .Include(p => p.Comentarios)
                .FirstOrDefaultAsync(p =>
                    p.Id == id);

            if (publicacion == null)
            {
                return Json(new
                {
                    likes = 0,
                    comentarios = 0
                });
            }

            return Json(new
            {
                likes =
                    publicacion.Likes?.Count ?? 0,

                comentarios =
                    publicacion.Comentarios?.Count ?? 0
            });
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var usuarioId =
                HttpContext.Session.GetInt32(
                    "UsuarioId"
                );

            if (usuarioId == null)
                return Unauthorized();

            var publicacion =
                await _context.Publicaciones
                .FirstOrDefaultAsync(p =>
                    p.Id == id);

            if (publicacion == null)
                return NotFound();

            var usuario =
                await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.Id == usuarioId);

            if (usuario == null)
                return Unauthorized();

            if (publicacion.UsuarioId != usuario.Id)
                return Forbid();

            _context.Publicaciones.Remove(publicacion);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true
            });
        }

        // ==========================================
// OBTENER PUBLICACIONES POR USUARIO
// ==========================================
[HttpGet]
public async Task<IActionResult> ObtenerPorUsuario(int usuarioId)
{
    try
    {
        var publicaciones =
            await _context.Publicaciones
            .Include(p => p.Likes)
            .Include(p => p.Comentarios)
            .Where(p => p.UsuarioId == usuarioId)
            .OrderByDescending(p => p.Fecha)
            .ToListAsync();

        var data =
            publicaciones.Select(p =>
            {
                var usuario =
                    _context.Usuarios
                    .FirstOrDefault(u =>
                        u.Id == p.UsuarioId);

                return new
                {
                    id = p.Id,

                    usuarioId = p.UsuarioId,

                    usuario =
                        usuario != null
                        ? usuario.Nombre
                        : "Usuario",

                    foto =
                        usuario != null
                        ? NormalizePath(
                            usuario.FotoPerfilUrl
                        )
                        : "/img/user-default.png",

                    mediaUrl =
                        NormalizePath(
                            p.MediaUrl
                        ),

                    tipoMedia =
                        string.IsNullOrWhiteSpace(
                            p.TipoMedia)
                        ? "imagen"
                        : p.TipoMedia.ToLower(),

                    fecha = p.Fecha,

                    likes =
                        p.Likes?.Count ?? 0,

                    comentarios =
                        p.Comentarios?.Count ?? 0
                };
            }).ToList();

        return Json(data);
    }
    catch (Exception ex)
    {
        return BadRequest(new
        {
            error = true,
            mensaje = ex.Message
        });
    }
}

        private string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "/img/user-default.png";

            path =
                path.Replace("\\", "/").Trim();

            if (path.ToLower().StartsWith("http"))
                return path;

            if (!path.StartsWith("/"))
                path = "/" + path;

            return path;
        }
    }
}