using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OllinBarberApp.Models
{
    public class Notificacion
    {
        public int Id { get; set; }

        [Required]
        public int BarberoId { get; set; }

        [ValidateNever]
        public Barbero? Barbero { get; set; }

        public int? CitaId { get; set; }

        [ValidateNever]
        public Cita? Cita { get; set; }

        [Required]
        [StringLength(300)]
        public string Mensaje { get; set; } = string.Empty;

        public bool Leida { get; set; } = false;

        public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;
    }
}
