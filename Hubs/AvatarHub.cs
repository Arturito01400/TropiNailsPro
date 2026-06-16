using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace TropiNailsPro.Hubs
{
    public class AvatarHub : Hub
    {
        // 🔹 Este método se puede llamar desde el cliente para forzar actualización de avatar
        public async Task ActualizarAvatar(string usuario, string fotoUrl)
        {
            // 🔹 Enviar a todos los clientes conectados la actualización del avatar
            await Clients.All.SendAsync("RecibirAvatar", usuario, fotoUrl);
        }

        // 🔹 Opcional: notificar cuando un usuario se conecta
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            // Puedes enviar algo al cliente que se conectó si quieres
        }

        // 🔹 Opcional: notificar cuando un usuario se desconecta
        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
            // Aquí podrías manejar estado de usuarios offline si quieres
        }
    }
}