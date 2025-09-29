using System;
using System.Text;

namespace ServerCore
{
    /// <summary>
    /// Clase que contiene las funciones que se utilizan para indicar el flujo de una operación con el cliente en el servidor
    /// </summary>
    public class ServerStateBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ServerStateBase()
        {

        }

        /// <summary>
        /// Referencia al proceso principal donde se encuentra el socket principal que disparó el flujo
        /// </summary>
        public object MainProcees { get; set; }

        /// <summary>
        /// Función virtual para sobre escribirla que se utiliza cuando se requiera un mensaje de
        /// bienvenida a una conexión de un cliente
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual string WelcomeMessage(object args)
        {
            return "";
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza para indicar el principio del flujo
        /// </summary>
        public virtual void OnStart()
        {
            var sb = new StringBuilder();
            sb.Append(DateTime.Now);
            sb.Append(" - Iniciando servidor");
            Utilities.EscribirLog(sb.ToString(), Utilities.tipoLog.INFORMACION);
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza para indicar en el flujo que un cliente se cierra
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnClientClose(object args)
        {
            var sb = new StringBuilder();
            sb.Append(DateTime.Now);
            sb.Append(" - Cerrando cliente");
            sb.Append(" - Se ha desconectado el cliente: ");
            sb.Append((args as ClientStateBase).UniqueClientId.ToString());
            sb.Append(", desde la IP: ");
            sb.Append((args as ClientStateBase).IpClient);
            Utilities.EscribirLog(sb.ToString(), Utilities.tipoLog.INFORMACION);
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza para indicar en el flujo que hay una conexión
        /// </summary>
        public virtual void OnConnection()
        {
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza indicar en el flujo que se acepta una solicitud de mensaje
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnAccept(object args)
        {
            var sb = new StringBuilder();
            sb.Append(DateTime.Now);
            sb.Append(" - Aceptando conexión");
            sb.Append(" - Se ha conectado el cliente: ");
            sb.Append((args as ClientStateBase).UniqueClientId.ToString());
            sb.Append(", desde la IP: ");
            sb.Append((args as ClientStateBase).IpClient);
            Utilities.EscribirLog(sb.ToString(), Utilities.tipoLog.INFORMACION);
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza indicar en el flujo que se ha recibido un mensaje
        /// </summary>
        public virtual void OnReceive()
        {
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza indicar en el flujo que se ha enviado un mensaje
        /// </summary>
        public virtual void OnSent()
        {
        }
    }
}
