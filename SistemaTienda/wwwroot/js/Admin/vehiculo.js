var dataTable;

$(function () {
    cargarDataTable();
});

function cargarDataTable() {
    dataTable = $("#tblVehiculos").DataTable({
        ajax: {
            url: "/Admin/Vehiculos/GetAll",
            type: "GET",
            dataType: "json",
            dataSrc: "data"
        },
        columns: [
            { data: "placa", width: "10%" },
            { data: "marca", width: "15%" },
            { data: "modelo", width: "15%" },
            { data: "anio", width: "10%" },
            { data: "kilometraje", width: "10%" },
            { data: "estado", width: "10%" },
            {
                data: "precioPorDia",
                width: "10%",
                render: d => "$" + parseFloat(d).toFixed(2)
            },
            {
                data: "urlImagen",
                width: "10%",
                render: d => d
                    ? `<img src="${d}" style="width:80px;" />`
                    : ""
            },
            {
                data: "placa",
                render: placa => `
    <div class="d-flex justify-content-center gap-2">
      <a href="/Admin/Vehiculos/Edit/${placa}" class="btn btn-success btn-sm">
        <i class="far fa-edit"></i> Editar
      </a>
      <button onclick="Delete('/Admin/Vehiculos/Delete/${placa}')" class="btn btn-danger btn-sm">
        <i class="far fa-trash-alt"></i> Borrar
      </button>
    </div>`
            }
        ],
        language: {
            emptyTable: "No hay registros",
            info: "Mostrando _START_ a _END_ de _TOTAL_",
            infoEmpty: "Mostrando 0 a 0 de 0",
            infoFiltered: "(filtrado de _MAX_ totales)",
            lengthMenu: "Mostrar _MENU_ registros",
            search: "Buscar:",
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
        title: "¿Está seguro de borrar?",
        text: "¡Este contenido no se puede recuperar!",
        type: "warning",
        showCancelButton: true,
        confirmButtonText: "Sí, borrar!"
    }, function () {
        $.ajax({
            type: 'DELETE',
            url: url,
            success: function (data) {
                if (data.success) {
                    toastr.success(data.message);
                    dataTable.ajax.reload();
                } else {
                    toastr.error(data.message);
                }
            },
            error: function () {
                toastr.error("Error en el servidor.");
            }
        });
    });
}