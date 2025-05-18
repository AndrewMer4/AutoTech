var tblRentas;

$(() => initTablaRentas());

function initTablaRentas() {
    tblRentas = $("#tblRentas").DataTable({
        responsive: true,
        ajax: {
            url: "/Empleado/EmpleadoRentas/GetAll",
            type: "GET",
            dataType: "json",
            dataSrc: "data"
        },
        columns: [
            { data: "cliente", width: "20%" },
            { data: "vehiculo", width: "20%" },
            { data: "fechaInicio", width: "15%" },
            { data: "fechaFin", width: "15%" },
            {
                data: "total", width: "10%",
                render: d => "$" + parseFloat(d).toFixed(2)
            },
            {
                data: "id",
                width: "20%",
                orderable: false,
                searchable: false,
                render: id => `
  <div class="d-flex justify-content-center gap-2">
    <a href="/Empleado/EmpleadoRentas/Edit/${id}" class="btn btn-success btn-sm">
      <i class="far fa-edit"></i> Editar
    </a>
    <button onclick="borrarRenta('/Empleado/EmpleadoRentas/Delete/${id}')" class="btn btn-danger btn-sm">
      <i class="far fa-trash-alt"></i> Borrar
    </button>
  </div>`
            }
        ],
        language: idiomaES,
        width: "100%"
    });
}

function borrarRenta(url) {
    swal({
        title: "¿Está seguro de borrar?",
        text: "¡Este registro no se podrá recuperar!",
        icon: "warning",
        buttons: ["Cancelar", "Sí, borrar"],
        dangerMode: true
    }).then(conf => {
        if (!conf) return;
        $.ajax({
            type: "DELETE",
            url,
            success: r => {
                r.success ? toastr.success(r.message)
                    : toastr.error(r.message);
                tblRentas.ajax.reload();
            },
            error: () => toastr.error("Error en el servidor.")
        });
    });
}

/* Re-uso el mismo diccionario del otro archivo */
importIdioma();   // 👉 pequeño truco para no duplicar código
function importIdioma() {/* ocupado en runtim */ }
