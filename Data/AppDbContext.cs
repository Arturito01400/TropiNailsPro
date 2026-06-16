using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Models;

namespace TropiNailsPro.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // ======================================================
        // TABLAS
        // ======================================================

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cita> Citas { get; set; }
        public DbSet<ModeloUnas> ModelosUnas { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Gasto> Gastos { get; set; }

        // 🔥 RESTAURADO CORRECTAMENTE
        public DbSet<UsuarioPerfil> UsuariosPerfil { get; set; }

        public DbSet<UserPost> UserPosts { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Mensaje> Mensajes { get; set; }
        public DbSet<MensajeEliminadoUsuario> MensajesEliminadosUsuarios { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Manicurista> Manicuristas { get; set; }
        public DbSet<SuscripcionPago> SuscripcionPagos { get; set; }
        public DbSet<Suscripcion> Suscripciones { get; set; }
        public DbSet<Cliente> Clientes { get; set; }

        public DbSet<Historia> Historias { get; set; }
        public DbSet<Catalogo> Catalogos { get; set; }

        public DbSet<CuentaBancaria> CuentasBancarias { get; set; }
        public DbSet<PagoTransferencia> PagosTransferencia { get; set; }

        public DbSet<Notificacion> Notificaciones { get; set; }

        // 🔹 RED SOCIAL
        public DbSet<Publicacion> Publicaciones { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Seguidor> Seguidores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ======================================================
            // MAPEO DE TABLAS
            // ======================================================

            modelBuilder.Entity<Usuario>().ToTable("Usuarios");
            modelBuilder.Entity<Cita>().ToTable("Citas");
            modelBuilder.Entity<ModeloUnas>().ToTable("ModelosUnas");
            modelBuilder.Entity<Pago>().ToTable("Pagos");
            modelBuilder.Entity<Producto>().ToTable("Productos");
            modelBuilder.Entity<Gasto>().ToTable("Gastos");

            // 🔥 RESTAURADO
            modelBuilder.Entity<UsuarioPerfil>().ToTable("UsuariosPerfil");

            modelBuilder.Entity<UserPost>().ToTable("UserPosts");
            modelBuilder.Entity<PasswordResetToken>().ToTable("PasswordResetTokens");
            modelBuilder.Entity<Mensaje>().ToTable("Mensajes");
            modelBuilder.Entity<MensajeEliminadoUsuario>()
    .ToTable("MensajesEliminadosUsuarios");
            modelBuilder.Entity<Cliente>().ToTable("Clientes");
            modelBuilder.Entity<SuscripcionPago>().ToTable("SuscripcionPagos");
            modelBuilder.Entity<Suscripcion>().ToTable("Suscripciones");
            modelBuilder.Entity<Manicurista>().ToTable("Manicuristas");

            modelBuilder.Entity<Historia>().ToTable("Historias");
            modelBuilder.Entity<Catalogo>().ToTable("Catalogos");
            modelBuilder.Entity<CuentaBancaria>().ToTable("CuentasBancarias");
            modelBuilder.Entity<PagoTransferencia>().ToTable("PagosTransferencia");
            modelBuilder.Entity<Notificacion>().ToTable("Notificaciones");

            modelBuilder.Entity<Publicacion>().ToTable("Publicaciones");
            modelBuilder.Entity<Comentario>().ToTable("Comentarios");
            modelBuilder.Entity<Like>().ToTable("Likes");
            modelBuilder.Entity<Seguidor>().ToTable("Seguidores");

            // ======================================================
            // CONFIGURACIÓN CITAS
            // ======================================================

            modelBuilder.Entity<Cita>(entity =>
            {
                entity.Property(c => c.Hora)
                    .HasColumnType("time")
                    .IsRequired();

                entity.Property(c => c.HoraFin)
                    .HasColumnType("time")
                    .IsRequired(false);

                entity.Property(c => c.Estado)
                    .HasMaxLength(20);

                entity.Property(c => c.Servicio)
                    .HasMaxLength(150);

                entity.Property(c => c.NombreClienta)
                    .HasMaxLength(150);

                entity.HasIndex(c => c.ManicuristaId);

                entity.HasIndex(c => c.Fecha);

                entity.HasIndex(c => new
                {
                    c.ManicuristaId,
                    c.Fecha
                });
            });

            // ======================================================
            // CLIENTE → MANICURISTA
            // ======================================================

            modelBuilder.Entity<Cliente>()
                .HasOne(c => c.Manicurista)
                .WithMany(m => m.Clientes)
                .HasForeignKey(c => c.ManicuristaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.ManicuristaId);

            // ======================================================
            // PRODUCTO → MANICURISTA
            // ======================================================

            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Manicurista)
                .WithMany()
                .HasForeignKey(p => p.ManicuristaId)
                .OnDelete(DeleteBehavior.Cascade);

            // ======================================================
            // USUARIOS
            // ======================================================

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Telefono)
                .IsUnique();

            // ======================================================
            // 🔥 PERFIL USUARIO (FIX DEFINITIVO)
            // ======================================================

            modelBuilder.Entity<UsuarioPerfil>()
                .HasIndex(p => p.UsuarioId)
                .IsUnique();

            modelBuilder.Entity<UsuarioPerfil>()
                .HasOne(p => p.Usuario)
                .WithMany()
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // ======================================================
            // 🔥 RED SOCIAL
            // ======================================================

            modelBuilder.Entity<Publicacion>()
                .HasOne(p => p.Usuario)
                .WithMany(u => u.Publicaciones)
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comentario>()
                .HasOne(c => c.Usuario)
                .WithMany(u => u.Comentarios)
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Like>()
                .HasOne(l => l.Usuario)
                .WithMany(u => u.Likes)
                .HasForeignKey(l => l.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // ======================================================
            // RELACIONES INTERNAS
            // ======================================================

            modelBuilder.Entity<Comentario>()
                .HasOne(c => c.Publicacion)
                .WithMany(p => p.Comentarios)
                .HasForeignKey(c => c.PublicacionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comentario>()
                .HasOne(c => c.ComentarioPadre)
                .WithMany(c => c.Respuestas)
                .HasForeignKey(c => c.ComentarioPadreId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Like>()
                .HasOne(l => l.Publicacion)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PublicacionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ======================================================
            // 🔹 CONSTRAINTS
            // ======================================================

            modelBuilder.Entity<Like>()
                .HasIndex(l => new
                {
                    l.UsuarioId,
                    l.PublicacionId
                })
                .IsUnique();

            modelBuilder.Entity<Seguidor>()
                .HasIndex(s => new
                {
                    s.SeguidorId,
                    s.SeguidoId
                })
                .IsUnique();

            // ======================================================
            // 🔥 SEGUIDORES
            // ======================================================

            modelBuilder.Entity<Seguidor>(entity =>
            {
                entity.HasIndex(s => new
                {
                    s.SeguidorId,
                    s.SeguidoId
                })
                .IsUnique();

                // quien sigue
                entity.HasOne(s => s.SeguidorUsuario)
                    .WithMany(u => u.Siguiendo)
                    .HasForeignKey(s => s.SeguidorId)
                    .OnDelete(DeleteBehavior.Restrict);

                // a quien sigue
                entity.HasOne(s => s.SeguidoUsuario)
                    .WithMany(u => u.Seguidores)
                    .HasForeignKey(s => s.SeguidoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}