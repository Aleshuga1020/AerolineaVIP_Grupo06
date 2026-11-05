using System;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin;
using Owin;
using ConAutenticacion.Models;

[assembly: OwinStartupAttribute(typeof(ConAutenticacion.Startup))]
namespace ConAutenticacion
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }

        // Roles y usuarios iniciales creados por codigo; las contrasenas se cifran con PasswordHasher.
        public static void CrearRolesYUsuariosIniciales()
        {
            // La base la crea el script SQL; EF no debe intentar crearla ni validar el modelo.
            Database.SetInitializer<ApplicationDbContext>(null);

            try
            {
                var context = new ApplicationDbContext();

                // Garantiza que las columnas Nombres y Apellidos existan en AspNetUsers si la tabla existe sin ellas.
                context.Database.ExecuteSqlCommand(@"
                    IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NOT NULL
                    BEGIN
                        IF COL_LENGTH(N'dbo.AspNetUsers', N'Nombres') IS NULL
                            ALTER TABLE dbo.AspNetUsers ADD Nombres NVARCHAR(100) NULL;
                        IF COL_LENGTH(N'dbo.AspNetUsers', N'Apellidos') IS NULL
                            ALTER TABLE dbo.AspNetUsers ADD Apellidos NVARCHAR(100) NULL;
                    END
                ");

                var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
                var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

                foreach (var rol in new[] { "SuperUsuario", "Administrador", "Counter", "Cliente" })
                {
                    if (!roleManager.RoleExists(rol)) roleManager.Create(new IdentityRole(rol));
                }

                CrearUsuario(userManager, "superusuario", "super@aerolinea.com", "Super_2026*", "Super", "Usuario", "SuperUsuario");
                CrearUsuario(userManager, "administrador", "admin@aerolinea.com", "Admin_2026*", "Admin", "Sistema", "Administrador");
                CrearUsuario(userManager, "counter01", "counter@aerolinea.com", "Counter_2026*", "Counter", "Mostrador", "Counter");
                CrearUsuario(userManager, "cliente01", "cliente@aerolinea.com", "Cliente_2026*", "Cliente", "Demo", "Cliente");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "No se pudo preparar la base AerolineaVIP_Identity. Verifique que ejecuto el script completo " +
                    "database/Script_BD_AerolineaVIP_Unificado.sql y que la cadena DefaultConnection en Web.config " +
                    "(usuario, clave y servidor) coincide con su SQL Server. Detalle: " + ex.Message, ex);
            }
        }

        private static void CrearUsuario(UserManager<ApplicationUser> um, string userName, string email,
                                         string password, string nombres, string apellidos, string rol)
        {
            var user = um.FindByName(userName);
            if (user == null)
            {
                user = new ApplicationUser { UserName = userName, Email = email, Nombres = nombres, Apellidos = apellidos };
                if (!um.Create(user, password).Succeeded) return;
            }
            if (!um.IsInRole(user.Id, rol)) um.AddToRole(user.Id, rol);
        }
    }
}
