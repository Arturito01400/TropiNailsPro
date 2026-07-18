using Microsoft.AspNetCore.Mvc;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using TropiNailsPro.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;

namespace TropiNailsPro.Controllers
{
    public class ProductosController : Controller
    {
       private readonly AppDbContext _context;
private readonly AzureBlobService _blobService;
private readonly string _defaultImagen = "/img/default-product.png";
private readonly TimeService _timeService;

        public ProductosController(
    AppDbContext context,
    AzureBlobService blobService,
    TimeService timeService)
{
    _context = context;
    _blobService = blobService;
    _timeService = timeService;
}

        // ======================================================
        // 🔐 SEGURIDAD GLOBAL (TU LÓGICA ORIGINAL INTACTA)
        // ======================================================
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var usuarioId = context.HttpContext.Session.GetInt32("UsuarioId");
            var plan = context.HttpContext.Session.GetString("UsuarioPlan");

            if (usuarioId == null)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == usuarioId.Value);
            bool enPrueba = usuario != null && (DateTime.Now - usuario.FechaRegistro).TotalDays <= 15;

            if (!enPrueba && plan != "Premium")
            {
                TempData["Error"] = "El inventario es exclusivo del plan Premium 💎";
                TempData["CerrarApp"] = true;
                context.Result = new RedirectToActionResult("Dashboard", "Dashboard", null);
                return;
            }

            base.OnActionExecuting(context);
        }

        // ======================================================
        // ✅ INDEX MODERNO
        // ======================================================
        public IActionResult Index()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            var productos = _context.Productos
                .Where(p => p.ManicuristaId == usuarioId && p.Activo)
                .OrderByDescending(p => p.FechaRegistro)
                .ToList();

            ViewBag.TotalProductos = productos.Count;
            ViewBag.TotalInvertido = productos.Sum(p => p.TotalCalculado);
            ViewBag.StockBajo = productos.Count(p => p.StockBajo);
            ViewBag.ProductosAgotados = productos.Count(p => p.Cantidad == 0);

            // 🔹 asegurar que la imagen default exista si falta
            foreach (var prod in productos)
            {
                prod.ImagenUrl = AjustarImagen(prod.ImagenUrl);
            }

            return View(productos);
        }

        // ======================================================
        // CREATE
        // ======================================================
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto, IFormFile? imagen)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (ModelState.IsValid)
            {
                producto.ManicuristaId = usuarioId!.Value;
                producto.FechaRegistro = _timeService.ObtenerHoraLocal();
                producto.FechaActualizacion = _timeService.ObtenerHoraLocal();

                await GuardarImagen(producto, imagen);

                _context.Productos.Add(producto);
                _context.SaveChanges();

                TempData["Exito"] = "✅ Producto agregado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            return View(producto);
        }

        // ======================================================
        // EDIT
        // ======================================================
        public IActionResult Edit(int id)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            var producto = _context.Productos
                .FirstOrDefault(p => p.Id == id && p.ManicuristaId == usuarioId);

            if (producto == null)
                return NotFound();

            producto.ImagenUrl = AjustarImagen(producto.ImagenUrl);

            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Producto producto, IFormFile? imagen)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            var existente = _context.Productos
                .FirstOrDefault(p => p.Id == producto.Id && p.ManicuristaId == usuarioId);

            if (existente == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                existente.Nombre = producto.Nombre;
                existente.Descripcion = producto.Descripcion;
                existente.Cantidad = producto.Cantidad;
                existente.PrecioUnitario = producto.PrecioUnitario;
                existente.StockMinimo = producto.StockMinimo;
                existente.Categoria = producto.Categoria;
                existente.CodigoBarras = producto.CodigoBarras;
                existente.VentaAutomatica = producto.VentaAutomatica;
                existente.Activo = producto.Activo;
               existente.FechaActualizacion = _timeService.ObtenerHoraLocal();

               await GuardarImagen(existente, imagen);

                _context.SaveChanges();

                TempData["Exito"] = "✅ Producto actualizado.";
                return RedirectToAction(nameof(Index));
            }

            return View(producto);
        }

        // ======================================================
        // DELETE
        // ======================================================
        public IActionResult Delete(int id)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            var producto = _context.Productos
                .FirstOrDefault(p => p.Id == id && p.ManicuristaId == usuarioId);

            if (producto == null)
                return NotFound();

            producto.ImagenUrl = AjustarImagen(producto.ImagenUrl);

            return View(producto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            var producto = _context.Productos
                .FirstOrDefault(p => p.Id == id && p.ManicuristaId == usuarioId);

            if (producto == null)
                return NotFound();

            _context.Productos.Remove(producto);
            _context.SaveChanges();

            TempData["Exito"] = "🗑️ Producto eliminado.";
            return RedirectToAction(nameof(Index));
        }

        // ======================================================
        // 🔥 NUEVO → VENTA AUTOMÁTICA (STOCK INTELIGENTE)
        // ======================================================
        [HttpPost]
        public IActionResult RegistrarVenta(int productoId, int cantidad)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            var producto = _context.Productos
                .FirstOrDefault(p => p.Id == productoId && p.ManicuristaId == usuarioId);

            if (producto == null)
                return NotFound();

            if (!producto.PermitirStockNegativo && producto.Cantidad < cantidad)
            {
                TempData["Error"] = "❌ No hay stock suficiente.";
                return RedirectToAction(nameof(Index));
            }

            if (producto.VentaAutomatica)
                producto.Cantidad -= cantidad;

           producto.FechaActualizacion = _timeService.ObtenerHoraLocal();

            _context.SaveChanges();

            TempData["Exito"] = "🛒 Venta registrada y stock actualizado.";
            return RedirectToAction(nameof(Index));
        }

        // ======================================================
        // 🔥 UTILIDAD IMAGEN CORREGIDA
        // ======================================================
        private async Task GuardarImagen(
    Producto producto,
    IFormFile? imagen)
{
    if (imagen == null || imagen.Length == 0)
        return;

    var extension =
        Path.GetExtension(imagen.FileName)
        .ToLower();

    var nombreArchivo =
        Guid.NewGuid().ToString()
        + extension;

    using var stream =
        imagen.OpenReadStream();

    string urlAzure =
        await _blobService.SubirArchivoCarpetaAsync(
            stream,
            $"productos/{producto.ManicuristaId}",
            nombreArchivo,
            imagen.ContentType
        );

    producto.ImagenUrl = urlAzure;
}

        // ======================================================
        // 🔹 FUNCIÓN AUXILIAR PARA AJUSTAR RUTA DE IMAGEN
        // ======================================================
        private string AjustarImagen(string? imagenUrl)
{
    if (string.IsNullOrWhiteSpace(imagenUrl))
        return _defaultImagen;

    var path = imagenUrl.Replace("\\", "/").Trim();

    if (path.ToLower() == "null" ||
        path.ToLower() == "undefined")
        return _defaultImagen;


    // Si ya viene desde Azure Blob
    if (path.StartsWith("http://") ||
        path.StartsWith("https://"))
    {
        return path;
    }


    // Compatibilidad con imágenes antiguas locales
    if (!path.StartsWith("/"))
        path = "/" + path;


    return path;
}
    }
}