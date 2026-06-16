using Microsoft.AspNetCore.Mvc;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using System.Linq;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace TropiNailsPro.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================
        // LOGIN (GET)
        // =========================================
        public IActionResult Login()
        {
            return View();
        }

        // =========================================
        // LOGIN (POST)
        // =========================================
        [HttpPost]
        public IActionResult Login(
            string usuarioLogin,
            string password)
        {
            // 🔥 LIMPIAR DATOS
            usuarioLogin =
                usuarioLogin?.Trim();

            password =
                password?.Trim();

            var user = _context.Usuarios
                .FirstOrDefault(u =>
                    u.UsuarioLogin != null &&
                    u.UsuarioLogin.Trim() ==
                    usuarioLogin &&
                    u.Clave == password &&
                    u.Activo == true);

            if (user == null)
            {
                ViewBag.Error =
                    "Usuario o contraseña incorrectos";

                return View();
            }

            HttpContext.Session.SetString(
                "Usuario",
                user.UsuarioLogin!);

            return RedirectToAction(
                "Index",
                "Dashboard");
        }

        // =========================================
        // REGISTER (GET)
        // =========================================
        public IActionResult Register()
        {
            return View();
        }

        // =========================================
        // REGISTER (POST)
        // =========================================
        [HttpPost]
        public async Task<IActionResult> Register(
            Usuario usuario)
        {
            // =========================================
            // 🔥 MOSTRAR ERRORES REALES
            // =========================================

            if (!ModelState.IsValid)
            {
                var errores = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                ViewBag.Error =
                    string.Join(" | ", errores);

                return View(usuario);
            }

            // =========================================
            // 🔥 LIMPIAR DATOS
            // =========================================

            usuario.Nombre =
                usuario.Nombre?.Trim();

            usuario.UsuarioLogin =
                usuario.UsuarioLogin?.Trim();

            usuario.Email =
                usuario.Email?.Trim();

            usuario.Telefono =
                usuario.Telefono?.Trim();

            // =========================================
            // 🔥 VALIDAR EMAIL
            // =========================================

            if (!string.IsNullOrWhiteSpace(
                usuario.Email))
            {
                string emailNuevo =
                    usuario.Email
                    .Trim()
                    .ToLower();

                bool emailExiste =
                    await _context.Usuarios
                    .AsNoTracking()
                    .AnyAsync(u =>
                        u.Email != null &&
                        u.Email.Trim().ToLower() ==
                        emailNuevo);

                if (emailExiste)
                {
                    var correoReal =
                        await _context.Usuarios
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u =>
                            u.Email != null &&
                            u.Email.Trim().ToLower() ==
                            emailNuevo);

                    if (correoReal == null)
                    {
                        emailExiste = false;
                    }
                }

                if (emailExiste)
                {
                    ViewBag.Error =
                        "El correo ya está registrado";

                    return View(usuario);
                }
            }

            // =========================================
            // 🔥 VALIDAR USUARIO LOGIN
            // =========================================

            if (!string.IsNullOrWhiteSpace(
                usuario.UsuarioLogin))
            {
                string loginNuevo =
                    usuario.UsuarioLogin
                    .Trim()
                    .ToLower();

                bool loginExiste =
                    await _context.Usuarios
                    .AsNoTracking()
                    .AnyAsync(u =>
                        u.UsuarioLogin != null &&
                        u.UsuarioLogin.Trim().ToLower() ==
                        loginNuevo);

                if (loginExiste)
                {
                    ViewBag.Error =
                        "El usuario ya existe";

                    return View(usuario);
                }
            }

            // =========================================
            // 🔥 VALIDAR TELEFONO
            // =========================================

            if (!string.IsNullOrWhiteSpace(
                usuario.Telefono))
            {
                string telefonoNuevo =
                    usuario.Telefono.Trim();

                bool telefonoExiste =
                    await _context.Usuarios
                    .AsNoTracking()
                    .AnyAsync(u =>
                        u.Telefono != null &&
                        u.Telefono.Trim() ==
                        telefonoNuevo);

                if (telefonoExiste)
                {
                    ViewBag.Error =
                        "El teléfono ya está registrado";

                    return View(usuario);
                }
            }

            // =========================================
            // 🔥 DATOS DEFAULT
            // =========================================

            usuario.FechaRegistro =
                DateTime.Now;

            usuario.Activo = true;

            if (string.IsNullOrWhiteSpace(
                usuario.FotoPerfil))
            {
                usuario.FotoPerfil =
                    "/img/user-default.png";
            }

            // =========================================
            // 🔥 EVITAR NULLS
            // =========================================

            usuario.Email ??= "";
            usuario.Telefono ??= "";
            usuario.UsuarioLogin ??= "";

            // =========================================
            // 🔥 GUARDAR
            // =========================================

            _context.Usuarios.Add(usuario);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Cuenta creada correctamente";

            return RedirectToAction(
                "Login");
        }

        // =========================================
        // RESET (GET)
        // =========================================
        public IActionResult Reset()
        {
            return View();
        }

        // =========================================
        // RESET (POST)
        // =========================================
        [HttpPost]
        public IActionResult Reset(
            string email)
        {
            // 🔥 LIMPIAR EMAIL
            email = email?.Trim();

            var user = _context.Usuarios
                .FirstOrDefault(u =>
                    u.Email != null &&
                    u.Email.Trim().ToLower() ==
                    email.ToLower());

            if (user == null)
            {
                ViewBag.Error =
                    "Correo no encontrado";

                return View();
            }

            user.ResetToken =
                Guid.NewGuid().ToString();

            user.TokenExpira =
                DateTime.Now.AddMinutes(30);

            _context.SaveChanges();

            // 🔥 SIMULACIÓN ENVÍO CORREO
            Console.WriteLine(
                $"Link reset: https://localhost:5001/Account/NewPassword/{user.ResetToken}");

            ViewBag.Ok =
                "Se envió el correo de recuperación";

            return View();
        }
    }
}