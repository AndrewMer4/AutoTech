// EmpleadoRentasController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaTienda.AccesoDatos.Data.Repository.iRepository;
using SistemaTienda.Models;
using SistemaTienda.Models.ViewModels;
using SistemaTienda.Utilidades;
using System.Linq;

namespace SistemaTienda.Areas.Empleado.Controllers
{
    [Authorize(Roles = CNT.Empleado)]
    [Area("Empleado")]
    public class EmpleadoRentasController : Controller
    {
        private readonly IContenedorTrabajo _contenedor;

        public EmpleadoRentasController(IContenedorTrabajo contenedor)
        {
            _contenedor = contenedor;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult GetAll()
        {
            var data = _contenedor.Renta
                .GetAll(includeProperties: "Cliente,Vehiculo")
                .Select(r => new {
                    r.Id,
                    Cliente = r.Cliente!.Nombres + " " + r.Cliente.Apellidos,
                    Vehiculo = r.Vehiculo!.Placa,
                    FechaInicio = r.FechaInicio.ToString("yyyy-MM-dd"),
                    FechaFin = r.FechaFin.ToString("yyyy-MM-dd"),
                    r.Total
                });
            return Json(new { data });
        }

        [HttpGet]
        public IActionResult Create()
        {
            var vm = new RentaVM
            {
                Renta = new Renta(),
                ListaClientes = _contenedor.Cliente.GetAll()
                    .Select(c => new SelectListItem
                    {
                        Text = $"{c.Nombres} {c.Apellidos}",
                        Value = c.Id.ToString()
                    }),
                ListaVehiculos = _contenedor.Vehiculo.GetAll(v => v.Estado == "Disponible")
                    .Select(v => new SelectListItem
                    {
                        Text = $"{v.Placa} – {v.Marca} {v.Modelo} ({v.PrecioPorDia:C})",
                        Value = v.Placa
                    })
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(RentaVM vm)
        {
            var veh = _contenedor.Vehiculo
                .GetFirstOrDefault(v => v.Placa == vm.Renta.VehiculoId);
            if (veh == null)
                ModelState.AddModelError("", "Vehículo no válido.");
            else
            {
                var dias = (vm.Renta.FechaFin - vm.Renta.FechaInicio).Days;
                if (dias <= 0)
                    ModelState.AddModelError("", "La fecha fin debe ser posterior a la de inicio.");
                else
                    vm.Renta.Total = dias * veh.PrecioPorDia;
            }

            if (!ModelState.IsValid)
            {
                vm.ListaClientes = _contenedor.Cliente.GetAll()
                    .Select(c => new SelectListItem
                    {
                        Text = $"{c.Nombres} {c.Apellidos}",
                        Value = c.Id.ToString()
                    });
                vm.ListaVehiculos = _contenedor.Vehiculo.GetAll(v => v.Estado == "Disponible")
                    .Select(v => new SelectListItem
                    {
                        Text = $"{v.Placa} – {v.Marca} {v.Modelo} ({v.PrecioPorDia:C})",
                        Value = v.Placa
                    });
                return View(vm);
            }

            _contenedor.Renta.Add(vm.Renta);
            veh!.Estado = "Rentado";
            _contenedor.Vehiculo.Update(veh);
            _contenedor.Save();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var renta = _contenedor.Renta.Get(id);
            if (renta == null) return NotFound();

            var vm = new RentaVM
            {
                Renta = renta,
                ListaClientes = _contenedor.Cliente.GetAll()
                    .Select(c => new SelectListItem
                    {
                        Text = $"{c.Nombres} {c.Apellidos}",
                        Value = c.Id.ToString(),
                        Selected = c.Id == renta.ClienteId
                    }),
                ListaVehiculos = _contenedor.Vehiculo.GetAll()
                    .Select(v => new SelectListItem
                    {
                        Text = $"{v.Placa} – {v.Marca} {v.Modelo} ({v.PrecioPorDia:C}) – {v.Estado}",
                        Value = v.Placa,
                        Selected = v.Placa == renta.VehiculoId
                    })
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(RentaVM vm)
        {
            var veh = _contenedor.Vehiculo
                .GetFirstOrDefault(v => v.Placa == vm.Renta.VehiculoId);
            if (veh == null)
                ModelState.AddModelError("", "Vehículo no válido.");
            else
            {
                var dias = (vm.Renta.FechaFin - vm.Renta.FechaInicio).Days;
                if (dias <= 0)
                    ModelState.AddModelError("", "La fecha fin debe ser posterior a la de inicio.");
                else
                    vm.Renta.Total = dias * veh.PrecioPorDia;
            }

            if (!ModelState.IsValid)
            {
                vm.ListaClientes = _contenedor.Cliente.GetAll()
                    .Select(c => new SelectListItem
                    {
                        Text = $"{c.Nombres} {c.Apellidos}",
                        Value = c.Id.ToString()
                    });
                vm.ListaVehiculos = _contenedor.Vehiculo.GetAll()
                    .Select(v => new SelectListItem
                    {
                        Text = $"{v.Placa} – {v.Marca} {v.Modelo} ({v.PrecioPorDia:C}) – {v.Estado}",
                        Value = v.Placa
                    });
                return View(vm);
            }

            var original = _contenedor.Renta.Get(vm.Renta.Id);
            if (original.VehiculoId != vm.Renta.VehiculoId)
            {
                var oldVeh = _contenedor.Vehiculo
                    .GetFirstOrDefault(v => v.Placa == original.VehiculoId);
                if (oldVeh != null)
                {
                    oldVeh.Estado = "Disponible";
                    _contenedor.Vehiculo.Update(oldVeh);
                }
                veh!.Estado = "Rentado";
                _contenedor.Vehiculo.Update(veh);
            }

            _contenedor.Renta.Update(vm.Renta);
            _contenedor.Save();

            return RedirectToAction(nameof(Index));
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var renta = _contenedor.Renta.Get(id);
            if (renta == null)
                return Json(new { success = false, message = "Renta no encontrada." });

            var veh = _contenedor.Vehiculo
                .GetFirstOrDefault(v => v.Placa == renta.VehiculoId);
            if (veh != null)
            {
                veh.Estado = "Disponible";
                _contenedor.Vehiculo.Update(veh);
            }

            _contenedor.Renta.Remove(renta);
            _contenedor.Save();
            return Json(new { success = true, message = "Renta eliminada." });
        }
    }
}
