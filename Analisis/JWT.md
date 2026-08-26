# ¿Dónde se guarda el token: en la Base de Datos o no? (Guía Sencilla)

Cuando creas un sistema de inicio de sesión moderno, **no tienes que elegir uno u otro**, sino que **se usan ambos al mismo tiempo** (trabajan en equipo).

---

## 1. La analogía de la discoteca

Para entenderlo fácilmente, imagina que entras a una discoteca:

1. **Access Token (La pulsera de papel del día):** * Pagas en la entrada y te ponen una pulsera que dice que puedes estar ahí por 1 hora. 
   * Cada vez que pides una bebida en la barra, el mozo **solo mira tu pulsera** y te la da al instante. No va a revisar a la computadora de la entrada. Es rápido y ágil.
2. **Refresh Token (Tu documento de identidad / cédula):** * Lo guardas seguro en tu billetera. 
   * Cuando pasa la hora y la pulsera caduca, vas con tu documento a la entrada para que te den una **nueva pulsera** por otra hora, sin necesidad de volver a escribir tu contraseña.

---

## 2. ¿Tienen la misma información?

* **Access Token (JWT):** Lleva dentro los datos básicos del usuario (como tu ID, nombre y rol) en un formato codificado para que el servidor los lea rápidamente sin consultar nada.
* **Refresh Token:** Suele ser solo un código largo, aleatorio y opaco. Por sí solo no dice nada; su único trabajo es servir de llave para ir a la base de datos y decir: *"Hola, soy válido, dame un nuevo Access Token"*.

---

## 3. ¿Cómo funcionan los dos juntos?

1. **Inicias sesión** una sola vez con tu usuario y contraseña.
2. El servidor te entrega **las dos cosas**: 
   * El *Access Token* (para usarlo rápido en cada clic).
   * El *Refresh Token* (que **sí se guarda en la base de datos** para tener control de tu sesión).
3. Cada vez que navegas por la aplicación, usas tu *Access Token*.
4. Cuando el *Access Token* expira (por ejemplo, a los 15 minutos por seguridad), tu aplicación usa automáticamente el *Refresh Token* por detrás para pedir uno nuevo, sin que tengas que iniciar sesión otra vez.

---

## Resumen final

| Tipo de Token | ¿Se guarda en Base de Datos? | ¿Qué función cumple? |
| :--- | :--- | :--- |
| **Access Token (JWT)** | **No** | Valida quién eres rápidamente mediante su firma digital. |
| **Refresh Token / Sesión** | **Sí** | Permite renovar el acceso y revocarlo (cerrar sesión) cuando sea necesario. |




# ¿Dónde se guarda cada token? (Base de datos vs. Navegador)

Para entender exactamente dónde vive cada token, veamos la división de responsabilidades entre el **Servidor (Base de Datos)** y el **Cliente (Navegador web)**.

---

## 1. ¿Dónde se guarda cada uno?

| Tipo de Token | ¿Dónde se guarda? | ¿Por qué ahí? |
| :--- | :--- | :--- |
| **Access Token (JWT)** | **En el Navegador** (Memoria RAM / JavaScript) | Como expira muy rápido (ej. 15 minutos), no vale la pena guardarlo en la base de datos. Se mantiene temporalmente en el cliente para hacer peticiones rápidas. |
| **Refresh Token** | **En la Base de Datos** Y **En el Navegador** | • **En el Servidor (BD):** Se guarda para que el sistema sepa qué sesiones están activas y pueda **revocarlo** si el usuario cierra sesión.<br>• **En el Navegador:** Se guarda de forma muy segura (en una **Cookie HTTP-only**) para que el navegador pueda enviarlo automáticamente al servidor cuando necesite un nuevo Access Token. |

---

## 2. Resumen visual en el navegador

* **Access Token:** Vive normalmente en la **memoria RAM** de la pestaña del navegador mientras la app está abierta. Si cierras la pestaña o el navegador, suele borrarse por seguridad.
* **Refresh Token:** Se guarda en una **Cookie segura (HTTP-only)** en el navegador. 
  * *Nota de seguridad:* Que sea "HTTP-only" significa que JavaScript **no puede leerla**, lo que protege el token contra ataques maliciosos (como XSS) en caso de que alguien intente inyectar código en tu web.