# PROJECT_CONTEXT.md — FilaCero (Desarrollo Móvil Integral)

**Equipo:** Juan Cruz, Tania Portillo, Octavio Guerrero, Uriel Osuna · **Grupo:** IDyGS10B · UTS Sur de Sonora
**Fecha de revisión:** 22 de noviembre de 2026
**Base de la revisión:** código de `Movil-CeroFila-main.zip` (carpeta `FilaCero_NET11_UI_Final`). El repositorio `API-FilaCero-main.zip` aún no se ha contrastado (ver "Pendientes").

## 1. Contexto y propósito

FilaCero busca reducir filas y tiempos de espera en consultorios de atención rápida (por ejemplo, consultorios anexos a farmacias) mediante pre-registro del paciente, triaje inicial, emisión de turno con QR firmado y seguimiento del estado del turno.

El proyecto de esta materia se centra en la **app móvil del paciente** (.NET MAUI 11, C# y XAML), pensada para tolerar pérdida de cobertura dentro de locales comerciales, cuidar batería y datos, y sincronizarse con un backend.

> **Prototipo académico, no apto para atención médica real.** El triaje es demostrativo (`DemoTriageRules`), no está validado por personal clínico y no debe usarse con datos de pacientes reales.

## 2. Estructura real del repositorio móvil

```
FilaCero.sln
src/
  FilaCero.Mobile/   App MAUI: MauiProgram.cs, App.xaml.cs, MainTabsPage.cs (Shell),
                     Pages/ (Home, Register, Triage, Ticket, Settings),
                     Services/ (ApiConfiguration, FilaCeroApi, AppState, QueueStream)
  FilaCero.Domain/   Models.cs (DTOs, ClinicNames, QueueStatuses) y Rules.cs (validación de registro, triaje demo)
  FilaCero.Api/      API de apoyo ASP.NET Core 11 (Minimal API en Program.cs, sin controladores):
                     Security/TokenService.cs, Realtime/QueueHub.cs,
                     Data/ (IClinicStore, MemoryClinicStore, PostgresClinicStore)
tests/FilaCero.SmokeTests/   Comprobaciones de dominio (registro y triaje)
scripts/PRUEBA_FLUJO_API.ps1 Prueba REST de extremo a extremo
db/01_schema.sql             Esquema PostgreSQL (modo opcional)
docs/                        ARQUITECTURA.md, PLAN_SCRUM_Y_PRUEBAS.md, REVISION_ESTRUCTURAL.md
.github/workflows/ci.yml     CI: compila API/dominio y corre SmokeTests (NO compila la app MAUI)
docker-compose.yml, INICIAR_API_WINDOWS.bat, PRUEBAS_WINDOWS.bat
```

El repositorio **API-FilaCero-main** (backend separado con `Controllers/`, `Services/HorarioService.cs`, `Models/Dto.cs`, `bd/` y `FilaCero.Api.http`) es un segundo proyecto y no forma parte de este zip.

## 3. Alcance (In Scope)

- **Cliente móvil del paciente** con cinco secciones en la barra inferior (Shell): Inicio, Registro, Triaje, Mi turno y Ajustes.
- **Servicios móviles:** `ApiConfiguration` (URL interna, sin que el paciente escriba IP), `FilaCeroApi` (HttpClient + JSON + Bearer, timeout de 30 s), `AppState` (sesión y último pase en SecureStorage) y `QueueStream` (WebSocket `/ws/queues`).
- **Dominio compartido** (`FilaCero.Domain`): contratos y reglas usados por app y API.
- **API local de demostración** (`src/FilaCero.Api`) para ejecutar el flujo completo (modo DEMO en memoria o PostgreSQL).
- **Pruebas:** SmokeTests de dominio, script `PRUEBA_FLUJO_API.ps1` y CI básico.

### Endpoints que consume la app

| Método | Ruta | Uso |
|---|---|---|
| GET | `/api/health` | Comprobar servicio (Ajustes) |
| POST | `/api/mobile/register` | Registro; devuelve token Bearer de 2 h |
| GET | `/api/me` | Paciente, triaje y turno actuales |
| POST | `/api/triage` | Evaluar y guardar triaje |
| POST | `/api/turns` | Emitir turno con QR firmado |
| GET | `/api/turns/mine` | Estado actual del turno |
| WS | `/ws/queues` | Evento `queue.updated` (subprotocolos `filacero`, `fc-session.<token>`) |

## 4. Exclusiones (Out of Scope del incremento actual)

- Pagos o cobros en la app.
- Expediente clínico electrónico o historial médico.
- Panel administrativo, plantillas o reportes. Los endpoints `/api/admin/*` existen solo como **herramienta de prueba** (clave `X-Admin-Key` de demostración) para simular cambios de estado; no son parte de la app del paciente.
- Publicación en Google Play / App Store y despliegue productivo (no hay API pública; `ProductionApiUrl` está vacío).
- Notificaciones push y WebSocket en segundo plano: los avisos y la vibración funcionan solo con la app abierta.
- Validación clínica de las reglas de triaje.

## 5. Reglas y restricciones

- **Acceso a datos mediado por API:** la app nunca se conecta a la base de datos.
- **Secuencia del flujo:** Registro → Triaje → Turno. No se emite turno sin paciente registrado y triaje respondido.
- **Triaje rojo:** si hay dolor torácico, dificultad respiratoria o pérdida de conocimiento, se bloquea el turno virtual y se indica acudir a urgencias. Se aplica en la app (`TicketPage`) y en el servidor (`IssueTurnAsync`).
- **Responsividad:** llamadas de red con `async/await`; WebSocket en lugar de polling continuo.
- **Disponibilidad sin conexión:** el último pase (`TurnDto` con `QrPayload`) se guarda en SecureStorage y se re-dibuja sin red. El token de sesión se guarda aparte. Al abrir, solo se acepta un pase cuyo QR empiece con `FC1|A-`.
- **Privacidad:** el QR no contiene CURP, NSS ni nombre; solo datos ficticios en pruebas.
- **Trabajo en GitHub:** ramas por integrante (`Dev/INTEGRANTE`) hacia `main`, contrastadas con CI y pruebas.

## 6. Decisiones aprobadas

- .NET 11 / MAUI 11 para app, dominio y API; contratos compartidos en `FilaCero.Domain`.
- Navegación con Shell y `TabBar` inferior de cinco secciones.
- La dirección del servidor se resuelve en `ApiConfiguration.ResolveBaseUrl()`, sin campo de IP en Ajustes:
  - Emulador Android (Debug): `http://10.0.2.2:5071`.
  - Teléfono Android físico (Debug): `http://127.0.0.1:5071` con `adb reverse tcp:5071 tcp:5071`.
  - Simulador iOS (Debug): `http://localhost:5071`.
  - Producción: `ProductionApiUrl` (vacío hasta publicar la API con HTTPS).
- Persistencia local con SecureStorage (sesión y último pase).
- Autenticación de prototipo: token HMAC-SHA256 de 2 h, sin refresh ni roles.
- Almacenamiento intercambiable en la API (`IClinicStore`): memoria (DEMO) o PostgreSQL.

## 7. Decisiones pendientes

| # | Pendiente | Detalle |
|---|---|---|
| P1 | **Alinear contratos con `API-FilaCero-main`** | Comparar `FilaCero.Domain/Models.cs` y las rutas de arriba con `Dto.cs` y los controladores (`AuthController`, `CitasController`, `ClinicasController`, `SucursalesController`). Hoy usan nombres distintos (turnos vs. citas). Decidir cuál backend usará la entrega final. |
| P2 | **Fecha de nacimiento vs. NSS** | `RegisterPage` convierte la fecha en un NSS ficticio (`yyyyMMdd001`) y envía CURP vacía para pasar la validación de 11 dígitos. Decidir si se agrega un campo real de fecha al contrato. |
| P3 | **Clínicas fijas vs. sucursales** | Las tres clínicas están fijas en `ClinicNames` y sus direcciones/distancias están escritas a mano en `RegisterPage`. Decidir si vendrán de la API (sucursales). |
| P4 | **Reconexión del WebSocket** | `QueueStream` reintenta cada 5 s sin límite mientras la app está activa. Decidir backoff o tope de reintentos para cuidar batería. |
| P5 | **URL de producción y HTTPS** | `ProductionApiUrl` vacío; sin backend público la app solo muestra el último pase. |
| P6 | **Alcance de autenticación** | Sesión de prototipo (2 h, sin refresh). Definir si se requiere algo más. |
| P7 | **Compilación y pruebas reales** | El README indica que el código se escribió sin SDK .NET y no se compiló ni ejecutó. Falta validar en Visual Studio y en emulador/dispositivo; el CI no compila la app MAUI. |
