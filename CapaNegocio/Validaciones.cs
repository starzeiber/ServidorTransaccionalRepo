﻿using System;
using System.Text.RegularExpressions;

namespace CapaNegocio
{

    /// <summary>
    /// Enumerado para indicar el tipo de formato a otorgar a una cadena de caracteres
    /// </summary>
    public enum TipoFormato
    {
        N = 0,
        AN,
        ANS
    };

    /// <summary>
    /// Validaciones que contiene globalmente la DLL
    /// </summary>
    public static class Validaciones
    {

        /// <summary>
        /// Función que realiza la adecuación de una cadena de caracteres dependiendo el tipo de formato establecido
        /// </summary>
        /// <param name="cadena">Cadena de caracteres a realizar un formato</param>
        /// <param name="tipoFormato">Tipo de formato a otorgar</param>
        /// <param name="longitud">Longitud final de la cadena</param>
        /// <returns></returns>
        public static Object formatoValor(String cadena, TipoFormato tipoFormato, int longitud)
        {
            try
            {
                switch (tipoFormato)
                {
                    case TipoFormato.N:
                        while (cadena.Length < longitud)
                        {
                            cadena = "0" + cadena;
                        }
                        break;
                    case TipoFormato.AN:
                        while (cadena.Length < longitud)
                        {
                            cadena += " ";
                        }
                        break;
                    default:
                        while (cadena.Length < longitud)
                        {
                            cadena += " ";
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                UtileriaVariablesGlobales.log.EscribirLogError("Error en darle formato a una cadena: " + ex.Message);
                return String.Empty;
            }
            return cadena;
        }

        /// <summary>
        /// función que valida si la entrada tiene carateres especiales
        /// </summary>
        /// <param name="valor"></param>
        /// <returns></returns>
        public static Boolean revisionCaracteresEspeciales(String valor)
        {
            var regex = new Regex(@"[^a-zA-Z0-9:/ ]");
            if (regex.IsMatch(valor))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
