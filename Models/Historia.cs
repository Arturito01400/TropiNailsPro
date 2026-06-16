using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TropiNailsPro.Models
{
    public class Historia
    {
        [Key]
        public int Id { get; set; }

        // ID de la manicurista que sube la historia
        [Required]
        public int ManicuristaId { get; set; }

        [ForeignKey("ManicuristaId")]
        public virtual Manicurista? Manicurista { get; set; }

        // Tipo de archivo (Imagen o Video)
        [Required]
        public TipoArchivo Tipo { get; set; }

        // Ruta del archivo guardado en /uploads/historias
        [Required]
        [StringLength(255)]
        public string RutaArchivo { get; set; } = string.Empty;

        // Fecha en que se sube
        public DateTime FechaSubida { get; set; } = DateTime.Now;

        // Getter adicional para compatibilidad con RedSocialController
        [NotMapped]
        public DateTime Fecha => FechaSubida;

        // Las historias expiran en 24 horas (tipo Instagram)
        public DateTime FechaExpira { get; set; } = DateTime.Now.AddHours(24);
    }
}