// Areas/Empleado/Controllers/EmpleadoPagosController.cs
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaTienda.AccesoDatos.Data.Repository.iRepository;
using SistemaTienda.Data;
using SistemaTienda.Models;
using SistemaTienda.Models.ViewModels;
using SistemaTienda.Utilidades;

namespace SistemaTienda.Areas.Empleado.Controllers
{
    [Authorize(Roles = CNT.Empleado)]
    [Area("Empleado")]
    public class EmpleadoPagosController : Controller
    {
        private readonly IContenedorTrabajo _contenedor;
        private readonly ApplicationDbContext _context;

        public EmpleadoPagosController(IContenedorTrabajo contenedor, ApplicationDbContext context)
        {
            _contenedor = contenedor ?? throw new ArgumentNullException(nameof(contenedor));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ---------- INDEX & DATATABLE  ----------
        [HttpGet]
        public IActionResult Index()
        {
            // Reutilizamos la lógica de Admin para mostrar sidebar de vehículos rentados
            var vehiculosRentados = _context.Vehiculo
                .Where(v => v.Estado == "Rentado")
                .AsNoTracking()
                .ToList();

            var vm = new PagosIndexVM
            {
                VehiculosRentados = vehiculosRentados
            };

            return View(vm);
        }

        // GET /Empleado/EmpleadoPagos/GetAll  → para DataTable (trae rentaId y pagoId)
        [HttpGet]
        public IActionResult GetAll()
        {
            // 1) Cargamos todas las Rentas con Cliente y Vehículo
            var rentas = _context.Renta
                .Include(r => r.Cliente)
                .Include(r => r.Vehiculo)
                .AsNoTracking()
                .ToList();

            // 2) Cargamos todos los Pagos
            var pagos = _context.Pago
                .AsNoTracking()
                .ToList();

            // 3) Proyectamos el resultado exactamente igual que en Admin
            var resultado = rentas.Select(r =>
            {
                var pago = pagos.FirstOrDefault(p => p.RentaId == r.Id);

                return new
                {
                    id = pago?.Id ?? 0,                                  // pagoId (0 si aún no existe)
                    rentaId = r.Id,                                      // ID de la renta asociada
                    clienteNombre = $"{r.Cliente.Nombres} {r.Cliente.Apellidos}",
                    vehiculoInfo = $"{r.Vehiculo.Marca} {r.Vehiculo.Modelo} (Placa: {r.Vehiculo.Placa})",
                    fechaInicio = r.FechaInicio.ToString("yyyy-MM-dd"),
                    fechaFin = r.FechaFin.ToString("yyyy-MM-dd"),
                    total = r.Total,
                    estado = pago?.Estado ?? "Pendiente",                // “Pendiente” si no hay pago
                    tienePago = pago != null,
                    pagoId = pago?.Id ?? 0
                };
            }).ToList();

            return Json(new { data = resultado });
        }

        // POST /Empleado/EmpleadoPagos/Create  ← (en Admin se usaba raramente; lo dejamos igual)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Renta renta)
        {
            if (!ModelState.IsValid)
                return View(renta);

            _context.Renta.Add(renta);
            _context.SaveChanges();

            var pago = new Pago
            {
                RentaId = renta.Id,
                Monto = renta.Total,
                Estado = "Pendiente",
                FechaPago = null
            };
            _context.Pago.Add(pago);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // POST /Empleado/EmpleadoPagos/CompletarPago?pagoId=XXX&rentaId=YYY
        [HttpPost]
        public IActionResult CompletarPago(int pagoId, int rentaId = 0)
        {
            // 1) Intentar recuperar el Pago existente
            var pago = _context.Pago.Find(pagoId);

            if (pago == null)
            {
                // Si no existe, creamos uno nuevo (la renta ya está en BD)
                if (rentaId > 0)
                {
                    var renta = _context.Renta.Find(rentaId);
                    if (renta == null)
                        return Json(new { success = false, message = "Renta no encontrada." });

                    pago = new Pago
                    {
                        RentaId = renta.Id,
                        Monto = renta.Total,
                        Estado = "Pendiente",
                        FechaPago = null
                    };
                    _context.Pago.Add(pago);
                    _context.SaveChanges();
                }
                else
                {
                    return Json(new { success = false, message = "Pago no encontrado." });
                }
            }

            pago.Estado = "Confirmado";
            pago.FechaPago = DateTime.Now;
            _context.Pago.Update(pago);
            _context.SaveChanges();

            return Json(new { success = true, message = "Pago confirmado correctamente" });
        }

        // GET /Empleado/EmpleadoPagos/Graficas
        [HttpGet]
        public IActionResult Graficas()
        {
            // 1. Calcular ganancias totales de pagos completados
            var gananciasTotales = _context.Pago
                .Where(p => p.Estado == "Confirmado")
                .Sum(p => p.Monto);

            // 2. Obtener datos para gráfica de pastel (top 5 vehículos más rentados)
            var vehiculosRentados = _context.Renta
                .Include(r => r.Vehiculo)
                .GroupBy(r => r.Vehiculo)
                .Select(g => new
                {
                    VehiculoInfo = $"{g.Key.Marca} {g.Key.Modelo}",
                    Placa = g.Key.Placa,
                    CantidadRentas = g.Count(),
                    Porcentaje = 0m
                })
                .OrderByDescending(x => x.CantidadRentas)
                .Take(5)
                .ToList();

            var totalRentas = vehiculosRentados.Sum(x => x.CantidadRentas);

            var vehiculosConPorcentaje = vehiculosRentados
                .Select(v => new
                {
                    v.VehiculoInfo,
                    v.Placa,
                    v.CantidadRentas,
                    Porcentaje = totalRentas > 0
                                 ? Math.Round((v.CantidadRentas * 100m) / totalRentas, 1)
                                 : 0
                })
                .ToList();

            var model = new GraficasVM
            {
                GananciasTotales = gananciasTotales,
                VehiculoMasRentado = vehiculosConPorcentaje.FirstOrDefault()?.VehiculoInfo ?? "No hay datos",
                CantidadRentasVehiculo = vehiculosConPorcentaje.FirstOrDefault()?.CantidadRentas ?? 0,
                DistribucionVehiculos = vehiculosConPorcentaje
            };

            return View(model);
        }

        // GET /Empleado/EmpleadoPagos/GetRentas?term=…  → para Select2 en “Renta”
        [HttpGet]
        public IActionResult GetRentas(string term)
        {
            // IDs de rentas ya pagadas
            var rentasConPagoIds = _contenedor.Pago
                .GetAll().Select(p => p.RentaId);

            // Solo rentas sin pago
            var query = _contenedor.Renta
                .GetAll(includeProperties: "Cliente,Vehiculo")
                .Where(r => !rentasConPagoIds.Contains(r.Id));

            if (!string.IsNullOrWhiteSpace(term))
            {
                term = term.Trim();
                query = query.Where(r =>
                    (r.Vehiculo.Placa?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Vehiculo.Marca?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Vehiculo.Modelo?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Cliente.Nombres?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Cliente.Apellidos?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                );
            }

            var items = query
                .Select(r => new
                {
                    id = r.Id,
                    text = $"{r.Vehiculo.Placa} – {r.Vehiculo.Marca} {r.Vehiculo.Modelo} / " +
                           $"{r.Cliente.Nombres} {r.Cliente.Apellidos}"
                })
                .ToList();

            return Json(new { results = items });
        }

        // ----------  EDIT  ----------
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var pago = _context.Pago.Find(id);
            if (pago == null)
                return NotFound();

            var vm = new PagoVM
            {
                Pago = pago,
                ListaRentas = ObtenerListaDeRentas()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, PagoVM vm)
        {
            if (id != vm.Pago.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vm.Pago);
                    _context.SaveChanges();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PagoExists(vm.Pago.Id))
                        return NotFound();
                    throw;
                }
            }

            vm.ListaRentas = ObtenerListaDeRentas();
            return View(vm);
        }

