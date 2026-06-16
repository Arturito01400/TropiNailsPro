using System.ComponentModel.DataAnnotations;

namespace TropiNailsPro.Models
{
    public class Gasto
    {
        public int Id { get; set; }

        [Required]
        public string Descripcion { get; set; } = string.Empty;

        [Range(0.01, 1000000)]
        public decimal Monto { get; set; }

        public DateTime FechaGasto { get; set; } = DateTime.Now;

        [Required]
        public string Categoria { get; set; } = string.Empty;

        public string? Notas { get; set; }
    }
}
