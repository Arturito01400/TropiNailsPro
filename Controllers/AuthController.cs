using TropiNailsPro.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using TropiNailsPro.Models;
using TropiNailsPro.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Models.ViewModels;

namespace TropiNailsPro.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public AuthController(
            AppDbContext context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // =====================================================
        // LOGIN
        // =====================================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string Identificador,
            string Clave)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u =>
                u.Email == Identificador ||
                u.Telefono == Identificador ||
                u.UsuarioLogin == Identificador);

            if (usuario == null ||
    !PasswordService.Verify(
        usuario.Clave,
        Clave))
{
    TempData["Error"] =
        "Usuario o contraseña incorrectos.";

    return View();
}

if (usuario.Rol == "Manicurista")
{
    var manicurista = await _context.Manicuristas
    .FirstOrDefaultAsync(m =>
        m.UsuarioId == usuario.Id);

if (manicurista == null)
{
    TempData["Error"] =
        "No se encontró la manicurista.";

    return View();
}

var suscripcion = await _context.Suscripciones
    .Where(s => s.ManicuristaId == manicurista.Id)
    .OrderByDescending(s => s.FechaInicio)
    .FirstOrDefaultAsync();

    var ahora = DateTime.UtcNow;

    bool expirada =
        suscripcion == null ||
        !suscripcion.Activa ||
        suscripcion.Cancelada ||
        suscripcion.FechaVencimiento <= ahora;

    if (expirada)
{
    TempData["Error"] =
        "Tu suscripción está vencida. Debes renovar para continuar.";

    var claimsVencida = new List<Claim>
    {
        new Claim(
            ClaimTypes.NameIdentifier,
            usuario.Id.ToString()),

        new Claim(
            ClaimTypes.Name,
            usuario.Nombre ?? ""),

        new Claim(
            ClaimTypes.Email,
            usuario.Email ?? ""),

        new Claim(
            "UsuarioId",
            usuario.Id.ToString()),

        new Claim(
            "Rol",
            usuario.Rol ?? "Clienta")
    };

    var identityVencida =
        new ClaimsIdentity(
            claimsVencida,
            CookieAuthenticationDefaults.AuthenticationScheme);

    var principalVencida =
        new ClaimsPrincipal(identityVencida);

    await HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principalVencida);

    return RedirectToAction(
        "Vencida",
        "Suscripcion");
}
}


            // =====================================================
            // FOTO PERFIL ESTABLE
            // =====================================================

            if (string.IsNullOrWhiteSpace(
                usuario.FotoPerfil))
            {
                usuario.FotoPerfil =
                    "/img/user-default.png";
            }

            if (!usuario.FotoPerfil.StartsWith("/"))
            {
                usuario.FotoPerfil =
                    "/" + usuario.FotoPerfil;
            }

            // =====================================================
            // 🔥 MANICURISTA ID REAL
            // =====================================================

            int manicuristaIdFinal = 0;

            if (usuario.Rol == "Manicurista")
            {
                // 🔥 BUSCAR MANICURISTA REAL
                var manicuristaReal =
    await _context.Manicuristas
    .FirstOrDefaultAsync(m =>
        m.UsuarioId == usuario.Id);

                // 🔥 SI NO EXISTE -> CREAR AUTOMATICO
                if (manicuristaReal == null)
                {
                    string codigoGenerado =
                        Guid.NewGuid()
                        .ToString("N")
                        .Substring(0, 10);

                    manicuristaReal =
new Manicurista
{
    UsuarioId = usuario.Id,

    NombreNegocio =
        string.IsNullOrWhiteSpace(usuario.Nombre)
        ? "Mi Negocio"
        : usuario.Nombre,

    CodigoPublico = codigoGenerado,

    Plan = "Prueba",

    FechaInicioPrueba = DateTime.UtcNow,

    FechaVencimiento = DateTime.UtcNow.AddDays(15),

    Activa = true
};

                    _context.Manicuristas
                        .Add(manicuristaReal);

                    await _context.SaveChangesAsync();
                }

                // 🔥 SI EL CODIGO ESTA VACIO -> GENERARLO
                if (string.IsNullOrWhiteSpace(
                    manicuristaReal.CodigoPublico))
                {
                    manicuristaReal.CodigoPublico =
                        Guid.NewGuid()
                        .ToString("N")
                        .Substring(0, 10);

                    await _context.SaveChangesAsync();
                }

                manicuristaIdFinal =
                    manicuristaReal.Id;
            }
            else
{
    var manicuristaIdReal = await _context.Usuarios
        .Where(u => u.Id == usuario.Id)
        .Select(u => u.ManicuristaId)
        .FirstOrDefaultAsync();

    if (manicuristaIdReal.HasValue && manicuristaIdReal.Value > 0)
    {
        manicuristaIdFinal = manicuristaIdReal.Value;
    }
    else
    {
        TempData["Error"] =
            "Tu cuenta no está asociada a ninguna manicurista.";

        return View();
    }
}
            

            HttpContext.Session.SetInt32(
                "UsuarioId",
                usuario.Id);

            HttpContext.Session.SetString(
                "UsuarioNombre",
                usuario.Nombre ?? "");

            HttpContext.Session.SetString(
                "UsuarioRol",
                usuario.Rol ?? "Clienta");

            HttpContext.Session.SetInt32(
                "ManicuristaId",
                manicuristaIdFinal);

            HttpContext.Session.SetString(
                "Rol",
                usuario.Rol ?? "Clienta");

                HttpContext.Session.SetString(
    "UsuarioPlan",
    usuario.Plan ?? "Premium");

            // =====================================================
            // PERFIL
            // =====================================================

            var perfil = await _context.UsuariosPerfil
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p =>
                    p.UsuarioId == usuario.Id);

            if (perfil == null)
            {
                bool perfilExiste =
                    await _context.UsuariosPerfil
                    .AnyAsync(p =>
                        p.UsuarioId == usuario.Id);

                if (!perfilExiste)
                {
                    perfil = new UsuarioPerfil
                    {
                        UsuarioId = usuario.Id,
                        Usuario = null,

                        FotoUrl = usuario.FotoPerfil,

                        Instagram = usuario.Instagram ?? "",
                        TikTok = usuario.TikTok ?? "",
                        Facebook = usuario.Facebook ?? "",
                        WhatsApp = usuario.WhatsApp ?? "",

                        Activo = true
                    };

                    _context.UsuariosPerfil.Add(perfil);

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch
                    {
                        // 🔥 evita romper login
                    }
                }
            }

            // =====================================================
            // FOTO SESSION ESTABLE
            // =====================================================

            string fotoPerfil = usuario.FotoPerfil;

            if (string.IsNullOrWhiteSpace(
                fotoPerfil))
            {
                fotoPerfil =
                    "/img/user-default.png";
            }

            if (!fotoPerfil.StartsWith("/"))
            {
                fotoPerfil =
                    "/" + fotoPerfil;
            }

            HttpContext.Session.SetString(
                "UsuarioFoto",
                fotoPerfil);

            // =====================================================
            // CLAIMS
            // =====================================================

            // =====================================================
