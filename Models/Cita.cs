using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace OllinBarberApp.Models
{
    public class Cita
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string ClienteNombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(
            @"^[0-9]{10}$",
            ErrorMessage = "Ingrese un número celular válido de 10 dígitos.")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar una fecha y hora.")]
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