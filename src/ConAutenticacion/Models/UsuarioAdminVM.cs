using System.ComponentModel.DataAnnotations;

namespace ConAutenticacion.Models
{
    public class CrearUsuarioVM
    {
        [Required(ErrorMessage = "Ingrese el nombre de usuario.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 50 caracteres.")]
        [RegularExpression(@"^[A-Za-z0-9_.-]+$", ErrorMessage = "El usuario solo puede tener letras, numeros, punto, guion y guion bajo.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Ingrese el correo electronico.")]
        [EmailAddress(ErrorMessage = "El correo electronico no es valido.")]
        [StringLength(256)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Ingrese los nombres.")]
        [StringLength(100, ErrorMessage = "Los nombres no pueden superar 100 caracteres.")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Ingrese los apellidos.")]
        [StringLength(100, ErrorMessage = "Los apellidos no pueden superar 100 caracteres.")]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "Ingrese una contrasena inicial.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Seleccione el rol del usuario.")]
        public string Rol { get; set; }
    }

    public class EditarUsuarioVM
    {
        public string Id { get; set; }

        public string UserName { get; set; }

        [Required(ErrorMessage = "Ingrese el correo electronico.")]
        [EmailAddress(ErrorMessage = "El correo electronico no es valido.")]
        [StringLength(256)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Ingrese los nombres.")]
        [StringLength(100, ErrorMessage = "Los nombres no pueden superar 100 caracteres.")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Ingrese los apellidos.")]
        [StringLength(100, ErrorMessage = "Los apellidos no pueden superar 100 caracteres.")]
        public string Apellidos { get; set; }
    }
}
