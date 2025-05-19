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
            _contenedor = contenedor;
            _context = context;
        }

        // ----------  INDEX & DATATABLE  ----------
        [HttpGet] public IActionResult Index() => View();

        [HttpGet]
        public IActionResult GetAll()
        {
            var data = _context.Pago
                .Include(p => p.Renta).ThenInclude(r => r.Cliente)
                .Include(p => p.Renta).ThenInclude(r => r.Vehiculo)
                .AsNoTracking()
                .Select(p => new {
                    id = p.Id,
                    cliente = $"{p.Renta.Cliente.Nombres} {p.Renta.Cliente.Apellidos}",
                    vehiculo = $"{p.Renta.Vehiculo.Marca} {p.Renta.Vehiculo.Modelo}",
                    monto = p.Monto,
                    fechaPago = p.FechaPago.ToString("yyyy-MM-dd HH:mm"),
                    estado = p.Estado
                });
            return Json(new { data });
        }

        // ----------  CREATE ----------
        [HttpGet]
        public IActionResult Create() => View(BuildVM(new Pago()));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(PagoVM vm)
        {
            if (!ModelState.IsValid)
                return View(BuildVM(vm.Pago));

            var renta = _contenedor.Renta.Get(vm.Pago.RentaId);
            if (renta is null)
            {
                ModelState.AddModelError("", "Renta no válida.");
                return View(BuildVM(vm.Pago));
            }

            vm.Pago.Monto = renta.Total;
            vm.Pago.FechaPago = DateTime.Now;
            vm.Pago.Estado = "Pendiente";

            _contenedor.Pago.Add(vm.Pago);
            _contenedor.Save();
            return RedirectToAction(nameof(Index));
        }

        // ----------  EDIT ----------
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var pago = _contenedor.Pago.Get(id);
            if (pago is null) return NotFound();
            return View(BuildVM(pago));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(PagoVM vm)
        {
            if (!ModelState.IsValid)
                return View(BuildVM(vm.Pago));

            _contenedor.Pago.Update(vm.Pago);
            _contenedor.Save();
            return RedirectToAction(nameof(Index));
        }

        // ---------- DELETE ----------
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var pago = _contenedor.Pago.Get(id);
            if (pago is null)
                return Json(new { success = false, message = "Pago no encontrado." });

            _contenedor.Pago.Remove(pago);
            _contenedor.Save();
            return Json(new { success = true, message = "Pago eliminado." });
        }

        // ---------- RECIBO ----------
        [HttpGet]
        public IActionResult Recibo(int id)
        {
            var pago = _context.Pago
                .Include(p => p.Renta).ThenInclude(r => r.Cliente)
                .Include(p => p.Renta).ThenInclude(r => r.Vehiculo)
                .AsNoTracking()
                .FirstOrDefault(p => p.Id == id);
            return pago is null ? NotFound() : View("~/Areas/Admin/Views/Pagos/Recibo.cshtml", pago);
        }

       
        private PagoVM BuildVM(Pago pago) =>
            new()
            {
                Pago = pago,
                ListaRentas = _contenedor.Renta
                    .GetAll(includeProperties: "Cliente,Vehiculo")
                    .Select(r => new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = $"{r.Cliente.Nombres} {r.Cliente.Apellidos} — {r.Vehiculo.Marca} {r.Vehiculo.Modelo}",
                        Selected = r.Id == pago.RentaId
                    })
            };
    }
}