// CLAIMS
// =====================================================

var claims = new List<Claim>
{
    new Claim(
        ClaimTypes.NameIdentifier,
        usuario.Id.ToString()),

    new Claim(
        ClaimTypes.Name,
        usuario.Nombre ?? ""),

    new Claim(
        ClaimTypes.Email,
        usuario.Email ?? ""),

    new Claim(
        "UsuarioId",
        usuario.Id.ToString()),

    new Claim(
        "Rol",
        usuario.Rol ?? "Clienta")
};

            var identity =
                new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

            var principal =
                new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    AllowRefresh = true,
                    ExpiresUtc =
                        DateTimeOffset.UtcNow
                        .AddHours(8)
                });

            return RedirectToAction(
                "Index",
                "Dashboard");
        }

        // =====================================================
        // REGISTRO POR LINK
        // =====================================================

        [HttpGet]
[Route("registro")]
public async Task<IActionResult> Register(
    int? manicuristaId,
    string? codigo)
{
    Manicurista? manicurista = null;

    // =====================================================
    // 🔥 BUSCAR POR CODIGO
    // =====================================================

    if (!string.IsNullOrWhiteSpace(codigo))
    {
        manicurista =
            await _context.Manicuristas
            .FirstOrDefaultAsync(m =>
                m.CodigoPublico == codigo);

        // 🔥 SI EXISTE PERO NO TIENE CODIGO
        if (manicurista != null &&
            string.IsNullOrWhiteSpace(
                manicurista.CodigoPublico))
        {
            manicurista.CodigoPublico =
                Guid.NewGuid()
                .ToString("N")
                .Substring(0, 10);

            await _context.SaveChangesAsync();
        }
    }
    // =====================================================
    // 🔥 BUSCAR POR ID
    // =====================================================
    else if (
        manicuristaId.HasValue &&
        manicuristaId.Value > 0)
    {
        manicurista =
            await _context.Manicuristas
            .FirstOrDefaultAsync(m =>
                m.Id == manicuristaId.Value);

        // 🔥 SI NO TIENE CODIGO -> GENERARLO
        if (manicurista != null &&
            string.IsNullOrWhiteSpace(
                manicurista.CodigoPublico))
        {
            manicurista.CodigoPublico =
                Guid.NewGuid()
                .ToString("N")
                .Substring(0, 10);

            await _context.SaveChangesAsync();
        }
    }

    // =====================================================
    // 🔥 DATOS PARA LA VISTA
    // =====================================================

    if (manicurista != null)
    {
        ViewBag.ManicuristaId =
            manicurista.Id;

        ViewBag.EsClientaDirecta =
            true;

        ViewBag.ManicuristaNombre =
            string.IsNullOrWhiteSpace(
                manicurista.NombreNegocio)
            ? "Manicurista"
            : manicurista
                .NombreNegocio
                .Trim();

        ViewBag.LinkRegistro =
            $"{Request.Scheme}://{Request.Host}/registro?codigo={manicurista.CodigoPublico}";
    }
    else
    {
        // 🔥 EVITA /registro?codigo=error
        ViewBag.LinkRegistro =
            $"{Request.Scheme}://{Request.Host}/registro";
    }

    return View();
}

        // =====================================================
        // REGISTER POST
        // =====================================================

        [HttpPost]
        [Route("registro")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            Usuario model,
            string tipoRegistro,
            int? manicuristaId)
        {
            ModelState.Remove("ResetToken");
            ModelState.Remove("TokenExpira");
            ModelState.Remove("CodigoSMS");
            ModelState.Remove("CodigoSMSExpira");
            ModelState.Remove("FotoPerfil");
            ModelState.Remove("Instagram");
            ModelState.Remove("TikTok");
            ModelState.Remove("Facebook");
            ModelState.Remove("WhatsApp");
            ModelState.Remove("CodigoReferencia");
            ModelState.Remove("ManicuristaId");

            if (!ModelState.IsValid)
            {
                ViewBag.ManicuristaId =
                    manicuristaId;

                return View(
                    "Register",
                    model);
            }

            model.Email =
                model.Email?.Trim();

            model.UsuarioLogin =
                model.UsuarioLogin?.Trim();

            model.Telefono =
                model.Telefono?.Trim();

            // =====================================================
            // VALIDAR EMAIL
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                model.Email))
            {
                string emailNuevo =
                    model.Email
                    .Trim()
                    .ToLower();

                bool correoExiste =
                    await _context.Usuarios
                    .AsNoTracking()
                    .AnyAsync(u =>
                        !string.IsNullOrWhiteSpace(u.Email) &&
                        u.Email.Trim().ToLower() ==
                        emailNuevo);

                if (correoExiste)
                {
                    TempData["Error"] =
                        "El correo ya está registrado.";

                    return View(
                        "Register",
                        model);
                }
            }

            // =====================================================
            // VALIDAR LOGIN
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                model.UsuarioLogin))
            {
                string loginNuevo =
                    model.UsuarioLogin
                    .Trim()
                    .ToLower();

                var loginsExistentes =
                    await _context.Usuarios
                    .AsNoTracking()
                    .Where(u =>
                        !string.IsNullOrWhiteSpace(
                            u.UsuarioLogin))
                    .Select(u =>
                        u.UsuarioLogin
                        .Trim()
                        .ToLower())
                    .ToListAsync();

                if (loginsExistentes.Contains(
                    loginNuevo))
                {
                    TempData["Error"] =
                        "El usuario login ya existe.";

                    return View(
                        "Register",
                        model);
                }
            }

            // =====================================================
            // VALIDAR TELEFONO
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                model.Telefono))
            {
                string telefonoNuevo =
                    model.Telefono.Trim();

                var telefonosExistentes =
                    await _context.Usuarios
                    .AsNoTracking()
                    .Where(u =>
                        !string.IsNullOrWhiteSpace(
                            u.Telefono))
                    .Select(u =>
                        u.Telefono.Trim())
                    .ToListAsync();

                if (telefonosExistentes.Contains(
                    telefonoNuevo))
                {
                    TempData["Error"] =
                        "El teléfono ya está registrado.";

                    return View(
                        "Register",
                        model);
                }
            }

            if (string.IsNullOrWhiteSpace(
                model.FotoPerfil))
            {
                model.FotoPerfil =
                    "/img/user-default.png";
            }

            model.Clave =
                PasswordService.Hash(
                    model.Clave);

            model.Rol =
                tipoRegistro == "Manicurista"
                ? "Manicurista"
                : "Clienta";

            model.FechaRegistro =
                DateTime.Now;

            model.PlanActivo = true;

            model.Plan = "Premium";

            // CLIENTA INVITADA POR MANICURISTA
