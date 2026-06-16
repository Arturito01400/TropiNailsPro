using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Linq;
using System;
using System.IO;

namespace TropiNailsPro.Controllers
{
    [Authorize]
    public class CatalogosController : Controller
    {
        private readonly AppDbContext _context;

        public CatalogosController(AppDbContext context)
        {
            _context = context;
        }

        // ===============================
        // VER CATALOGOS
        // ===============================
        public async Task<IActionResult> Index()
        {
            var usuarioId =
                HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
                return RedirectToAction(
                    "Login",
                    "Auth"
                );

            var catalogos = await _context.Catalogos
                .Where(c =>
                    c.ManicuristaId == usuarioId.Value)
                .OrderByDescending(c =>
                    c.FechaSubida)
                .ToListAsync();

            return View(catalogos);
        }

// ===============================
// PERFIL PUBLICO
// ===============================
[AllowAnonymous]
public async Task<IActionResult> Perfil(int id)
{
    try
    {
        // 🔥 USUARIO REAL
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == id);

        if (usuario == null)
        {
            return RedirectToAction(
                "Index",
                "Home"
            );
        }

        // 🔥 PUBLICACIONES
        var publicaciones = await _context.Publicaciones
            .AsNoTracking()
            .Include(p => p.Usuario)
            .Include(p => p.Likes)
            .Include(p => p.Comentarios)
            .Where(p =>
                p.UsuarioId == id)
            .OrderByDescending(p =>
                p.Fecha)
            .ToListAsync();

        // 🔥 NORMALIZAR MEDIA
        foreach (var post in publicaciones)
        {
            if (string.IsNullOrWhiteSpace(post.MediaUrl))
            {
                post.MediaUrl =
                    "/img/user-default.png";
            }
            else if (!post.MediaUrl.StartsWith("/"))
            {
                post.MediaUrl =
                    "/" + post.MediaUrl;
            }

            if (string.IsNullOrWhiteSpace(post.TipoMedia))
            {
                post.TipoMedia = "imagen";
            }
        }

        // 🔥 VIEWBAGS
        ViewBag.UsuarioLogueado =
            HttpContext.Session.GetInt32(
                "UsuarioId"
            );

        ViewBag.UsuarioPerfil =
            usuario.Id;

        ViewBag.CatalogoPersonal =
            publicaciones;

        // 🔥 RETORNAR VISTA
        return View(
            "~/Views/Catalogos/PerfilCatalogoPublicaciones.cshtml",
            usuario
        );
    }
    catch (Exception ex)
    {
        return Content(
            ex.InnerException?.Message
            ?? ex.Message
        );
    }
}

        // ===============================
        // CREATE VIEW
        // ===============================
        public IActionResult Create()
        {
            return View();
        }

