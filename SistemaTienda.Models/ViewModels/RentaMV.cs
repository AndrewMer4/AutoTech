using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaTienda.Models.ViewModels
{
    public class RentaVM
    {
        public Renta Renta { get; set; } = new Renta();

        public IEnumerable<SelectListItem> ListaClientes { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> ListaVehiculos { get; set; } = Enumerable.Empty<SelectListItem>();

        [NotMapped]
        public string ClienteSearch { get; set; } = "";

        [NotMapped]
        public string VehiculoSearch { get; set; } = "";
        public IEnumerable<Vehiculo> VehiculosDisponibles { get; set; } = Enumerable.Empty<Vehiculo>();
    }
}
