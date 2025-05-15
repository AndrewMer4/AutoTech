using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaTienda.Models
{
    public class Vehiculo
    {
        [Key]
        [Required]
        [Display(Name = "Matrícula")]
        [RegularExpression(@"^[A-Z]\s\d{3}\s\d{3}$", ErrorMessage = "La matrícula debe tener formato 'A 123 456'.")]
        public string Placa { get; set; }

        [Required]
        [StringLength(50)]
        public string Marca { get; set; }

        [Required]
        [StringLength(50)]
        public string Modelo { get; set; }

        [Required]
        [Display(Name = "Año")]
        [Range(2000, 2026)]
        public int Anio { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Kilometraje { get; set; }

        [Required]
        public string Estado { get; set; }

        [Display(Name = "Imagen")]
        public string UrlImagen { get; set; }

        [Required]
        [Display(Name = "Precio por Día")]
        [Range(0.01, double.MaxValue)]
        [DisplayFormat(DataFormatString = "{0:0.00}", ApplyFormatInEditMode = true)]
        public decimal PrecioPorDia { get; set; }
    }
}
