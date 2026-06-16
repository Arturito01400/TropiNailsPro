using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;

namespace TropiNailsPro.Hubs
{
    public class NotificationHub : Hub
    {
        // ==========================================
        // CUANDO EL USUARIO SE CONECTA
        // ==========================================
        public override async Task OnConnectedAsync()
        {
            try
            {
                var httpContext = Context.GetHttpContext();

                if (httpContext != null)
                {
                    var manicuristaId = httpContext.Session.GetInt32("UsuarioId");

                    if (manicuristaId.HasValue)
                    {
                        var grupo = $"manicurista-{manicuristaId.Value}";

                        await Groups.AddToGroupAsync(Context.ConnectionId, grupo);

                        Console.WriteLine($"✅ Usuario conectado al grupo: {grupo}");
                    }
                    else
                    {
                        Console.WriteLine("⚠️ UsuarioId es NULL → el usuario NO entró al grupo");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ HttpContext es NULL en SignalR");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error en OnConnectedAsync: " + ex.Message);
            }

            await base.OnConnectedAsync();
        }

        // ==========================================
        // CUANDO SE DESCONECTA
        // ==========================================
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var httpContext = Context.GetHttpContext();

                if (httpContext != null)
                {
                    var manicuristaId = httpContext.Session.GetInt32("UsuarioId");

                    if (manicuristaId.HasValue)
                    {
                        var grupo = $"manicurista-{manicuristaId.Value}";

                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, grupo);

                        Console.WriteLine($"❌ Usuario salió del grupo: {grupo}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error en OnDisconnectedAsync: " + ex.Message);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ==========================================
        // 🔥 ENVIAR A UN MANICURISTA (GRUPO)
        // ==========================================
        public async Task EnviarANoticacionManicurista(int manicuristaId, string mensaje, string? url = null)
        {
            var grupo = $"manicurista-{manicuristaId}";

            await Clients.Group(grupo)
                .SendAsync("RecibirNotificacion", mensaje, url);

            await Clients.Group(grupo)
                .SendAsync("ActualizarContador", 1);
        }

        // ==========================================
        // ENVIAR A TODOS (OPCIONAL)
        // ==========================================
        public async Task SendNotificationToAll(string mensaje, string? url = null)
        {
            await Clients.All.SendAsync("RecibirNotificacion", mensaje, url);
        }
    }
}