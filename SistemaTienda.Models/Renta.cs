﻿#nullable enable

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaTienda.Models
{
    public class Renta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Clientes? Cliente { get; set; }

        [Required]
        [Display(Name = "Vehículo (Matrícula)")]
        public string VehiculoId { get; set; } = null!;   // ahora string

        [ForeignKey("VehiculoId")]
        public Vehiculo? Vehiculo { get; set; }

        [Required]
        [Display(Name = "Fecha de Inicio")]
        [DataType(DataType.Date)]
        public DateTime FechaInicio { get; set; }

        [Required]
        [Display(Name = "Fecha de Fin")]
        [DataType(DataType.Date)]
        public DateTime FechaFin { get; set; }

        [Display(Name = "Total")]
        public decimal Total { get; set; }

        [NotMapped]
        [Display(Name = "Precio por Día")]
        public decimal PrecioPorDia => Vehiculo?.PrecioPorDia ?? 0m;
    }
}

#nullable restore
