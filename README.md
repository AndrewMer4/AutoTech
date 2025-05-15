# AutoTech Rent a Car - Sistema de Gestión de Alquiler de Vehículos

## Descripción del Proyecto

AutoTech Rent a Car es una empresa dedicada al alquiler de vehículos. Este proyecto consiste en el desarrollo de un sistema web interno que optimiza la gestión de la flota vehicular, reservas, contratos y mantenimiento, eliminando procesos manuales y reduciendo errores administrativos.

## Tabla de Contenidos

1. Características
2. Arquitectura y Tecnologías
3. Pre-requisitos
4. Instalación paso a paso
5. Uso del sistema
6. Pruebas
7. Roadmap
8. Contribuciones
9. Licencia
10. Contacto

## Características

- **Gestión de flota**: altas, bajas y edición de vehículos con imágenes, kilometraje y precio por día.
- **Reservas y contratos**: creación rápida de contratos, cálculo automático de costos y generación de PDF.
- **Mantenimiento preventivo**: historial de servicios, alertas por kilometraje y fechas.
- **Roles y permisos**: autenticación con _Identity_, roles _Admin_ y _Empleado_ y autorización granular.
- **Reportes**: estadísticas de ocupación, ingresos y mantenimiento exportables a CSV/PDF.
- **Integración Select2 & DataTables**: búsqueda avanzada y tablas responsivas.
- **Diseño responsive**: interfaz moderna con Bootstrap 5 y fuente **Bebas Neue**.

## Arquitectura y Tecnologías

**Frontend:** ASP.NET Razor Views, Bootstrap 5, jQuery, DataTables, Select2  
**Backend:** ASP.NET Core 7 MVC con controladores RESTful y AutoMapper  
**Persistencia:** Entity Framework Core y SQL Server Express  
**Seguridad:** ASP.NET Identity con hashing PBKDF2  
**DevOps:** GitHub Actions para CI/CD y cobertura de pruebas con Coverlet  

## Pre-requisitos

- Visual Studio 2022 o superior
- .NET 7 SDK
- SQL Server 2019+ (o Docker con la imagen oficial)
- Node.js >= 18 (para dependencias front-end opcionales)

## Uso del sistema

1. **Iniciar sesión** como _Admin_ o _Empleado_.
2. **Vehículos** → agregar o editar información, subir imágenes.
3. **Rentas** → seleccionar cliente y vehículo, definir fechas y confirmar contrato.
4. **Pagos** → registrar abonos y generar recibos.
5. **Reportes** → descargar estadísticas en PDF/CSV.


## Roadmap

- [ ] Integrar pasarela de pago Stripe
- [ ] Módulo de notificaciones por correo/SMS
- [ ] App móvil React Native (lectura de contratos y check-in vehicular)
- [ ] Soporte multilenguaje (ES-SV / EN-US)

## Contribuciones

Las contribuciones son bienvenidas 🧡. Para comenzar:

1. Abre un **Issue** describiendo la mejora o bug.
2. Crea un **branch** con el prefijo `feature/` o `bugfix/`.
3. Envía un **Pull Request** con una descripción clara y referencia al Issue.
4. Espera la revisión y al menos **1 aprobación** de un colaborador.

Consulta `CONTRIBUTING.md` para la guía completa y estándares de código.

## Licencia

Distribuido bajo la licencia MIT. Consulta `LICENSE` para más detalles.

## Contacto

- **Líder Técnico:** Juan Pérez – juan.perez@autotech.com  
- **Product Owner:** María Gómez – maria.gomez@autotech.com  

---

> "Conducir la innovación, mantenernos en movimiento." 🚗💨
