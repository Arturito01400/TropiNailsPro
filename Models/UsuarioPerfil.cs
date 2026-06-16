using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace TropiNailsPro.Models
{
    // 🔥 IMPORTANTE:
    // ESTA CLASE AHORA FUNCIONA COMO
    // MODELO AUXILIAR / COMPATIBILIDAD
    // Y NO COMO ENTIDAD REAL DE EF CORE
    [NotMapped]
    public class UsuarioPerfil
    {
        [Key]
        public int Id { get; set; }

        // 🔥 RELACIÓN REAL CON USUARIO
        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario? Usuario { get; set; }

        // =========================================
        // 🔥 DATOS DERIVADOS (NO SE GUARDAN)
        // =========================================

        [NotMapped]
        public string UsuarioLogin => Usuario?.Nombre ?? "";

        [NotMapped]
        public string NombreCompleto
        {
            get => Usuario?.Nombre ?? "";
            set
            {
                if (Usuario != null)
                    Usuario.Nombre = value;
            }
        }

        [NotMapped]
        public string Telefono
        {
            get => Usuario?.Telefono ?? "";
            set
            {
                if (Usuario != null)
                    Usuario.Telefono = value;
            }
        }

        [NotMapped]
        public string Email => Usuario?.Email ?? "";

        // =========================================
        // PASSWORD (SOLO PARA VISTAS)
        // =========================================

        [NotMapped]
        public string? Password { get; set; }

        // =========================================
        // FOTO
        // =========================================

        public string FotoUrl { get; set; } = "/img/user-default.png";

        // FIX COMPATIBILIDAD
        [NotMapped]
        public string FotoPerfil
        {
            get => FotoUrl;
            set => FotoUrl = value;
        }

        [NotMapped]
        public IFormFile? FotoArchivo { get; set; }

        [NotMapped]
        public string FotoPerfilComputed
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FotoUrl))
                    return "/img/user-default.png";

                var path = FotoUrl.Replace("\\", "/").Trim();

                if (path.ToLower() == "null" || path.ToLower() == "undefined")
                    return "/img/user-default.png";

                if (path.Contains("C:/") || path.Contains("D:/"))
                    return "/img/user-default.png";

                if (!path.StartsWith("/"))
                    path = "/" + path;

                return path;
            }
        }

        public string ObtenerFoto()
        {
            return FotoPerfilComputed;
        }

        // =========================================
        // REDES
        // =========================================

        public string? Instagram { get; set; }

        public string? TikTok { get; set; }

        public string? Facebook { get; set; }

        public string? WhatsApp { get; set; }

        // =========================================
        // ESTADÍSTICAS
        // =========================================

        public int Citas { get; set; }

        public int Clientes { get; set; }

        public int Servicios { get; set; }

        public bool Activo { get; set; } = true;

        // =========================================
        // RELACIONES
        // =========================================

        public virtual ICollection<Publicacion> Publicaciones { get; set; }
            = new List<Publicacion>();

        public virtual ICollection<Comentario> Comentarios { get; set; }
            = new List<Comentario>();

        public virtual ICollection<Like> Likes { get; set; }
            = new List<Like>();
    }
}