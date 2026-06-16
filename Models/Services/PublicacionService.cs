using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;

namespace TropiNailsPro.Services
{
    public class PublicacionService
    {
        private readonly AppDbContext _context;

        public PublicacionService(AppDbContext context)
        {
            _context = context;
        }

        // ============================================
        // OBTENER TODAS LAS PUBLICACIONES DE UN USUARIO
        // ============================================
        public async Task<List<Publicacion>> ObtenerPorUsuarioAsync(int usuarioId)
        {
            return await _context.Publicaciones
                .Where(p => p.UsuarioId == usuarioId)
                .Include(p => p.Usuario)
                .Include(p => p.Comentarios)
                    .ThenInclude(c => c.Usuario)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.Fecha)
                .AsNoTracking()
                .ToListAsync();
        }

        // ============================================
        // FEED DEL PERFIL (CATÁLOGO DE UN USUARIO)
        // ============================================
        public async Task<List<Publicacion>> ObtenerFeedPorUsuarioConLikesYComentariosAsync(int usuarioId)
        {
            return await _context.Publicaciones
                .Where(p => p.UsuarioId == usuarioId)
                .Include(p => p.Usuario)
                .Include(p => p.Comentarios)
                    .ThenInclude(c => c.Usuario)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.Fecha)
                .AsNoTracking()
                .ToListAsync();
        }

        // ============================================
        // FEED GLOBAL (PUBLICACIONES DE TODOS)
        // ============================================
        public async Task<List<Publicacion>> ObtenerTodasAsync()
        {
            var publicaciones = await _context.Publicaciones
                .Include(p => p.Usuario)
                .Include(p => p.Comentarios)
                    .ThenInclude(c => c.Usuario)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.Fecha)
                .AsNoTracking()
                .ToListAsync();

            // 🔹 Protección extra para evitar null
            if (publicaciones == null)
                publicaciones = new List<Publicacion>();

            return publicaciones;
        }

        // ============================================
        // OBTENER PUBLICACIÓN POR ID
        // ============================================
        public async Task<Publicacion> ObtenerPorIdAsync(int publicacionId)
        {
            return await _context.Publicaciones
                .Include(p => p.Usuario)
                .Include(p => p.Comentarios)
                    .ThenInclude(c => c.Usuario)
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(p => p.Id == publicacionId);
        }

        // ============================================
        // CREAR NUEVA PUBLICACIÓN
        // ============================================
        public async Task<Publicacion> CrearPublicacionAsync(Publicacion publicacion)
        {
            _context.Publicaciones.Add(publicacion);
            await _context.SaveChangesAsync();
            return publicacion;
        }

        // ============================================
        // ELIMINAR PUBLICACIÓN
        // ============================================
        public async Task<bool> EliminarPublicacionAsync(int publicacionId)
        {
            var publicacion = await _context.Publicaciones.FindAsync(publicacionId);

            if (publicacion == null)
                return false;

            _context.Publicaciones.Remove(publicacion);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}