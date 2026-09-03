using System;
using System.ComponentModel.DataAnnotations;

namespace TropiNailsPro.Models
{
    public class PushSubscription
    {
        public int Id { get; set; }

        // ======================================================
        // USUARIO DUEÑO DE ESTA SUSCRIPCIÓN
        // ======================================================

        [Required]
        public int UsuarioId { get; set; }

        // ======================================================
        // DATOS ENTREGADOS POR EL NAVEGADOR
        // ======================================================

        [Required]
        [MaxLength(2000)]
        public string Endpoint { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string P256dh { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Auth { get; set; } = string.Empty;

        // ======================================================
        // INFORMACIÓN DEL DISPOSITIVO
        // ======================================================

        [MaxLength(50)]
        public string? Plataforma { get; set; }

        [MaxLength(100)]
        public string? Navegador { get; set; }

        [MaxLength(1000)]
        public string? UserAgent { get; set; }

        // ======================================================
        // CONTROL
        // ======================================================

        public bool Activa { get; set; } = true;

        public DateTime FechaRegistro { get; set; }

        public DateTime? UltimoUso { get; set; }

        // ======================================================
        // RELACIÓN CON USUARIO
        // ======================================================

        public virtual Usuario? Usuario { get; set; }
    }
}