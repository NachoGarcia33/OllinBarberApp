using Microsoft.AspNetCore.Identity;
using System;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace OllinBarberApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El celular es obligatorio")]
        [RegularExpression(@"^[0-9]{10}$",
            ErrorMessage = "Ingrese un número celular válido de 10 dígitos")]
        public string Celular { get; set; } = string.Empty;

        public bool EsBarbero { get; set; }

        public bool Disponible { get; set; }
    }
}

