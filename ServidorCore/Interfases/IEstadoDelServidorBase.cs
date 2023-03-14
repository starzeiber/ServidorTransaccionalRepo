namespace ServerCore
{
    /// <summary>
    /// Clase que contiene las funciones que se utilizan para indicar el flujo de una operación con el cliente en el servidor
    /// </summary>
    public interface IEstadoDelServidorBase
    {

        /// <summary>
        /// Función virtual para sobre escribirla que se utiliza cuando se requiera un mensaje de
        /// bienvenida a una conexión de un cliente
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        string MensajeBienvenida(object args);
        /// <summary>
        /// función virtual para sobre escribirla que se utiliza indicar en el flujo que se acepta una solicitud de mensaje
        /// </summary>
        /// <param name="args"></param>
        void OnAceptacion(object args);
        /// <summary>
        /// función virtual para sobre escribirla que se utiliza para indicar en el flujo que un cliente se cierra
        /// </summary>
        /// <param name="args"></param>
        void OnClienteCerrado(object args);
        /// <summary>
        /// función virtual para sobre escribirla que se utiliza para indicar en el flujo que hay una conexión
        /// </summary>
        void OnConexion();
        /// <summary>
        /// función virtual para sobre escribirla que se utiliza indicar en el flujo que se ha enviado un mensaje
        /// </summary>
        void OnEnviado();
        /// <summary>
        /// función virtual para sobre escribirla que se utiliza para indicar el principio del flujo
        /// </summary>
        void OnInicio();
        /// <summary>
        /// función virtual para sobre escribirla que se utiliza indicar en el flujo que se ha recibido un mensaje
        /// </summary>
        void OnRecibido();
    }
}