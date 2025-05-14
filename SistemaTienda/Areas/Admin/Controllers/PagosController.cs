using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTienda.AccesoDatos.Data.Repository.iRepository;
using SistemaTienda.Data;
using SistemaTienda.Models;
using SistemaTienda.Models.ViewModels;

namespace SistemaTienda.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class PagosController : Controller
    {
        private readonly IContenedorTrabajo _contenedor;
        private readonly ApplicationDbContext _context;

        public PagosController(IContenedorTrabajo contenedorTrabajo,
                               ApplicationDbContext context)
        {
            _contenedor = contenedorTrabajo
                          ?? throw new ArgumentNullException(nameof(contenedorTrabajo));
            _context = context
                          ?? throw new ArgumentNullException(nameof(context));
        }

        // ----------  INDEX & DATATABLE  ----------

        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult GetAll()
        {
            var pagos = _context.Pago
                .Include(p => p.Renta).ThenInclude(r => r.Cliente)
                .Include(p => p.Renta).ThenInclude(r => r.Vehiculo)
                .AsNoTracking()
                .AsEnumerable()   // ← LINQ to Objects
                .Select(p => new
                {
                    id = p.Id,
                    clienteNombre = p.Renta?.Cliente != null
                        ? $"{p.Renta.Cliente.Nombres} {p.Renta.Cliente.Apellidos}"
                        : string.Empty,
                    vehiculoInfo = p.Renta?.Vehiculo != null
                        ? $"{p.Renta.Vehiculo.Marca} {p.Renta.Vehiculo.Modelo}"
                        : string.Empty,
                    monto = p.Monto,
                    fechaPago = p.FechaPago.ToString("yyyy-MM-dd"),
                    estado = p.Estado ?? string.Empty
                })
                .ToList();

            return Json(new { data = pagos });
        }

        // ----------  SELECT2 (Rentas)  ----------

        [HttpGet]
        public IActionResult GetRentas(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new { results = Array.Empty<object>() });

            term = term.Trim();

            var items = _contenedor.Renta
                .GetAll(includeProperties: "Cliente,Vehiculo")
                .AsEnumerable()
                .Where(r =>
                    (r.Vehiculo?.Placa?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Vehiculo?.Marca?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Vehiculo?.Modelo?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Cliente?.Nombres?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Cliente?.Apellidos?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                )
                .Select(r => new
                {
                    id = r.Id,
                    text = $"{r.Vehiculo?.Placa} – {r.Vehiculo?.Marca} {r.Vehiculo?.Modelo} / " +
                           $"{r.Cliente?.Nombres} {r.Cliente?.Apellidos}"
                })
                .ToList();

            return Json(new { results = items });
        }

        // ----------  CREATE  ----------

        [HttpGet]
        public IActionResult Create() =>
            View(new PagoVM { Pago = new Pago() });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(PagoVM vm)
        {
            if (vm.Pago.RentaId == 0)
                ModelState.AddModelError(nameof(vm.Pago.RentaId),
                                         "Debe seleccionar una renta.");

            var renta = _contenedor.Renta.Get(vm.Pago.RentaId);
            if (renta == null)
                ModelState.AddModelError(nameof(vm.Pago.RentaId),
                                         "Renta no válida.");

            if (!ModelState.IsValid)
                return View(vm);

            vm.Pago.Monto = renta.Total;
            vm.Pago.FechaPago = DateTime.Now;
            vm.Pago.Estado = "Pendiente";

            _contenedor.Pago.Add(vm.Pago);
            _contenedor.Save();

            return RedirectToAction(nameof(Index));
        }

        // ----------  EDIT  ----------

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var pago = _contenedor.Pago.Get(id);
            if (pago == null)
                return NotFound();

            return View(new PagoVM { Pago = pago });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(PagoVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            _contenedor.Pago.Update(vm.Pago);
            _contenedor.Save();

            return RedirectToAction(nameof(Index));
        }

        // ----------  DELETE  ----------

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var pago = _contenedor.Pago.Get(id);
            if (pago == null)
                return Json(new
                {
                    success = false,
                    message = "Error eliminando el pago"
                });

            _contenedor.Pago.Remove(pago);
            _contenedor.Save();

            return Json(new
            {
                success = true,
                message = "Pago eliminado correctamente"
            });
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

            return View(pago);
        }
    }
}
