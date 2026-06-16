using TropiNailsPro.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TropiNailsPro.Services
{
    public class SuscripcionService
    {
        private readonly AppDbContext _context;

        public SuscripcionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task VerificarEstadosAsync()
        {
            var ahora = DateTime.UtcNow;

            var suscripcionesVencidas = await _context.Suscripciones
                .Where(s => s.FechaVencimiento < ahora && s.Activa && !s.Cancelada)
                .ToListAsync();

            if (suscripcionesVencidas.Count == 0)
                return;

            foreach (var s in suscripcionesVencidas)
            {
                s.Activa = false;
                s.EstadoPago = "VENCIDA"; // 🔥 recomendado
            }

            await _context.SaveChangesAsync();
        }
    }
}