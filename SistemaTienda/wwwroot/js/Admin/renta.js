// wwwroot/js/Admin/renta.js

var dataTable;

$(function () {
    initSelect2();
    cargarDataTable();
});

// 1) Inicializa todos los <select class="js-select2"> (Create/Edit)
function initSelect2() {
    $("select.js-select2").each(function () {
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
                data: params => ({ term: params.term }),
                processResults: data => ({ results: data.results })
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

// 2) Carga la DataTable de rentas
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
            { data: "fechaInicio", title: "Fecha Inicio", width: "15%" },
            { data: "fechaFin", title: "Fecha Fin", width: "15%" },
            {
                data: "total",
                title: "Total",
                width: "10%",
                render: val => "$" + parseFloat(val).toFixed(2)
            },
            {
                data: "id",
                title: "Acciones",
                width: "15%",
                orderable: false,
                searchable: false,
                render: id => `
  <div class="d-flex justify-content-center gap-2">
    <a href="/Admin/Rentas/Edit/${id}" class="btn btn-success btn-sm">
      <i class="far fa-edit"></i> Editar
    </a>
    <button onclick="DeleteRenta('/Admin/Rentas/Delete/${id}')" class="btn btn-danger btn-sm">
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

// 3) Elimina una renta usando POST + antiforgery
function DeleteRenta(url) {
    var token = $('input[name="__RequestVerificationToken"]').val() || "";

    swal({
        title: "¿Estás seguro?",
        text: "¡Este registro no se podrá recuperar!",
        icon: "warning",
        buttons: ["Cancelar", "Sí, borrar"],
        dangerMode: true
    }).then(willDelete => {
        if (!willDelete) return;

        $.ajax({
            url: url,
            type: "POST",
            headers: { "RequestVerificationToken": token },
            success: res => {
                if (res.success) toastr.success(res.message);
                else toastr.error(res.message);
                dataTable.ajax.reload();
            },
            error: () => toastr.error("Error en el servidor.")
        });
    });
}
