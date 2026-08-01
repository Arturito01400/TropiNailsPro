using System.ComponentModel.DataAnnotations;

namespace TropiNailsPro.Models
{
    public class GastoPersonal
    {
        public int Id { get; set; }


        [Required]
        public string Descripcion { get; set; } = string.Empty;


        [Range(0.01, 1000000)]
        public decimal Monto { get; set; }


        [Required]
        public string Categoria { get; set; } = string.Empty;


        public DateTime FechaGasto { get; set; } = DateTime.Now;


        public string? Notas { get; set; }



        // Relación con usuario/manicurista
        public int ManicuristaId { get; set; }


        public Manicurista? Manicurista { get; set; }
    }
}