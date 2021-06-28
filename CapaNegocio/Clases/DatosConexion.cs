using System;

namespace CapaNegocio
{
    /// <summary>
    /// Clase que contiene las propiedades de conexión hacia la plataforma
    /// </summary>
    public class DatosConexion
    {
        /// <summary>
        /// IP del switch en la plataforma
        /// </summary>
        public String ip { get; set; }
        /// <summary>
        /// Puerto por el que se enviará la trama
        /// </summary>
        public int puerto { get; set; }
        /// <summary>
        /// Tiempo de espera de la DLL para conexión y recepción
        /// </summary>
        public int timeOut { get; set; }
        /// <summary>
        /// Siglas asociadas a grupo
        /// </summary>
        public string siglas { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DatosConexion()
        {
            ip = "";
            puerto = 0;
            timeOut = 30;
            siglas = "";
        }
    }
}
