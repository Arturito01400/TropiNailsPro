using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace TropiNailsPro.Hubs
{
    public class CallHub : Hub
    {
        // ==============================
        // INICIAR LLAMADA (OFFER)
        // ==============================
        public async Task LlamarUsuario(string usuarioDestino, string signal)
        {
            var usuarioOrigen = Context.User?.Identity?.Name;

            if (string.IsNullOrEmpty(usuarioDestino) || string.IsNullOrEmpty(usuarioOrigen))
                return;

            await Clients.User(usuarioDestino)
                .SendAsync("RecibirLlamada", usuarioOrigen, signal);
        }

        // ==============================
        // RESPONDER LLAMADA (ANSWER)
        // ==============================
        public async Task EnviarRespuesta(string usuarioDestino, string signal)
        {
            var usuarioOrigen = Context.User?.Identity?.Name;

            if (string.IsNullOrEmpty(usuarioDestino) || string.IsNullOrEmpty(usuarioOrigen))
                return;

            await Clients.User(usuarioDestino)
                .SendAsync("RespuestaLlamada", usuarioOrigen, signal);
        }

        // ==============================
        // ICE CANDIDATES (CLAVE PARA AUDIO)
        // ==============================
        public async Task EnviarIceCandidate(string usuarioDestino, string candidate)
        {
            var usuarioOrigen = Context.User?.Identity?.Name;

            if (string.IsNullOrEmpty(usuarioDestino) || string.IsNullOrEmpty(usuarioOrigen))
                return;

            await Clients.User(usuarioDestino)
                .SendAsync("RecibirIceCandidate", usuarioOrigen, candidate);
        }

        // ==============================
        // COLGAR LLAMADA
        // ==============================
        public async Task ColgarLlamada(string usuarioDestino)
        {
            var usuarioOrigen = Context.User?.Identity?.Name;

            if (string.IsNullOrEmpty(usuarioDestino) || string.IsNullOrEmpty(usuarioOrigen))
                return;

            await Clients.User(usuarioDestino)
                .SendAsync("LlamadaFinalizada", usuarioOrigen);
        }

        // ==============================
        // DEBUG / CONEXIÓN
        // ==============================
        public override async Task OnConnectedAsync()
        {
            var usuario = Context.User?.Identity?.Name;

            if (!string.IsNullOrEmpty(usuario))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, usuario);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            var usuario = Context.User?.Identity?.Name;

            if (!string.IsNullOrEmpty(usuario))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, usuario);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}