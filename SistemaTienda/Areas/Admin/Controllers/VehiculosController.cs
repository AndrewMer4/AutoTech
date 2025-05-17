using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using SistemaTienda.AccesoDatos.Data.Repository.iRepository;
using SistemaTienda.Models;
using System;
using System.IO;
using System.Linq;

namespace SistemaTienda.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class VehiculosController : Controller
    {
        private readonly IContenedorTrabajo _contenedorTrabajo;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public VehiculosController(
            IContenedorTrabajo contenedorTrabajo,
            IWebHostEnvironment hostingEnvironment)
        {
            _contenedorTrabajo = contenedorTrabajo;
            _hostingEnvironment = hostingEnvironment;
        }

        
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        
        [HttpGet]
        public IActionResult GetAll()
        {
            var vehiculos = _contenedorTrabajo.Vehiculo.GetAll();
            return Json(new { data = vehiculos });
        }

      
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Vehiculo vehiculo)
        {
            if (ModelState.IsValid)
            {
                var archivo = HttpContext.Request.Form.Files.FirstOrDefault();
                if (archivo != null)
                {
                    var wwwRoot = _hostingEnvironment.WebRootPath;
                    var nombre = Guid.NewGuid() + Path.GetExtension(archivo.FileName);
                    var carpeta = Path.Combine(wwwRoot, "imagenes/vehiculos");
                    Directory.CreateDirectory(carpeta);
                    var ruta = Path.Combine(carpeta, nombre);

                    using var stream = new FileStream(ruta, FileMode.Create);
                    archivo.CopyTo(stream);

                    vehiculo.UrlImagen = "/imagenes/vehiculos/" + nombre;
                }

                _contenedorTrabajo.Vehiculo.Add(vehiculo);
                _contenedorTrabajo.Save();
                return RedirectToAction(nameof(Index));
            }
            return View(vehiculo);
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
            if (ModelState.IsValid)
            {
                var dbVeh = _contenedorTrabajo.Vehiculo
                    .GetFirstOrDefault(v => v.Placa == vehiculo.Placa);
                if (dbVeh == null)
                    return NotFound();

                var archivo = HttpContext.Request.Form.Files.FirstOrDefault();
                if (archivo != null)
                {
                    if (!string.IsNullOrEmpty(dbVeh.UrlImagen))
                    {
                        var antigua = Path.Combine(
                            _hostingEnvironment.WebRootPath,
                            dbVeh.UrlImagen.TrimStart('/'));
                        if (System.IO.File.Exists(antigua))
                            System.IO.File.Delete(antigua);
                    }

                    var wwwRoot = _hostingEnvironment.WebRootPath;
                    var nombre = Guid.NewGuid() + Path.GetExtension(archivo.FileName);
                    var carpeta = Path.Combine(wwwRoot, "imagenes/vehiculos");
                    Directory.CreateDirectory(carpeta);
                    var ruta = Path.Combine(carpeta, nombre);

                    using var stream = new FileStream(ruta, FileMode.Create);
                    archivo.CopyTo(stream);

                    dbVeh.UrlImagen = "/imagenes/vehiculos/" + nombre;
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
            return View(vehiculo);
        }

        [HttpDelete]
        public IActionResult Delete(string placa)
        {
            var vehiculo = _contenedorTrabajo.Vehiculo.GetFirstOrDefault(v => v.Placa == placa);
            if (vehiculo == null)
                return Json(new { success = false, message = "Vehículo no encontrado" });

            bool tieneRentas = _contenedorTrabajo.Renta.GetAll(r => r.VehiculoId == placa).Any();
            if (tieneRentas)
                return Json(new { success = false, message = "No se puede eliminar el vehículo porque tiene rentas asociadas" });

            // Eliminar imagen si existe
            if (!string.IsNullOrEmpty(vehiculo.UrlImagen))
            {
                var imagenPath = Path.Combine(_hostingEnvironment.WebRootPath, vehiculo.UrlImagen.TrimStart('/'));
                if (System.IO.File.Exists(imagenPath))
                {
                    System.IO.File.Delete(imagenPath);
                }
            }

            _contenedorTrabajo.Vehiculo.Remove(vehiculo);
            _contenedorTrabajo.Save();

            return Json(new { success = true, message = "Vehículo eliminado correctamente" });
        }
    }
}
