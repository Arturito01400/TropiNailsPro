using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TropiNailsPro.Models
{
    public class Publicacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public int ManicuristaId { get; set; }

        // 🔥 TEXTO SEGURO
        [StringLength(1000)]
        public string? Texto { get; set; } = string.Empty;

        // 🔥 MEDIA
        public string? MediaUrl { get; set; }

        [NotMapped]
        public string? ImagenUrl
        {
            get => MediaUrl;
            set => MediaUrl = value;
        }

        // 🔥 TIPO SEGURO
        [StringLength(20)]
        public string? TipoMedia { get; set; } = "imagen";

        public DateTime Fecha { get; set; } = DateTime.Now;

        public int? DuracionVideo { get; set; }

        // ==========================================
        // 🔥 RELACIONES SEGURAS
        // ==========================================

        [ForeignKey(nameof(UsuarioId))]
        public virtual Usuario? Usuario { get; set; }

        [ForeignKey(nameof(ManicuristaId))]
        public virtual Manicurista? Manicurista { get; set; }

        // ==========================================
        // 🔥 COLECCIONES SEGURAS
        // ==========================================

        [InverseProperty(nameof(Comentario.Publicacion))]
        public virtual ICollection<Comentario> Comentarios
            { get; set; }
            = new List<Comentario>();

        [InverseProperty(nameof(Like.Publicacion))]
        public virtual ICollection<Like> Likes
            { get; set; }
            = new List<Like>();

        // ==========================================
        // 🔥 SEGURIDAD EXTRA
        // ==========================================

        [NotMapped]
        public Usuario? UsuarioSafe => Usuario;

        [NotMapped]
        public Manicurista? ManicuristaSafe => Manicurista;

        // MINIATURA SEGURA
        [NotMapped]
        public string Miniatura
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(TipoMedia)
                    &&
                    TipoMedia.ToLower().Contains("video"))
                {
                    return "/img/video-placeholder.png";
                }

                return MediaUrl
                    ?? "/img/user-default.png";
            }
        }

        [NotMapped]
public bool Siguiendo { get; set; }
    }
}