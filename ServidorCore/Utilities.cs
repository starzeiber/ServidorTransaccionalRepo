using System.Reflection;

namespace ServerCore
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    internal static class Utilities
    {
        /// <summary>
        /// Represents the response codes for various transaction outcomes and error states.
        /// </summary>
        /// <remarks>This enumeration defines a set of standardized response codes that indicate the
        /// result of a transaction or the occurrence of specific errors. These codes can be used to interpret the
        /// outcome of operations and handle errors appropriately in the application.</remarks>
        internal enum CodigosRespuesta
        {
            /// <summary>
            /// 
            /// </summary>
            TransaccionExitosa = 0,
            /// <summary>
            /// 
            /// </summary>
            TerminalInvalida = 2,
            /// <summary>
            /// 
            /// </summary>
            Denegada = 4,
            /// <summary>
            /// 
            /// </summary>
            ErrorEnRed = 5,
            /// <summary>
            /// 
            /// </summary>
            TimeOutInterno = 6,
            /// <summary>
            /// 
            /// </summary>
            ErrorGuardandoDB = 7,
            /// <summary>
            /// 
            /// </summary>
            NoExisteOriginal = 9,
            /// <summary>
            /// 
            /// </summary>
            ErrorTELCELTablaLLena = 15,
            /// <summary>
            /// 
            /// </summary>
            ErrorAccesoDB = 16,
            /// <summary>
            /// 
            /// </summary>
            ErrorFormato = 30,
            /// <summary>
            /// 
            /// </summary>
            NumeroTelefono = 35,
            /// <summary>
            /// 
            /// </summary>
            ErrorProceso = 50,
            /// <summary>
            /// 
            /// </summary>
            ErrorProcesoSockets = 51,
            /// <summary>
            /// 
            /// </summary>
            ClienteBloqueado = 65,
            /// <summary>
            /// 
            /// </summary>
            SinCreditoDisponible = 66,
            /// <summary>
            /// 
            /// </summary>
            ErrorObteniendoCredito = 67,
            /// <summary>
            /// 
            /// </summary>
            ErrorConexionServer = 70,
            /// <summary>
            /// 
            /// </summary>
            SinRespuestaCarrier = 71,
            /// <summary>
            /// 
            /// </summary>
            CarrierAbajo = 73,
            /// <summary>
            /// Indica un tiempo de espera excedido en el proceso
            /// </summary>
            ErrorEnElProceso = 74,
            /// <summary>
            /// 
            /// </summary>
            MontoInvalido = 88
        }

        /// <summary>
        /// Tipo de log a escribir
        /// </summary>
        internal enum LogType
        {
            Info = 0,
            Warning = 1,
            Error = 2
        }

        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        [Obfuscation(Exclude = true)]

        internal static void Log(string message, LogType logType)
        {            
            switch (logType)
            {
                case LogType.Info:
                    logger.Info(message);
                    break;
                case LogType.Warning:
                    logger.Warn(message);
                    break;
                case LogType.Error:
                    logger.Error(message);
                    break;
                default:
                    logger.Debug(message);
                    break;
            }
        }

        internal const int milisecondsTimeOutLock = 500;
    }
}
