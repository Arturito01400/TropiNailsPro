using Microsoft.AspNetCore.Mvc;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using Microsoft.EntityFrameworkCore;

namespace TropiNailsPro.Controllers
{
    public class PagosTransferenciaController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PagosTransferenciaController(
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // =========================================
        // SUBIR COMPROBANTE DE TRANSFERENCIA
        // =========================================

        [HttpPost]
        public async Task<IActionResult> SubirComprobante(
            int CuentaId,
            IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                TempData["Error"] =
                    "Debe seleccionar un comprobante.";

                return RedirectToAction(
                    "VerCuentas",
                    "CuentasBancarias");
            }

            // =========================================
            // OBTENER DATOS CLIENTA
            // =========================================

            var clienteNombre =
                HttpContext.Session.GetString("UsuarioNombre");

            var clienteFoto =
                HttpContext.Session.GetString("UsuarioFoto");

            // =========================================
            // BUSCAR CUENTA
            // =========================================

            var cuenta =
                await _context.CuentasBancarias
                .FirstOrDefaultAsync(c => c.Id == CuentaId);

            if (cuenta == null)
            {
                TempData["Error"] =
                    "Cuenta bancaria no encontrada.";

                return RedirectToAction(
                    "VerCuentas",
                    "CuentasBancarias");
            }

            // =========================================
            // ASEGURAR CARPETA UPLOADS
            // =========================================

            var carpetaUploads =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads"
                );

            if (!Directory.Exists(carpetaUploads))
            {
                Directory.CreateDirectory(carpetaUploads);
            }

            // =========================================
            // CREAR NOMBRE ÚNICO
            // =========================================

            var nombreArchivo =
                Guid.NewGuid().ToString() +
                Path.GetExtension(archivo.FileName);

            var ruta =
                Path.Combine(
                    carpetaUploads,
                    nombreArchivo
                );

            using (var stream =
                new FileStream(ruta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            // =========================================
            // GUARDAR PAGO
            // =========================================

            var pago = new PagoTransferencia
            {
                CuentaBancariaId = CuentaId,

                ImagenComprobante =
                    "/uploads/" + nombreArchivo,

                FechaPago = DateTime.Now,

                ClienteNombre =
                    clienteNombre ?? "Clienta",

                ClienteFoto =
                    string.IsNullOrWhiteSpace(clienteFoto)
                    ? "/img/user-default.png"
                    : clienteFoto,

                ManicuristaId =
                    cuenta.ManicuristaId
            };

            _context.PagosTransferencia.Add(pago);

            await _context.SaveChangesAsync();

            TempData["Exito"] =
                "Comprobante enviado correctamente.";

            return RedirectToAction(
                "VerCuentas",
                "CuentasBancarias");
        }

        // =========================================
        // VER PAGOS FILTRADOS POR CUENTA
        // =========================================

        public async Task<IActionResult> Index(int cuentaId)
        {
            var manicuristaId =
                HttpContext.Session.GetInt32(
                    "ManicuristaId"
                );

            if (manicuristaId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var pagos =
                await _context.PagosTransferencia

                .Include(p => p.CuentaBancaria)

                .Where(p =>
                    p.ManicuristaId ==
                    manicuristaId.Value
                    &&
                    p.CuentaBancariaId == cuentaId)

                .OrderByDescending(p => p.FechaPago)

                .ToListAsync();

            ViewBag.CuentaId = cuentaId;

            return View(pagos);
        }

        // =========================================
        // ELIMINAR VOUCHER
        // =========================================

        [HttpPost]
        public async Task<IActionResult> Eliminar(
            int id)
        {
            var pago =
                await _context.PagosTransferencia
                .FirstOrDefaultAsync(
                    p => p.Id == id);

            if (pago == null)
            {
                TempData["Error"] =
                    "Voucher no encontrado.";

                return RedirectToAction(
                    "Index");
            }

            // =========================================
            // ELIMINAR IMAGEN FÍSICA
            // =========================================

            if (!string.IsNullOrWhiteSpace(
                pago.ImagenComprobante))
            {
                var rutaImagen =
                    Path.Combine(
                        _environment.WebRootPath,
                        pago.ImagenComprobante
                        .TrimStart('/')
                        .Replace("/", "\\")
                    );

                if (System.IO.File.Exists(
                    rutaImagen))
                {
                    System.IO.File.Delete(
                        rutaImagen);
                }
            }

            // =========================================
            // ELIMINAR REGISTRO
            // =========================================

            _context.PagosTransferencia
                .Remove(pago);

            await _context.SaveChangesAsync();

            TempData["Exito"] =
                "Voucher eliminado correctamente.";

            return RedirectToAction(
                "Index",
                new
                {
                    cuentaId =
                    pago.CuentaBancariaId
                });
        }
    }
}