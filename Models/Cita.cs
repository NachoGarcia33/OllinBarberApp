using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OllinBarberApp.Models
{
    public class Cita
    {
        public int Id { get; set; }

        public string ClienteNombre { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public DateTimeOffset FechaHora { get; set; }

        public string Barbero { get; set; } = string.Empty;

        public int? BarberoId { get; set; }

        [ValidateNever]
        public Barbero? BarberoEntidad { get; set; }

        public int ServicioId { get; set; }

        [ValidateNever]
        public Servicio? Servicio { get; set; }

        public EstadoCita Estado { get; set; } = EstadoCita.Pendiente;
    }
}
