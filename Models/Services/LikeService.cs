using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;

namespace TropiNailsPro.Services
{
    public class LikeService
    {
        private readonly AppDbContext _context;

        public LikeService(AppDbContext context)
        {
            _context = context;
        }

        // Agregar un like
        public async Task<Like> CrearLikeAsync(Like like)
        {
            _context.Likes.Add(like);
            await _context.SaveChangesAsync();
            return like;
        }

        // Quitar un like
        public async Task<bool> EliminarLikeAsync(int likeId)
        {
            var like = await _context.Likes.FindAsync(likeId);
            if (like == null)
                return false;

            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();
            return true;
        }

        // Verificar si un usuario ya le dio like a una publicación
        public async Task<bool> ExisteLikeAsync(int usuarioId, int publicacionId)
        {
            return await _context.Likes
                .AnyAsync(l => l.UsuarioId == usuarioId && l.PublicacionId == publicacionId);
        }
    }
}