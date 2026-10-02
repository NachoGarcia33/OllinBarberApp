using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OllinBarberApp.Models
{
    public class SuscripcionPush
    {
        public int Id { get; set; }

        [Required]
        public int BarberoId { get; set; }

        [ValidateNever]
        public Barbero? Barbero { get; set; }

        [Required]
        public string Endpoint { get; set; } = string.Empty;

        [Required]
        public string P256dh { get; set; } = string.Empty;

        [Required]
        public string Auth { get; set; } = string.Empty;

        public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;
    }
}
