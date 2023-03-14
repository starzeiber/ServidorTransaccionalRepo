namespace ServerCore
{
    internal static class Configuracion
    {
        /// <summary>
        /// Limite para el marcador de bytes procesados en el servidor
        /// </summary>
        internal const int LIMITE_BYTES_CONTADOR = 2147480000;

        /// <summary>
        /// Variable que indicará si el server entra en modo test.
        /// El modo Test, responderá a toda petición bien formada, con una código de autorización 
        /// y respuesta simulado sin enviar la trama a un proveedor externo
        /// </summary>
        internal static bool modoTest = false;

        /// <summary>
        /// Activación para que el servidor pueda enviar mensajes a otro proveedor
        /// </summary>
        internal static bool modoRouter = false;

        /// <summary>
        /// TimeOut en segundos sobre cualquier petición de un cliente
        /// </summary>
        internal static int timeOutCliente = 50;
    }
}
