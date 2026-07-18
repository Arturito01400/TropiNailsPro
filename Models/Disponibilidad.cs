using System;
using System.ComponentModel.DataAnnotations;

namespace TropiNailsPro.Models
{
    public class Disponibilidad
    {
        public int Id { get; set; }

        [Required]
        public int ManicuristaId { get; set; }

        public Manicurista? Manicurista { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public TimeSpan Hora { get; set; }

        public bool Disponible { get; set; } = true;

        // 🔥 Nota opcional que verá la clienta al momento de agendar
        [MaxLength(300)]
        public string? Nota { get; set; }

        public DateTime FechaRegistro { get; set; }
    }
}