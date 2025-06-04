using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using SistemaTienda.AccesoDatos.Data.Repository.iRepository;
using SistemaTienda.Models;
using SistemaTienda.Models.ViewModels;

namespace SistemaTienda.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class VehiculosController : Controller
    {
        private readonly IContenedorTrabajo _contenedorTrabajo;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public VehiculosController(
            IContenedorTrabajo contenedorTrabajo,
            IWebHostEnvironment hostingEnvironment)
        {
            _contenedorTrabajo = contenedorTrabajo
                                    ?? throw new ArgumentNullException(nameof(contenedorTrabajo));
            _hostingEnvironment = hostingEnvironment
                                    ?? throw new ArgumentNullException(nameof(hostingEnvironment));
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult GetAll()
        {
            var vehiculos = _contenedorTrabajo.Vehiculo.GetAll().ToList();
            return Json(new { data = vehiculos });
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Vehiculo vehiculo)
        {
            // Validar si la placa ya existe
            if (_contenedorTrabajo.Vehiculo.Exists(v => v.Placa == vehiculo.Placa))
            {
                ModelState.AddModelError("Placa", "Esta placa ya está registrada");
            }

            if (!ModelState.IsValid)
                return View(vehiculo);

            var archivo = HttpContext.Request.Form.Files.FirstOrDefault();
            if (archivo != null)
            {
                var wwwRoot = _hostingEnvironment.WebRootPath;
                var fileName = Guid.NewGuid() + Path.GetExtension(archivo.FileName);
                var carpeta = Path.Combine(wwwRoot, "imagenes", "vehiculos");
                Directory.CreateDirectory(carpeta);
                var ruta = Path.Combine(carpeta, fileName);

                using var stream = new FileStream(ruta, FileMode.Create);
                archivo.CopyTo(stream);

                vehiculo.UrlImagen = $"/imagenes/vehiculos/{fileName}";
            }

            _contenedorTrabajo.Vehiculo.Add(vehiculo);
            _contenedorTrabajo.Save();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest();

            var vehiculo = _contenedorTrabajo.Vehiculo
                .GetFirstOrDefault(v => v.Placa == id);
            if (vehiculo == null)
                return NotFound();

            return View(vehiculo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Vehiculo vehiculo)
        {
            // Validar si la placa fue modificada (aunque el campo es readonly, es buena práctica verificarlo)
            var dbVeh = _contenedorTrabajo.Vehiculo
                .GetFirstOrDefault(v => v.Placa == vehiculo.Placa);

            if (dbVeh == null)
            {
                ModelState.AddModelError(string.Empty, "Vehículo no encontrado");
                return View(vehiculo);
            }

            if (!ModelState.IsValid)
                return View(vehiculo);

            var archivo = HttpContext.Request.Form.Files.FirstOrDefault();
            if (archivo != null)
            {
                if (!string.IsNullOrEmpty(dbVeh.UrlImagen))
                {
                    var vieja = Path.Combine(
                        _hostingEnvironment.WebRootPath,
                        dbVeh.UrlImagen.TrimStart('/'));
                    if (System.IO.File.Exists(vieja))
                        System.IO.File.Delete(vieja);
                }

                var wwwRoot = _hostingEnvironment.WebRootPath;
                var fileName = Guid.NewGuid() + Path.GetExtension(archivo.FileName);
                var carpeta = Path.Combine(wwwRoot, "imagenes", "vehiculos");
                Directory.CreateDirectory(carpeta);
                var ruta = Path.Combine(carpeta, fileName);

                using var stream = new FileStream(ruta, FileMode.Create);
                archivo.CopyTo(stream);

                dbVeh.UrlImagen = $"/imagenes/vehiculos/{fileName}";
            }

            dbVeh.Marca = vehiculo.Marca;
            dbVeh.Modelo = vehiculo.Modelo;
            dbVeh.Anio = vehiculo.Anio;
            dbVeh.Kilometraje = vehiculo.Kilometraje;
            dbVeh.Estado = vehiculo.Estado;
            dbVeh.PrecioPorDia = vehiculo.PrecioPorDia;

            _contenedorTrabajo.Vehiculo.Update(dbVeh);
            _contenedorTrabajo.Save();
            return RedirectToAction(nameof(Index));
        }
      
        [HttpGet]
        public IActionResult Mantenimiento()
        {
            var vehiculosEnMantenimiento = _contenedorTrabajo.Vehiculo
                .GetAll(v => v.Estado == "En Mantenimiento")
                .ToList();

            var vm = new MantenimientoVM
            {
                VehiculosEnMantenimiento = vehiculosEnMantenimiento
            };

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarADisponible(string placa)
        {
            if (string.IsNullOrEmpty(placa))
                return Json(new { success = false, message = "Placa inválida" });

            var vehiculo = _contenedorTrabajo.Vehiculo
                .GetFirstOrDefault(v => v.Placa == placa);

            if (vehiculo == null)
                return Json(new { success = false, message = "Vehículo no encontrado" });

            vehiculo.Estado = "Disponible";
            _contenedorTrabajo.Vehiculo.Update(vehiculo);
            _contenedorTrabajo.Save();

            return Json(new { success = true, message = "Vehículo marcado como disponible" });
        }

        // Ahora recibe la placa como "id" de la ruta /Delete/{id}
        [HttpDelete]
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return Json(new { success = false, message = "Placa inválida" });

            var vehiculo = _contenedorTrabajo.Vehiculo
                .GetFirstOrDefault(v => v.Placa == id);
            if (vehiculo == null)
                return Json(new { success = false, message = "Vehículo no encontrado" });

            bool tieneRentas = _contenedorTrabajo.Renta
                .GetAll(r => r.VehiculoId == id)
                .Any();
            if (tieneRentas)
                return Json(new { success = false, message = "No se puede eliminar: tiene rentas asociadas" });

            if (!string.IsNullOrEmpty(vehiculo.UrlImagen))
            {
                var vieja = Path.Combine(
                    _hostingEnvironment.WebRootPath,
                    vehiculo.UrlImagen.TrimStart('/'));
                if (System.IO.File.Exists(vieja))
                    System.IO.File.Delete(vieja);
            }

            _contenedorTrabajo.Vehiculo.Remove(vehiculo);
            _contenedorTrabajo.Save();
            return Json(new { success = true, message = "Vehículo eliminado correctamente" });
        }
    }
}