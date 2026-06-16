using System.ComponentModel.DataAnnotations;

namespace TropiNailsPro.Models
{
    public class Cliente
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Nombre { get; set; } = "";

        [Phone]
        public string Telefono { get; set; } = "";

        [EmailAddress]
        public string? Email { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // 🔥 RELACIÓN CON MANICURISTA
        public int ManicuristaId { get; set; }
        public Manicurista Manicurista { get; set; } = null!;
    }
}