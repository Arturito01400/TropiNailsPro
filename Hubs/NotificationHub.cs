using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

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
                    // 🔥 USAMOS EL ID REAL DE LA MANICURISTA
                    var manicuristaId =
                        httpContext.Session.GetInt32("ManicuristaId");

                    if (manicuristaId.HasValue &&
                        manicuristaId.Value > 0)
                    {
                        var grupo =
                            $"manicurista-{manicuristaId.Value}";

                        await Groups.AddToGroupAsync(
                            Context.ConnectionId,
                            grupo);

                        Console.WriteLine(
                            $"✅ SignalR conectado al grupo: {grupo}");
                    }
                    else
                    {
                        Console.WriteLine(
                            "⚠️ ManicuristaId es NULL o 0 → no se agregó al grupo SignalR.");
                    }
                }
                else
                {
                    Console.WriteLine(
                        "⚠️ HttpContext es NULL en SignalR.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "❌ Error en OnConnectedAsync: " +
                    ex.Message);
            }

            await base.OnConnectedAsync();
        }

        // ==========================================
        // CUANDO SE DESCONECTA
        // ==========================================
        public override async Task OnDisconnectedAsync(
            Exception? exception)
        {
            try
            {
                var httpContext = Context.GetHttpContext();

                if (httpContext != null)
                {
                    var manicuristaId =
                        httpContext.Session.GetInt32("ManicuristaId");

                    if (manicuristaId.HasValue &&
                        manicuristaId.Value > 0)
                    {
                        var grupo =
                            $"manicurista-{manicuristaId.Value}";

                        await Groups.RemoveFromGroupAsync(
                            Context.ConnectionId,
                            grupo);

                        Console.WriteLine(
                            $"❌ SignalR desconectado del grupo: {grupo}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "❌ Error en OnDisconnectedAsync: " +
                    ex.Message);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ==========================================
        // 🔥 ENVIAR A UNA MANICURISTA
        // ==========================================
        public async Task EnviarANoticacionManicurista(
            int manicuristaId,
            string mensaje,
            string? url = null)
        {
            if (manicuristaId <= 0)
                return;

            var grupo =
                $"manicurista-{manicuristaId}";

            await Clients.Group(grupo)
                .SendAsync(
                    "RecibirNotificacion",
                    mensaje,
                    url);

            await Clients.Group(grupo)
                .SendAsync(
                    "ActualizarContador",
                    1);
        }

        // ==========================================
        // 🔥 ENVIAR A TODOS
        // ==========================================
        public async Task SendNotificationToAll(
            string mensaje,
            string? url = null)
        {
            await Clients.All.SendAsync(
                "RecibirNotificacion",
                mensaje,
                url);
        }
    }
}