# SaborExpress Backend

API .NET 8 del proyecto de **yersonct**. Monolito modular con PostgreSQL, JWT/cookies, SignalR, pagos Wompi y notificaciones. Repositorio: https://github.com/yersonct/SaborExpressBack.

## Requisitos y arranque

Instalar .NET SDK 8 y PostgreSQL. PostgreSQL es el único proveedor con migraciones soportadas.

1. Copiar `.env.example` a `.env`, completar la conexión y generar una clave JWT aleatoria de al menos 32 bytes. Nunca subir `.env`.
2. Configurar orígenes exactos, URL pública y credenciales Brevo/Wompi propias para las integraciones utilizadas.
3. Ejecutar `dotnet restore` y `dotnet build -c Release`.
4. Con la herramienta `dotnet-ef` 8 instalada, aplicar las migraciones **contra la conexión elegida explícitamente**: `dotnet ef database update --connection "<conexion-local>"`. La fábrica de diseño usa una conexión sin credenciales de producción para generar migraciones; no toma `.env` para actualizar bases.
5. En una base nueva, habilitar una sola vez `Database__ApplyMigrationsOnStartup=true` y `Database__SeedOnStartup=true` con los datos del gerente semilla propios; después volver a `false`.
6. Iniciar `dotnet run --no-launch-profile --urls http://localhost:5000`.

`/health` comprueba vivacidad. `/health/ready` responde 503 si falta conexión o existen migraciones pendientes. `/swagger/v1/swagger.json` publica el contrato en cualquier ambiente; Swagger UI se encuentra en `/swagger` solo en desarrollo. `/metrics` exige un JWT vigente con rol GERENTE; configurar el scraper en red privada con credenciales propias.

## Variables

El ejemplo enumera las variables necesarias: `ConnectionStrings__PostgresConnection`, `JwtSettings__*`, `AllowedOrigins__0`, `App__FrontendBaseUrl`, `AppSettings__PublicBaseUrl`, `InitialManager__*`, `Brevo__ApiKey` y `Wompi__*`. Los separadores `__` representan secciones de configuración .NET.

Las migraciones y semillas no se ejecutan por defecto. Fuera de desarrollo, migrar al arrancar también exige `Database__BackupConfirmed=true`; esta bandera debe establecerse después de un respaldo verificado. No migrar ni sembrar automáticamente en despliegues habituales.

## Contratos y datos

Las rutas `/api` se conservan para los clientes instalados; `/api/v1` ofrece los mismos endpoints para nuevas integraciones. Los listados generales de pedidos, pagos y clientes aceptan `page` (desde 1) y `pageSize` (1–100), con orden estable y límites aplicados en SQL. En `/api` la paginación es opcional: omitir ambos parámetros conserva el resultado completo esperado por clientes y reportes existentes. En `/api/v1` se aplica por defecto página 1 y tamaño 100. Consumir páginas sucesivas hasta obtener menos elementos que `pageSize`; las respuestas paginadas indican `X-Page` y `X-Page-Size`.

Los registros nuevos de clientes deben enviar `acceptedPrivacyPolicy: true` y `privacyPolicyVersion: "2026-10-30"` tras aceptación explícita. El servidor registra fecha UTC y versión. Las cuentas previas mantienen consentimiento desconocido, sin inventar aceptación.

Los errores incluyen `code`, `message`, `traceId` y `error` como compatibilidad para clientes anteriores; los fallos inesperados devuelven 500 genérico y se registran sin devolver detalles internos. Las subidas admiten PDF/JPEG/PNG/WEBP con firma, extensión y MIME coherentes y máximo 5 MiB.

La migración de correcciones del informe renombra cinco tablas/columnas, unifica importes en decimal(14,2), añade consentimiento y restricciones CHECK. Revisar el SQL generado y los datos existentes antes de aplicarla; un CHECK rechaza datos históricos inválidos en lugar de modificarlos silenciosamente. No ejecutar rollback destructivo para deshacerla: restaurar un respaldo verificado y volver al digest previo según `docs/release/`.

## Validación

`dotnet test tests/SaborExpressBack.Security.Tests/SaborExpressBack.Security.Tests.csproj -c Release`

Ejecutar también las suites QA e InvoicePdf de `tests/`. El workflow `backend-ci.yml` compila, ejecuta todas las suites y exporta el contrato: `dotnet run -c Release --no-build -- --export-openapi=artifacts/openapi.json`.

La rotación de credenciales comprometidas, restricciones Firebase/MapTiler y limpieza del historial deben realizarse en sus servicios/cuentas y coordinarse con todos los colaboradores. Este cambio de código no certifica que las credenciales antiguas hayan sido revocadas.
