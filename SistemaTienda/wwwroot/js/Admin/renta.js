// wwwroot/js/Admin/renta.js

// Función de borrado reutilizable
function DeleteRenta(url) {
    swal({
        title: "¿Estás seguro?",
        text: "¡Este registro no se podrá recuperar!",
        icon: "warning",
        buttons: ["Cancelar", "Sí, borrar"],
        dangerMode: true,
    }, function (willDelete) {
        if (!willDelete) return;
        $.ajax({
            type: "DELETE",
            url: url,
            success: function (res) {
                if (res.success) toastr.success(res.message);
                else toastr.error(res.message);
                if ($.fn.DataTable.isDataTable("#tblRentas")) {
                    $("#tblRentas").DataTable().ajax.reload();
                }
            },
            error: function () {
                toastr.error("Error en el servidor.");
            }
        });
    });
}

document.addEventListener("DOMContentLoaded", function () {
    // 1) Inicializar DataTable si existe
    var $tbl = $("#tblRentas");
    if ($tbl.length) {
        $tbl.DataTable({
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
                { data: "fechaInicio", title: "Fecha Inicio", width: "15%" },
                { data: "fechaFin", title: "Fecha Fin", width: "15%" },
                {
                    data: "total", title: "Total", width: "10%",
                    render: function (val) { return "$" + parseFloat(val).toFixed(2); }
                },
                {
                    data: "id", title: "Acciones", width: "20%",
                    orderable: false,
                    searchable: false,
                    render: function (id) {
                        return `
  <div class="d-flex justify-content-center gap-2">
    <a href="/Admin/Rentas/Edit/${id}" class="btn btn-success btn-sm">
      <i class="far fa-edit"></i> Editar
    </a>
    <button onclick="DeleteRenta('/Admin/Rentas/Delete/${id}')" class="btn btn-danger btn-sm">
      <i class="far fa-trash-alt"></i> Borrar
    </button>
  </div>`;
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
                    first: "Primero", last: "Último",
                    next: "Siguiente", previous: "Anterior"
                }
            },
            width: "100%"
        });
    }

    // 2) Inicializar Select2 en cualquier <select class="js-select2">
    document.querySelectorAll("select.js-select2").forEach(function (el) {
        var $el = $(el);
        var url = el.dataset.url;
        var initId = el.dataset.initId;
        var initText = el.dataset.initText;

        $el.select2({
            placeholder: el.dataset.placeholder || "Escriba para buscar…",
            allowClear: true,
            ajax: {
                url: url,
                dataType: "json",
                delay: 250,
                data: function (params) { return { term: params.term }; },
                processResults: function (data) { return { results: data.results }; }
            },
            minimumInputLength: 1,
            width: "100%"
        });

        // Pre-seleccionar la opción existente
        if (initId && initText) {
            var option = new Option(initText, initId, true, true);
            $el.append(option).trigger("change");
        }
    });
});
