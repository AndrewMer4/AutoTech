// wwwroot/js/Admin/pago.js

var dataTable;

$(function () {
    cargarDataTable();

    // Filtro por estado
    $('.filter-btn').click(function () {
        $('.filter-btn').removeClass('active');
        $(this).addClass('active');

        let filtro = $(this).data('filter');

        if (filtro === 'all') {
            dataTable.column(5).search('').draw();
        } else if (filtro === 'completed') {
            dataTable.column(5).search('Confirmado').draw();
        } else if (filtro === 'pending') {
            dataTable.column(5).search('Pendiente').draw();
        }
    });
});

function cargarDataTable() {
    dataTable = $('#tblPagos').DataTable({
        ajax: {
            url: '/Admin/Pagos/GetAll',
            type: 'GET',
            dataType: 'json',
            dataSrc: 'data'
        },
        columns: [
            { data: 'clienteNombre', width: '20%' },
            { data: 'vehiculoInfo', width: '20%' },
            {
                data: 'fechaInicio',
                width: '15%',
                render: function (data) {
                    return new Date(data).toLocaleDateString();
                }
            },
            {
                data: 'fechaFin',
                width: '15%',
                render: function (data) {
                    return new Date(data).toLocaleDateString();
                }
            },
            {
                data: 'total',
                width: '10%',
                render: function (data) {
                    return '$' + parseFloat(data).toFixed(2);
                }
            },
            {
                data: 'estado',
                width: '10%',
                render: function (data) {
                    var badgeClass = data === 'Confirmado'
                        ? 'bg-success'
                        : data === 'Pendiente'
                            ? 'bg-warning text-dark'
                            : 'bg-secondary';
                    return `<span class="badge ${badgeClass}">${data}</span>`;
                }
            },
            {
                data: 'pagoId',
                width: '20%',
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    // “row” contiene { id: pagoId, rentaId, clienteNombre, …, estado, pagoId }
                    // Si está “Pendiente” → mostrar solo “Pagar” (pasando ambos: pagoId y rentaId)
                    if (row.estado === 'Pendiente') {
                        return `
                            <div class="d-flex gap-1 justify-content-center">
                                <button onclick="completarPago(${data}, ${row.rentaId})"
                                        class="btn btn-primary btn-sm">
                                    <i class="fas fa-money-bill-wave"></i> Pagar
                                </button>
                            </div>`;
                    }

                    // Si está “Confirmado” → mostrar “Editar” + “Recibo”
                    if (row.estado === 'Confirmado') {
                        return `
                            <div class="d-flex gap-1 justify-content-center">
                                <a href="/Admin/Pagos/Edit/${data}" class="btn btn-success btn-sm">
                                    <i class="far fa-edit"></i> Editar
                                </a>
                                <a href="/Admin/Pagos/Recibo/${data}" class="btn btn-info btn-sm">
                                    <i class="fas fa-file-invoice"></i> Recibo
                                </a>
                            </div>`;
                    }

                    // Para cualquier otro estado (si acaso)
                    return `<span class="text-muted">--</span>`;
                }
            }
        ],
        language: {
            emptyTable: "No hay registros de pagos",
            info: "Mostrando _START_ a _END_ de _TOTAL_ entradas",
            lengthMenu: "Mostrar _MENU_ entradas",
            loadingRecords: "Cargando...",
            processing: "Procesando...",
            search: "Buscar:",
            paginate: {
                first: "Primero",
                last: "Último",
                next: "Siguiente",
                previous: "Anterior"
            }
        },
        responsive: true,
        width: '100%'
    });
}

function completarPago(pagoId, rentaId) {
    Swal.fire({
        title: "¿Completar pago?",
        text: "¿Desea marcar este pago como completado?",
        icon: "warning",
        showCancelButton: true,
        confirmButtonText: "Sí, completar",
        cancelButtonText: "Cancelar"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/Admin/Pagos/CompletarPago',
                type: 'POST',
                data: {
                    pagoId: pagoId,   // Será 0 si aún no existe un pago en BD
                    rentaId: rentaId   // ID de la renta asociada
                },
                success: function (response) {
                    if (response.success) {
                        toastr.success(response.message);
                        dataTable.ajax.reload();
                    } else {
                        toastr.error(response.message);
                    }
                },
                error: function () {
                    toastr.error("Error al procesar la solicitud");
                }
            });
        }
    });
}
