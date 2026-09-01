# Configuración de idioma por sede y usuario

## 1. Objetivo

El sistema debe permitir configurar el idioma de la aplicación en dos niveles:

- **Configuración de sede:** afecta a todos los usuarios pertenecientes a una sede.
- **Configuración de usuario:** afecta únicamente al usuario que la configure.

Los idiomas disponibles serán:

- Español (`ES`)
- Inglés (`EN`)
- Portugués (`PT`)
- Francés (`FR`)

La configuración de usuario tendrá prioridad sobre la configuración de sede.

---

## 2. Problema

El sistema tiene dos tipos de configuración:

```text
Configuración
│
├── Configuración de sede
│   └── Afecta a los usuarios de la sede
│
└── Configuración de usuario
    └── Afecta únicamente al usuario
```

Por ejemplo:

```text
Sede Neiva
Idioma: ES

Usuario 1
Idioma: EN

Usuario 2
Idioma: NULL

Usuario 3
Idioma: NULL
```

El resultado sería:

```text
Usuario 1 → EN
Usuario 2 → ES
Usuario 3 → ES
```

El usuario 1 tiene una configuración personalizada, por lo tanto no utiliza el idioma de la sede.

---

# 3. Regla de prioridad

La regla principal será:

```text
Idioma efectivo =
    Idioma del usuario
    SI el usuario tiene uno configurado

    De lo contrario:
    Idioma de la sede
```

En forma de código:

```csharp
userLanguage ?? branchLanguage
```

Donde:

- `userLanguage` = idioma configurado por el usuario.
- `branchLanguage` = idioma configurado por la sede.

---

# 4. Idiomas disponibles

Se recomienda utilizar un `enum` para evitar valores inválidos.

```csharp
public enum Language
{
    ES,
    EN,
    PT,
    FR
}
```

Los valores representan:

| Código | Idioma |
|---|---|
| ES | Español |
| EN | Inglés |
| PT | Portugués |
| FR | Francés |

---

# 5. Configuración de sede

La configuración de sede debe almacenar el idioma predeterminado para esa sede.

```csharp
public class BranchConfiguration
{
    public Guid Id { get; set; }

    public Guid BranchId { get; set; }

    public Language Language { get; set; }
}
```

Ejemplo:

```text
BranchConfiguration

BranchId                         Language
-----------------------------------------
sede-neiva                       ES
sede-bogota                      EN
sede-cali                        PT
```

Esto significa que todos los usuarios que no tengan un idioma personalizado utilizarán el idioma configurado en su sede.

---

# 6. Configuración de usuario

La configuración de usuario debe permitir que el idioma sea opcional.

```csharp
public class UserConfiguration
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Language? Language { get; set; }
}
```

El `?` es importante porque permite almacenar `NULL`.

Los valores posibles serían:

```text
ES
EN
PT
FR
NULL
```

`NULL` significa:

```text
El usuario no ha seleccionado un idioma personalizado.
Utilizar el idioma de la sede.
```

---

# 7. Ejemplo completo

Supongamos que tenemos:

```text
Sede Neiva
Idioma: ES
```

Y tres usuarios:

```text
Usuario Yerson
Idioma: EN

Usuario Paula
Idioma: NULL

Usuario Karen
Idioma: NULL
```

El idioma efectivo será:

```text
Yerson → EN
Paula  → ES
Karen  → ES
```

---

# 8. Cambio de configuración de sede

Supongamos que el gerente cambia el idioma de la sede:

```text
Antes:

Sede Neiva → ES
```

Después:

```text
Sede Neiva → PT
```

Los usuarios quedarían:

```text
Yerson → EN
Paula  → PT
Karen  → PT
```

Yerson mantiene `EN` porque tiene una configuración personalizada.

Paula y Karen cambian automáticamente a `PT` porque utilizan la configuración de sede.

---

# 9. Cambio de configuración del usuario

Si Paula selecciona inglés:

```text
Paula → EN
```

Entonces:

```text
Sede Neiva → PT

Yerson → EN
Paula  → EN
Karen  → PT
```

La configuración de Paula no modifica la configuración de la sede.

Tampoco modifica la configuración de los demás usuarios.

---

# 10. Obtener el idioma efectivo

El backend debe tener una lógica centralizada para determinar qué idioma utilizar.

```csharp
public Language GetEffectiveLanguage(
    UserConfiguration userConfiguration,
    BranchConfiguration branchConfiguration)
{
    return userConfiguration.Language
           ?? branchConfiguration.Language;
}
```

La operación:

```csharp
userConfiguration.Language
    ?? branchConfiguration.Language
```

significa:

```text
Si UserConfiguration.Language tiene valor:
    utilizar ese valor.

Si UserConfiguration.Language es NULL:
    utilizar BranchConfiguration.Language.
```

---

# 11. Flujo de funcionamiento

