using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OllinBarberApp.Models;

namespace OllinBarberApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Servicio> Servicios { get; set; }

        public DbSet<Cita> Citas { get; set; }

        public DbSet<Barbero> Barberos { get; set; }

        public DbSet<Producto> Productos { get; set; }

        public DbSet<Venta> Ventas { get; set; }

        public DbSet<VentaDetalle> VentaDetalles { get; set; }

        public DbSet<ConfiguracionSistema> ConfiguracionSistema { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Cita>()
                .Property(c => c.Estado)
                .HasConversion<string>();

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

            modelBuilder.Entity<VentaDetalle>()
                .HasOne(v => v.Venta)
                .WithMany(v => v.Detalles)
                .HasForeignKey(v => v.VentaId);

            modelBuilder.Entity<VentaDetalle>()
                .HasOne(v => v.Producto)
                .WithMany()
                .HasForeignKey(v => v.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
