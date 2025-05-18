/* Pago – Empleado */
var tblPagos;

$(() => initTablaPagos());

function initTablaPagos() {
    tblPagos = $("#tblPagos").DataTable({
        responsive: true,
        ajax: {
            url: "/Empleado/EmpleadoPagos/GetAll",
            type: "GET",
            dataType: "json",
            dataSrc: "data"
        },
        columns: [
            { data: "id", width: "5%" },
            { data: "cliente", width: "20%" },
            { data: "vehiculo", width: "20%" },
            {
                data: "monto", width: "10%",
                render: d => "$" + parseFloat(d).toFixed(2)
            },
            { data: "fechaPago", width: "15%" },
            { data: "estado", width: "10%" },
            {
                data: "id",
                width: "20%",
                orderable: false,
                searchable: false,
                render: id => `
  <div class="d-flex justify-content-center gap-2">
    <a href="/Empleado/EmpleadoPagos/Edit/${id}" class="btn btn-success btn-sm">
      <i class="fas fa-edit"></i> Editar
    </a>
    <button onclick="borrarPago('/Empleado/EmpleadoPagos/Delete/${id}')" class="btn btn-danger btn-sm">
      <i class="fas fa-trash-alt"></i> Borrar
    </button>
    <a href="/Empleado/EmpleadoPagos/Recibo/${id}" target="_blank" class="btn btn-primary btn-sm">
      <i class="fas fa-file-invoice"></i> Recibo
    </a>
  </div>`
            }
        ],
        language: idiomaES,
        width: "100%"
    });
}

function borrarPago(url) {
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
                tblPagos.ajax.reload();
            },
            error: () => toastr.error("Error en el servidor.")
        });
    });
}

/* Diccionario ES común a todos los datatables */
const idiomaES = {
    emptyTable: "No hay registros",
    info: "Mostrando _START_ a _END_ de _TOTAL_ entradas",
    infoEmpty: "Mostrando 0 a 0 de 0 entradas",
    infoFiltered: "(filtrado de _MAX_ totales)",
    lengthMenu: "Mostrar _MENU_ entradas",
    loadingRecords: "Cargando...",
    processing: "Procesando...",
    search: "Buscar:",
    zeroRecords: "Sin resultados encontrados",
    paginate: {
        first: "Primero", last: "Último",
        next: "Siguiente", previous: "Anterior"
    }
};
