// Areas/Empleado/Controllers/EmpleadoRentasController.cs
#nullable enable

using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTienda.AccesoDatos.Data.Repository.iRepository;
using SistemaTienda.Models;
using SistemaTienda.Models.ViewModels;
using SistemaTienda.Utilidades;

namespace SistemaTienda.Areas.Empleado.Controllers
{
    [Authorize(Roles = CNT.Empleado)]
    [Area("Empleado")]
    public class EmpleadoRentasController : Controller
    {
        private readonly IContenedorTrabajo _contenedor;

        public EmpleadoRentasController(IContenedorTrabajo contenedor)
        {
            _contenedor = contenedor ?? throw new ArgumentNullException(nameof(contenedor));
        }

        // -----------------------------------
        // GET: /Empleado/EmpleadoRentas
        // -----------------------------------
        [HttpGet]
        public IActionResult Index() => View();

        // -----------------------------------
        // GET: /Empleado/EmpleadoRentas/GetAll
        // -----------------------------------
        [HttpGet]
        [HttpGet]
        public IActionResult GetAll()
        {
            // 1) Liberar autom. vehículos de rentas vencidas (FechaFin < hoy)
            LiberarVehiculosVencidos();

            // 2) Construir lista de rentas con Cliente y Vehículo incluidos
            DateTime hoy = DateTime.Today;
            var data = _contenedor.Renta
                .GetAll(includeProperties: "Cliente,Vehiculo")
                .Select(r => new
                {
                    id = r.Id,
                    cliente = r.Cliente != null
                                  ? $"{r.Cliente.DUI} – {r.Cliente.Nombres} {r.Cliente.Apellidos}"
                                  : string.Empty,
                    vehiculo = r.Vehiculo != null
                                  ? $"{r.Vehiculo.Placa} – {r.Vehiculo.Marca} {r.Vehiculo.Modelo}"
                                  : string.Empty,
                    fechaInicio = r.FechaInicio.ToString("yyyy-MM-dd"),
                    fechaFin = r.FechaFin.ToString("yyyy-MM-dd"),
                    total = r.Total,
                    estadoPago = _contenedor.Pago
                                     .GetAll(p => p.RentaId == r.Id)
                                     .Any() ? "Pagado" : "Pendiente",
                    vehEstado = r.Vehiculo?.Estado ?? "N/A",
                    esVencida = r.FechaFin.Date <= hoy
                })
                .ToList();

            return Json(new { data });
        }

