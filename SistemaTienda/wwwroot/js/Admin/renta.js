// wwwroot/js/admin-rentas.js (o en la vista correspondiente)
var dataTable;

$(function () {
    initSelect2();
    cargarDataTable();
});

// 1) Inicializa los <select> de Cliente y Vehículo en Create/Edit
function initSelect2() {
    $("select.js-select2-client, select.js-select2-vehiculo").each(function () {
        var $el = $(this);
        var url = $el.data("url");
        var initId = $el.data("init-id");
        var initText = $el.data("init-text");

        $el.select2({
            placeholder: $el.data("placeholder") || "Escriba para buscar…",
            allowClear: true,
            ajax: {
                url: url,
                dataType: "json",
                delay: 250,
                data: function (params) {
                    return { term: params.term };
                },
                processResults: function (data) {
                    return { results: data.results };
                }
            },
            minimumInputLength: 1,
            width: "100%"
        });

        // Preselecciona la opción en Edit
        if (initId && initText) {
            var option = new Option(initText, initId, true, true);
            $el.append(option).trigger("change");
        }
    });
}

// 2) Carga la DataTable de rentas con lógica de “Renta finalizada”
function cargarDataTable() {
    dataTable = $("#tblRentas").DataTable({
        responsive: true,
        ajax: {
            url: "/Admin/Rentas/GetAll",
            type: "GET",
            dataType: "json",
            dataSrc: "data"
        },
        columns: [
            { data: "cliente", title: "Cliente", width: "20%" },
            { data: "vehiculo", title: "Vehículo", width: "20%" },
            { data: "fechaInicio", title: "Inicio", width: "15%" },
            { data: "fechaFin", title: "Fin", width: "15%" },
            {
                data: "total",
                title: "Total",
                width: "10%",
                render: function (val) {
                    return "$" + parseFloat(val).toFixed(2);
                }
            },
            {
                data: null,
                title: "Acciones",
                width: "20%",
                orderable: false,
                searchable: false,
                className: "text-center",
                render: function (data, type, row) {
                    // 1) Renta NO vencida (fechaFin > hoy): mostrar Editar + Borrar
                    if (!row.esVencida) {
                        return `
                            <div class="d-flex justify-content-center gap-2">
                                <a href="/Admin/Rentas/Edit/${data.id}" 
                                   class="btn btn-success btn-sm">
                                    <i class="far fa-edit"></i>
                                </a>
                             
                            </div>
                        `;
                    }

                    // 2) Renta vencida (fechaFin ≤ hoy): mostrar solo “Renta finalizada”
                    return `<span class="fw-bold text-success">Renta finalizada</span>`;
                }
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

// 3) Función para borrar una renta
function Delete(url) {
    Swal.fire({
        title: "¿Está seguro de borrar?",
        text: "¡Este contenido no se puede recuperar!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonText: "Sí, borrar!"
    }).then((result) => {
        if (result.isConfirmed) {
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
        }
    });
}
