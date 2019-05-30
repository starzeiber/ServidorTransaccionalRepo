using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServidorCore
{
    /// <summary>
    /// Clase que contiene las propiedades y métodos para poder agregar a la lista de clientes bloqueados
    /// </summary>
    public class ClienteBloqueado
    {
        /// <summary>
        /// IP del cliente a bloquear
        /// </summary>
        public IPAddress ipBloqueado { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ClienteBloqueado()
        {
            this.ipBloqueado = null;
            this.razonDelBloqueo = "";
            this.segundosBloqueo = 0;
            this.fechaHoraBloqueo = DateTime.MinValue;
            this.estaActivoBloqueo = true;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ip">Ip del cliente a bloquear</param>
        /// <param name="reason">La razón del bloqueo</param>
        /// <param name="seconds">tiempo en segundos que durará el bloqueo</param>
        /// <param name="active">Variable para indicar que se activa o desactiva el bloqueo</param>
        public ClienteBloqueado(IPAddress ip, string reason, double seconds, bool active)
        {
            this.ipBloqueado = ip;
            this.razonDelBloqueo = reason;
            this.segundosBloqueo = seconds;
            this.fechaHoraBloqueo = DateTime.Now;
            this.estaActivoBloqueo = true;
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
