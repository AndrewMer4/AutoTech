using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaTienda.Models.ViewModels
{
    public class MantenimientoVM
    {
        public IEnumerable<Vehiculo> VehiculosEnMantenimiento { get; set; }
        public string PlacaSeleccionada { get; set; }
    }
}