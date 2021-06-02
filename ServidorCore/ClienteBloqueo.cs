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
    public class ClienteBloqueo
    {
        /// <summary>
        /// IP del cliente a bloquear
        /// </summary>
        public IPAddress ipBloqueada { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ClienteBloqueo()
        {
            this.ipBloqueada = null;
            this.razonDelBloqueo = "";
            this.segundosBloqueo = 0;
            this.fechaHoraBloqueo = DateTime.MinValue;
            this.estaActivoBloqueo = true;
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
            this.ipBloqueada = ip;
            this.razonDelBloqueo = razonBloqueo;
            this.segundosBloqueo = segundosDeBloqueo;
            this.fechaHoraBloqueo = DateTime.Now;
            // me di cuenta de que siempre lo activo
            // this.estaActivoBloqueo = true;
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
