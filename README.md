# Aerolínea VIP

Sistema web para la compra y reserva de pasajes aéreos. Desarrollado en ASP.NET MVC (.NET Framework 4.8.1) con SQL Server y ASP.NET Identity.

| Dato | Detalle |
|---|---|
| Universidad | Universidad de las Fuerzas Armadas ESPE |
| Carrera | Tecnologías de la Información |
| Materia | Desarrollo Web para Integración de Tecnologías |
| NRC | 36641 |
| Docente | Ing. Juan Fernando Galarraga Hurtado |
| Grupo | 6 |

**Integrantes**
- Alejandro de la Cruz
- Kennet Risco

## Estructura del repositorio

- `src/`: código fuente. Contiene la solución `Prog_03.slnx` y el proyecto `ConAutenticacion`.
- `database/`: script SQL `Script_BD_AerolineaVIP_Unificado.sql` que crea las bases `AerolineaVIP` y `AerolineaVIP_Identity`.
- `docs/`: documentación de la primera entrega y manual de uso (PDF).

## Puesta en marcha

1. Ejecutar `database/Script_BD_AerolineaVIP_Unificado.sql` completo (F5) en SQL Server Management Studio.
2. Abrir `src/Prog_03.slnx` en Visual Studio con .NET Framework 4.8.1.
3. Restaurar los paquetes NuGet y compilar.
4. Ejecutar la aplicación. Al iniciar, `Global.asax.cs` crea los roles y los usuarios de prueba.

Requisitos: SQL Server 2016 o superior en `localhost`. Las cadenas de conexión están en `src/ConAutenticacion/Web.config`.

Si al ejecutar aparece un error que menciona `AerolineaVIP_Identity` o `ExecuteSqlCommand`:
- Verifique que el script `database/Script_BD_AerolineaVIP_Unificado.sql` se ejecutó completo (las dos bases deben existir).
- Verifique que el usuario y la clave de `DefaultConnection` y `AerolineaVIPContext` en `src/ConAutenticacion/Web.config` coinciden con los de su SQL Server (en este proyecto: `sa` con la clave configurada en esos archivos).

## Usuarios de prueba

| Usuario | Contraseña | Rol |
|---|---|---|
| superusuario | Super_2026* | SuperUsuario |
| administrador | Admin_2026* | Administrador |
| counter01 | Counter_2026* | Counter |
| cliente01 | Cliente_2026* | Cliente |

## Manual breve

**Cliente**
1. Iniciar sesión con `cliente01` o registrarse.
2. En **Buscar vuelos**, elegir origen, destino y fecha.
3. Elegir el vuelo y los asientos, registrar los pasajeros y revisar el resumen.
4. Pagar. Es un pago simulado: las tarjetas cuyo número termina en 0000 se rechazan.
5. En **Mis reservas** se consultan y gestionan las reservas. En **Check-in** se confirma el ingreso y en **Comprobantes** se ven los comprobantes.

**Counter (mostrador)**
1. Iniciar sesion con `counter01` y abrir **Mostrador (Counter)**.
2. Buscar la reserva por su codigo `RV########`.
3. En el detalle, cambiar el asiento de un pasajero si el vuelo aun no sale y no tiene check-in completado.

**Administrador**
- En **Administracion** se gestionan vuelos, aeronaves y reservas (CRUD).
- En **Usuarios y pasajeros** se crean, editan y eliminan usuarios. No se eliminan cuentas con reservas.
- Se consultan los reportes.

**Superusuario**
- Incluye lo del administrador y la gestión de usuarios y roles.

## Mapa de navegación

El mapa sigue el apartado 11.4.2 del enunciado. Cada pantalla tiene la ruta indicada en el inventario de 21 pantallas (apartado 11.4.5).

Flujo principal de compra:

```
Buscar (5) -> Resultados (6) -> Disponibilidad y asientos (7, 8)
  -> Pasajeros (9) -> Confirmar reserva (10) -> Pagar (11) -> Confirmación (12)
  -> Mis reservas (13) -> Gestionar (14), Check-in (15), Comprobantes (16)
```

