using System;
using System.Net;

namespace ServerCore
{
    /// <summary>
    /// Clase que contiene las propiedades y métodos para poder agregar a la lista de clientes bloqueados
    /// </summary>
    public class ClientBlocked
    {
        //TODO falta la implementación de esta clase en el servidor

        /// <summary>
        /// IP del cliente a bloquear
        /// </summary>
        public IPAddress BlockedIp { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ClientBlocked()
        {
            BlockedIp = null;
            BlocketJustification = "";
            LockUpTime = 0;
            DateTimeBlocket = DateTime.MinValue;
            IsBlocked = true;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ip">Ip del cliente a bloquear</param>
        /// <param name="razonBloqueo">La razón del bloqueo</param>
        /// <param name="segundosDeBloqueo">tiempo en segundos que durará el bloqueo</param>
        /// <param name="estaActivoBloqueo">Variable para indicar que se activa o desactiva el bloqueo para la IP</param>
        public ClientBlocked(IPAddress ip, string razonBloqueo, double segundosDeBloqueo, bool estaActivoBloqueo)
        {
            BlockedIp = ip;
            BlocketJustification = razonBloqueo;
            LockUpTime = segundosDeBloqueo;
            DateTimeBlocket = DateTime.Now;
            this.IsBlocked = estaActivoBloqueo;
        }

        /// <summary>
        /// Motivo por el que se bloquea la IP
        /// </summary>
        public string BlocketJustification { get; set; }

        /// <summary>
        /// Tiempo en segundos que se va a bloquear la IP: 0 => bloqueo permanente
        /// </summary>
        public double LockUpTime { get; set; }

        /// <summary>
        /// Fecha y hora en que fue bloqueado
        /// </summary>
        public DateTime DateTimeBlocket { get; set; }

        /// <summary>
        /// Get or set if ban is active
        /// </summary>
        public bool IsBlocked { get; set; }
    }
}
