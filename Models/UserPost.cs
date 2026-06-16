using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace TropiNailsPro.Models
{
    // =====================================================
    // ⭐ UserPost
    // Representa cada foto publicada por el usuario
    // (Galería estilo Instagram / WhatsApp Status)
    // =====================================================
    public class UserPost
    {
        // 🔹 ID principal
        [Key]
        public int Id { get; set; }


        // =====================================================
        // 🔹 RELACIÓN CON USUARIO
        // =====================================================

        [Required]
        [Display(Name = "Usuario")]
        public int UsuarioId { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public Usuario Usuario { get; set; } = null!;


        // =====================================================
        // 🔹 CONTENIDO
        // =====================================================

        [StringLength(200)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }


        // =====================================================
        // 🔹 IMAGEN
        // =====================================================

        // Ruta guardada en BD
        [Required]
        [StringLength(255)]
        public string FotoUrl { get; set; } = string.Empty;


        // Archivo temporal (solo formulario)
        [NotMapped]
        public IFormFile? FotoArchivo { get; set; }


        // =====================================================
        // 🔹 METADATOS (PRODUCCIÓN)
        // =====================================================

        // Fecha de creación automática
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;


        // Para soft-delete (no borrar físicamente)
        public bool Activo { get; set; } = true;


        // ⭐ NUEVO → likes futuros / métricas
        public int Likes { get; set; } = 0;


        // ⭐ NUEVO → orden visual (más reciente primero)
        [NotMapped]
        public string FechaFormateada => FechaCreacion.ToString("dd/MM/yyyy HH:mm");
    }
}
