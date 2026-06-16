using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace TropiNailsPro.Models
{
    public class Usuario : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [Phone]
        [StringLength(20)]
        public string? Telefono { get; set; }

        [Required]
[StringLength(500)]
public string Clave { get; set; } = string.Empty;

        public string? ResetToken { get; set; }
        public DateTime? TokenExpira { get; set; }

        [StringLength(6)]
        public string? CodigoSMS { get; set; }
        public DateTime? CodigoSMSExpira { get; set; }

        [StringLength(50)]
        public string? UsuarioLogin { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [Required, StringLength(50)]
        public string Rol { get; set; } = "Clienta";

        public bool CorreoConfirmado { get; set; } = false;
        public bool Activo { get; set; } = true;

        [StringLength(255)]
        public string? FotoPerfil { get; set; }

        [NotMapped]
        public IFormFile? FotoPerfilArchivo { get; set; }

        [NotMapped]
        public string FotoPerfilUrl
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FotoPerfil))
                    return "/img/user-default.png";

                var path = FotoPerfil.Replace("\\", "/").Trim();

                if (path.ToLower() == "null" || path.ToLower() == "undefined")
                    return "/img/user-default.png";

                if (!path.StartsWith("/"))
                    path = "/" + path;

                if (!path.Contains("/uploads/") && !path.Contains("/img/"))
                    return "/img/user-default.png";

                return path;
            }
        }

        [StringLength(255)]
        public string? Instagram { get; set; }

        [StringLength(255)]
        public string? TikTok { get; set; }

        [StringLength(255)]
        public string? Facebook { get; set; }

        [StringLength(255)]
        public string? WhatsApp { get; set; }

        // 🔥 RELACIÓN CORRECTA (NO TOCAR MÁS)
        public int? ManicuristaId { get; set; }

        [ForeignKey("ManicuristaId")]
        public Usuario? Manicurista { get; set; }

        // 🔥 CLIENTAS (sin cambio de lógica)
        public virtual ICollection<Usuario> Clientas { get; set; } = new List<Usuario>();

        [StringLength(20)]
        public string Plan { get; set; } = "Basico";

        public bool PlanActivo { get; set; } = false;

        public DateTime? FechaInicioPlan { get; set; }

public DateTime? FechaVencimientoPlan { get; set; }

        [StringLength(10)]
        public string? CodigoReferencia { get; set; }

        public virtual ICollection<Publicacion> Publicaciones { get; set; } = new List<Publicacion>();
        public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
        public virtual ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();

        // 🔥 FIX DEFINITIVO DE SEGUIDORES (CORRECTO PARA EF CORE)

        [InverseProperty(nameof(Seguidor.SeguidoUsuario))]
        public virtual ICollection<Seguidor> Seguidores { get; set; } = new List<Seguidor>();

        [InverseProperty(nameof(Seguidor.SeguidorUsuario))]
        public virtual ICollection<Seguidor> Siguiendo { get; set; } = new List<Seguidor>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(Telefono))
            {
                yield return new ValidationResult(
                    "Debe ingresar al menos un correo o un teléfono.",
                    new[] { nameof(Email), nameof(Telefono) });
            }
        }
    }
}