using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using TropiNailsPro.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System;

namespace TropiNailsPro.Controllers
{
    public class FeedController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PublicacionService _publicacionService;

        public FeedController(
            AppDbContext context,
            PublicacionService publicacionService)
        {
            _context = context;
            _publicacionService = publicacionService;
        }


        // ============================================
        // 🌴 FEED PRINCIPAL
        // ============================================
        public async Task<IActionResult> Index()
{
    var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

    var publicaciones =
        await _publicacionService.ObtenerTodasAsync();

    if (publicaciones == null)
    {
        publicaciones = new List<Publicacion>();
    }

    // 🔥 obtener lista de usuarios seguidos
    var seguidos = usuarioId != null
        ? await _context.Seguidores
            .Where(s => s.SeguidorId == usuarioId)
            .Select(s => s.SeguidoId)
            .ToListAsync()
        : new List<int>();

    // 🔥 marcar cada publicación con estado correcto
    foreach (var p in publicaciones)
    {
        p.Siguiendo = seguidos.Contains(p.UsuarioId);
    }

    return View(publicaciones);
}

        // ============================================
        // 📄 DETALLE PUBLICACIÓN
        // ============================================
        public async Task<IActionResult> Detalle(int id)
        {
            var publicacion =
                await _context.Publicaciones
                .Include(p => p.Usuario)
                .Include(p => p.Comentarios)
                    .ThenInclude(c => c.Usuario)
                .Include(p => p.Likes)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (publicacion == null)
                return NotFound();

            return View(publicacion);
        }


        // ============================================
        // 🔧 RESPALDO
        // ============================================
        private async Task<List<Publicacion>>
        ObtenerTodasPublicaciones()
        {
            return await _context.Publicaciones
                .Include(p => p.Usuario)
                .Include(p => p.Comentarios)
                    .ThenInclude(c => c.Usuario)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.Fecha)
                .AsNoTracking()
                .ToListAsync();
        }



        // ============================================
        // 🚀 FEED INTELIGENTE
        // ============================================
        [HttpGet]
        public async Task<IActionResult> FeedInteligente()
        {
            var usuarioId =
                HttpContext.Session.GetInt32("UsuarioId");

            if (usuarioId == null)
                return RedirectToAction(
                    "Login",
                    "Auth"
                );

            var hace24h =
                DateTime.Now.AddHours(-24);

            var seguidos =
                await _context.Seguidores
                .Where(s => s.SeguidorId == usuarioId)
                .Select(s => s.SeguidoId)
                .ToListAsync();


            var publicaciones =
                await _context.Publicaciones
                .Include(p => p.Usuario)
                .Include(p => p.Comentarios)
                .Include(p => p.Likes)
                .AsNoTracking()
                .ToListAsync();


            var feed = publicaciones
                .Select(p => new
                {
                    Publicacion = p,

                    score =
                        (seguidos.Contains(
                            p.UsuarioId
                        ) ? 80 : 0)

                        + (p.Likes.Count * 3)

                        + (p.Comentarios.Count * 5)

                        + (p.Likes
                           .Where(l => l.Fecha >= hace24h)
                           .Count() * 4)

                        + (p.Comentarios
                           .Where(c => c.Fecha >= hace24h)
                           .Count() * 6)

                        - ((DateTime.Now - p.Fecha)
                        .TotalHours * 0.5)
                })

                .OrderByDescending(p => p.score)
                .Select(p => p.Publicacion)
                .ToList();


            return View(
                "Index",
                feed
            );
        }



        // ============================================
        // 🔎 BUSCADOR GLOBAL
        // ============================================
        [HttpGet]
        public async Task<IActionResult> BuscarGlobal(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new
                {
                    usuarios = new List<object>(),
                    hashtags = new List<object>()
                });
            }

            q = q.Trim();

            // ✅ CORREGIDO SIN UserName NI Usuario
            var usuarios =
                await _context.Usuarios
                .Where(u =>
                    u.Nombre.Contains(q)
                )
                .Select(u => new
                {
                    id = u.Id,
                    nombre = u.Nombre,
                    usuario = u.Nombre,
                    foto = u.FotoPerfil
                })
                .Take(12)
                .ToListAsync();


            var hashtags =
                new List<object>
                {
                    new { nombre="NailArt"},
                    new { nombre="Acrilicas"},
                    new { nombre="GelX"},
                    new { nombre="TropiNails"}
                };


            return Json(new
            {
                usuarios,
                hashtags
            });
        }




        // ============================================
        // 🔥 TENDENCIAS
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Tendencias()
        {
            var publicaciones =
                await _context.Publicaciones
                .Include(p => p.Usuario)
                .Include(p => p.Likes)
                .Include(p => p.Comentarios)
                .AsNoTracking()
                .OrderByDescending(
                    p => p.Likes.Count +
                         p.Comentarios.Count
                )
                .Take(50)
                .ToListAsync();

            return View(
                "Index",
                publicaciones
            );
        }



        // ============================================
        // # HASHTAG
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Hashtag(string id)
        {
            var publicaciones =
                await _context.Publicaciones
                .Include(p => p.Usuario)
                .Include(p => p.Likes)
                .Include(p => p.Comentarios)
                .OrderByDescending(p => p.Fecha)
                .Take(30)
                .AsNoTracking()
                .ToListAsync();

            return View(
                "Index",
                publicaciones
            );
        }



        // ============================================
        // ✨ RECOMENDADOS
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Recomendados()
        {
            var publicaciones =
                await _context.Publicaciones
                .Include(p => p.Usuario)
                .Include(p => p.Likes)
                .Include(p => p.Comentarios)
                .AsNoTracking()
                .OrderByDescending(
                    p => p.Fecha
                )
                .Take(40)
                .ToListAsync();

            return View(
                "Index",
                publicaciones
            );
        }

    }
}