        private bool PagoExists(int id)
        {
            return _context.Pago.Any(e => e.Id == id);
        }

        private IEnumerable<SelectListItem> ObtenerListaDeRentas()
        {
            return _context.Renta
                .Include(r => r.Cliente)
                .Include(r => r.Vehiculo)
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = $"{r.Cliente.Nombres} {r.Cliente.Apellidos} — " +
                           $"{r.Vehiculo.Marca} {r.Vehiculo.Modelo} (Placa: {r.Vehiculo.Placa})"
                })
                .ToList();
        }

        // ----------  DELETE  ----------
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var pago = _context.Pago.Find(id);
            if (pago == null)
                return Json(new { success = false, message = "Pago no encontrado" });

            _context.Pago.Remove(pago);
            _context.SaveChanges();

            return Json(new { success = true, message = "Pago eliminado correctamente" });
        }

        // ----------  RECIBO  ----------
        [HttpGet]
        public IActionResult Recibo(int id)
        {
            var pago = _context.Pago
                .Include(p => p.Renta).ThenInclude(r => r.Cliente)
                .Include(p => p.Renta).ThenInclude(r => r.Vehiculo)
                .AsNoTracking()
                .FirstOrDefault(p => p.Id == id);

            if (pago == null)
                return NotFound();

            // Usamos la vista de Admin para el recibo
            return View("~/Areas/Admin/Views/Pagos/Recibo.cshtml", pago);
        }
    }
}