if (model.Rol == "Clienta" &&
    manicuristaId.HasValue)
{
    var manicurista =
        await _context.Manicuristas
        .FirstOrDefaultAsync(m =>
            m.Id == manicuristaId.Value);

    if (manicurista != null)
{
    model.ManicuristaId =
        manicurista.Id;
}
}

            try
            {
                _context.Usuarios.Add(model);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    ex.InnerException?.Message ??
                    ex.Message;

                return View(
                    "Register",
                    model);
            }

            // =====================================================
            // 🔥 CREAR MANICURISTA AUTOMATICA
            // =====================================================

            if (model.Rol == "Manicurista")
            {
                bool existeManicurista =
    await _context.Manicuristas
    .AnyAsync(m =>
        m.UsuarioId == model.Id);

                if (!existeManicurista)
{
    string codigoNuevo =
        Guid.NewGuid()
        .ToString("N")
        .Substring(0, 10);

    var nuevaManicurista =
        new Manicurista
        {
            UsuarioId = model.Id,

            NombreNegocio =
                string.IsNullOrWhiteSpace(model.Nombre)
                ? "Mi Negocio"
                : model.Nombre,

            CodigoPublico = codigoNuevo,

            Plan = "Prueba",

            FechaInicioPrueba = DateTime.UtcNow,

            FechaVencimiento = DateTime.UtcNow.AddDays(15),

            Activa = true
        };

    _context.Manicuristas.Add(nuevaManicurista);

    await _context.SaveChangesAsync();

    // 🔥 SUSCRIPCIÓN GRATUITA DE 15 DÍAS
    var suscripcionPrueba =
        new Suscripcion
        {
            ManicuristaId = nuevaManicurista.Id,

            FechaInicio = DateTime.UtcNow,

            FechaVencimiento =
                DateTime.UtcNow.AddDays(15),

            Plan = "Prueba Gratis",

            Activa = true,

            Cancelada = false,

            MetodoPago = "Trial",

            EstadoPago = "TRIAL",

            Monto = 0,

            Moneda = "USD"
        };

    _context.Suscripciones.Add(suscripcionPrueba);

    await _context.SaveChangesAsync();
}
            
            }
            bool perfilYaExiste =
                await _context.UsuariosPerfil
                .AnyAsync(p =>
                    p.UsuarioId == model.Id);

            if (!perfilYaExiste)
            {
                var perfilNuevo =
                    new UsuarioPerfil
                    {
                        UsuarioId = model.Id,
                        Usuario = null,

                        FotoUrl =
                            model.FotoPerfil,

                        Instagram =
                            model.Instagram ?? "",

                        TikTok =
                            model.TikTok ?? "",

                        Facebook =
                            model.Facebook ?? "",

                        WhatsApp =
                            model.WhatsApp ?? "",

                        Activo = true
                    };

                _context.UsuariosPerfil
                    .Add(perfilNuevo);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch
                {
                }
            }

            TempData["Mensaje"] =
                "🎉 Registro exitoso.";

            return RedirectToAction(
                "Login",
                "Auth");
        }

