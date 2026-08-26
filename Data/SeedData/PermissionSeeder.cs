using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Permissions.Models;
using SaborExpress.Modules.RolePermissions.Models;
using SaborExpress.Modules.Roles.Models;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Data.SeedData
{
    public static class PermissionSeeder
    {
        // Name = PermissionNames.X (en español, es lo que ve el usuario y lo que usa el código)
        private static readonly Dictionary<string, (PermissionModule Modulo, string Descripcion)> InfoPorPermiso = new()
        {
            [PermissionNames.CrearSede] = (PermissionModule.Branches, "Crear una nueva sede"),
            [PermissionNames.EditarSede] = (PermissionModule.Branches, "Editar información de una sede"),
            [PermissionNames.EliminarSede] = (PermissionModule.Branches, "Eliminar una sede"),
            [PermissionNames.GestionarEmpleados] = (PermissionModule.Employees, "Gestionar empleados en general"),
            [PermissionNames.CrearEmpleado] = (PermissionModule.Employees, "Crear un nuevo empleado"),
            [PermissionNames.EliminarEmpleado] = (PermissionModule.Employees, "Dar de baja a un empleado"),
            [PermissionNames.AprobarRegistros] = (PermissionModule.Employees, "Aprobar registros pendientes"),
            [PermissionNames.VerReportes] = (PermissionModule.Reports, "Ver reportes del sistema"),
            [PermissionNames.GestionarMenu] = (PermissionModule.DailyMenu, "Planear el menú del día"),
            [PermissionNames.ActivarDesactivarPlato] = (PermissionModule.DailyMenu, "Activar o desactivar un plato a media jornada"),

            [PermissionNames.CrearPedido] = (PermissionModule.Orders, "Crear un pedido nuevo"),
            [PermissionNames.EditarPedidoPropio] = (PermissionModule.Orders, "Editar líneas de un pedido propio"),
            [PermissionNames.EditarCualquierPedido] = (PermissionModule.Orders, "Editar cualquier pedido, no solo el propio"),
            [PermissionNames.CancelarPedido] = (PermissionModule.Orders, "Cancelar un pedido"),
            [PermissionNames.ActualizarEstadoPedido] = (PermissionModule.Orders, "Mover el pedido entre estados de preparación"),

            [PermissionNames.CambiarEstadoMesa] = (PermissionModule.Tables, "Marcar mesa Ocupada/Libre/Reservada"),

            [PermissionNames.RegistrarPago] = (PermissionModule.Payments, "Registrar un cobro"),
            [PermissionNames.ReembolsarPago] = (PermissionModule.Payments, "Procesar un reembolso"),

            [PermissionNames.TomarEntrega] = (PermissionModule.Deliveries, "Tomar un pedido disponible del pool"),
            [PermissionNames.ActualizarEstadoEntrega] = (PermissionModule.Deliveries, "Actualizar el estado de una entrega"),
        };

        private static readonly Dictionary<string, string[]> PermisosPorRol = new()
        {
            [RoleNames.Gerente] = new[]
            {
                PermissionNames.CrearSede, PermissionNames.EditarSede, PermissionNames.EliminarSede,
                PermissionNames.GestionarEmpleados, PermissionNames.CrearEmpleado, PermissionNames.EliminarEmpleado,
                PermissionNames.AprobarRegistros, PermissionNames.VerReportes, PermissionNames.GestionarMenu,
                PermissionNames.ActivarDesactivarPlato,
                PermissionNames.EditarCualquierPedido, PermissionNames.CancelarPedido, PermissionNames.ActualizarEstadoPedido,
                PermissionNames.CambiarEstadoMesa, PermissionNames.RegistrarPago, PermissionNames.ReembolsarPago,
            },
            [RoleNames.Administrador] = new[]
            {
                PermissionNames.EditarSede, PermissionNames.GestionarEmpleados, PermissionNames.CrearEmpleado,
                PermissionNames.EliminarEmpleado, PermissionNames.AprobarRegistros, PermissionNames.VerReportes,
                PermissionNames.GestionarMenu, PermissionNames.ActivarDesactivarPlato,
                PermissionNames.EditarCualquierPedido, PermissionNames.CancelarPedido, PermissionNames.ActualizarEstadoPedido,
                PermissionNames.CambiarEstadoMesa, PermissionNames.RegistrarPago, PermissionNames.ReembolsarPago
            },
            [RoleNames.Mesero] = new[]
            {
                PermissionNames.CrearPedido, PermissionNames.EditarPedidoPropio,
                PermissionNames.CancelarPedido, PermissionNames.CambiarEstadoMesa
            },
            [RoleNames.Cajero] = new[]
            {
                PermissionNames.EditarPedidoPropio, PermissionNames.RegistrarPago, PermissionNames.ReembolsarPago
            },
            [RoleNames.Cocinero] = new[]
            {
                PermissionNames.ActualizarEstadoPedido, PermissionNames.ActivarDesactivarPlato
            },
            [RoleNames.Repartidor] = new[]
            {
                PermissionNames.TomarEntrega, PermissionNames.ActualizarEstadoEntrega
            },
        };

        private static readonly Dictionary<string, string> DescripcionesPorRol = new()
        {
            [RoleNames.Gerente] = "Supervisa todas las sedes del restaurante",
            [RoleNames.Administrador] = "Administra una sede",
            [RoleNames.Mesero] = "Toma y gestiona pedidos en mesa",
            [RoleNames.Cajero] = "Registra pedidos y cobra en mostrador",
            [RoleNames.Cocinero] = "Prepara los pedidos en cocina",
            [RoleNames.Repartidor] = "Entrega pedidos a domicilio",
        };

        public static async Task SeedAsync(AppDbContext context)
        {
            var todosLosPermisos = PermisosPorRol.Values
                .SelectMany(p => p)
                .Distinct()
                .ToList();

            foreach (var nombre in todosLosPermisos)
            {
                bool existe = await context.Permissions.AnyAsync(p => p.Name == nombre);
                if (!existe)
                {
                    if (!InfoPorPermiso.TryGetValue(nombre, out var info))
                        throw new InvalidOperationException(
                            $"El permiso '{nombre}' no tiene Module/Descripción en InfoPorPermiso. Agrégalo antes de continuar.");

                    context.Permissions.Add(new Permission
                    {
                        Name = nombre,
                        Module = info.Modulo,
                        Description = info.Descripcion
                    });
                }
            }
            await context.SaveChangesAsync();

            var permisosDb = await context.Permissions.ToListAsync();

            foreach (var (nombreRol, permisosDelRol) in PermisosPorRol)
            {
                var rol = await context.Roles.FirstOrDefaultAsync(r => r.Name == nombreRol);

                if (rol is null)
                {
                    rol = new Role
                    {
                        Name = nombreRol,
                        Description = DescripcionesPorRol.TryGetValue(nombreRol, out var desc) ? desc : nombreRol,
                        RequiresCv = false,
                        Status = true
                    };
                    context.Roles.Add(rol);
                    await context.SaveChangesAsync();
                }

                foreach (var nombrePermiso in permisosDelRol)
                {
                    var permiso = permisosDb.First(p => p.Name == nombrePermiso);

                    bool yaAsignado = await context.RolePermissions
                        .AnyAsync(rp => rp.RoleId == rol.Id && rp.PermissionId == permiso.Id);

                    if (!yaAsignado)
                    {
                        context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = rol.Id,
                            PermissionId = permiso.Id
                        });
                    }
                }
            }

            await context.SaveChangesAsync();
        }
    }
}