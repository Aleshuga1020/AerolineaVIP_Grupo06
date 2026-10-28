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
            CrearRolesYUsuariosIniciales();
        }

        // Roles y usuarios iniciales creados por codigo; las contrasenas se cifran con PasswordHasher.
        private void CrearRolesYUsuariosIniciales()
        {
            var context = new ApplicationDbContext();
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

            foreach (var rol in new[] { "SuperUsuario", "Administrador", "Cliente" })
            {
                if (!roleManager.RoleExists(rol)) roleManager.Create(new IdentityRole(rol));
            }

            CrearUsuario(userManager, "superusuario", "super@aerolinea.com", "Super_2026*", "Super", "Usuario", "SuperUsuario");
            CrearUsuario(userManager, "administrador", "admin@aerolinea.com", "Admin_2026*", "Admin", "Sistema", "Administrador");
            CrearUsuario(userManager, "cliente01", "cliente@aerolinea.com", "Cliente_2026*", "Cliente", "Demo", "Cliente");
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
