using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TropiNailsPro.Models
{
    public class Like
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PublicacionId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        // ============================================
        // RELACIÓN PUBLICACIÓN
        // ============================================

        [ForeignKey(nameof(PublicacionId))]
        [InverseProperty(nameof(Publicacion.Likes))]
        public virtual Publicacion Publicacion { get; set; } = null!;

        // ============================================
        // RELACIÓN USUARIO
        // ============================================

        [ForeignKey(nameof(UsuarioId))]
        [InverseProperty(nameof(Usuario.Likes))]
        public virtual Usuario Usuario { get; set; } = null!;

        // ============================================
        // DTO OPCIONAL
        // ============================================

        [NotMapped]
        public string? NombreUsuario { get; set; }

        [NotMapped]
        public string? FotoUsuario { get; set; }
    }
}