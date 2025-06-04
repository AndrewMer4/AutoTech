// wwwroot/js/Empleado/pago.js

var tblPagos;

$(function () {
    initTablaPagos();

    // Filtro por estado: "all", "completed", "pending"
    $('.filter-btn').click(function () {
        $('.filter-btn').removeClass('active');
        $(this).addClass('active');

        let filtro = $(this).data('filter');

        // La columna índice 5 corresponde a "Estado" 
        // (0=clienteNombre, 1=vehiculoInfo, 2=fechaInicio, 3=fechaFin, 4=total, 5=estado, 6=acciones)
        if (filtro === 'all') {
            tblPagos.column(5).search('').draw();
        } else if (filtro === 'completed') {
            tblPagos.column(5).search('Confirmado').draw();
        } else if (filtro === 'pending') {
            tblPagos.column(5).search('Pendiente').draw();
        }
    });
});

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
            // 0) Cliente
            { data: 'clienteNombre', width: '20%' },

            // 1) Vehículo
            { data: 'vehiculoInfo', width: '20%' },

            // 2) Fecha Inicio
            {
                data: 'fechaInicio',
                width: '15%',
                render: function (data) {
                    return new Date(data).toLocaleDateString();
                }
            },

            // 3) Fecha Fin
            {
                data: 'fechaFin',
                width: '15%',
                render: function (data) {
                    return new Date(data).toLocaleDateString();
                }
            },

            // 4) Total
            {
                data: 'total',
                width: '10%',
                render: function (data) {
                    return '$' + parseFloat(data).toFixed(2);
                }
            },

            // 5) Estado
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

            // 6) Acciones (usar pagoId y rentaId)
            {
                data: 'pagoId',
                width: '20%',
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    // row = { id, rentaId, clienteNombre, vehiculoInfo, fechaInicio, fechaFin, total, estado, tienePago, pagoId }
                    if (row.estado === 'Pendiente') {
                        return `
                            <div class="d-flex gap-1 justify-content-center">
                                <button onclick="completarPago(${data}, ${row.rentaId})"
                                        class="btn btn-primary btn-sm">
                                    <i class="fas fa-money-bill-wave"></i> Pagar
                                </button>
                            </div>`;
                    }
                    if (row.estado === 'Confirmado') {
                        return `
                            <div class="d-flex gap-1 justify-content-center">
                                <a href="/Empleado/EmpleadoPagos/Edit/${data}" class="btn btn-success btn-sm">
                                    <i class="far fa-edit"></i> Editar
                                </a>
                                <a href="/Empleado/EmpleadoPagos/Recibo/${data}" class="btn btn-info btn-sm">
                                    <i class="fas fa-file-invoice"></i> Recibo
                                </a>
                            </div>`;
                    }
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
        }
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
                url: '/Empleado/EmpleadoPagos/CompletarPago',
                type: 'POST',
                data: {
                    pagoId: pagoId,
                    rentaId: rentaId
                },
                success: function (response) {
                    if (response.success) {
                        toastr.success(response.message);
                        tblPagos.ajax.reload();
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
