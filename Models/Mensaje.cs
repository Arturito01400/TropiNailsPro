using System;

namespace TropiNailsPro.Models
{
    public class Mensaje
    {
        public int Id { get; set; }

        // 🔹 ACTUAL (NO LO TOCAMOS)
        public string Remitente { get; set; } = string.Empty;
        public string Destinatario { get; set; } = string.Empty;

        // 🔥 NUEVO (PARA CHAT PRO)
        public int? EmisorId { get; set; }
        public int? ReceptorId { get; set; }

        // mensaje
        public string Contenido { get; set; } = string.Empty;

        // tipo: texto, imagen, audio, archivo
        public string Tipo { get; set; } = "texto";

        // fecha
        public DateTime Fecha { get; set; } = DateTime.Now;

        // leído
        public bool Leido { get; set; } = false;

        // 🔥 ESTADO PRO
        public string Estado { get; set; } = "Enviado"; 
        // Enviado | Recibido | Leido
    }
}