using Microsoft.AspNetCore.Mvc;
using SistemaTienda.Utilidades;

public class RedirectController : Controller
{

    public IActionResult ToAdmin()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole(CNT.Admin))
                return RedirectToAction("Index", "Home", new { area = "Admin" });

            if (User.IsInRole(CNT.Empleado))
                return RedirectToAction("Index", "Home", new { area = "Empleado" });
        }

        return RedirectToPage("/Account/Login", new { area = "Identity" });
    }
}

//using Microsoft.AspNetCore.Http;