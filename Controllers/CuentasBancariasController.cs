using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using System.Security.Claims;

namespace TropiNailsPro.Controllers
{
    public class CuentasBancariasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CuentasBancariasController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        #region 🛠️ Helpers

        private int? ObtenerManicuristaId()
        {
            var manicuristaId = HttpContext.Session.GetInt32("ManicuristaId");

            if (manicuristaId != null)
                return manicuristaId;

            if (User.Identity?.IsAuthenticated == true)
            {
                var claim = User.Claims.FirstOrDefault(c => c.Type == "UsuarioId");

                if (claim != null && int.TryParse(claim.Value, out int id))
                {
                    HttpContext.Session.SetInt32("ManicuristaId", id);
                    return id;
                }
            }

            return null;
        }

        private void CargarViewBags()
        {
            ViewBag.TiposCuenta = new List<SelectListItem>
            {
                new SelectListItem { Text = "Ahorros", Value = "Ahorros" },
                new SelectListItem { Text = "Corriente", Value = "Corriente" }
            };
        }

        private async Task<string> GuardarLogo(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return null;

            string carpeta = Path.Combine(_env.WebRootPath, "img", "bancos");

            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            string nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);
            string rutaCompleta = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            return "/img/bancos/" + nombreArchivo;
        }

        #endregion

        #region CRUD

        public async Task<IActionResult> Index()
        {

Console.WriteLine("===== CUENTAS =====");
Console.WriteLine(
    "UsuarioId: " +
    HttpContext.Session.GetInt32("UsuarioId"));

Console.WriteLine(
    "ManicuristaId Session: " +
    HttpContext.Session.GetInt32("ManicuristaId"));

Console.WriteLine("===================");

            var manicuristaId = ObtenerManicuristaId();

            if (manicuristaId == null)
                return RedirectToAction("Login", "Auth");

            var cuentas = await _context.CuentasBancarias
                .Where(c => c.ManicuristaId == manicuristaId)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            return View(cuentas);
        }

        public IActionResult Create()
        {
            if (ObtenerManicuristaId() == null)
                return RedirectToAction("Login", "Auth");

            CargarViewBags();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CuentaBancaria cuenta, IFormFile LogoFile)
        {
            var manicuristaId = ObtenerManicuristaId();

            if (manicuristaId == null)
                return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                cuenta.ManicuristaId = manicuristaId.Value;

                if (LogoFile != null)
                {
                    cuenta.LogoPersonalizado = await GuardarLogo(LogoFile);
                }

                _context.CuentasBancarias.Add(cuenta);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            CargarViewBags();
            return View(cuenta);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var manicuristaId = ObtenerManicuristaId();

            if (manicuristaId == null)
                return RedirectToAction("Login", "Auth");

            var cuenta = await _context.CuentasBancarias
                .FirstOrDefaultAsync(c => c.Id == id && c.ManicuristaId == manicuristaId);

            if (cuenta == null)
                return NotFound();

            CargarViewBags();
            return View(cuenta);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CuentaBancaria cuenta, IFormFile LogoFile)
        {
            var manicuristaId = ObtenerManicuristaId();

            if (manicuristaId == null)
                return RedirectToAction("Login", "Auth");

            var cuentaBD = await _context.CuentasBancarias
                .FirstOrDefaultAsync(c => c.Id == id && c.ManicuristaId == manicuristaId);

            if (cuentaBD == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                cuentaBD.Banco = cuenta.Banco;
                cuentaBD.Titular = cuenta.Titular;
                cuentaBD.NumeroCuenta = cuenta.NumeroCuenta;
                cuentaBD.TipoCuenta = cuenta.TipoCuenta;

                if (LogoFile != null)
                {
                    cuentaBD.LogoPersonalizado = await GuardarLogo(LogoFile);
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            CargarViewBags();
            return View(cuenta);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var manicuristaId = ObtenerManicuristaId();

            if (manicuristaId == null)
                return RedirectToAction("Login", "Auth");

            var cuenta = await _context.CuentasBancarias
                .FirstOrDefaultAsync(c => c.Id == id && c.ManicuristaId == manicuristaId);

            if (cuenta == null)
                return NotFound();

            return View(cuenta);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var manicuristaId = ObtenerManicuristaId();

            if (manicuristaId == null)
                return RedirectToAction("Login", "Auth");

            var cuenta = await _context.CuentasBancarias
                .FirstOrDefaultAsync(c => c.Id == id && c.ManicuristaId == manicuristaId);

            if (cuenta != null)
            {
                _context.CuentasBancarias.Remove(cuenta);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region 👁️ Vista Cliente (CORREGIDA Y SEGURA)

        // 🔥 FIX IMPORTANTE: ya NO recibe parámetros externos
        public async Task<IActionResult> VerCuentas()
{
    var usuarioId =
        HttpContext.Session.GetInt32("UsuarioId");

    var rol =
        HttpContext.Session.GetString("UsuarioRol");

    if (usuarioId == null)
        return RedirectToAction("Login", "Auth");

    int manicuristaRealId;

    // CLIENTA
    if (rol == "Clienta")
{
    var clienta = await _context.Usuarios
        .FirstOrDefaultAsync(u =>
            u.Id == usuarioId.Value);

    if (clienta == null)
        return RedirectToAction("Login", "Auth");

    manicuristaRealId =
        clienta.ManicuristaId ?? 0;
}
    else
    {
        // MANICURISTA
        manicuristaRealId =
            HttpContext.Session.GetInt32("ManicuristaId") ?? 0;
    }

    var cuentas = await _context.CuentasBancarias
        .Where(c =>
            c.ManicuristaId == manicuristaRealId)
        .OrderByDescending(c => c.Id)
        .ToListAsync();

    return View(cuentas);
}

        #endregion
    }
}