        // ===============================
        // SUBIR CATALOGO
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Catalogo catalogo,
            IFormFile Archivo)
        {
            if (Archivo == null ||
                Archivo.Length == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Debe seleccionar un archivo."
                );

                return View(catalogo);
            }

            var usuarioId =
                HttpContext.Session.GetInt32(
                    "UsuarioId"
                );

            if (usuarioId == null)
                return RedirectToAction(
                    "Login",
                    "Auth"
                );

            if (Archivo.Length >
                20 * 1024 * 1024)
            {
                ModelState.AddModelError(
                    "",
                    "El archivo es demasiado grande."
                );

                return View(catalogo);
            }

            string[] allowedTypes =
            {
                "image/jpeg",
                "image/png",
                "image/gif",
                "video/mp4",
                "video/webm"
            };

            if (!allowedTypes.Contains(
                Archivo.ContentType))
            {
                ModelState.AddModelError(
                    "",
                    "Solo se permiten imágenes o videos."
                );

                return View(catalogo);
            }

            string uploadsFolder =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/catalogos"
                );

            if (!Directory.Exists(
                uploadsFolder))
            {
                Directory.CreateDirectory(
                    uploadsFolder
                );
            }

            string fileName =
                Guid.NewGuid().ToString()
                + Path.GetExtension(
                    Archivo.FileName
                );

            string filePath =
                Path.Combine(
                    uploadsFolder,
                    fileName
                );

            // 🔥 IMAGEN
            if (Archivo.ContentType
                .StartsWith("image"))
            {
                using var image =
                    await Image.LoadAsync(
                        Archivo.OpenReadStream()
                    );

                image.Mutate(x =>
                    x.Resize(new ResizeOptions
                    {
                        Size = new Size(1080, 1080),
                        Mode = ResizeMode.Max
                    })
                );

                await image.SaveAsync(filePath);

                catalogo.Tipo =
                    TipoArchivo.Imagen;
            }
            else
            {
                using var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create
                    );

                await Archivo.CopyToAsync(
                    stream
                );

                catalogo.Tipo =
                    TipoArchivo.Video;
            }

            catalogo.RutaArchivo =
                "/uploads/catalogos/" + fileName;

            // 🔥 CORRECCION REAL
            catalogo.ManicuristaId =
                usuarioId.Value;

            catalogo.FechaSubida =
                DateTime.Now;

            _context.Catalogos.Add(catalogo);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index)
            );
        }

        // ===============================
        // API
        // ===============================
        [HttpGet]
        public async Task<IActionResult>
            ObtenerCatalogos(
            int manicuristaId)
        {
            try
            {
                // 🔥 USA SESION REAL
                if (manicuristaId == 0)
                {
                    manicuristaId =
                        HttpContext.Session
                        .GetInt32(
                            "UsuarioId"
                        ) ?? 0;
                }

                if (manicuristaId == 0)
                {
                    return Json(
                        new List<object>()
                    );
                }

                // 🔥 PERFIL REAL
                var perfil =
                    await _context.UsuariosPerfil
                    .AsNoTracking()
                    .Include(p => p.Usuario)
                    .FirstOrDefaultAsync(p =>
                        p.UsuarioId ==
                        manicuristaId);

                if (perfil == null)
                {
                    return Json(
                        new List<object>()
                    );
                }

                // 🔥 PUBLICACIONES
                var publicaciones =
                    await _context.Publicaciones
                    .AsNoTracking()
                    .Include(p => p.Usuario)
                    .Include(p => p.Likes)
                    .Include(p => p.Comentarios)
                    .Where(p =>
                        p.UsuarioId ==
                        perfil.UsuarioId
                        &&
                        !string.IsNullOrWhiteSpace(
                            p.MediaUrl))
                    .OrderByDescending(p =>
                        p.Fecha)
                    .ToListAsync();

                var catalogos =
                    publicaciones
                    .Select(p => new
                    {
                        id = p.Id,

                        rutaArchivo =
                            string.IsNullOrWhiteSpace(
                                p.MediaUrl)
                            ? "/img/user-default.png"
                            : p.MediaUrl,

                        tipo =
                            !string.IsNullOrWhiteSpace(
                                p.TipoMedia)
                            &&
                            p.TipoMedia
                            .ToLower()
                            .Contains("video")
                                ? "video"
                                : "imagen",

                        fecha =
                            p.Fecha.ToString(
                                "dd/MM/yyyy HH:mm"
                            ),

                        likes =
                            p.Likes?.Count ?? 0,

                        comentarios =
                            p.Comentarios?.Count ?? 0
                    })
                    .ToList();

                return Json(catalogos);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = true,
                    mensaje = ex.Message,
                    inner =
                        ex.InnerException?.Message
                });
            }
        }

        // ===============================
        // EDITAR
        // ===============================
        public async Task<IActionResult>
            Edit(int id)
        {
            var catalogo =
                await _context.Catalogos
                .FindAsync(id);

            if (catalogo == null)
            {
                return NotFound();
            }

            return View(catalogo);
        }

        // ===============================
        // ELIMINAR
        // ===============================
        public async Task<IActionResult>
            Delete(int id)
        {
            var catalogo =
                await _context.Catalogos
                .FindAsync(id);

            if (catalogo == null)
            {
                return NotFound();
            }

            return View(catalogo);
        }
    }
}