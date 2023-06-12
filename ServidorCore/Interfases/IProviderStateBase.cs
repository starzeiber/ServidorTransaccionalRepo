namespace ServerCore
{
    /// <summary>
    /// Clase que contiene las propiedades de un proveedor en el flujo del servidor
    /// </summary>
    public interface IProviderStateBase
    {
        /// <summary>
        /// Función que guardará la operación con el proveedor
        /// </summary>
        void SaveTransaction();

        /// <summary>
        /// Ingresa de forma segura el valor de la instancia de socket principal para un retorno de flujo
        /// </summary>
        /// <param name="obj"></param>
        void SetObjectRequestClient(object obj);

        /// <summary>
        /// Función que obtiene la trama de petición al proveedor
        /// </summary>
        void GetRequestMessage();
        /// <summary>
        /// Función que obtiene la trama de respuesta de una proveedor
        /// </summary>
        void GetResponseMessage();
        /// <summary>
        /// Función virtual para poder sobre escribirla, en esta se controla
        /// toda la operación sobre el mensaje del cliente así como su mensaje de respuesta
        /// </summary>
        void ProcessProviderMessage(string trama);

    }
}