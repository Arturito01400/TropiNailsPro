using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using System.Linq;
using System.Threading.Tasks;

namespace TropiNailsPro.Controllers
{
    public class LikesController : Controller
    {
        private readonly AppDbContext _context;

        public LikesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Ver(int id)
        {
            var likes = await _context.Likes
                .AsNoTracking()

                // 🔥 CARGAR USUARIO REAL
                .Include(l => l.Usuario)

                .Where(l => l.PublicacionId == id)
                .OrderByDescending(l => l.Fecha)
                .ToListAsync();

            return View(
                "~/Views/Publicaciones/VerLikes.cshtml",
                likes);
        }
    }
}