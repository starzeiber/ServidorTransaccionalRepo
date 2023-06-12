using System;
using System.Diagnostics;

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
        internal object MainProcess { get; set; }

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
            Trace.TraceInformation(DateTime.Now.ToString() + ". Se ha iniciado el servidor");
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza para indicar en el flujo que un cliente se cierra
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnClientClosed(object args)
        {
            Trace.TraceInformation(DateTime.Now.ToString() + ". Se ha desconectado el cliente: " + (args as ClientStateBase).UniqueId.ToString() +
                ", desde la IP:" + (args as ClientStateBase).ClientIp);
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
        public virtual void OnAcept(object args)
        {
            Trace.TraceInformation(DateTime.Now.ToString() + ". Se ha conectado el cliente: " + (args as ClientStateBase).UniqueId.ToString() +
                ", desde la IP:" + (args as ClientStateBase).ClientIp);
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
        public virtual void OnSend()
        {
        }
    }
}
