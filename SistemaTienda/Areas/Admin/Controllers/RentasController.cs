using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTienda.AccesoDatos.Data.Repository.iRepository;
using SistemaTienda.Models;
using SistemaTienda.Models.ViewModels;

namespace Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class RentasController : Controller
    {
        private readonly IContenedorTrabajo _contenedor;

        public RentasController(IContenedorTrabajo contenedor)
        {
            _contenedor = contenedor
                          ?? throw new ArgumentNullException(nameof(contenedor));
        }

        // GET: /Admin/Rentas
        [HttpGet]
        public IActionResult Index() => View();

        // GET /Admin/Rentas/GetAll → para DataTable
        [HttpGet]
        public IActionResult GetAll()
        {
            var data = _contenedor.Renta
                .GetAll(includeProperties: "Cliente,Vehiculo")
                .Select(r => new {
                    id = r.Id,
                    cliente = r.Cliente is not null
                                      ? $"{r.Cliente.DUI} – {r.Cliente.Nombres} {r.Cliente.Apellidos}"
                                      : string.Empty,
                    vehiculo = r.Vehiculo is not null
                                      ? $"{r.Vehiculo.Placa} – {r.Vehiculo.Marca} {r.Vehiculo.Modelo}"
                                      : string.Empty,
                    fechaInicio = r.FechaInicio.ToString("yyyy-MM-dd"),
                    fechaFin = r.FechaFin.ToString("yyyy-MM-dd"),
                    total = r.Total
                })
                .ToList();

            return Json(new { data });
        }

        // GET /Admin/Rentas/GetClientes?term=…  → para Select2 en “Cliente”
        [HttpGet]
        public IActionResult GetClientes(string term)
        {
            var results = _contenedor.Cliente
                .GetAll()
                .Where(c =>
                    string.IsNullOrWhiteSpace(term)
                    || c.DUI.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || c.Nombres.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || c.Apellidos.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(c => new {
                    id = c.Id,
                    text = $"{c.DUI} – {c.Nombres} {c.Apellidos}"
                })
                .ToList();

            return Json(new { results });
        }

        // GET /Admin/Rentas/GetVehiculos?term=…  → para Select2 en “Vehículo”
        [HttpGet]
        public IActionResult GetVehiculos(string term)
        {
            var results = _contenedor.Vehiculo
                .GetAll()
                // sólo vehículos disponibles
                .Where(v => v.Estado == "Disponible" &&
                    (string.IsNullOrWhiteSpace(term)
                     || v.Placa.Contains(term, StringComparison.OrdinalIgnoreCase)
                     || v.Marca.Contains(term, StringComparison.OrdinalIgnoreCase)
                     || v.Modelo.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .Select(v => new {
                    id = v.Placa,
                    text = $"{v.Placa} – {v.Marca} {v.Modelo}",
                    placa = v.Placa,
                    marca = v.Marca,
                    modelo = v.Modelo,
                    precio = v.PrecioPorDia,
                    urlImagen = v.UrlImagen
                })
                .ToList();

            return Json(new { results });
        }


        // GET: /Admin/Rentas/Create
        [HttpGet]
        public IActionResult Create()
        {
            var renta = new Renta
            {
                FechaInicio = DateTime.Today,
                FechaFin = DateTime.Today.AddDays(1)
            };

            var vm = new RentaVM
            {
                Renta = renta,
                VehiculosDisponibles = _contenedor.Vehiculo
                    .GetAll(v => v.Estado == "Disponible")
                    .ToList()
            };

            return View(vm);
        }

        // POST: /Admin/Rentas/Create
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(RentaVM vm)
        {
            if (vm.Renta.ClienteId == 0)
                ModelState.AddModelError(nameof(vm.Renta.ClienteId), "Debe seleccionar un cliente.");
            if (string.IsNullOrWhiteSpace(vm.Renta.VehiculoId))
                ModelState.AddModelError(nameof(vm.Renta.VehiculoId), "Debe seleccionar un vehículo.");

            var veh = _contenedor.Vehiculo
                       .GetFirstOrDefault(v => v.Placa == vm.Renta.VehiculoId);
            if (veh is null)
                ModelState.AddModelError(nameof(vm.Renta.VehiculoId), "Vehículo no válido.");

            if (!ModelState.IsValid)
            {
                vm.VehiculosDisponibles = _contenedor.Vehiculo
                    .GetAll(v => v.Estado == "Disponible")
                    .ToList();
                return View(vm);
            }

            var dias = (vm.Renta.FechaFin - vm.Renta.FechaInicio).Days;
            vm.Renta.Total = dias * veh.PrecioPorDia;
            veh.Estado = "Rentado";

            _contenedor.Renta.Add(vm.Renta);
            _contenedor.Vehiculo.Update(veh);
            _contenedor.Save();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Rentas/Edit/{id}
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var renta = _contenedor.Renta
                .GetFirstOrDefault(r => r.Id == id, includeProperties: "Cliente,Vehiculo");
            if (renta == null)
                return NotFound();

            var vm = new RentaVM
            {
                Renta = renta,
                ClienteSearch = $"{renta.Cliente?.DUI} – {renta.Cliente?.Nombres} {renta.Cliente?.Apellidos}",
                VehiculoSearch = $"{renta.Vehiculo?.Placa} – {renta.Vehiculo?.Marca} {renta.Vehiculo?.Modelo}",
                VehiculosDisponibles = _contenedor.Vehiculo
                    .GetAll(v => v.Estado == "Disponible" || v.Placa == renta.VehiculoId)
                    .ToList()
            };

            return View(vm);
        }

        // POST: /Admin/Rentas/Edit
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Edit(RentaVM vm)
        {
            var veh = _contenedor.Vehiculo
                       .GetFirstOrDefault(v => v.Placa == vm.Renta.VehiculoId);
            if (veh is null)
                ModelState.AddModelError(nameof(vm.Renta.VehiculoId), "Vehículo no válido.");

            if (!ModelState.IsValid)
            {
                vm.VehiculosDisponibles = _contenedor.Vehiculo
                    .GetAll(v => v.Estado == "Disponible" || v.Placa == vm.Renta.VehiculoId)
                    .ToList();
                return View(vm);
            }

            var original = _contenedor.Renta.Get(vm.Renta.Id);
            if (original?.VehiculoId != vm.Renta.VehiculoId)
            {
                var oldVeh = _contenedor.Vehiculo.GetFirstOrDefault(v => v.Placa == original.VehiculoId);
                if (oldVeh is not null)
                {
                    oldVeh.Estado = "Disponible";
                    _contenedor.Vehiculo.Update(oldVeh);
                }
                veh.Estado = "Rentado";
                _contenedor.Vehiculo.Update(veh);
            }

            var days = (vm.Renta.FechaFin - vm.Renta.FechaInicio).Days;
            vm.Renta.Total = days * veh.PrecioPorDia;

            _contenedor.Renta.Update(vm.Renta);
            _contenedor.Save();

            return RedirectToAction(nameof(Index));
        }

        // DELETE: /Admin/Rentas/Delete/{id}  (AJAX)
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var renta = _contenedor.Renta.Get(id);
            if (renta == null)
                return Json(new { success = false, message = "Renta no encontrada." });

            var veh = _contenedor.Vehiculo.GetFirstOrDefault(v => v.Placa == renta.VehiculoId);
            if (veh is not null)
            {
                veh.Estado = "Disponible";
                _contenedor.Vehiculo.Update(veh);
            }

            _contenedor.Renta.Remove(renta);
            _contenedor.Save();

            return Json(new { success = true, message = "Renta eliminada correctamente." });
        }
    }
}
