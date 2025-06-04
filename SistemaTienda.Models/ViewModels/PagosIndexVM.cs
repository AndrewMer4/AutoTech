using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SistemaTienda.Models;

namespace SistemaTienda.Models.ViewModels
{ public class PagosIndexVM
    {
        public IEnumerable<Vehiculo> VehiculosRentados { get; set; }
    }
}
