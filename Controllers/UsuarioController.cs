using Microsoft.AspNetCore.Mvc;
using TropiNailsPro.Data;
using TropiNailsPro.Models;

namespace TropiNailsPro.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public UsuarioController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // 🔹 Mostrar perfil
        public IActionResult Perfil(int id = 1)
        {
            var usuario = _context.Usuarios.Find(id);
            if (usuario == null) return NotFound();

            // 🔹 Forzar actualización de la imagen con query string de timestamp
            if (!string.IsNullOrEmpty(usuario.FotoPerfil))
            {
                usuario.FotoPerfil += "?v=" + DateTime.Now.Ticks;
            }

            return View(usuario);
        }

        // 🔹 GET Editar
        public IActionResult Edit(int id = 1)
        {
            var usuario = _context.Usuarios.Find(id);
            return View(usuario);
        }

        // 🔹 POST Editar con imagen
        [HttpPost]
        public async Task<IActionResult> Edit(Usuario usuario, IFormFile? foto)
        {
            if (ModelState.IsValid)
            {
                if (foto != null)
                {
                    string carpeta = Path.Combine(_env.WebRootPath, "uploads/perfiles");
                    string nombreArchivo = Guid.NewGuid() + Path.GetExtension(foto.FileName);
                    string ruta = Path.Combine(carpeta, nombreArchivo);

                    using (var stream = new FileStream(ruta, FileMode.Create))
                    {
                        await foto.CopyToAsync(stream);
                    }

                    // 🔹 Guardar la URL sin query string
                    usuario.FotoPerfil = "/uploads/perfiles/" + nombreArchivo;
                }

                _context.Update(usuario);
                await _context.SaveChangesAsync();

                return RedirectToAction("Perfil", new { id = usuario.Id });
            }

            return View(usuario);
        }
    }
}