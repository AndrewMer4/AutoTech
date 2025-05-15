using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;
using SistemaTienda.AccesoDatos.Data.Repository.iRepository;
using SistemaTienda.Data;
using SistemaTienda.Models;
using SistemaTienda.Models.ViewModels;

namespace Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class RentasController : Controller
    {
        private readonly IContenedorTrabajo _contenedor;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public RentasController(
            IContenedorTrabajo contenedorTrabajo,
            ApplicationDbContext context,
            IWebHostEnvironment hostingEnvironment)
        {
            _contenedor = contenedorTrabajo;
            _context = context;
            _env = hostingEnvironment;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult GetAll()
        {
            var rentas = _contenedor.Renta
                .GetAll(includeProperties: "Cliente,Vehiculo")
                .Select(r => new {
                    r.Id,
                    Cliente = r.Cliente is null
                        ? ""
                        : $"{r.Cliente.DUI} – {r.Cliente.Nombres} {r.Cliente.Apellidos}",
                    Vehiculo = r.Vehiculo is null
                        ? ""
                        : $"{r.Vehiculo.Placa} – {r.Vehiculo.Marca} {r.Vehiculo.Modelo}",
                    FechaInicio = r.FechaInicio.ToString("yyyy-MM-dd"),
                    FechaFin = r.FechaFin.ToString("yyyy-MM-dd"),
                    r.Total
                });
            return Json(new { data = rentas });
        }

        [HttpGet]
        public IActionResult GetClientes(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new { results = Array.Empty<object>() });

            var items = _contenedor.Cliente
                .GetAll(c =>
                    (c.DUI ?? "")
                    .Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (c.Nombres ?? "")
                    .Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (c.Apellidos ?? "")
                    .Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(c => new {
                    id = c.Id,
                    text = $"{c.DUI} – {c.Nombres} {c.Apellidos}"
                });
            return Json(new { results = items });
        }

        [HttpGet]
        public IActionResult GetVehiculos(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new { results = Array.Empty<object>() });

            var items = _contenedor.Vehiculo
                .GetAll(v =>
                    v.Estado == "Disponible" &&
                    ((v.Placa ?? "")
                    .Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (v.Marca ?? "")
                    .Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (v.Modelo ?? "")
                    .Contains(term, StringComparison.OrdinalIgnoreCase)))
                .Select(v => new {
                    id = v.Placa,
                    text = $"{v.Placa} – {v.Marca} {v.Modelo} ({v.PrecioPorDia:C})"
                });
            return Json(new { results = items });
        }

        [HttpGet]
        public IActionResult Create() =>
            View(new RentaVM { Renta = new Renta() });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(RentaVM vm)
        {
            if (vm.Renta.ClienteId == 0 || string.IsNullOrWhiteSpace(vm.Renta.VehiculoId))
                ModelState.AddModelError("", "Debe seleccionar un cliente y un vehículo.");

            var veh = _contenedor.Vehiculo
                .GetFirstOrDefault(v => v.Placa == vm.Renta.VehiculoId);
            if (veh is null)
                ModelState.AddModelError("", "Vehículo no válido.");

            if (!ModelState.IsValid)
                return View(vm);

            var dias = (vm.Renta.FechaFin - vm.Renta.FechaInicio).Days;
            vm.Renta.Total = dias * veh.PrecioPorDia;
            veh.Estado = "Rentado";

            _contenedor.Renta.Add(vm.Renta);
            _contenedor.Vehiculo.Update(veh);
            _contenedor.Save();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var renta = _contenedor.Renta.Get(id);
            if (renta is null)
                return NotFound();

            var vm = new RentaVM
            {
                Renta = renta,
                ListaClientes = _contenedor.Cliente
                    .GetAll()
                    .Select(c => new SelectListItem
                    {
                        Text = $"{c.Nombres} {c.Apellidos} – {c.DUI}",
                        Value = c.Id.ToString(),
                        Selected = c.Id == renta.ClienteId
                    }),
                ListaVehiculos = _contenedor.Vehiculo
                    .GetAll()
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
            if (veh is null)
                ModelState.AddModelError("", "Vehículo no válido.");

            if (!ModelState.IsValid)
            {
                vm.ListaClientes = _contenedor.Cliente
                    .GetAll()
                    .Select(c => new SelectListItem
                    {
                        Text = $"{c.Nombres} {c.Apellidos} – {c.DUI}",
                        Value = c.Id.ToString()
                    });
                vm.ListaVehiculos = _contenedor.Vehiculo
                    .GetAll()
                    .Select(v => new SelectListItem
                    {
                        Text = $"{v.Placa} – {v.Marca} {v.Modelo} ({v.PrecioPorDia:C}) – {v.Estado}",
                        Value = v.Placa
                    });
                return View(vm);
            }

            var original = _contenedor.Renta.Get(vm.Renta.Id);
            if (original?.VehiculoId != vm.Renta.VehiculoId)
            {
                var oldVeh = _contenedor.Vehiculo
                    .GetFirstOrDefault(v => v.Placa == original.VehiculoId);
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

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var renta = _contenedor.Renta.Get(id);
            if (renta is null)
                return Json(new { success = false, message = "Renta no encontrada." });

            var veh = _contenedor.Vehiculo
                .GetFirstOrDefault(v => v.Placa == renta.VehiculoId);
            if (veh is not null)
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
