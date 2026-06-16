using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using TropiNailsPro.Hubs;
using TropiNailsPro.Services;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace TropiNailsPro.Controllers
{
    [Authorize]
    public class PerfilController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<OnlineHub> _hub;
        private readonly IHubContext<AvatarHub> _avatarHub;
        private readonly NotificacionService _notificacionService;
        private readonly PublicacionService _publicacionService;

        public PerfilController(
            AppDbContext context,
            IWebHostEnvironment env,
            IHubContext<OnlineHub> hub,
            IHubContext<AvatarHub> avatarHub,
            NotificacionService notificacionService,
            PublicacionService publicacionService)
        {
            _context = context;
            _env = env;
            _hub = hub;
            _avatarHub = avatarHub;
            _notificacionService = notificacionService;
            _publicacionService = publicacionService;
        }

        // =====================================================
        // INDEX
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var usuarioNombre =
                HttpContext.Session.GetString("UsuarioNombre");

            if (string.IsNullOrEmpty(usuarioNombre))
                return RedirectToAction("Login", "Auth");

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(x =>
                    x.Nombre.ToLower() ==
                    usuarioNombre.Trim().ToLower());

            if (usuario == null)
            {
                usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u =>
                        u.UsuarioLogin == usuarioNombre);

                if (usuario == null)
                    return RedirectToAction("Login", "Auth");
            }

            // =====================================================
            // FOTO PERFIL ESTABLE
            // =====================================================

            if (string.IsNullOrWhiteSpace(usuario.FotoPerfil))
            {
                usuario.FotoPerfil =
                    "/img/user-default.png";
            }
            else
            {
                if (!usuario.FotoPerfil.StartsWith("/"))
                {
                    usuario.FotoPerfil =
                        "/" + usuario.FotoPerfil;
                }

                string rutaFisica = Path.Combine(
                    _env.WebRootPath,
                    usuario.FotoPerfil.TrimStart('/'));

                // SOLO VALIDAR SI ES UNA FOTO SUBIDA
                if (usuario.FotoPerfil.Contains("/uploads/"))
                {
                    if (!System.IO.File.Exists(rutaFisica))
                    {
                        usuario.FotoPerfil =
                            "/img/user-default.png";
                    }
                }
            }

            HttpContext.Session.SetString(
                "UsuarioFoto",
                usuario.FotoPerfil);

            ViewBag.EnLinea = true;

            var catalogoPersonal =
                await _publicacionService
                .ObtenerFeedPorUsuarioConLikesYComentariosAsync(
                    usuario.Id);

            ViewBag.CatalogoPersonal =
                catalogoPersonal;

            var catalogos = await _context.Catalogos
                .Include(c => c.Manicurista)
                .OrderByDescending(c => c.FechaSubida)
                .Take(20)
                .ToListAsync();

            ViewBag.Catalogos = catalogos;

            return View(usuario);
        }

        // =====================================================
        // EDITAR
        // =====================================================
        public async Task<IActionResult> Editar()
        {
            var usuarioNombre =
                HttpContext.Session.GetString("UsuarioNombre");

            if (string.IsNullOrEmpty(usuarioNombre))
                return RedirectToAction("Login", "Auth");

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(x =>
                    x.Nombre.ToLower() ==
                    usuarioNombre.Trim().ToLower());

            if (usuario == null)
            {
                usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u =>
                        u.UsuarioLogin == usuarioNombre);

                if (usuario == null)
                    return RedirectToAction("Login", "Auth");
            }

            // FOTO SEGURA
            if (string.IsNullOrWhiteSpace(usuario.FotoPerfil))
            {
                usuario.FotoPerfil =
                    "/img/user-default.png";
            }

            if (!usuario.FotoPerfil.StartsWith("/"))
            {
                usuario.FotoPerfil =
                    "/" + usuario.FotoPerfil;
            }

            return View(usuario);
        }

        [HttpGet]
        public IActionResult EditModal()
        {
            return RedirectToAction("Editar");
        }

        // =====================================================
        // GUARDAR
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(
            Usuario model,
            IFormFile? foto)
        {
            var usuarioNombre =
                HttpContext.Session.GetString("UsuarioNombre");

            if (string.IsNullOrEmpty(usuarioNombre))
                return RedirectToAction("Login", "Auth");

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(x =>
                    x.Id == model.Id);

            if (usuario == null)
                return RedirectToAction("Login", "Auth");

            // =====================================================
            // ACTUALIZAR DATOS
            // =====================================================

            usuario.Nombre = model.Nombre;
            usuario.Telefono = model.Telefono;
            usuario.Instagram = model.Instagram;
            usuario.TikTok = model.TikTok;
            usuario.Facebook = model.Facebook;
            usuario.WhatsApp = model.WhatsApp;

            // =====================================================
            // FOTO PERFIL
            // =====================================================

            if (foto != null && foto.Length > 0)
            {
                string carpeta = Path.Combine(
                    _env.WebRootPath,
                    "uploads",
                    "perfiles");

                if (!Directory.Exists(carpeta))
                {
                    Directory.CreateDirectory(carpeta);
                }

                string extension =
                    Path.GetExtension(foto.FileName);

                string nombreArchivo =
                    Guid.NewGuid().ToString() + extension;

                string rutaArchivo = Path.Combine(
                    carpeta,
                    nombreArchivo);

                using (var stream = new FileStream(
                    rutaArchivo,
                    FileMode.Create))
                {
                    await foto.CopyToAsync(stream);
                }

                usuario.FotoPerfil =
                    "/uploads/perfiles/" + nombreArchivo;

                HttpContext.Session.SetString(
                    "UsuarioFoto",
                    usuario.FotoPerfil);

                await _avatarHub.Clients.All.SendAsync(
                    "RecibirAvatar",
                    usuario.Nombre,
                    usuario.FotoPerfil
                );
            }

            _context.Usuarios.Update(usuario);

            await _context.SaveChangesAsync();

            await _notificacionService
                .EnviarNotificacionTiempoReal(
                    usuarioNombre,
                    "Tu perfil fue actualizado correctamente 👤");

            await _notificacionService
                .ActualizarContador(
                    usuarioNombre,
                    1);

            TempData["Mensaje"] =
                "Perfil actualizado correctamente";

            return RedirectToAction("Index");
        }
    }
}