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

## Usuarios de prueba

| Usuario | Contraseña | Rol |
|---|---|---|
| superusuario | Super_2026* | SuperUsuario |
| administrador | Admin_2026* | Administrador |
| cliente01 | Cliente_2026* | Cliente |

## Manual breve

**Cliente**
1. Iniciar sesión con `cliente01` o registrarse.
2. En **Buscar vuelos**, elegir origen, destino y fecha.
3. Elegir el vuelo y los asientos, registrar los pasajeros y revisar el resumen.
4. Pagar. Es un pago simulado: las tarjetas cuyo número termina en 0000 se rechazan.
5. En **Mis reservas** se consultan y gestionan las reservas. En **Check-in** se confirma el ingreso y en **Comprobantes** se ven los comprobantes.

**Administrador**
- En **Administracion** se gestionan vuelos, aeronaves y reservas (CRUD).
- Se consultan los reportes.

**Superusuario**
- Incluye lo del administrador y la gestión de usuarios y roles.

## Notas

- La recuperación de contraseña no envía correos porque no hay servicio de correo configurado.
- Los comprobantes son académicos y no tienen validez tributaria.
