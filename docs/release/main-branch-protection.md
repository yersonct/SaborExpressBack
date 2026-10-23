# Protección obligatoria de `main`

Este archivo documenta los controles que se configuran en GitHub. No se pueden activar desde un commit del repositorio y requieren permisos de administrador.

En **Settings → Rules → Rulesets** (o **Settings → Branches → Branch protection rule**) cree una regla para `main` con estos requisitos:

- Bloquear actualizaciones directas; permitir cambios solamente mediante pull request.
- Exigir al menos una aprobación. Descartar aprobaciones obsoletas cuando cambie el commit de la PR.
- Exigir que la rama esté actualizada con `main` antes de combinarla.
- Exigir los checks `production-release-gate`, `security-gate` y `GitGuardian Security Checks`.
- Bloquear force-push y eliminación de `main`.
- Aplicar la regla también a administradores o definir explícitamente quién puede omitirla.

El check `production-release-gate` valida la evidencia de QA y smoke, el código aprobado y los despliegues reales de Render mediante sus IDs y commits. Requiere `RENDER_API_KEY` y los IDs de servicio configurados en GitHub. No reemplaza la revisión humana ni los checks de seguridad. El contrato Render Git no exige un digest de imagen GHCR.

La aprobación registrada en el JSON no sustituye la aprobación de la PR. Configure también el entorno GitHub `production` con revisores autorizados. Si Render despliega automáticamente desde `main`, la protección de la PR debe estar activa antes del merge; el smoke posterior verifica un despliegue ya realizado.

## Comprobación

Después de guardar la regla, confirme en GitHub que:

1. `main` muestra la regla activa.
2. Una PR sin aprobación no se puede combinar.
3. Una PR con un check requerido fallido no se puede combinar.
4. Un push directo a `main` es rechazado.

La conexión de automatización usada para esta HU no tiene permiso de administración del repositorio. Por eso no se declara que la regla haya quedado activa hasta que un administrador la configure y verifique en GitHub.
