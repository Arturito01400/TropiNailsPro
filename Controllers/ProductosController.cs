using Microsoft.AspNetCore.Mvc;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
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
        private readonly string _uploadPath;
        private readonly string _defaultImagen = "uploads/default-product.png";

        public ProductosController(AppDbContext context)
        {
            _context = context;

            // 🔥 crea carpeta automática en wwwroot/uploads
            _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(_uploadPath))
                Directory.CreateDirectory(_uploadPath);

            // 🔹 aseguramos que la imagen default exista
            var defaultPath = Path.Combine(_uploadPath, "default-product.png");
            if (!System.IO.File.Exists(defaultPath))
            {
                using var fs = System.IO.File.Create(defaultPath); // crea un archivo vacío temporal
            }
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
        public IActionResult Create(Producto producto, IFormFile? imagen)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (ModelState.IsValid)
            {
                producto.ManicuristaId = usuarioId!.Value;
                producto.FechaRegistro = DateTime.Now;
                producto.FechaActualizacion = DateTime.Now;

                GuardarImagen(producto, imagen);

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
        public IActionResult Edit(Producto producto, IFormFile? imagen)
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
                existente.FechaActualizacion = DateTime.Now;

                GuardarImagen(existente, imagen);

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

            producto.FechaActualizacion = DateTime.Now;

            _context.SaveChanges();

            TempData["Exito"] = "🛒 Venta registrada y stock actualizado.";
            return RedirectToAction(nameof(Index));
        }

        // ======================================================
        // 🔥 UTILIDAD IMAGEN CORREGIDA
        // ======================================================
        private void GuardarImagen(Producto producto, IFormFile? imagen)
        {
            if (imagen == null || imagen.Length == 0)
                return;

            // Genera un nombre único
            var fileName = Guid.NewGuid() + Path.GetExtension(imagen.FileName);

            // Ruta física en wwwroot/uploads
            var path = Path.Combine(_uploadPath, fileName);

            // Guarda el archivo en disco
            using var stream = new FileStream(path, FileMode.Create);
            imagen.CopyTo(stream);

            // ⚡ Ruta relativa correcta para Razor: "uploads/nombreArchivo.ext"
            producto.ImagenUrl = "uploads/" + fileName;
        }

        // ======================================================
        // 🔹 FUNCIÓN AUXILIAR PARA AJUSTAR RUTA DE IMAGEN
        // ======================================================
        private string AjustarImagen(string? imagenUrl)
        {
            if (string.IsNullOrEmpty(imagenUrl))
                return _defaultImagen;

            var fileName = Path.GetFileName(imagenUrl);
            var rutaFisica = Path.Combine(_uploadPath, fileName);

            if (!System.IO.File.Exists(rutaFisica))
                return _defaultImagen;

            return "uploads/" + fileName;
        }
    }
}