using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConAutenticacion.Models
{
    // Validaciones de formato del servidor. No consultan ningun servicio externo.
    public static class Validaciones
    {
        public static bool SoloDigitos(string s)
        {
            return !string.IsNullOrEmpty(s) && s.All(char.IsDigit);
        }

        // Algoritmo de Luhn para la verificacion de formato del numero de tarjeta.
        public static bool EsLuhnValido(string numero)
        {
            if (!SoloDigitos(numero)) return false;
            int suma = 0;
            bool doble = false;
            for (int i = numero.Length - 1; i >= 0; i--)
            {
                int d = numero[i] - '0';
                if (doble)
                {
                    d *= 2;
                    if (d > 9) d -= 9;
                }
                suma += d;
                doble = !doble;
            }
            return suma % 10 == 0;
        }

        // Formato MM/AA y que el mes no este vencido.
        public static bool VencimientoValido(string mmAa)
        {
            if (string.IsNullOrEmpty(mmAa) || !Regex.IsMatch(mmAa, @"^(0[1-9]|1[0-2])/\d{2}$")) return false;
            int mes = int.Parse(mmAa.Substring(0, 2));
            int anio = 2000 + int.Parse(mmAa.Substring(3, 2));
            var fin = new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes));
            return fin >= DateTime.Today;
        }

        public static bool NombreValido(string s)
        {
            return !string.IsNullOrWhiteSpace(s) && Regex.IsMatch(s.Trim(), @"^[\p{L} ]{2,100}$");
        }

        public static bool DocumentoValido(string tipo, string numero)
        {
            if (string.IsNullOrEmpty(numero)) return false;
            if (tipo == "CEDULA") return Regex.IsMatch(numero, @"^\d{10}$");
            if (tipo == "PASAPORTE") return Regex.IsMatch(numero, @"^[A-Za-z0-9]{5,20}$");
            return false;
        }

        public static bool CorreoValido(string s)
        {
            return s.Length <= 256 && Regex.IsMatch(s, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        public static bool TelefonoValido(string s)
        {
            return Regex.IsMatch(s, @"^[0-9+ ]{1,20}$");
        }
    }
}
