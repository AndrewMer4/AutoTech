using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaTienda.Models.ViewModels
{
    public class GraficasVM
    {
        public decimal GananciasTotales { get; set; }
        public string VehiculoMasRentado { get; set; }
        public int CantidadRentasVehiculo { get; set; }
        public dynamic DistribucionVehiculos { get; set; }
    }

}
