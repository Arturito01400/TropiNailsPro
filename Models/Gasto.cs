using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TropiNailsPro.Models
{
    public class Gasto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(150)]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto es obligatorio.")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 1000000, ErrorMessage = "El monto debe ser mayor que cero.")]
        public decimal Monto { get; set; }

        [Required]
        public DateTime FechaGasto { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "La categoría es obligatoria.")]
        [StringLength(100)]
        public string Categoria { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notas { get; set; }

        // Manicurista propietaria del gasto
        public int ManicuristaId { get; set; }

        // Relación con la manicurista
        public Manicurista? Manicurista { get; set; }
    }
}