# SaborExpress — ¿Branch o Configuration? + Guía de front: Branding global (idioma, color, logo)

## Primero, la respuesta a tu pregunta: van en `Configuration`, NO en `Branch`

Esto es importante para no mezclar responsabilidades. Cada entidad tiene un
propósito distinto:

| Entidad | Qué representa | Ejemplo de sus campos |
|---|---|---|
| `Branch` | **Datos físicos/administrativos** de una sucursal concreta | Nombre, dirección, teléfono, si está activa |
| `Configuration` (`BranchSetting`) | **Parámetros configurables** del sistema, que pueden ser globales o por sede | `TAX_RATE`, horario, y ahora también idioma/color/logo |

### Por qué NO va en `Branch`

Si agregas `Language`, `ThemeColor`, `LogoUrl` como columnas directas en la
tabla `Branch`, pasaría esto:

1. **Tendrías que repetir el mismo valor en cada fila** — si el logo es el mismo para las 5 sedes, terminarías con la misma URL copiada 5 veces en la base de datos. Si mañana cambias el logo, tendrías que actualizar las 5 filas (o hacer un `UPDATE` masivo).
2. **`Branch` dejaría de ser solo "datos de la sede"** — se volvería una mezcla rara de "datos administrativos" + "configuración visual de marca", que son cosas que cambian por razones completamente distintas y las edita gente distinta (un Administrador cambia el teléfono de su sede; el Gerente/marketing cambia el logo de toda la cadena).
3. **Ya construiste `Configuration` exactamente para esto** — tiene `Key` + `Value` + `BranchId` nullable. Es el patrón correcto para "un parámetro que puede ser global o específico".

### Por qué SÍ va en `Configuration`

Porque el campo `BranchId` nullable que ya diseñaste es **exactamente** la solución:

```
Key = "BRAND_LOGO_URL", Value = "https://.../logo.png", BranchId = null
   → Un solo registro, aplica a TODAS las sedes automáticamente
```

No duplicas nada, no tocas la tabla `Branch`, y reusas el CRUD que ya tienes
construido y probado (`GET/POST/PUT/DELETE /api/Configurations`).

---

## Las Keys nuevas que vas a necesitar

| Key | Ejemplo de `Value` | `BranchId` |
|---|---|---|
| `APP_LANGUAGE` | `"es"` | `null` (global) |
| `THEME_PRIMARY_COLOR` | `"#E63946"` | `null` (global) |
| `THEME_MODE` | `"light"` | `null` (global) |
| `BRAND_LOGO_URL` | `"https://cdn.../logo.png"` | `null` (global) |

No necesitas crear ninguna tabla ni endpoint nuevo — solo vas a **insertar
nuevas filas** en la tabla `Configuration` que ya existe, usando el mismo
`POST /api/Configurations` que ya tienes.

---

## Guía de front: cómo interactúa esto con la app

### 1. Al arrancar la app (cualquier usuario: cliente, mesero, gerente...)

```
La app se abre (splash screen)
        ↓
Front llama: GET /api/Configurations/branch/{branchId opcional}
   (o simplemente GET /api/Configurations si quieres traer TODO de una vez)
        ↓
Backend responde con la lista completa de parámetros (globales + de la sede)
        ↓
El front busca en esa lista las Keys que le interesan:
   - APP_LANGUAGE       → configura el idioma de los textos
   - THEME_PRIMARY_COLOR → pinta los botones/acentos con ese color
   - THEME_MODE          → activa modo claro u oscuro
   - BRAND_LOGO_URL      → pone esa imagen en el header/splash
```

### Ejemplo de respuesta que recibiría el front

```json
[
  { "id": 10, "key": "APP_LANGUAGE", "value": "es", "branchId": null },
  { "id": 11, "key": "THEME_PRIMARY_COLOR", "value": "#E63946", "branchId": null },
  { "id": 12, "key": "THEME_MODE", "value": "light", "branchId": null },
  { "id": 13, "key": "BRAND_LOGO_URL", "value": "https://cdn.saborexpress.com/logo.png", "branchId": null },
  { "id": 14, "key": "TAX_RATE", "value": "19", "branchId": null }
]
```

