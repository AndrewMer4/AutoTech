var dataTable;

$(function () {
    cargarDataTable();
});

function cargarDataTable() {
    dataTable = $("#tblPagos").DataTable({
        ajax: {
            url: "/Admin/Pagos/GetAll",
            type: "GET",
            dataType: "json"    // ¡asegúrate de que es dataType!
        },
        columns: [
            { data: "id", width: "5%" },
            { data: "clienteNombre", width: "20%" },
            { data: "vehiculoInfo", width: "20%" },
            {
                data: "monto",
                render: d => "$" + parseFloat(d).toFixed(2),
                width: "10%"
            },
            {
                data: "fechaPago",
                render: d => new Date(d).toLocaleDateString(),
                width: "15%"
            },
            { data: "estado", width: "10%" },
            {
                data: "id",
                render: id => `
      <div class="d-flex justify-content-center gap-2">
        <a href="/Admin/Pagos/Edit/${id}" class="btn btn-success btn-sm">
          <i class="far fa-edit"></i> Editar
        </a>
        <button onclick="Delete('/Admin/Pagos/Delete/${id}')" class="btn btn-danger btn-sm">
          <i class="far fa-trash-alt"></i> Eliminar
        </button>
        <a href="/Admin/Pagos/Recibo/${id}" target="_blank" class="btn btn-primary btn-sm">
          <i class="fas fa-file-invoice"></i> Recibo
        </a>
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
        responsive: true,
        width: "100%"
    });
}
z