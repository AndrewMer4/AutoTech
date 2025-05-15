var dataTable;

$(function () {
    cargarDataTable();
});

function cargarDataTable() {
    dataTable = $("#tblRentas").DataTable({
        ajax: {
            url: "/Admin/Rentas/GetAll",
            type: "GET",
            datatype: "json"
        },
        columns: [
            {
                data: "cliente",
                title: "Cliente",
                width: "20%"
            },
            {
                data: "vehiculo",
                title: "Vehículo",
                width: "20%"
            },
            {
                data: "fechaInicio",
                title: "Fecha Inicio",
                width: "15%"
            },
            {
                data: "fechaFin",
                title: "Fecha Fin",
                width: "15%"
            },
            {
                data: "total",
                title: "Total",
                width: "10%",
                render: val => "$" + parseFloat(val).toFixed(2)
            },
            // Pagos (incluye además el botón de Recibo)
            {
                data: "id",
                render: id => `
    <div class="d-flex justify-content-center gap-2">
      <a href="/Admin/Rentas/Edit/${id}" class="btn btn-success btn-sm">
        <i class="far fa-edit"></i> Editar
      </a>
      <button onclick="Delete('/Admin/Rentas/Delete/${id}')" class="btn btn-danger btn-sm">
        <i class="far fa-trash-alt"></i> Borrar
      </button>
    </div>`
            }
        ],
        language: {
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
                first: "Primero",
                last: "Último",
                next: "Siguiente",
                previous: "Anterior"
            }
        },
        width: "100%"
    });
}

function Delete(url) {
    swal({
        title: "¿Estás seguro?",
        text: "¡Este registro no se podrá recuperar!",
        icon: "warning",
        buttons: ["Cancelar", "Sí, borrar"]
    }).then(willDelete => {
        if (willDelete) {
            $.ajax({
                type: "DELETE",
                url: url,
                success: data => {
                    if (data.success) toastr.success(data.message);
                    else toastr.error(data.message);
                    dataTable.ajax.reload();
                },
                error: () => toastr.error("Error en el servidor.")
            });
        }
    });
}
