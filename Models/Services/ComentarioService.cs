using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;

namespace TropiNailsPro.Services
{
    public class ComentarioService
    {
        private readonly AppDbContext _context;

        public ComentarioService(AppDbContext context)
        {
            _context = context;
        }

        // Obtener comentarios de una publicación específica
        public async Task<List<Comentario>> ObtenerPorPublicacionAsync(int publicacionId)
        {
            return await _context.Comentarios
                .Where(c => c.PublicacionId == publicacionId)
                .Include(c => c.Usuario)
                .OrderBy(c => c.Fecha)
                .ToListAsync();
        }

        // Crear un nuevo comentario
        public async Task<Comentario> CrearComentarioAsync(Comentario comentario)
        {
            _context.Comentarios.Add(comentario);
            await _context.SaveChangesAsync();
            return comentario;
        }

        // Eliminar un comentario por Id
        public async Task<bool> EliminarComentarioAsync(int comentarioId)
        {
            var comentario = await _context.Comentarios.FindAsync(comentarioId);
            if (comentario == null)
                return false;

            _context.Comentarios.Remove(comentario);
            await _context.SaveChangesAsync();
            return true;
        }

        // Obtener un comentario por Id
        public async Task<Comentario> ObtenerPorIdAsync(int comentarioId)
        {
            return await _context.Comentarios
                .Include(c => c.Usuario)
                .Include(c => c.Publicacion)
                .FirstOrDefaultAsync(c => c.Id == comentarioId);
        }
    }
}