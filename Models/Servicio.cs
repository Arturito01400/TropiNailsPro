using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TropiNailsPro.Models
{
    public class Servicio
    {
        public int Id { get; set; }

        // =========================================
        // PROFESIONAL DUEÑA DEL SERVICIO
        // =========================================

        public int ManicuristaId { get; set; }

        [ForeignKey(nameof(ManicuristaId))]
        public virtual Manicurista? Manicurista { get; set; }

        // =========================================
        // INFORMACIÓN DEL SERVICIO
        // =========================================

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } = "";

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        // Precio opcional
        [Column(TypeName = "decimal(10,2)")]
        public decimal? Precio { get; set; }

        // Duración opcional
        public int? DuracionMinutos { get; set; }

        // Si está disponible para mostrarse
        public bool Activo { get; set; } = true;
    }
}