using NLog.Common;
using System;
using System.Diagnostics;
using System.Text;

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
            var sb =new  StringBuilder();
            sb.Append(DateTime.Now);
            sb.Append(" - Iniciando servidor");
            Utileria.EscribirLog(sb.ToString(), Utileria.tipoLog.INFORMACION);
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza para indicar en el flujo que un cliente se cierra
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnClienteCerrado(object args)
        {
            var sb = new StringBuilder();
            sb.Append(DateTime.Now);
            sb.Append(" - Cerrando cliente");
            sb.Append(" - Se ha desconectado el cliente: ");
            sb.Append((args as EstadoDelClienteBase).IdUnicoCliente.ToString());
            sb.Append(", desde la IP: ");
            sb.Append((args as EstadoDelClienteBase).IpCliente);
            Utileria.EscribirLog(sb.ToString(),Utileria.tipoLog.INFORMACION);
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
            var sb = new StringBuilder();
            sb.Append(DateTime.Now);
            sb.Append(" - Aceptando conexión");
            sb.Append(" - Se ha conectado el cliente: ");
            sb.Append((args as EstadoDelClienteBase).IdUnicoCliente.ToString());
            sb.Append(", desde la IP: ");
            sb.Append((args as EstadoDelClienteBase).IpCliente);
            Utileria.EscribirLog(sb.ToString(), Utileria.tipoLog.INFORMACION);
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
