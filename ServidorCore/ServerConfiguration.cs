namespace ServerCore
{
    /// <summary>
    /// Provides configuration settings for the server, including operational modes and timeout values.
    /// </summary>
    /// <remarks>This class contains constants and static fields that define various server configuration
    /// options. It is intended for internal use and is not exposed to external consumers.</remarks>
    internal static class ServerConfiguration
    {
        /// <summary>
        /// Limite para el marcador de bytes procesados en el servidor
        /// </summary>
        internal const int LIMITE_BYTES_CONTADOR = 2147480000;

        /// <summary>
        /// Variable que indicará si el server entra en modo test
        /// </summary>
        internal static bool testMode = false;

        /// <summary>
        /// Activación para que el servidor pueda enviar mensajes a otro proveedor
        /// </summary>
        internal static bool routerMode = false;

        /// <summary>
        /// Represents the default client timeout value, in seconds, for internal operations.
        /// </summary>
        /// <remarks>This value is used as the default timeout for client-related operations within the
        /// system. It is not exposed for external configuration and is intended for internal use only.</remarks>
        internal static int clientTimeOut = 40;

        /// <summary>
        /// Represents the default timeout value, in seconds, for the provider.
        /// </summary>
        /// <remarks>This value is used internally to configure the maximum time a provider operation can
        /// take before timing out.</remarks>
        internal static int providerTimeout = 25;
    }
}
