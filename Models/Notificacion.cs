using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TropiNailsPro.Models
{
    public class Notificacion
    {
        public int Id { get; set; }

        // ==========================================
        // RELACIÓN CON LA MANICURISTA
        // ==========================================

        [Required]
        public int ManicuristaId { get; set; }

        // ==========================================
        // MENSAJE DE LA NOTIFICACIÓN
        // ==========================================

        [Required(ErrorMessage = "El mensaje es obligatorio")]
        [StringLength(250)]
        [Display(Name = "Mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        // ==========================================
        // TIPO DE NOTIFICACIÓN
        // ==========================================

        [StringLength(50)]
        [Display(Name = "Tipo")]
        public string? Tipo { get; set; }

        // ==========================================
        // URL PARA REDIRECCIONAR
        // ==========================================

        [StringLength(200)]
        [Display(Name = "Url")]
        public string? Url { get; set; }

        // ==========================================
        // ESTADO DE LECTURA
        // ==========================================

        [Display(Name = "Leída")]
        public bool Leida { get; set; } = false;

        // ==========================================
        // FECHA DE CREACIÓN
        // ==========================================

        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; } = DateTime.Now;

        // ==========================================
        // RELACIÓN OPCIONAL
        // ==========================================

        [ForeignKey("ManicuristaId")]
        public Manicurista? Manicurista { get; set; }
    }
}