```text
              Usuario inicia sesión
                       │
                       ▼
              Obtener usuario
                       │
                       ▼
             Obtener configuración
                  del usuario
                       │
                       ▼
             ¿Tiene idioma?
                /          \
              Sí            No
              │              │
              ▼              ▼
       Usar idioma       Obtener idioma
        del usuario        de la sede
              │              │
              └──────┬───────┘
                     ▼
              Idioma efectivo
                     │
                     ▼
            Obtener información
              en ese idioma
```

---

# 12. Traducciones de los datos

Es importante separar:

### Configuración del idioma

Determina qué idioma quiere utilizar el usuario.

```text
UserConfiguration
        ↓
Language = EN
```

### Traducciones

Determinan qué texto mostrar en cada idioma.

Por ejemplo, para un producto:

```text
Product
----------------
Id
Price
```

Y una tabla de traducciones:

```text
ProductTranslation

ProductId | Language | Name
----------|----------|----------------
1         | ES       | Hamburguesa
1         | EN       | Burger
1         | PT       | Hambúrguer
1         | FR       | Hamburger
```

La configuración de idioma **no debe traducir automáticamente el texto almacenado**.

La configuración solamente indica qué traducción debe consultar el backend.

---

# 13. Ejemplo de consulta

Si el idioma efectivo es:

```text
EN
```

El backend puede consultar:

```sql
SELECT
    p.id,
    p.price,
    pt.name
FROM product p
INNER JOIN product_translation pt
    ON pt.product_id = p.id
WHERE pt.language = 'EN';
```

Resultado:

```json
{
    "id": 1,
    "name": "Burger",
    "price": 15000
}
```

Si el idioma efectivo es:

```text
ES
```

Resultado:

```json
{
    "id": 1,
    "name": "Hamburguesa",
    "price": 15000
}
```

---

# 14. Qué debe hacer PostgreSQL

PostgreSQL debe encargarse de **almacenar**:

```text
Configuración de sede
Configuración de usuario
Traducciones
```

PostgreSQL no debe encargarse de decidir toda la lógica de prioridad.

La base de datos almacena:

```text
UserConfiguration
        ↓
Language = EN
```

Y:

```text
BranchConfiguration
        ↓
Language = ES
```

El backend determina:

```text
EN
```

porque la configuración del usuario tiene prioridad.

---

# 15. Qué debe hacer el backend

El backend C# debe:

1. Identificar al usuario autenticado.
2. Obtener su configuración.
3. Obtener la configuración de su sede.
4. Determinar el idioma efectivo.
5. Consultar las traducciones correspondientes.
6. Devolver los datos en el idioma seleccionado.

La regla central debe ser:

```csharp
Language effectiveLanguage =
    userConfiguration.Language
    ?? branchConfiguration.Language;
```

---

# 16. Comportamiento esperado

| Configuración sede | Configuración usuario | Idioma efectivo |
|---|---|---|
| ES | NULL | ES |
| ES | EN | EN |
| EN | NULL | EN |
| EN | PT | PT |
| PT | NULL | PT |
| FR | ES | ES |

La configuración del usuario siempre tiene prioridad cuando existe.

---

# 17. Ventajas de este diseño

### Configuración de sede

Permite que el gerente establezca una configuración general.

```text
Gerente
   ↓
Sede
   ↓
Usuarios sin configuración personalizada
```

### Configuración de usuario

Permite personalización individual.

```text
Usuario
   ↓
Configuración propia
   ↓
Sobrescribe configuración de sede
```

### Traducciones independientes

Las traducciones no dependen directamente de quién seleccionó el idioma.

```text
Idioma efectivo
      ↓
ProductTranslation
      ↓
Texto correspondiente
```

---

# 18. Regla final de arquitectura

La solución puede resumirse así:

```text
                  ┌────────────────────┐
                  │ Configuración Sede │
                  │       ES           │
                  └─────────┬──────────┘
                            │
                            ▼
                    Valor predeterminado
                            │
                            │
                  ┌─────────┴──────────┐
                  │                    │
                  ▼                    ▼
            Usuario A             Usuario B
            Language=EN           Language=NULL
                  │                    │
                  ▼                    ▼
                 EN                    ES
                  │                    │
                  └──────────┬─────────┘
                             ▼
                     Idioma efectivo
                             │
                             ▼
                  Tabla de traducciones
                             │
                             ▼
                       Resultado API
```

## Conclusión

La configuración de idioma debe manejarse en **dos niveles**:

```text
Sede → idioma predeterminado
Usuario → idioma personalizado
```

La prioridad será:

```text
Usuario > Sede
```

Y las traducciones deben mantenerse separadas de estas configuraciones.

De esta manera, cuando el gerente cambie el idioma de una sede, todos los usuarios que no hayan elegido un idioma propio adoptarán automáticamente el nuevo idioma, mientras que quienes tengan una preferencia personal continuarán utilizando la suya.