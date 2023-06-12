namespace ServerCore.Constants
{
    /// <summary>
    /// Compendio de enumerados y funciones de ayuda al Core
    /// </summary>
    public static class ServerCoreConstants
    {
        /// <summary>
        /// Enumerado de códigos de respuesta exclusivos del Core
        /// </summary>
        public enum ProcessResponseCodes
        {
            /// <summary>
            /// 
            /// </summary>
            ProcessSuccess = 0,
            /// <summary>
            /// 
            /// </summary>
            TransactionDenied = 4,
            /// <summary>
            /// 
            /// </summary>
            NetworkError = 5,
            /// <summary>
            /// 
            /// </summary>
            InternalTimeOut = 6,
            /// <summary>
            /// 
            /// </summary>
            FormatError = 30,            
            /// <summary>
            /// 
            /// </summary>
            ProcessError = 50,
            /// <summary>
            /// 
            /// </summary>
            SocketCriticalError = 51,
            /// <summary>
            /// 
            /// </summary>
            ClientBlocked = 65,
            /// <summary>
            /// 
            /// </summary>
            ConnectErrorProvider = 70,
            /// <summary>
            /// 
            /// </summary>
            NoResponseProvider = 71,
            /// <summary>
            /// 
            /// </summary>
            ProviderDown = 73
        }

        /// <summary>
        /// Tipo de log a escribir
        /// </summary>
        internal enum LogType
        {
            Information = 0,
            warnning = 1,
            Error = 2
        }
    }
}
