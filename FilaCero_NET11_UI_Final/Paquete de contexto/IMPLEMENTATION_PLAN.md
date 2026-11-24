# IMPLEMENTATION_PLAN.md — Plan de implementación y verificación

Cada paso termina con un resultado que se puede comprobar. Registrar hallazgos en la rama de cada integrante (`Dev/INTEGRANTE`) antes de integrar a `main`.

## Paso 0 — Preparar el entorno
- Visual Studio Community 2026 con carga MAUI y Android SDK; SDK .NET 11 (`dotnet --info`, `dotnet workload list`).
- Abrir `FilaCero.sln` y restaurar NuGet.
- **Resultado:** `dotnet build src/FilaCero.Api/FilaCero.Api.csproj` compila sin errores.

## Paso 1 — Auditar contratos, rutas y configuración
1. Contrastar `src/FilaCero.Domain/Models.cs` y `Rules.cs` con los endpoints de `src/FilaCero.Api/Program.cs` (rutas, verbos y JSON).
2. Verificar `FilaCeroApi.cs`: rutas, `Authorization: Bearer`, manejo de `ApiError` y timeout de 30 s.
3. Verificar `ApiConfiguration.cs` frente a `Properties/launchSettings.json` (puerto 5071).
4. Subir `API-FilaCero-main.zip` y repetir la comparación con `Dto.cs`, los controladores, `HorarioService.cs`, `TokenService.cs` y `FilaCero.Api.http`.
- **Resultado:** tabla de diferencias (ruta, verbo, campo, ¿coincide?) con decisión para cada una. Cierra T1 y T2.

## Paso 2 — Validar estado local, ciclo de vida y tiempo real
1. `AppState.cs`: guardado y recuperación del pase, sesión caducada y limpieza.
2. `QueueStream.cs` y `App.xaml.cs`: inicio en `Activated`, parada en `Deactivated`, reconexión cada 5 s.
3. Decidir y, si procede, implementar backoff o tope de reintentos (T4).
- **Resultado:** con el servidor apagado la app no se congela y el pase sigue visible.

## Paso 3 — Revisar las cinco páginas
1. `MainTabsPage.cs`: orden de pestañas y métodos `GoTo` / `Reconnect`.
2. `RegisterPage`: validaciones, límite de 1 MB, manejo de errores, y revisión de la fecha enviada como NSS (T3).
3. `TriagePage`: lectura de presión arterial y bloqueo rojo.
4. `TicketPage`: estados del botón, QR y actualización manual.
5. `SettingsPage`: salud del servicio y limpieza.
- **Resultado:** lista de defectos con archivo y línea, corregidos o documentados.

## Paso 4 — Pruebas, base de datos y documentación
1. Ejecutar:
   ```powershell
   dotnet run --project tests/FilaCero.SmokeTests/FilaCero.SmokeTests.csproj
   dotnet build src/FilaCero.Api/FilaCero.Api.csproj
   dotnet build src/FilaCero.Mobile/FilaCero.Mobile.csproj -f net11.0-android
   powershell -ExecutionPolicy Bypass -File .\scripts\PRUEBA_FLUJO_API.ps1
   ```
2. Escenarios en emulador o dispositivo: flujo completo, modo avión en "Mi turno", urgencia roja en Triaje, cambio de estado con `PATCH /api/admin/turns/{id}` y vibración, y borrado local en Ajustes.
3. (Opcional) `docker compose up -d` y repetir el flujo en modo PostgreSQL con `db/01_schema.sql`.
4. Actualizar `README.md` y `docs/PLAN_SCRUM_Y_PRUEBAS.md` con lo realmente ejecutado: dejar de marcar como "no ejecutado" lo que ya se probó y no declarar métricas que no se midieron.
5. Considerar agregar la compilación de `FilaCero.Mobile` al CI (hoy solo cubre API, dominio y SmokeTests).
6. Integrar: PR de `Dev/INTEGRANTE` a `main`, revisado por otra persona.
- **Resultado:** criterios 1 a 7 de `CURRENT_TASK.md` marcados como cumplidos o con hallazgo documentado.

## Reparto sugerido

| Integrante | Foco |
|---|---|
| Juan Cruz | Paso 1: contratos y rutas (ambos repositorios) |
| Tania Portillo | Paso 3: `RegisterPage` y `TriagePage` |
| Octavio Guerrero | Paso 2 y Paso 3: `AppState`, `QueueStream`, `TicketPage` |
| Uriel Osuna | Paso 4: pruebas, CI y documentación |

*Es una propuesta; ajústenla según lo que ya acordaron en el equipo.*
