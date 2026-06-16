using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using TropiNailsPro.Hubs;
using TropiNailsPro.Services; // 🔥 NUEVO
using System.Net;
using System.Net.Mail;

namespace TropiNailsPro.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<OnlineHub> _hub;
        private readonly NotificacionService _notificacionService; // 🔥 NUEVO

        public UsuariosController(
            AppDbContext context,
            IWebHostEnvironment env,
            IHubContext<OnlineHub> hub,
            NotificacionService notificacionService) // 🔥 NUEVO
        {
            _context = context;
            _env = env;
            _hub = hub;
            _notificacionService = notificacionService; // 🔥 NUEVO
        }

        // ======================================================
        // 🔹 PERFIL (GET)
        // ======================================================
        public async Task<IActionResult> Perfil()
        {
            var id = HttpContext.Session.GetInt32("UsuarioId");

            if (id == null)
                return RedirectToAction("Login", "Auth");

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == id);

            if (usuario == null)
            {
                usuario = new Usuario
                {
                    Id = id.Value,
                    Nombre = "Usuario",
                    Email = "",
                    Telefono = "",
                    FotoPerfil = "/img/user-default.png"
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();
            }

            return View(usuario);
        }

        // ======================================================
        // 🔹 GUARDAR PERFIL NORMAL (POST FORM)
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Perfil(Usuario model, IFormFile? fotoArchivo)
        {
            var id = HttpContext.Session.GetInt32("UsuarioId");

            if (id == null)
                return RedirectToAction("Login", "Auth");

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == id);

            if (usuario == null)
                return RedirectToAction("Login", "Auth");

            usuario.Nombre = model.Nombre;
            usuario.Email = model.Email;
            usuario.Telefono = model.Telefono;

            if (fotoArchivo != null && fotoArchivo.Length > 0)
            {
                string carpeta = Path.Combine(_env.WebRootPath, "uploads", "perfiles");
                if (!Directory.Exists(carpeta))
                    Directory.CreateDirectory(carpeta);

                string nombreArchivo = Guid.NewGuid() + Path.GetExtension(fotoArchivo.FileName);
                string rutaCompleta = Path.Combine(carpeta, nombreArchivo);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await fotoArchivo.CopyToAsync(stream);
                }

                usuario.FotoPerfil = "/uploads/perfiles/" + nombreArchivo;
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre);
            HttpContext.Session.SetString("UsuarioEmail", usuario.Email ?? "");
            HttpContext.Session.SetString("UsuarioFoto", usuario.FotoPerfil ?? "/img/user-default.png");

            await _hub.Clients.All.SendAsync("RecibirFotoActualizada", usuario.FotoPerfil);
            await _hub.Clients.All.SendAsync("MensajeSistema", "Perfil actualizado correctamente");

            TempData["Mensaje"] = "Perfil actualizado correctamente ✅";

            return RedirectToAction("Perfil");
        }

        // ======================================================
        // 🔹 SUBIDA INSTANTÁNEA AJAX
        // ======================================================
        [HttpPost]
        public async Task<IActionResult> SubirFotoPerfil(IFormFile archivo)
        {
            var id = HttpContext.Session.GetInt32("UsuarioId");

            if (id == null || archivo == null || archivo.Length == 0)
                return BadRequest();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == id);

            if (usuario == null)
                return BadRequest();

            string carpeta = Path.Combine(_env.WebRootPath, "uploads", "perfiles");
            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            string nombreArchivo = Guid.NewGuid() + Path.GetExtension(archivo.FileName);
            string rutaCompleta = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            string rutaWeb = "/uploads/perfiles/" + nombreArchivo;
            usuario.FotoPerfil = rutaWeb;

            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UsuarioFoto", rutaWeb);
            await _hub.Clients.All.SendAsync("RecibirFotoActualizada", rutaWeb);
            await _hub.Clients.All.SendAsync("MensajeSistema", "Foto actualizada correctamente");

            return Content(rutaWeb);
        }

        // ======================================================
        // 🔹 REGISTRAR CLIENTA (CORREGIDO 🔥)
        // ======================================================
        [HttpPost]
        public async Task<IActionResult> RegistrarCliente(Usuario model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Datos incompletos");

            // 🔗 SOLO usa ManicuristaId si viene desde frontend
            _context.Usuarios.Add(model);
            await _context.SaveChangesAsync();

            // 🔔 NOTIFICACIÓN EN BD
            var notificacion = new Notificacion
            {
                ManicuristaId = model.ManicuristaId ?? 0,
                Mensaje = $"Nueva clienta registrada: {model.Nombre} 💅",
                Tipo = "clienta",
                Leida = false,
                Fecha = DateTime.Now
            };

            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();

            // 🔔 TIEMPO REAL SOLO A SU MANICURISTA (SEGURO 🔒)
            await _hub.Clients.Group($"manicurista-{model.ManicuristaId}")
                .SendAsync("RecibirNotificacion", notificacion.Mensaje);

            await _hub.Clients.Group($"manicurista-{model.ManicuristaId}")
                .SendAsync("ActualizarContador", 1);

            // 🔥 USAR SERVICE SIN ERROR
            if (model.ManicuristaId != null)
            {
                var manicurista = await _context.Usuarios.FindAsync(model.ManicuristaId);

                if (manicurista != null)
                {
                    await _notificacionService.EnviarNotificacionTiempoReal(
                        manicurista.Nombre, // ✅ CORREGIDO
                        notificacion.Mensaje);
                }
            }

            return Ok(new { mensaje = "Cliente registrado y notificación enviada ✅" });
        }
    }
}