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
            var usuarioId =
                HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
                return RedirectToAction("Login", "Auth");

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == usuarioId.Value);

            if (usuario == null)
                return RedirectToAction("Login", "Auth");

            // =====================================================
            // BUSCAR NEGOCIO / PROFESIONAL
            // =====================================================

            var manicurista = await _context.Manicuristas
                .FirstOrDefaultAsync(m => m.UsuarioId == usuario.Id);

            ViewBag.Manicurista = manicurista;

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
                if (!usuario.FotoPerfil.StartsWith("/") &&
                    !usuario.FotoPerfil.StartsWith("http://") &&
                    !usuario.FotoPerfil.StartsWith("https://"))
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

            // =====================================================
            // ESTADO EN LÍNEA
            // =====================================================

            ViewBag.EnLinea = true;

            // =====================================================
            // CATÁLOGO PERSONAL
            // =====================================================

            var catalogoPersonal =
                await _publicacionService
                .ObtenerFeedPorUsuarioConLikesYComentariosAsync(
                    usuario.Id);

            ViewBag.CatalogoPersonal =
                catalogoPersonal;

            // =====================================================
            // CATÁLOGOS
            // =====================================================

            var catalogos = await _context.Catalogos
                .Include(c => c.Manicurista)
                .OrderByDescending(c => c.FechaSubida)
                .Take(20)
                .ToListAsync();

            ViewBag.Catalogos =
                catalogos;

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

            // =====================================================
            // BUSCAR NEGOCIO DEL PROFESIONAL
            // =====================================================

            var manicurista = await _context.Manicuristas
                .FirstOrDefaultAsync(m => m.UsuarioId == usuario.Id);

            ViewBag.Manicurista =
                manicurista;

            // =====================================================
            // FOTO SEGURA
            // =====================================================

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
                usuario.FotoPerfil =
                    "/" + usuario.FotoPerfil;
            }

            // =====================================================
            // FOTO DEL NEGOCIO
            // =====================================================

            if (manicurista != null &&
                !string.IsNullOrWhiteSpace(manicurista.FotoNegocio))
            {
                if (!manicurista.FotoNegocio.StartsWith("/") &&
                    !manicurista.FotoNegocio.StartsWith("http://") &&
                    !manicurista.FotoNegocio.StartsWith("https://"))
                {
                    manicurista.FotoNegocio =
                        "/" + manicurista.FotoNegocio;
                }
            }

            return View(usuario);
        }

        // =====================================================
        // EDIT MODAL
        // =====================================================
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
            IFormFile? foto,
            IFormFile? fotoNegocio)
        {
            // =====================================================
            // USUARIO DE LA SESIÓN
            // =====================================================

            var usuarioId =
                HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
                return RedirectToAction("Login", "Auth");

            // =====================================================
            // BUSCAR USUARIO REAL
            // =====================================================

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(x =>
                    x.Id == usuarioId.Value);

            if (usuario == null)
                return RedirectToAction("Login", "Auth");

            // =====================================================
            // BUSCAR NEGOCIO DEL PROFESIONAL
            // =====================================================

            var manicurista = await _context.Manicuristas
                .FirstOrDefaultAsync(m =>
                    m.UsuarioId == usuario.Id);

            // =====================================================
            // ACTUALIZAR DATOS PERSONALES
            // =====================================================

            usuario.Nombre =
                model.Nombre;

            usuario.Telefono =
                model.Telefono;

            usuario.Instagram =
                model.Instagram;

            usuario.TikTok =
                model.TikTok;

            usuario.Facebook =
                model.Facebook;

            usuario.WhatsApp =
                model.WhatsApp;

            // =====================================================
            // ACTUALIZAR DATOS DEL NEGOCIO
            // =====================================================

            if (manicurista != null)
            {
                // -------------------------------------------------
                // NOMBRE DEL NEGOCIO
                // -------------------------------------------------

                if (Request.Form.ContainsKey("NombreNegocio"))
                {
                    var nombreNegocio =
                        Request.Form["NombreNegocio"].ToString();

                    if (!string.IsNullOrWhiteSpace(nombreNegocio))
                    {
                        manicurista.NombreNegocio =
                            nombreNegocio.Trim();
                    }
                }

                // -------------------------------------------------
                // TELÉFONO DEL NEGOCIO
                // -------------------------------------------------

                if (Request.Form.ContainsKey("TelefonoNegocio"))
                {
                    var telefonoNegocio =
                        Request.Form["TelefonoNegocio"].ToString();

                    manicurista.TelefonoNegocio =
                        string.IsNullOrWhiteSpace(telefonoNegocio)
                            ? null
                            : telefonoNegocio.Trim();
                }

                // -------------------------------------------------
                // DIRECCIÓN
                // -------------------------------------------------

                if (Request.Form.ContainsKey("DireccionNegocio"))
                {
                    var direccion =
                        Request.Form["DireccionNegocio"].ToString();

                    manicurista.DireccionNegocio =
                        string.IsNullOrWhiteSpace(direccion)
                            ? null
                            : direccion.Trim();
                }

                // -------------------------------------------------
                // CIUDAD
                // -------------------------------------------------

                if (Request.Form.ContainsKey("Ciudad"))
                {
                    var ciudad =
                        Request.Form["Ciudad"].ToString();

                    manicurista.Ciudad =
                        string.IsNullOrWhiteSpace(ciudad)
                            ? null
                            : ciudad.Trim();
                }

                // -------------------------------------------------
                // PROVINCIA
                // -------------------------------------------------

                if (Request.Form.ContainsKey("Provincia"))
                {
                    var provincia =
                        Request.Form["Provincia"].ToString();

                    manicurista.Provincia =
                        string.IsNullOrWhiteSpace(provincia)
                            ? null
                            : provincia.Trim();
                }

                // =================================================
                // LATITUD
                // =================================================

                if (Request.Form.ContainsKey("Latitud"))
                {
                    var latitudTexto =
                        Request.Form["Latitud"].ToString();

                    if (decimal.TryParse(
                        latitudTexto,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out decimal latitud))
                    {
                        manicurista.Latitud =
                            latitud;
                    }
                    else if (
                        string.IsNullOrWhiteSpace(latitudTexto))
                    {
                        manicurista.Latitud =
                            null;
                    }
                }

                // =================================================
                // LONGITUD
                // =================================================

                if (Request.Form.ContainsKey("Longitud"))
                {
                    var longitudTexto =
                        Request.Form["Longitud"].ToString();

                    if (decimal.TryParse(
                        longitudTexto,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out decimal longitud))
                    {
                        manicurista.Longitud =
                            longitud;
                    }
                    else if (
                        string.IsNullOrWhiteSpace(longitudTexto))
                    {
                        manicurista.Longitud =
                            null;
                    }
                }

                // =================================================
                // UBICACIÓN ACTIVA
                // =================================================

                if (Request.Form.ContainsKey("UbicacionActiva"))
                {
                    var ubicacionTexto =
                        Request.Form["UbicacionActiva"].ToString();

                    manicurista.UbicacionActiva =
                        ubicacionTexto == "true" ||
                        ubicacionTexto == "1" ||
                        ubicacionTexto == "on";
                }
            }

            // =====================================================
            // FOTO PERFIL → AZURE BLOB STORAGE
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
                    await _blobService
                    .SubirArchivoCarpetaAsync(
                        stream,
                        $"perfiles/{usuario.Id}",
                        nombreArchivo,
                        foto.ContentType
                    );

                usuario.FotoPerfil =
                    urlAzure;

                // -------------------------------------------------
                // ACTUALIZAR FOTO EN SESIÓN
                // -------------------------------------------------

                HttpContext.Session.SetString(
                    "UsuarioFoto",
                    usuario.FotoPerfil);

                // -------------------------------------------------
                // ACTUALIZAR AVATAR EN TIEMPO REAL
                // -------------------------------------------------

                await _avatarHub.Clients.All.SendAsync(
                    "RecibirAvatar",
                    usuario.Nombre,
                    usuario.FotoPerfil
                );
            }

            // =====================================================
            // FOTO DEL NEGOCIO → AZURE BLOB STORAGE
            // =====================================================

            if (fotoNegocio != null &&
                fotoNegocio.Length > 0 &&
                manicurista != null)
            {
                string extensionNegocio =
                    Path.GetExtension(
                        fotoNegocio.FileName)
                    .ToLower();

                string nombreArchivoNegocio =
                    Guid.NewGuid() +
                    extensionNegocio;

                using var streamNegocio =
                    fotoNegocio.OpenReadStream();

                string urlAzureNegocio =
                    await _blobService
                    .SubirArchivoCarpetaAsync(
                        streamNegocio,
                        $"negocios/{usuario.Id}",
                        nombreArchivoNegocio,
                        fotoNegocio.ContentType
                    );

                manicurista.FotoNegocio =
                    urlAzureNegocio;
            }

            // =====================================================
            // GUARDAR USUARIO
            // =====================================================

            _context.Usuarios.Update(usuario);

            // =====================================================
            // GUARDAR NEGOCIO
            // =====================================================

            if (manicurista != null)
            {
                _context.Manicuristas.Update(
                    manicurista);
            }

            // =====================================================
            // GUARDAR TODO
            // =====================================================

            await _context.SaveChangesAsync();

            // =====================================================
            // NOTIFICACIÓN
            // =====================================================

            await _notificacionService
                .EnviarNotificacionTiempoReal(
                    usuario.Nombre,
                    "Tu perfil fue actualizado correctamente 👤");

            await _notificacionService
                .ActualizarContador(
                    usuario.Nombre,
                    1);

            // =====================================================
            // MENSAJE
            // =====================================================

            TempData["Mensaje"] =
                "Perfil actualizado correctamente";

            return RedirectToAction("Index");
        }
    }
}