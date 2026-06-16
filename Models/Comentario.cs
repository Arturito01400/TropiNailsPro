using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TropiNailsPro.Models
{
    public class Comentario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PublicacionId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public string Texto { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.Now;

        // =========================================
        // OPCIONALES
        // =========================================
        public string? StickerUrl { get; set; }

        public int? ComentarioPadreId { get; set; }

        public string? Reaccion { get; set; }

        // =========================================
        // RELACIONES PRINCIPALES
        // =========================================

        [ForeignKey(nameof(PublicacionId))]
        [InverseProperty(nameof(Publicacion.Comentarios))]
        public virtual Publicacion Publicacion { get; set; } = null!;

        [ForeignKey(nameof(UsuarioId))]
        public virtual Usuario Usuario { get; set; } = null!;

        // =========================================
        // AUTO RELACIÓN (CORREGIDA)
        // =========================================

        [ForeignKey(nameof(ComentarioPadreId))]
        [InverseProperty(nameof(Comentario.Respuestas))]
        public virtual Comentario? ComentarioPadre { get; set; }

        [InverseProperty(nameof(ComentarioPadre))]
        public virtual ICollection<Comentario> Respuestas { get; set; }
            = new List<Comentario>();
    }
}