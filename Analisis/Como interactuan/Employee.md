# SaborExpress — Guía de front: cómo interactúa el Gerente/Administrador con "Employees"

A diferencia de `Customers`, este módulo **no tiene vista propia para el
empleado** (el empleado ve sus datos a través de otras pantallas, como su
turno o su perfil general) — `Employees` es exclusivamente el panel de
gestión de personal que usa el Gerente/Administrador para contratar, editar
y dar de baja trabajadores.

---

## Vista del Gerente/Administrador: "Personal"

### 1. Listar empleados

```
Gerente/Administrador entra a "Personal" en el panel
        ↓
Front llama: GET /api/Employees
        ↓
Backend filtra automaticamente segun el rol de quien pregunta:
  - Gerente -> ve empleados de TODAS las sedes
  - Administrador -> ve SOLO los de su propia sede
        ↓
Responde con la lista (sin el PDF del CV, solo si tiene o no: HasCv)
```

### Ejemplo de respuesta

```json
[
  {
    "id": 5,
    "name": "Luis",
    "lastName": "Gomez",
    "document": "1023456789",
    "email": "luis@saborexpress.com",
    "roleNames": ["Mesero"],
    "branchId": 1,
    "status": "Activo",
    "hasCv": true
  }
]
```

### Cómo se pinta

Una tabla, con un filtro de estado arriba (Activos / Retirados / Todos):

| Nombre | Documento | Rol | Sede | Estado | Acciones |
|---|---|---|---|---|---|
| Luis Gomez | 1023456789 | Mesero | Sede Centro | Activo | 👁️ Ver · ✏️ Editar · 📄 CV · 🗑️ Dar de baja |

**Nota técnica para el front:** el campo `hasCv` es solo un booleano — sirve
para decidir si mostrar el ícono de "Ver CV" o dejarlo deshabilitado. El PDF
en sí **no viaja** en esta llamada (se pidió aparte que así fuera, para que el
listado sea rápido incluso con muchos empleados con CV cargado). Si el front
necesita el archivo, se pide con el endpoint del punto 5.

---

### 2. Crear un empleado nuevo

```
Gerente/Administrador toca "+ Nuevo empleado"
        ↓
Front muestra formulario: Nombre, Apellido, Documento, Email, Telefono,
Direccion, Roles (multi-select), Sede, Pago base, Horario, CV (opcional)
        ↓
Llena y da "Crear"
        ↓
Front llama: POST /api/Employees (multipart/form-data, por el archivo del CV)
        ↓
Backend crea User + Employee, genera una contrasena temporal inservible,
y envia un codigo de activacion al correo del empleado (para que EL MISMO
defina su propia contrasena la primera vez que entra)
        ↓
201 Created
```

### Ejemplo de request (form-data, no JSON puro por el archivo)

```
name: Luis
lastName: Gomez
document: 1023456789
email: luis@saborexpress.com
phone: 3001234567
address: Calle 45 # 10-20
roleIds: [3]
branchId: 1
basePay: 1300000
startTime: 08:00:00
endTime: 17:00:00
cv: (archivo PDF, opcional)
```

### Qué le muestra el front

```
"Empleado registrado correctamente. Se envio un codigo de activacion
a luis@saborexpress.com para que confirme su cuenta."
```

**Importante:** el empleado nuevo **no puede loguearse todavia** con esta
cuenta — necesita pasar primero por `POST /api/Auth/activate-account` (el
mismo endpoint de Auth que ya construimos) usando el codigo que le llego al
correo. El front del empleado deberia guiarlo a esa pantalla, no a un login
directo.

---

### 3. Ver el detalle de un empleado

```
Gerente/Administrador toca "Ver" en la fila de Luis
        ↓
Front llama: GET /api/Employees/5
        ↓
Backend valida que quien pregunta tenga permiso de ver ese empleado puntual
        ↓
200 OK con el detalle completo
```

**Regla de seguridad importante para el front:** si un Administrador de la
Sede A intenta ver el detalle de un empleado de la Sede B (por ejemplo,
escribiendo el Id directo en la URL), el backend responde `403 Forbidden` —
aunque ese empleado nunca hubiera aparecido en su listado. El front debe
mostrar un mensaje claro de "No tienes acceso a este empleado" en ese caso,
no un error genérico.

---

### 4. Editar un empleado

```
Gerente/Administrador toca "Editar" en la fila de Luis
        ↓
Front abre el formulario precargado con GET /api/Employees/5
        ↓
Cambia el telefono y agrega un segundo rol
        ↓
Front llama: PUT /api/Employees/5
        ↓
200 OK -> toast "Datos actualizados correctamente"
```

**Misma regla de sede que en el punto 3:** un Administrador no puede editar
empleados de otra sede, aunque conozca el Id. El Gerente sí puede editar
cualquiera.

---

### 5. Ver / descargar el CV

```
Gerente/Administrador toca el icono de CV en la fila de Luis
        ↓
Front llama: GET /api/Employees/5/cv
        ↓
Si tiene CV -> el navegador descarga o abre el PDF
Si NO tiene CV -> 404, el front deberia mostrar
   "Este empleado no tiene CV cargado" en vez de un error crudo
```

**Nota de rendimiento:** a diferencia del listado, este endpoint puntual sí
trae el PDF completo — es lo esperado, porque acá el usuario pidió
explícitamente verlo. La distinción es: listar = liviano, un empleado
puntual + su CV = completo.

---

### 6. Dar de baja a un empleado (soft-delete)

```
Gerente/Administrador toca "Dar de baja" en la fila de Luis
        ↓
Front muestra confirmacion: "Esta accion marcara al empleado como Retirado.
No se borran sus datos historicos (pedidos, pagos, etc.)"
        ↓
Confirma
        ↓
Front llama: DELETE /api/Employees/5
        ↓
204 No Content -> el front quita la fila de la vista "Activos"
   (pero sigue existiendo si el filtro cambia a "Retirados" o "Todos")
```

**Por qué es soft-delete y no borrado real:** un empleado retirado puede
seguir apareciendo como responsable en `OrderDetailHistory`,
`OrderStatusHistory`, `Payment`, etc. — borrarlo de verdad rompería esos
registros históricos. El front debe comunicar esto como "dar de baja", nunca
como "eliminar", para no confundir al usuario sobre qué está pasando
realmente.

---

## Resumen: reglas para el front de este módulo

1. **El listado (`GET /api/Employees`) nunca trae el PDF del CV** — solo un booleano `hasCv`. Para ver el archivo real, hay que pedir el endpoint puntual del CV.
2. **El filtro de sede es automático según el rol** — el front no necesita mandar ni preguntar la sede del usuario logueado, el backend ya resuelve "Gerente ve todo, Administrador ve solo lo suyo" en cada endpoint (listar, ver detalle, editar).
3. **Un empleado nuevo no puede loguearse de inmediato** — necesita activar su cuenta primero con el código que le llega al correo, vía `POST /api/Auth/activate-account`.
4. **"Dar de baja" es soft-delete** — el registro sigue existiendo, solo cambia su estado. El front debe comunicarlo así, no como un borrado definitivo.
5. **La creación y edición usan `multipart/form-data`**, no JSON puro, porque ambos endpoints aceptan un archivo (CV) opcional.

---

## Próximo paso sugerido

Un solo `employeesApi` con estas funciones cubre todo el módulo: `create()`,
`getAll(estado)`, `getById()`, `update()`, `deactivate()`, `getCv()`. Todas
requieren rol Gerente o Administrador — no hay ninguna función pública ni de
autoservicio como en `Customers`. La pantalla de "Personal" en el panel
administrativo es la única que consume este servicio.