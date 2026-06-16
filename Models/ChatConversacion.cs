using System;

namespace TropiNailsPro.Models
{
    // 💡 Este modelo es SOLO para mostrar conversaciones en el dashboard
    // NO es una tabla de base de datos
    public class ChatConversacion
    {
        // Usuario con quien se tiene la conversación
        public string Usuario { get; set; } = string.Empty;

        // Último mensaje de la conversación
        public string UltimoMensaje { get; set; } = string.Empty;

        // Fecha del último mensaje
        public DateTime Fecha { get; set; }

        // 🔥 Cantidad de mensajes no leídos
        public int NoLeidos { get; set; } = 0;

        // 🔥 Tipo del último mensaje (texto, imagen, audio, etc.)
        public string Tipo { get; set; } = "texto";
    }
}