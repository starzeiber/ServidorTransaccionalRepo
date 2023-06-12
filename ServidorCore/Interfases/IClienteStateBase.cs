using System;
using System.Net.Sockets;

namespace ServerCore
{
    /// <summary>
    /// Clase contiene toda la información relevante de un cliente así como un socket
    /// que será el de trabajo para el envío y recepción de mensajes
    /// </summary>
    public interface IClienteStateBase
    {
        /// <summary>
        /// Identificador único para un cliente
        /// </summary>
        Guid UniqueId { get; set; }
        /// <summary>
        /// Ip del cliente de donde se recibe el mensaje
        /// </summary>
        string ClientIp { get; set; }
        /// <summary>
        /// Puerto del cliente por donde se recibe el mensaje
        /// </summary>
        int ClientPort { get; set; }
        /// <summary>
        /// Socket asignado de trabajo sobre la conexión del cliente
        /// </summary>
        Socket SocketToWork { get; set; }
        /// <summary>
        /// Tiempo de espera general del lado del cliente
        /// </summary>
        int TimeOut { get; set; }

        /// <summary>
        /// Función que podrá guardar un registro en base de datos 
        /// o en cuyo caso, si está en modo router el servidor, actualizar un registro previamente guardado
        /// </summary>
        void SaveTransaction();
        /// <summary>
        /// Función para obtener la trama de respuesta al cliente dependiendo de su mensajería entrante
        /// </summary>
        void GetResponseMessage();
        /// <summary>
        /// Función virtual para poder sobre escribirla, en esta se controla
        /// toda la operación sobre el mensaje del cliente así como su mensaje de respuesta
        /// </summary>
        /// <param name="mensajeCliente">Mensaje que se recibe de un cliente</param>
        void ProcessMessage(string mensajeCliente);
    }
}