| N.º | Pantalla | Ruta | Perfil |
|---|---|---|---|
| 1 | Página de inicio | /Home/Index | Público |
| 2 | Inicio de sesión | /Account/Login | Visitante |
| 3 | Registro de usuarios | /Account/Register | Visitante |
| 4 | Recuperación de acceso | /Account/ForgotPassword | Visitante |
| 5 | Búsqueda de vuelos | /Vuelos/Buscar | Público |
| 6 | Resultados de búsqueda y horarios | /Vuelos/Resultados | Público |
| 7 | Consulta de disponibilidad de asientos | /Vuelos/Disponibilidad/{vueloId} | Público |
| 8 | Selección de vuelo y asiento | /Reservas/SeleccionAsiento/{vueloId} | Cliente |
| 9 | Registro de datos del pasajero | /Reservas/Pasajeros | Cliente |
| 10 | Reserva de pasajes | /Reservas/Confirmar | Cliente |
| 11 | Proceso de compra y pago | /Compras/Pagar/{reservaId} | Cliente titular |
| 12 | Confirmación de compra | /Compras/Confirmacion/{reservaId} | Cliente titular |
| 13 | Consulta de reservas | /Reservas/MisReservas | Cliente |
| 14 | Gestión de reservas y cancelaciones | /Reservas/Gestionar/{reservaId} | Cliente titular |
| 15 | Check-in | /CheckIn/Index | Cliente titular |
| 16 | Visualización de comprobantes | /Comprobantes/Index | Cliente titular |
| 17 | Panel del administrador | /Admin/Index | Administrador |
| 18 | Administración de vuelos e itinerarios | /Admin/Vuelos | Administrador |
| 19 | Administración de aeronaves | /Admin/Aeronaves | Administrador |
| 20 | Gestión de usuarios, pasajeros y reservas | /Admin/Gestion | Administrador |
| 21 | Consultas y reportes administrativos | /Admin/Reportes | Administrador |

Pantalla de mostrador (Counter), definida en el mapa del área de counter: `/Counter/Index` (consulta de reservas) y `/Counter/Detalle/{codigo}` (detalle y cambio de asiento).

Las pantallas 9 y 10 se procesan por POST desde el flujo de reserva. Si se accede a su URL directamente, el sistema regresa a la búsqueda con un mensaje.

Los parámetros `{reservaId}` reciben el código numérico de la reserva. Las rutas están definidas en `App_Start/RouteConfig.cs`. Las rutas de la tabla que no coinciden con el nombre del controlador (por ejemplo, `/Vuelos/Buscar`) son alias que apuntan a la acción correspondiente.

## Validaciones y mensajes de error

| Formulario | Validación del servidor | Mensaje al usuario |
|---|---|---|
| Búsqueda de vuelos | Origen, destino distintos, fecha no anterior a hoy, de 1 a 7 pasajeros | Lista de errores bajo el formulario |
| Asientos (pantallas 7 y 8) | Máximo 7 asientos, asiento libre al confirmar (transacción serializable) | "Uno de los asientos seleccionados acaba de ocuparse. Seleccione otro." |
| Pasajeros (pantalla 9) | Documento según tipo (CEDULA 10 dígitos, PASAPORTE 5-20), nombres y correo válidos | Mensaje por campo y resumen de errores |
| Pago (pantalla 11) | Número de tarjeta con Luhn, longitud según marca, vencimiento, código de seguridad | Mensaje de rechazo o validación por campo |
| Check-in | Ventana de 48 h a 1 h antes de la salida, reserva pagada, sin check-in previo | Mensaje con el motivo del bloqueo |
| Counter (buscar reserva) | Código RV + 8 dígitos y documento obligatorio | "Revise el código de reserva (RV + 8 dígitos) y el número de documento." |
| Counter (cambio de asiento) | Vuelo no ha salido, sin check-in completado, asiento libre | Mensaje de éxito o de error en el detalle |
| Vuelos (crear y editar) | Frecuencia activa, día de la semana de la fecha igual al de la frecuencia (RN-04), aeronave no NO_DISPONIBLE (RN-06), sin solapamiento de horarios de la aeronave, sin vuelo duplicado en la misma frecuencia y fecha | Mensaje indicando la regla incumplida |
| Aeronaves (crear y editar) | Matrícula única | "La matricula XX ya esta registrada en otra aeronave." |
| Usuarios (crear y editar) | Usuario único, correo único, contraseña con requisitos | Mensaje por campo |

## Notas

- La recuperación de contraseña no envía correos porque no hay servicio de correo configurado.
- Los comprobantes son académicos y no tienen validez tributaria.
