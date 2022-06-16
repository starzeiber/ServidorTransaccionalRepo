using System;
using System.Diagnostics;

namespace ServerCore
{
    /// <summary>
    /// Clase que contiene las funciones que se utilizan para indicar el flujo de una operación con el cliente en el servidor
    /// </summary>
    public class EstadoDelServidorBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public EstadoDelServidorBase()
        {

        }

        /// <summary>
        /// Referencia al proceso principal donde se encuentra el socket principal que disparó el flujo
        /// </summary>
        public object procesoPrincipal { get; set; }

        /// <summary>
        /// Función virtual para sobre escribirla que se utiliza cuando se requiera un mensaje de
        /// bienvenida a una conexión de un cliente
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual string mensajeBienvenida(object args)
        {
            return "";
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza para indicar el principio del flujo
        /// </summary>
        public virtual void OnInicio()
        {
            Trace.TraceInformation(DateTime.Now.ToString() + ". Se ha iniciado el servidor");
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza para indicar en el flujo que un cliente se cierra
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnClienteCerrado(object args)
        {
            Trace.TraceInformation(DateTime.Now.ToString() + ". Se ha desconectado el cliente: " + (args as EstadoDelClienteBase).IdUnicoCliente.ToString() +
                ", desde la IP:" + (args as EstadoDelClienteBase).IpCliente);
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza para indicar en el flujo que hay una conexión
        /// </summary>
        public virtual void OnConexion()
        {
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza indicar en el flujo que se acepta una solicitud de mensaje
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnAceptacion(object args)
        {
            Trace.TraceInformation(DateTime.Now.ToString() + ". Se ha conectado el cliente: " + (args as EstadoDelClienteBase).IdUnicoCliente.ToString() +
                ", desde la IP:" + (args as EstadoDelClienteBase).IpCliente);
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza indicar en el flujo que se ha recibido un mensaje
        /// </summary>
        public virtual void OnRecibido()
        {
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza indicar en el flujo que se ha enviado un mensaje
        /// </summary>
        public virtual void OnEnviado()
        {
        }
    }
}
