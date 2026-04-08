using System.Net;

namespace TransactionalServerCore
{
    /// <summary>
    /// Clase que contiene las propiedades y métodos para poder agregar a la lista de clientes bloqueados
    /// </summary>
    public class ClientBlocker
    {
        /// <summary>
        /// IP del cliente a bloquear
        /// </summary>
        public IPAddress IpBlocked { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ClientBlocker()
        {
            this.IpBlocked = null;
            this.reasonBlock = "";
            this.secondsBlocking = 0;
            this.DateTimeBlocking = DateTime.MinValue;
            this.ActivateBlocking = true;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ip">Ip del cliente a bloquear</param>
        /// <param name="reasonBlock">La razón del bloqueo</param>
        /// <param name="secondsBlocking">tiempo en segundos que durará el bloqueo</param>
        /// <param name="activateBlocking">Variable para indicar que se activa o desactiva el bloqueo para la IP</param>
        public ClientBlocker(IPAddress ip, string reasonBlock, double secondsBlocking, bool activateBlocking)
        {
            this.IpBlocked = ip;
            this.reasonBlock = reasonBlock;
            this.secondsBlocking = secondsBlocking;
            this.DateTimeBlocking = DateTime.Now;
            this.ActivateBlocking = activateBlocking;
        }

        /// <summary>
        /// Motivo por el que se bloquea la IP
        /// </summary>
        public string reasonBlock { get; set; }

        /// <summary>
        /// Tiempo en segundos que se va a bloquear la IP: 0 => bloqueo permanente
        /// </summary>
        public double secondsBlocking { get; set; }

        /// <summary>
        /// Fecha y hora en que fue bloqueado
        /// </summary>
        public DateTime DateTimeBlocking { get; set; }

        /// <summary>
        /// Get or set if ban is active
        /// </summary>
        public bool ActivateBlocking { get; set; }
    }
}
