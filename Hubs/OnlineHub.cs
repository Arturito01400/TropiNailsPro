using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using TropiNailsPro.Data;
using Microsoft.EntityFrameworkCore;

namespace TropiNailsPro.Hubs
{
    public class OnlineHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> _usuariosOnline = new();
        private readonly AppDbContext _context;

        public OnlineHub(AppDbContext context)
        {
            _context = context;
        }

        // =========================================
        // CONEXION
        // =========================================
        public override async Task OnConnectedAsync()
        {
            var http = Context.GetHttpContext();
            var nombre = http?.Request.Query["usuario"].ToString();

            if (string.IsNullOrEmpty(nombre))
            {
                await base.OnConnectedAsync();
                return;
            }

            _usuariosOnline.AddOrUpdate(
                nombre,
                Context.ConnectionId,
                (k, v) => Context.ConnectionId
            );

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Nombre == nombre);

            if (usuario != null)
            {
                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    $"manicurista-{usuario.Id}"
                );

                await EnviarDatos(usuario.Id);

                // Estado online
                await Clients.Group($"manicurista-{usuario.Id}")
                    .SendAsync("ManicuristaOnline");

                await NotificarSistema(
                    usuario.Id,
                    "info",
                    "Sistema",
                    "Conectado en tiempo real ✅"
                );
            }

            await base.OnConnectedAsync();
        }

        // =========================================
        // DESCONECTAR
        // =========================================
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var usuario = _usuariosOnline
                .FirstOrDefault(x => x.Value == Context.ConnectionId);

            if (!string.IsNullOrEmpty(usuario.Key))
            {
                _usuariosOnline.TryRemove(usuario.Key, out _);

                var dbUser = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Nombre == usuario.Key);

                if (dbUser != null)
                {
                    await Clients.Group($"manicurista-{dbUser.Id}")
                        .SendAsync("ManicuristaOffline");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // =========================================
        // NOTIFICACIONES
        // =========================================
        public async Task NotificarSistema(
            int manicuristaId,
            string tipo,
            string clienta,
            string extra
        )
        {
            await Clients.Group($"manicurista-{manicuristaId}")
                .SendAsync("MensajeSistema", new
                {
                    tipo,
                    clienta,
                    extra
                });
        }

        // =========================================
        private async Task EnviarDatos(int manicuristaId)
        {
            await ActualizarTodo(manicuristaId);
        }

        // =========================================
        // DASHBOARD DATA
        // =========================================
        public async Task ActualizarTodo(int manicuristaId)
        {
            var hoy = DateTime.Today;
            var manana = hoy.AddDays(1);

            var citas = await _context.Citas
                .Where(c => c.ManicuristaId == manicuristaId)
                .ToListAsync();

            var citasHoy = citas
                .Where(c => c.Fecha.Date == hoy)
                .OrderBy(c => c.PosicionFila)
                .Select(c => new
                {
                    id = c.Id,
                    nombre = c.NombreClienta,
                    hora = c.Hora,
                    servicio = c.Servicio,
                    posicion = c.PosicionFila,
                    estado = c.Estado
                })
                .ToList();

            var citasManana = citas
                .Where(c => c.Fecha.Date == manana)
                .OrderBy(c => c.PosicionFila)
                .Select(c => new
                {
                    id = c.Id,
                    nombre = c.NombreClienta,
                    hora = c.Hora,
                    servicio = c.Servicio,
                    posicion = c.PosicionFila,
                    estado = c.Estado
                })
                .ToList();

            var citasFuturas = citas
                .Where(c => c.Fecha.Date > manana)
                .OrderBy(c => c.Fecha)
                .ThenBy(c => c.PosicionFila)
                .Select(c => new
                {
                    fecha = c.Fecha.ToString("dd/MM/yyyy"),
                    id = c.Id,
                    nombre = c.NombreClienta,
                    hora = c.Hora,
                    servicio = c.Servicio,
                    posicion = c.PosicionFila,
                    estado = c.Estado
                })
                .ToList();

            await Clients.Group($"manicurista-{manicuristaId}")
                .SendAsync("ActualizarCitas", new
                {
                    citasHoy,
                    citasManana,
                    citasFuturas
                });

            await Clients.Group($"manicurista-{manicuristaId}")
                .SendAsync("ActualizarEstadisticas", new
                {
                    citasDiarias = citasHoy.Count,
                    citasMensuales = citas.Count(c => c.Fecha.Month == hoy.Month && c.Fecha.Year == hoy.Year),
                    citasAnuales = citas.Count(c => c.Fecha.Year == hoy.Year)
                });
        }

        // =========================================
        // 🔔 NUEVA NOTIFICACION PARA CAMPANITA
        // =========================================
        public async Task NuevaNotificacion(
            int manicuristaId,
            string mensaje
        )
        {
            await Clients.Group($"manicurista-{manicuristaId}")
                .SendAsync("NuevaNotificacion", new
                {
                    mensaje = mensaje,
                    fecha = DateTime.Now.ToString("HH:mm")
                });
        }

        // =========================================
        // 🔔 ACTUALIZAR CONTADOR DE NOTIFICACIONES
        // =========================================
        public async Task ActualizarContadorNotificaciones(
            int manicuristaId,
            int total
        )
        {
            await Clients.Group($"manicurista-{manicuristaId}")
                .SendAsync("ActualizarContadorNotificaciones", total);
        }
    }
}
