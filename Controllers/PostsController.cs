using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;

namespace TropiNailsPro.Controllers
{
    public class PostsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        // 🔥 límites producción
        private const int MAX_SIZE_MB = 5;
        private readonly string[] EXT_PERMITIDAS = [".jpg", ".jpeg", ".png", ".webp"];

        public PostsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ======================================================
        // 🔹 MÉTODO PRIVADO → usuario actual
        // ======================================================
        private async Task<Usuario?> ObtenerUsuarioActual()
        {
            var nombreSesion = HttpContext.Session.GetString("UsuarioNombre");

            if (string.IsNullOrEmpty(nombreSesion))
                return null;

            return await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Nombre == nombreSesion);
        }

        // ======================================================
        // 🔹 MÉTODO PRIVADO → Subir imagen (REUTILIZABLE)
        // ======================================================
        private async Task<string?> GuardarImagen(IFormFile? archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return null;

            // 🔥 Validar tamaño
            if (archivo.Length > MAX_SIZE_MB * 1024 * 1024)
                return null;

            // 🔥 Validar extensión
            var ext = Path.GetExtension(archivo.FileName).ToLower();
            if (!EXT_PERMITIDAS.Contains(ext))
                return null;

            string carpeta = Path.Combine(_env.WebRootPath, "uploads", "posts");

            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            string nombreArchivo = Guid.NewGuid() + ext;
            string rutaCompleta = Path.Combine(carpeta, nombreArchivo);

            using var stream = new FileStream(rutaCompleta, FileMode.Create);
            await archivo.CopyToAsync(stream);

            return "/uploads/posts/" + nombreArchivo;
        }

        // ======================================================
        // 🔹 FEED (GALERÍA estilo Instagram)
        // ======================================================
        public async Task<IActionResult> Index()
        {
            var usuario = await ObtenerUsuarioActual();

            if (usuario == null)
                return RedirectToAction("Login", "Auth");

            var posts = await _context.UserPosts
                .Where(p => p.UsuarioId == usuario.Id && p.Activo)
                .OrderByDescending(p => p.FechaCreacion)
                .AsNoTracking()
                .ToListAsync();

            return View(posts);
        }

        // ======================================================
        // 🔹 CREAR
        // ======================================================
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserPost model)
        {
            var usuario = await ObtenerUsuarioActual();

            if (usuario == null)
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(model);

            var ruta = await GuardarImagen(model.FotoArchivo);

            if (ruta == null)
            {
                ModelState.AddModelError("", "Imagen inválida o demasiado grande (máx 5MB).");
                return View(model);
            }

            model.UsuarioId = usuario.Id;
            model.FotoUrl = ruta;
            model.FechaCreacion = DateTime.UtcNow;
            model.Activo = true;

            _context.UserPosts.Add(model);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "✅ Post creado correctamente";

            return RedirectToAction(nameof(Index));
        }

        // ======================================================
        // 🔹 EDITAR
        // ======================================================
        public async Task<IActionResult> Edit(int id)
        {
            var usuario = await ObtenerUsuarioActual();

            if (usuario == null)
                return RedirectToAction("Login", "Auth");

            var post = await _context.UserPosts
                .FirstOrDefaultAsync(p => p.Id == id &&
                                          p.UsuarioId == usuario.Id &&
                                          p.Activo);

            if (post == null)
                return NotFound();

            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserPost model)
        {
            var usuario = await ObtenerUsuarioActual();

            if (usuario == null)
                return RedirectToAction("Login", "Auth");

            var post = await _context.UserPosts
                .FirstOrDefaultAsync(p => p.Id == id &&
                                          p.UsuarioId == usuario.Id &&
                                          p.Activo);

            if (post == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            post.Descripcion = model.Descripcion;

            var nuevaRuta = await GuardarImagen(model.FotoArchivo);
            if (nuevaRuta != null)
                post.FotoUrl = nuevaRuta;

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "✅ Post actualizado correctamente";

            return RedirectToAction(nameof(Index));
        }

        // ======================================================
        // 🔹 ELIMINAR (SOFT DELETE)
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var usuario = await ObtenerUsuarioActual();

            if (usuario == null)
                return RedirectToAction("Login", "Auth");

            var post = await _context.UserPosts
                .FirstOrDefaultAsync(p => p.Id == id &&
                                          p.UsuarioId == usuario.Id &&
                                          p.Activo);

            if (post == null)
                return NotFound();

            post.Activo = false;

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "✅ Post eliminado correctamente";

            return RedirectToAction(nameof(Index));
        }
    }
}
