using System;
using System.Collections.Generic;

namespace ConAutenticacion.Models
{
    // Datos de un pasajero tal como se envian desde el formulario (no es una entidad de BD).
    public class PasajeroFormulario
    {
        public string TipoDocumentoCodigo { get; set; }
        public string NumeroDocumento { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
    }

    // Estado del flujo de reserva: vuelo, asientos elegidos y pasajeros.
    public class FlujoReservaVM
    {
        public FlujoReservaVM()
        {
            Asientos = new List<int>();
            Pasajeros = new List<PasajeroFormulario>();
            CodigosAsiento = new Dictionary<int, string>();
        }

        public int VueloId { get; set; }
        public vw_VueloDetalle Vuelo { get; set; }
        public List<int> Asientos { get; set; }
        public List<PasajeroFormulario> Pasajeros { get; set; }
        public Dictionary<int, string> CodigosAsiento { get; set; }
        public decimal Tarifa { get; set; }

        public decimal Total
        {
            get { return Tarifa * Asientos.Count; }
        }
    }

    public class AsientoVM
    {
        public int AsignacionId { get; set; }
        public Pasajero Pasajero { get; set; }
        public string CodigoAsiento { get; set; }
        public vw_VueloDetalle Vuelo { get; set; }
        public decimal Importe { get; set; }
        public string Estado { get; set; }
    }

    public class ReservaDetalleVM
    {
        public ReservaDetalleVM()
        {
            Asientos = new List<AsientoVM>();
        }

        public Reserva Reserva { get; set; }
        public Compra Compra { get; set; }
        public Pago UltimoPago { get; set; }
        public List<AsientoVM> Asientos { get; set; }
        public decimal Total { get; set; }
    }

    public class CheckInItemVM
    {
        public int AsignacionId { get; set; }
        public string Pasajero { get; set; }
        public string Documento { get; set; }
        public string Vuelo { get; set; }
        public string Ruta { get; set; }
        public string Asiento { get; set; }
        public bool Elegible { get; set; }
        public bool YaRegistrado { get; set; }
        public string Motivo { get; set; }
    }

    public class ReporteVM
    {
        public ReporteVM()
        {
            Encabezados = new string[0];
            Filas = new List<string[]>();
        }

        public string Consulta { get; set; }
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public DateTime Corte { get; set; }
        public string Mensaje { get; set; }
        public string[] Encabezados { get; set; }
        public List<string[]> Filas { get; set; }
    }

    public class GestionUsuarioVM
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Roles { get; set; }
    }
}
