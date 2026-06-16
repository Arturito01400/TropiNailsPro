using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TropiNailsPro.Models
{
    public class Catalogo
    {
        [Key]
        public int Id { get; set; }

        // ID de la manicurista propietaria del catálogo
        [Required]
        public int ManicuristaId { get; set; }

        [ForeignKey("ManicuristaId")]
        public virtual Manicurista? Manicurista { get; set; }

        // Tipo de archivo (Imagen o Video)
        [Required]
        public TipoArchivo Tipo { get; set; }

        // Ruta del archivo guardado en /uploads/catalogos
        [Required]
        [StringLength(255)]
        public string RutaArchivo { get; set; } = string.Empty;

        // Fecha en que se sube al catálogo
        public DateTime FechaSubida { get; set; } = DateTime.Now;

        // Getter adicional para compatibilidad con RedSocialController
        [NotMapped]
        public DateTime Fecha => FechaSubida;
    }
}