### Cómo lo usa el front (ejemplo conceptual)

```javascript
// Al iniciar la app, una sola vez:
const configs = await api.get('/api/Configurations');

const brandConfig = {
  language: configs.find(c => c.key === 'APP_LANGUAGE')?.value ?? 'es',
  primaryColor: configs.find(c => c.key === 'THEME_PRIMARY_COLOR')?.value ?? '#000000',
  mode: configs.find(c => c.key === 'THEME_MODE')?.value ?? 'light',
  logoUrl: configs.find(c => c.key === 'BRAND_LOGO_URL')?.value ?? null,
};

// Se guarda en el estado global de la app (Context, Redux, Zustand, lo que uses)
// y desde ahí toda la app lee ese estado para pintar colores, textos, logo, etc.
```

---

### 2. El Gerente cambia el color del tema (ejemplo de edición)

```
Gerente entra a "Configuración de Marca" en el panel
        ↓
Ve un selector de color con el valor actual (#E63946)
        ↓
Elige un color nuevo y da "Guardar"
        ↓
Front llama: PUT /api/Configurations/11
{
  "key": "THEME_PRIMARY_COLOR",
  "value": "#2A9D8F",
  "branchId": null
}
        ↓
Backend actualiza esa fila puntual
        ↓
Front actualiza el estado global de la app → toda la interfaz cambia
de color de inmediato, sin recargar la página
```

### 3. El Gerente cambia el logo (esto sí necesita una decisión de diseño)

Aquí es donde toca decidir algo, como conversamos antes — el `Value` de
`Configuration` es un string simple, así que lo más práctico es:

```
Gerente sube una imagen desde su computador
        ↓
Front la manda a un servicio de almacenamiento de imágenes
(Cloudinary, S3, o un endpoint tuyo que la guarde en wwwroot y devuelva una URL)
        ↓
Ese servicio devuelve una URL pública: "https://cdn.../logo-nuevo.png"
        ↓
Front llama: PUT /api/Configurations/13
{
  "key": "BRAND_LOGO_URL",
  "value": "https://cdn.../logo-nuevo.png",
  "branchId": null
}
        ↓
Se guarda solo la URL, no el archivo — igual que un avatar de usuario típico
```

**Nota importante:** esto significa que vas a necesitar, en algún momento,
un mini-endpoint de "subir imagen" que **no** vive en `Configurations` — vive
aparte (podría ser algo genérico tipo `POST /api/Uploads/image` que reciba
un archivo y devuelva la URL). `Configurations` solo se encarga de guardar
el link resultante, no de manejar el archivo en sí.

---

## Resumen de la decisión

| Pregunta | Respuesta |
|---|---|
| ¿Dónde van idioma/color/logo? | En `Configuration`, con `BranchId = null` (globales) |
| ¿Hay que tocar `Branch`? | No, `Branch` se queda solo con sus datos administrativos |
| ¿Hay que crear tablas nuevas? | No, ya existe `Configuration` con el diseño correcto |
| ¿Hay que crear endpoints nuevos? | Solo si quieres subir imágenes (`POST /api/Uploads/image`); todo lo demás usa el CRUD que ya tienes |
| ¿Quién puede editarlo? | Ya está resuelto: solo Gerente puede crear/editar/borrar `Configuration` |

---

## Próximo paso sugerido

Antes de escribir cualquier código nuevo, definamos:

1. ¿Dónde vas a alojar las imágenes del logo? (¿Cloudinary, S3, tu propio servidor con una carpeta `wwwroot/uploads`?) — de esto depende si necesitamos construir el endpoint de upload ahora o lo dejamos para después.
2. ¿El idioma/color es **una sola configuración para toda la app** (lo que hemos asumido aquí), o quieres que en algún futuro cada sede pueda tener su propio color/logo? Si es lo segundo, ya tienes la solución lista también — solo pondrías un `BranchId` específico en vez de `null` para esos casos puntuales.