namespace UServerCore
{
    internal static class Configuracion
    {
        /// <summary>
        /// Limite para el marcador de bytes procesados en el servidor
        /// </summary>
        internal const int LIMITE_BYTES_CONTADOR = 2147480000;

        /// <summary>
        /// Variable que indicará si el server entra en modo test
        /// </summary>
        internal static bool modoTest = false;

        /// <summary>
        /// Activación para que el servidor pueda enviar mensajes a otro proveedor
        /// </summary>
        internal static bool modoRouter = false;

        internal static int timeOutCliente = 50;
    }
}
