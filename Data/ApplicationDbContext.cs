using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OllinBarberApp.Models;

namespace OllinBarberApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Servicio> Servicios { get; set; }
        public DbSet<Cita> Citas { get; set; }
        public DbSet<Barbero> Barberos { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<VentaDetalle> VentaDetalles { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<PedidoDetalle> PedidoDetalles { get; set; }
        public DbSet<ConfiguracionSistema> ConfiguracionSistema { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Cita>()
                .Property(c => c.Estado)
                .HasConversion<string>();

            modelBuilder.Entity<Cita>()
                .Property(c => c.FechaHora)
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<Cita>()
                .HasOne(c => c.Servicio)
                .WithMany(s => s.Citas)
                .HasForeignKey(c => c.ServicioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cita>()
                .HasOne(c => c.BarberoEntidad)
                .WithMany(b => b.Citas)
                .HasForeignKey(c => c.BarberoId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Cita>()
                .HasIndex(c => c.FechaHora);

            modelBuilder.Entity<Cita>()
                .HasIndex(c => c.TokenConfirmacion)
                .IsUnique();

            modelBuilder.Entity<VentaDetalle>()
                .HasOne(v => v.Venta)
                .WithMany(v => v.Detalles)
                .HasForeignKey(v => v.VentaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VentaDetalle>()
                .HasOne(v => v.Producto)
                .WithMany()
                .HasForeignKey(v => v.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Barbero>().Property(b => b.Nombre).HasColumnType("text");
            modelBuilder.Entity<Barbero>().Property(b => b.ImagenUrl).HasColumnType("text");
            modelBuilder.Entity<Servicio>().Property(s => s.Nombre).HasColumnType("text");
            modelBuilder.Entity<Servicio>().Property(s => s.Tipo).HasColumnType("text");
            modelBuilder.Entity<Servicio>().Property(s => s.ImagenUrl).HasColumnType("text");
            modelBuilder.Entity<Producto>().Property(p => p.Nombre).HasColumnType("text");
            modelBuilder.Entity<Producto>().Property(p => p.Descripcion).HasColumnType("text");
            modelBuilder.Entity<Producto>().Property(p => p.Categoria).HasColumnType("text");
            modelBuilder.Entity<Producto>().Property(p => p.ImagenUrl).HasColumnType("text");
            modelBuilder.Entity<Cita>().Property(c => c.ClienteNombre).HasColumnType("text");
            modelBuilder.Entity<ApplicationUser>().Property(u => u.Nombre).HasColumnType("text");
            modelBuilder.Entity<Venta>().Property(v => v.ClienteNombre).HasColumnType("text");

            modelBuilder.Entity<Producto>()
                .HasIndex(p => new { p.Activo, p.Categoria, p.Marca });

            modelBuilder.Entity<Producto>()
                .ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Productos_Descuento", "\"DescuentoPorcentaje\" >= 0 AND \"DescuentoPorcentaje\" <= 100");
                });

            modelBuilder.Entity<Pedido>()
                .Property(p => p.Estado)
                .HasConversion<string>();

            modelBuilder.Entity<Pedido>()
                .Property(p => p.Fecha)
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<Pedido>()
                .HasIndex(p => p.Codigo)
                .IsUnique();

            modelBuilder.Entity<Pedido>()
                .HasIndex(p => p.Fecha);

            modelBuilder.Entity<Pedido>()
                .HasIndex(p => p.Estado);

            modelBuilder.Entity<Pedido>()
                .HasIndex(p => p.UsuarioId);

            modelBuilder.Entity<Pedido>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PedidoDetalle>()
                .HasOne(d => d.Pedido)
                .WithMany(p => p.Detalles)
                .HasForeignKey(d => d.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PedidoDetalle>()
                .HasOne(d => d.Producto)
                .WithMany()
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PedidoDetalle>()
                .ToTable(t =>
                    t.HasCheckConstraint("CK_PedidoDetalles_Cantidad", "\"Cantidad\" > 0"));
        }
    }
}
