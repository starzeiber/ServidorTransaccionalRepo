using System;
using System.Net.Sockets;

namespace ServerCore
{
    public interface IEstadoDelClienteBase
    {
        /// <summary>
        /// Identificador único para un cliente
        /// </summary>
        Guid IdUnicoCliente { get; set; }
        /// <summary>
        /// Ip del cliente de donde se recibe el mensaje
        /// </summary>
        string IpCliente { get; set; }
        /// <summary>
        /// Puerto del cliente por donde se recibe el mensaje
        /// </summary>
        int PuertoCliente { get; set; }
        /// <summary>
        /// Socket asignado de trabajo sobre la conexión del cliente
        /// </summary>
        Socket SocketDeTrabajo { get; set; }
        /// <summary>
        /// Tiempo de espera general del lado del cliente
        /// </summary>
        int TimeOut { get; set; }

        /// <summary>
        /// Función que podrá actualizar un registro guardado previamente en base de datos
        /// </summary>
        void ActualizarTransaccion();
        /// <summary>
        /// Función virtual para poder sobre escribirla, sirve para limpiar e inicializar 
        /// todas las variables del info y socket de trabajo
        /// </summary>
        void InicializarEstadoDelClienteBase();
        /// <summary>
        /// Función para obtener la trama de respuesta al cliente dependiendo de su mensajería entrante
        /// </summary>
        void ObtenerTramaRespuesta();
        /// <summary>
        /// Función virtual para poder sobre escribirla, en esta se controla
        /// toda la operación sobre el mensaje del cliente así como su mensaje de respuesta
        /// </summary>
        /// <param name="mensajeCliente">Mensaje que se recibe de un cliente</param>
        void ProcesarTrama(string mensajeCliente);
        /// <summary>
        /// Función que indica que hay una respuesta en proceso de envío al cliente
        /// </summary>
        void SeEstaProcesandoRespuesta();
        /// <summary>
        /// indica que no hay un proceso activo de envío de respuesta al cliente
        /// </summary>
        void SeFinalizaProcesoRespuesta();
    }
}