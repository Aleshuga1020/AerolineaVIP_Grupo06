using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ConAutenticacion.Models
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [Display(Name = "Correo electronico")]
        public string Email { get; set; }
    }

    public class ExternalLoginListViewModel
    {
        public string ReturnUrl { get; set; }
    }

    public class SendCodeViewModel
    {
        public string SelectedProvider { get; set; }
        public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
        public string ReturnUrl { get; set; }
        public bool RememberMe { get; set; }
    }

    public class VerifyCodeViewModel
    {
        [Required]
        public string Provider { get; set; }

        [Required]
        [Display(Name = "Codigo")]
        public string Code { get; set; }
        public string ReturnUrl { get; set; }

        [Display(Name = "Recordar este explorador?")]
        public bool RememberBrowser { get; set; }

        public bool RememberMe { get; set; }
    }

    public class ForgotViewModel
    {
        [Required]
        [Display(Name = "Correo electronico")]
        public string Email { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Nombre de usuario")]
        public string UserName { get; set; }

        //[Required]
        //[Display(Name = "Correo electronico")]
        // [EmailAddress]
        //public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Contrasena")]
        public string Password { get; set; }

        [Display(Name = "Recordar cuenta?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ingrese sus nombres.")]
        [RegularExpression(@"^[\p{L} ]{2,100}$", ErrorMessage = "Nombres: solo letras y espacios (2 a 100).")]
        [Display(Name = "Nombres")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Ingrese sus apellidos.")]
        [RegularExpression(@"^[\p{L} ]{2,100}$", ErrorMessage = "Apellidos: solo letras y espacios (2 a 100).")]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; }

        [Required]
        [RegularExpression(@"^[A-Za-z0-9._]{4,50}$", ErrorMessage = "Nombre de usuario: 4 a 50 caracteres (letras, digitos, punto o guion bajo).")]
        [Display(Name = "Nombre de usuario")]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Correo electronico")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "La contrasena debe tener entre 8 y 100 caracteres.", MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,100}$", ErrorMessage = "La contrasena debe incluir mayuscula, minuscula, numero y simbolo.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contrasena")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contrasena")]
        [Compare("Password", ErrorMessage = "La contrasena y la contrasena de confirmacion no coinciden.")]
        public string ConfirmPassword { get; set; }

        // Solo SuperUsuario puede elegir el rol; un visitante siempre obtiene Cliente.
        [Display(Name = "Rol")]
        public string Rol { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Debe aceptar las condiciones de uso.")]
        [Display(Name = "Acepto las condiciones de uso")]
        public bool AceptaCondiciones { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Correo electronico")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "El numero de caracteres de {0} debe ser al menos {2}.", MinimumLength = 3)]
        [DataType(DataType.Password)]
        [Display(Name = "Contrasena")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contrasena")]
        [Compare("Password", ErrorMessage = "La contrasena y la contrasena de confirmacion no coinciden.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [Display(Name = "Correo o nombre de usuario")]
        public string Email { get; set; }
    }
}