        // -----------------------------------
        // GET: /Empleado/EmpleadoRentas/GetClientes?term=…
        // -----------------------------------
        [HttpGet]
        public IActionResult GetClientes(string term)
        {
            var results = _contenedor.Cliente
                .GetAll()
                .Where(c =>
                    string.IsNullOrWhiteSpace(term) ||
                    c.DUI.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    c.Nombres.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    c.Apellidos.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(c => new
                {
                    id = c.Id,
                    text = $"{c.DUI} – {c.Nombres} {c.Apellidos}"
                })
                .ToList();

            return Json(new { results });
        }

        // -----------------------------------
        // GET: /Empleado/EmpleadoRentas/GetVehiculos?term=…
        // -----------------------------------
        [HttpGet]
        public IActionResult GetVehiculos(string term)
        {
            var results = _contenedor.Vehiculo
                .GetAll()
                .Where(v => v.Estado == "Disponible" &&
                    (string.IsNullOrWhiteSpace(term) ||
                     v.Placa.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                     v.Marca.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                     v.Modelo.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .Select(v => new
                {
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
        [HttpGet]
        public IActionResult Create()
        {
            // Inicializar Renta con FechaInicio = hoy, FechaFin = mañana
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

        // -----------------------------------
        // POST: /Empleado/EmpleadoRentas/Create
        // -----------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(RentaVM vm)
        {
            // 1) Validaciones básicas
            if (vm.Renta.ClienteId == 0)
                ModelState.AddModelError(nameof(vm.Renta.ClienteId), "Debe seleccionar un cliente.");
            if (string.IsNullOrWhiteSpace(vm.Renta.VehiculoId))
                ModelState.AddModelError(nameof(vm.Renta.VehiculoId), "Debe seleccionar un vehículo.");

            // 2) Intentar obtener el vehículo por placa (Trim + ToLower)
            var placaLower = vm.Renta.VehiculoId?.Trim().ToLower();
            var veh = _contenedor.Vehiculo
                .GetFirstOrDefault(v => v.Placa.Trim().ToLower() == placaLower);

            if (veh == null)
                ModelState.AddModelError(nameof(vm.Renta.VehiculoId), "Vehículo no válido.");

            if (!ModelState.IsValid)
            {
                // Si hay error, recargar lista de Vehículos Disponibles
                vm.VehiculosDisponibles = _contenedor.Vehiculo
                    .GetAll(v => v.Estado == "Disponible")
                    .ToList();
                return View(vm);
            }

            // 3) Validar que FechaFin > FechaInicio
            var dias = (vm.Renta.FechaFin - vm.Renta.FechaInicio).Days;
            if (dias <= 0)
            {
                ModelState.AddModelError(nameof(vm.Renta.FechaFin), "La fecha de fin debe ser posterior a la de inicio.");
                vm.VehiculosDisponibles = _contenedor.Vehiculo
                    .GetAll(v => v.Estado == "Disponible")
                    .ToList();
                return View(vm);
            }

            // 4) Calcular total y marcar vehículo como "Rentado"
            vm.Renta.Total = dias * veh.PrecioPorDia;

            veh.Estado = "Rentado";
            _contenedor.Vehiculo.Update(veh);
            _contenedor.Save();
            // —> Guardamos inmediatamente el cambio a "Rentado" antes de crear la Renta

            // 5) Crear la Renta y guardarla
            _contenedor.Renta.Add(vm.Renta);
            _contenedor.Save();

            // 6) Crear el Pago pendiente asociado a la nueva Renta
            var pagoPendiente = new Pago
            {
                RentaId = vm.Renta.Id,
                Monto = vm.Renta.Total,
                FechaPago = DateTime.Now,
                Estado = "Pendiente"
            };
            _contenedor.Pago.Add(pagoPendiente);
            _contenedor.Save();

            return RedirectToAction(nameof(Index));
        }
        // -----------------------------------
        // GET: /Empleado/EmpleadoRentas/Edit/{id}
        // -----------------------------------
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

        // -----------------------------------
        // POST: /Empleado/EmpleadoRentas/Edit
        // -----------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(RentaVM vm)
        {
            // 1) Validaciones básicas
            if (vm.Renta.ClienteId == 0)
                ModelState.AddModelError(nameof(vm.Renta.ClienteId), "Debe seleccionar un cliente.");
            if (string.IsNullOrWhiteSpace(vm.Renta.VehiculoId))
                ModelState.AddModelError(nameof(vm.Renta.VehiculoId), "Debe seleccionar un vehículo.");

            // 2) Intentar recuperar el nuevo vehículo por placa (Trim + ToLower)
            var placaNuevaLower = vm.Renta.VehiculoId.Trim().ToLower();
            var nuevoVeh = _contenedor.Vehiculo
                .GetFirstOrDefault(v => v.Placa.Trim().ToLower() == placaNuevaLower);
            if (nuevoVeh is null)
                ModelState.AddModelError(nameof(vm.Renta.VehiculoId), "Vehículo no válido.");

            if (!ModelState.IsValid)
            {
                // Si hay error, recargar lista de vehículos disponibles (incluye vehículo actual)
                vm.VehiculosDisponibles = _contenedor.Vehiculo
                    .GetAll(v => v.Estado == "Disponible" || v.Placa == vm.Renta.VehiculoId)
                    .ToList();
                return View(vm);
            }

            // 3) Recuperar la Renta original (entidad “tracked”)
            var original = _contenedor.Renta.Get(vm.Renta.Id);
            if (original is null)
                return NotFound();

            // 4) Si cambió el vehículo
            if (!string.Equals(original.VehiculoId, vm.Renta.VehiculoId, StringComparison.OrdinalIgnoreCase))
            {
                // a) Liberar el vehículo antiguo (si seguía “Rentado”)
                if (!string.IsNullOrWhiteSpace(original.VehiculoId))
                {
                    var placaViejaLower = original.VehiculoId.Trim().ToLower();
                    var viejoVeh = _contenedor.Vehiculo
                        .GetFirstOrDefault(v => v.Placa.Trim().ToLower() == placaViejaLower);
                    if (viejoVeh != null && viejoVeh.Estado.Equals("Rentado", StringComparison.OrdinalIgnoreCase))
                    {
                        viejoVeh.Estado = "Disponible";
                        _contenedor.Vehiculo.Update(viejoVeh);
                        _contenedor.Save();
                    }
                }

                // b) Marcar el nuevo vehículo como “Rentado” (si estaba “Disponible”)
                if (nuevoVeh.Estado.Equals("Disponible", StringComparison.OrdinalIgnoreCase))
                {
                    nuevoVeh.Estado = "Rentado";
                    _contenedor.Vehiculo.Update(nuevoVeh);
                    _contenedor.Save();
                }

                original.VehiculoId = vm.Renta.VehiculoId;
            }
            // Si no cambió vehículo, mantenemos su estado “Rentado”

            // 5) Actualizar cliente, fechas y total
            original.ClienteId = vm.Renta.ClienteId;
            original.FechaInicio = vm.Renta.FechaInicio;
            original.FechaFin = vm.Renta.FechaFin;
            var dias = (original.FechaFin - original.FechaInicio).Days;
            original.Total = dias * nuevoVeh.PrecioPorDia;

            _contenedor.Renta.Update(original);
            _contenedor.Save();

            // 6) Actualizar el Pago asociado si existe
            var pago = _contenedor.Pago
                .GetFirstOrDefault(p => p.RentaId == original.Id);
            if (pago != null)
            {
                pago.Monto = original.Total;
                _contenedor.Pago.Update(pago);
                _contenedor.Save();
            }

            return RedirectToAction(nameof(Index));
        }

        // -----------------------------------
        // DELETE: /Empleado/EmpleadoRentas/Delete/{id}
        // -----------------------------------
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var renta = _contenedor.Renta.Get(id);
            if (renta == null)
                return Json(new { success = false, message = "Renta no encontrada." });

            // Si tiene pagos, no permitir borrado
            bool tienePagos = _contenedor.Pago
                .GetAll(p => p.RentaId == id)
                .Any();
            if (tienePagos)
                return Json(new { success = false, message = "No se puede eliminar la renta porque tiene pagos asociados." });

            // Liberar vehículo (si seguía “Rentado”)
            if (!string.IsNullOrWhiteSpace(renta.VehiculoId))
            {
                var placaLower = renta.VehiculoId.Trim().ToLower();
                var veh = _contenedor.Vehiculo
                    .GetFirstOrDefault(v => v.Placa.Trim().ToLower() == placaLower);
                if (veh != null && veh.Estado.Equals("Rentado", StringComparison.OrdinalIgnoreCase))
                {
                    veh.Estado = "Disponible";
                    _contenedor.Vehiculo.Update(veh);
                }
            }

            _contenedor.Renta.Remove(renta);
            _contenedor.Save();

            return Json(new { success = true, message = "Renta eliminada correctamente." });
        }

        private void LiberarVehiculosVencidos()
        {
            DateTime hoy = DateTime.Today;

            // Solo rentas con FechaFin < hoy (no <=). Así no se libera el mismo día.
            var rentasVencidas = _contenedor.Renta
                .GetAll(r => r.FechaFin < hoy, includeProperties: "Vehiculo")
                .Where(r => r.Vehiculo != null &&
                            r.Vehiculo.Estado.Equals("Rentado", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!rentasVencidas.Any())
                return;

            foreach (var renta in rentasVencidas)
            {
                renta.Vehiculo!.Estado = "Disponible";
                _contenedor.Vehiculo.Update(renta.Vehiculo);
            }

            _contenedor.Save();
        }
    }
}
