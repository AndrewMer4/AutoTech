let registroTable;

$(document).ready(function () {
    registroTable = $('#tblRegistro').DataTable({
        ajax: {
            url: '/Admin/Registro/GetAll',
            type: 'GET',
            dataSrc: 'data'
        },
        columns: [
            { data: 'nombre', width: '20%' },
            { data: 'apellido', width: '20%' },
            { data: 'email', width: '25%' },
            { data: 'role', width: '15%' },
            {
                data: 'id',
                width: '20%',
                orderable: false,
                render: id => `
          <div class="d-flex justify-content-center gap-2">
            <a href="/Admin/Registro/Details/${id}" class="btn btn-info btn-sm">
              <i class="far fa-user"></i> Perfil
            </a>
           
          </div>`
            }
        ],
        language: {
            emptyTable: "No hay registros",
            info: "Mostrando _START_ a _END_ de _TOTAL_ entradas",
            lengthMenu: "Mostrar _MENU_ entradas",
            search: "Buscar:",
            paginate: {
                first: "Primero",
                last: "Último",
                next: "Siguiente",
                previous: "Anterior"
            }
        }
    });
});

function confirmDelete(id) {
    swal({
        title: "¿Estás seguro?",
        text: "¡No podrás revertir esto!",
        icon: "warning",
        buttons: ["Cancelar", "Sí, eliminar"],
        dangerMode: true
    }).then(willDelete => {
        if (!willDelete) return;
        // redirige a la página GET Delete para confirmación
        window.location.href = `/Admin/Registro/Delete/${id}`;
    });
}
/* <a href="/Admin/Registro/Edit/${id}" class="btn btn-success btn-sm">
              <i class="far fa-edit"></i> Editar
            </a>
            <button onclick="confirmDelete('${id}')" class="btn btn-danger btn-sm">
              <i class="far fa-trash-alt"></i> Eliminar
            </button>*/