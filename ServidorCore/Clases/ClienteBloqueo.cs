using System;
using System.Net;

namespace ServerCore
{
    /// <summary>
    /// Clase que contiene las propiedades y métodos para poder agregar a la lista de clientes bloqueados
    /// </summary>
    public class ClienteBloqueo
    {
        //TODO falta la implementación de esta clase en el servidor

        /// <summary>
        /// IP del cliente a bloquear
        /// </summary>
        public IPAddress ipBloqueada { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ClienteBloqueo()
        {
            ipBloqueada = null;
            razonDelBloqueo = "";
            segundosBloqueo = 0;
            fechaHoraBloqueo = DateTime.MinValue;
            estaActivoBloqueo = true;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ip">Ip del cliente a bloquear</param>
        /// <param name="razonBloqueo">La razón del bloqueo</param>
        /// <param name="segundosDeBloqueo">tiempo en segundos que durará el bloqueo</param>
        /// <param name="estaActivoBloqueo">Variable para indicar que se activa o desactiva el bloqueo para la IP</param>
        public ClienteBloqueo(IPAddress ip, string razonBloqueo, double segundosDeBloqueo, bool estaActivoBloqueo)
        {
            ipBloqueada = ip;
            razonDelBloqueo = razonBloqueo;
            segundosBloqueo = segundosDeBloqueo;
            fechaHoraBloqueo = DateTime.Now;
            this.estaActivoBloqueo = estaActivoBloqueo;
        }

        /// <summary>
        /// Motivo por el que se bloquea la IP
        /// </summary>
        public string razonDelBloqueo { get; set; }

        /// <summary>
        /// Tiempo en segundos que se va a bloquear la IP: 0 => bloqueo permanente
        /// </summary>
        public double segundosBloqueo { get; set; }

        /// <summary>
        /// Fecha y hora en que fue bloqueado
        /// </summary>
        public DateTime fechaHoraBloqueo { get; set; }

        /// <summary>
        /// Get or set if ban is active
        /// </summary>
        public bool estaActivoBloqueo { get; set; }
    }
}
