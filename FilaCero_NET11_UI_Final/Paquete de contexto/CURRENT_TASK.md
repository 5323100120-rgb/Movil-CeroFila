# CURRENT_TASK.md — Incremento actual de FilaCero.Mobile

## Tarea activa

Asegurar el recorrido completo del paciente en `FilaCero.Mobile` y verificarlo contra la API:

**Registro (elección de clínica) → Triaje → Emisión del turno con QR → Consulta sin conexión → Ajustes (salud del servicio y limpieza local)**

con estado centralizado en `AppState.cs` y actualizaciones en vivo por `QueueStream.cs`.

## Qué se implementa en este incremento

- Navegación entre las cinco pestañas desde `MainTabsPage.cs`, con saltos automáticos (`GoTo`) tras registro y triaje.
- `RegisterPage`: elección de clínica, fecha de nacimiento, documento opcional (PDF/JPG/PNG de hasta 1 MB) y validación con `RegistrationRules` antes de enviar.
- `TriagePage`: cuestionario, envío a `/api/triage` y bloqueo ante nivel rojo.
- `TicketPage`: emisión (`POST /api/turns`), actualización (`GET /api/turns/mine`) y QR dibujado desde el `QrPayload` guardado.
- `AppState`: guardado del pase en SecureStorage, recuperación al abrir y recuperación tras sesión caducada.
- `SettingsPage`: comprobar servicio (`/api/health`) y borrar sesión y pase locales.
- `QueueStream` + `App.xaml.cs`: WebSocket activo en `Activated` y detenido en `Deactivated`.
- Revisión de compatibilidad entre `FilaCeroApi.cs` / `FilaCero.Domain` y la API.

## Qué queda fuera

- Panel de administración y endpoints `/api/admin/*` (solo para simular estados en pruebas).
- Funciones nuevas fuera de las cinco pestañas.
- Cambios al esquema SQL que no sean necesarios.
- Push, segundo plano, pagos, expediente clínico y publicación en tiendas.
- Validación clínica del triaje.

## Decisiones aprobadas

- Flujo secuencial: sin registro → registrado (clínica asignada) → triaje guardado → turno con pase guardado. `AppState` y la API lo hacen cumplir:
  - `/api/triage` devuelve 409 si ya hay turno.
  - `/api/turns` devuelve 409 si el triaje es rojo.
- Separación entre vistas (`Pages/`), servicios (`Services/`) y contratos (`FilaCero.Domain`).
- El borrado en Ajustes es solo local; no modifica datos del servidor.
- No se borra el último pase al fallar la red o caducar la sesión.

## Decisiones pendientes

| # | Pendiente | Cómo cerrarlo |
|---|---|---|
| T1 | Contratos y rutas frente a `API-FilaCero-main` | Subir ese zip y comparar `Dto.cs` y controladores con `Models.cs` y la tabla de endpoints de `PROJECT_CONTEXT.md`. |
| T2 | `HorarioService` / `TokenService` de la otra API | Revisar si el formato de token y las reglas de horario coinciden con lo que espera la app (`FC1|A-...`, Bearer de 2 h). |
| T3 | Fecha de nacimiento enviada como NSS ficticio | Decidir si se corrige el contrato o se documenta como limitación. |
| T4 | Política de reconexión de `QueueStream` | Decidir backoff y tope de reintentos. |
| T5 | Ejecución real de pruebas | Correr SmokeTests, `PRUEBA_FLUJO_API.ps1` y escenarios en emulador; registrar resultados o hallazgos. |

## Criterios de aceptación verificables

| # | Criterio | Cómo verificarlo | Estado |
|---|---|---|---|
| 1 | **Registro:** con nombre inválido, NSS distinto de 11 dígitos o clínica fuera de la lista, `RegisterPage` no envía y muestra alerta "Revisa tus datos". Con datos válidos, registra y salta a Triaje. | Probar en la app y con `SmokeTests`. | Por ejecutar |
| 2 | **Triaje:** sin síntomas críticos se avanza a "Mi turno". Con dolor torácico, dificultad respiratoria o pérdida de conocimiento aparece el aviso de urgencia, no se navega a "Mi turno" y el botón de turno queda deshabilitado. | Probar en la app; `UT-TRIAGE-01` en SmokeTests. | Por ejecutar |
| 3 | **Turno y token:** al generar el turno se muestran código, clínica, estado y QR. | Probar con API local en modo DEMO. | Por ejecutar |
| 4 | **Sin conexión:** con modo avión, al abrir "Mi turno" el último pase y su QR siguen visibles con la etiqueta "ÚLTIMO PASE GUARDADO • SIN CONEXIÓN". | Probar en emulador/dispositivo (`FT-OFFLINE-07`). | Pendiente de prueba en dispositivo |
| 5 | **Actualización en vivo:** al cambiar el estado por `PATCH /api/admin/turns/{id}`, la app abierta actualiza estado y consultorio, y vibra con "Llamado a consultorio". La UI no se congela. | Probar con `INICIAR_API_WINDOWS.bat` y PowerShell; latencia sin medir (`IT-SOCKET-02`). | Pendiente de prueba en dispositivo |
| 6 | **Ajustes:** "Comprobar servicio" informa disponibilidad (con y sin API encendida) y "Eliminar datos locales" deja la app sin sesión ni pase. | Probar en la app. | Por ejecutar |
| 7 | **Compilación y pruebas:** compilan la API (`dotnet build src/FilaCero.Api/...`) y la app (`-f net11.0-android`). Pasan las SmokeTests y `PRUEBA_FLUJO_API.ps1`, o los hallazgos quedan documentados. | Ejecutar localmente; el CI solo cubre API, dominio y SmokeTests. | Sin ejecutar (el README indica que no se compiló) |
