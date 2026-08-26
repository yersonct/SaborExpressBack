// Shared/Constants/PermissionNames.cs
namespace SaborExpress.Shared.Constants
{
    public static class PermissionNames
    {
        // Sedes
        public const string CrearSede = "CREAR_SEDE";
        public const string EditarSede = "EDITAR_SEDE";
        public const string EliminarSede = "ELIMINAR_SEDE";

        // Empleados
        public const string GestionarEmpleados = "GESTIONAR_EMPLEADOS";
        public const string CrearEmpleado = "CREAR_EMPLEADO";
        public const string EliminarEmpleado = "ELIMINAR_EMPLEADO";
        public const string AprobarRegistros = "APROBAR_REGISTROS";

        // Reportes
        public const string VerReportes = "VER_REPORTES";

        // Menú
        public const string GestionarMenu = "GESTIONAR_MENU";
        public const string ActivarDesactivarPlato = "ACTIVAR_DESACTIVAR_PLATO";

        // Pedidos
        public const string CrearPedido = "CREAR_PEDIDO";
        public const string EditarPedidoPropio = "EDITAR_PEDIDO_PROPIO";
        public const string EditarCualquierPedido = "EDITAR_CUALQUIER_PEDIDO";
        public const string CancelarPedido = "CANCELAR_PEDIDO";
        public const string ActualizarEstadoPedido = "ACTUALIZAR_ESTADO_PEDIDO";

        // Mesas
        public const string CambiarEstadoMesa = "CAMBIAR_ESTADO_MESA";

        // Pagos
        public const string RegistrarPago = "REGISTRAR_PAGO";
        public const string ReembolsarPago = "REEMBOLSAR_PAGO";

        // Domicilios
        public const string TomarEntrega = "TOMAR_ENTREGA";
        public const string ActualizarEstadoEntrega = "ACTUALIZAR_ESTADO_ENTREGA";
    }
}