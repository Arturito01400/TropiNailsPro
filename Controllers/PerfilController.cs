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
        private readonly AzureBlobService _blobService;

       public PerfilController(
    AppDbContext context,
    IWebHostEnvironment env,
    IHubContext<OnlineHub> hub,
    IHubContext<AvatarHub> avatarHub,
    NotificacionService notificacionService,
    PublicacionService publicacionService,
    AzureBlobService blobService)
{
    _context = context;
    _env = env;
    _hub = hub;
    _avatarHub = avatarHub;
    _notificacionService = notificacionService;
    _publicacionService = publicacionService;
    _blobService = blobService;
}

        // =====================================================
        // INDEX
        // =====================================================
        public async Task<IActionResult> Index()
{
    var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

    if (usuarioId == null)
        return RedirectToAction("Login", "Auth");

    var usuario = await _context.Usuarios
        .FirstOrDefaultAsync(u => u.Id == usuarioId.Value);

    if (usuario == null)
        return RedirectToAction("Login", "Auth");


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
                if (
    !usuario.FotoPerfil.StartsWith("/") &&
    !usuario.FotoPerfil.StartsWith("http://") &&
    !usuario.FotoPerfil.StartsWith("https://")
)
{
    usuario.FotoPerfil =
        "/" + usuario.FotoPerfil;
}

                // SOLO VALIDAR FOTOS LOCALES
if (usuario.FotoPerfil.Contains("/uploads/"))
{
    string rutaFisica = Path.Combine(
        _env.WebRootPath,
        usuario.FotoPerfil.TrimStart('/'));

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
            var usuarioId =
    HttpContext.Session.GetInt32("UsuarioId");

if (usuarioId == null)
    return RedirectToAction("Login", "Auth");

var usuario = await _context.Usuarios
    .FirstOrDefaultAsync(u => u.Id == usuarioId.Value);

if (usuario == null)
    return RedirectToAction("Login", "Auth");


            // FOTO SEGURA
            if (string.IsNullOrWhiteSpace(usuario.FotoPerfil))
            {
                usuario.FotoPerfil =
                    "/img/user-default.png";
            }

           if (!string.IsNullOrWhiteSpace(usuario.FotoPerfil) &&
    !usuario.FotoPerfil.StartsWith("/") &&
    !usuario.FotoPerfil.StartsWith("http://") &&
    !usuario.FotoPerfil.StartsWith("https://"))
{
    usuario.FotoPerfil = "/" + usuario.FotoPerfil;
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
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

if (usuarioId == null)
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
    string extension =
        Path.GetExtension(foto.FileName)
        .ToLower();

    string nombreArchivo =
        Guid.NewGuid() + extension;

    using var stream =
        foto.OpenReadStream();

    string urlAzure =
        await _blobService.SubirArchivoCarpetaAsync(
            stream,
            $"perfiles/{usuario.Id}",
            nombreArchivo,
            foto.ContentType
        );

    usuario.FotoPerfil =
        urlAzure;

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
        usuario.Nombre,
        "Tu perfil fue actualizado correctamente 👤");

await _notificacionService
    .ActualizarContador(
        usuario.Nombre,
        1);

            TempData["Mensaje"] =
                "Perfil actualizado correctamente";

            return RedirectToAction("Index");
        }
    }
}