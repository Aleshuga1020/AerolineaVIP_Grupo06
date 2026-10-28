using System;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Owin;
using ConAutenticacion.Models;

namespace ConAutenticacion
{
    public partial class Startup
    {
        // Para obtener mas informacion sobre como configurar la autenticacion, visite https://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // Configure el contexto de base de datos, el administrador de usuarios y el administrador de inicios de sesion para usar una unica instancia por solicitud
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            // Permitir que la aplicacion use una cookie para almacenar informacion para el usuario que inicia sesion
            // y una cookie para almacenar temporalmente informacion sobre un usuario que inicia sesion con un proveedor de inicio de sesion de terceros
            // Configurar cookie de inicio de sesion
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                Provider = new CookieAuthenticationProvider
                {
                    // Permite a la aplicacion validar la marca de seguridad cuando el usuario inicia sesion.
                    // Es una caracteristica de seguridad que se usa cuando se cambia una contrasena o se agrega un inicio de sesion externo a la cuenta.
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
                }
            });            
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Permite que la aplicacion almacene temporalmente la informacion del usuario cuando se verifica el segundo factor en el proceso de autenticacion de dos factores.
            app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));

            // Permite que la aplicacion recuerde el segundo factor de verificacion de inicio de sesion, como el telefono o correo electronico.
            // Cuando selecciona esta opcion, el segundo paso de la verificacion del proceso de inicio de sesion se recordara en el dispositivo desde el que ha iniciado sesion.
            // Es similar a la opcion Recordarme al iniciar sesion.
            app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);

            // Quitar los comentarios de las siguientes lineas para habilitar el inicio de sesion con proveedores de inicio de sesion de terceros
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            //app.UseTwitterAuthentication(
            //   consumerKey: "",
            //   consumerSecret: "");

            //app.UseFacebookAuthentication(
            //   appId: "",
            //   appSecret: "");

            //app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
            //{
            //    ClientId = "",
            //    ClientSecret = ""
            //});
        }
    }
}