// =====================================================
// RECUPERAR CLAVE
// =====================================================

// =====================================================
// RECUPERAR CLAVE GET
// =====================================================

[HttpGet]
public IActionResult RecuperarClave()
{
    return View();
}

// =====================================================
// RECUPERAR CLAVE POST
// =====================================================

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RecuperarClave(
    string Identificador)
{
    if (string.IsNullOrWhiteSpace(
        Identificador))
    {
        TempData["Error"] =
            "Debes ingresar un correo o teléfono.";

        return View();
    }

    var usuario = await _context.Usuarios
        .FirstOrDefaultAsync(u =>
            u.Email == Identificador ||
            u.Telefono == Identificador);

    if (usuario == null)
    {
        TempData["Error"] =
            "No existe una cuenta asociada.";

        return View();
    }

    // =====================================================
    // TOKEN TEMPORAL
    // =====================================================

    string token =
        Guid.NewGuid().ToString();

    usuario.ResetToken = token;

    usuario.TokenExpira =
        DateTime.Now.AddHours(1);

    await _context.SaveChangesAsync();

    // =====================================================
    // LINK RECUPERACION
    // =====================================================

    string link =
        $"{Request.Scheme}://{Request.Host}/Auth/CambiarClave?token={token}";

    // =====================================================
    // ENVIAR EMAIL
    // =====================================================

    if (!string.IsNullOrWhiteSpace(
        usuario.Email))
    {
        await _emailService.EnviarCorreoAsync(
            usuario.Email,
            "Recuperación de contraseña",
            $@"
            <h2>Recuperar Contraseña</h2>

            <p>Haz clic en el botón:</p>

            <a href='{link}'
               style='padding:12px 20px;
               background:#d63384;
               color:white;
               text-decoration:none;
               border-radius:8px;'>
               Cambiar Contraseña
            </a>
            ");
    }

    TempData["Mensaje"] =
        "Te enviamos un link de recuperación.";

    return View();
}


// =====================================================
// CAMBIAR CLAVE GET
// =====================================================

[HttpGet]
public async Task<IActionResult> CambiarClave(
    string token)
{
    if (string.IsNullOrWhiteSpace(token))
    {
        return RedirectToAction(
            "Login");
    }

    var usuario = await _context.Usuarios
        .FirstOrDefaultAsync(u =>
            u.ResetToken == token &&
            u.TokenExpira > DateTime.Now);

    if (usuario == null)
    {
        TempData["Error"] =
            "El link expiró o es inválido.";

        return RedirectToAction(
            "RecuperarClave");
    }

    var model =
        new CambiarContrasenaViewModel
        {
            Token = token
        };

    return View(model);
}

// =====================================================
// CAMBIAR CLAVE POST
// =====================================================

// =====================================================
// CAMBIAR CLAVE POST
// =====================================================

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CambiarClave(
    CambiarContrasenaViewModel model)
{
    // =====================================================
    // VALIDAR MODELO
    // =====================================================

    if (!ModelState.IsValid)
    {
        return View(model);
    }

    // =====================================================
    // VALIDAR CONTRASEÑAS
    // =====================================================

    if (model.NuevaClave != model.ConfirmarClave)
    {
        TempData["Error"] =
            "Las contraseñas no coinciden.";

        return View(model);
    }

    // =====================================================
    // BUSCAR USUARIO POR TOKEN REAL
    // =====================================================

    var usuario = await _context.Usuarios
        .FirstOrDefaultAsync(u =>
            u.ResetToken != null &&
            u.ResetToken == model.Token &&
            u.TokenExpira != null &&
            u.TokenExpira > DateTime.Now);

    // =====================================================
    // TOKEN INVÁLIDO
    // =====================================================

    if (usuario == null)
    {
        TempData["Error"] =
            "El link expiró o es inválido.";

        return RedirectToAction(
            "RecuperarClave");
    }

    // =====================================================
    // ACTUALIZAR CONTRASEÑA REAL
    // =====================================================
// =====================================================
// CAMBIAR CONTRASEÑA REAL
// =====================================================

usuario.Clave =
    PasswordService.Hash(
        model.NuevaClave.Trim());

// DEBUG
Console.WriteLine("=================================");
Console.WriteLine("NUEVO HASH GENERADO:");
Console.WriteLine(usuario.Clave);
Console.WriteLine("=================================");

// =====================================================
// LIMPIAR TOKEN
// =====================================================

usuario.ResetToken = null;
usuario.TokenExpira = null;

// =====================================================
// ACTUALIZAR USUARIO
// =====================================================

_context.Usuarios.Update(usuario);

// =====================================================
// GUARDAR CAMBIOS
// =====================================================

await _context.SaveChangesAsync();

// =====================================================
// MENSAJE
// =====================================================

TempData["MensajeClave"] =
    "Tu contraseña fue cambiada correctamente.";

// =====================================================
// RETORNAR VISTA
// =====================================================

return View(model);
}
        // =====================================================
        // LOGOUT
        // =====================================================

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            HttpContext.Session.Clear();

            return RedirectToAction(
                "Login");
        }
    }
}