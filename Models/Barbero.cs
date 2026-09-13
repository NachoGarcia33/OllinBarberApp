using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OllinBarberApp.Models
{
    public class Barbero
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Ingresa un celular de 10 dígitos.")]
        public string Telefono { get; set; } = string.Empty;

        [StringLength(300)]
        public string ImagenUrl { get; set; } = string.Empty;

        public bool Disponible { get; set; } = true;
        public bool Activo { get; set; } = true;

        [ValidateNever]
        public ICollection<Cita> Citas { get; set; } = new List<Cita>();
    }
}
