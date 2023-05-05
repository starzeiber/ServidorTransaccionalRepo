namespace ServerCore
{
    /// <summary>
    /// Compendio de enumerados y funciones de ayuda al Core
    /// </summary>
    public static class Utileria
    {
        /// <summary>
        /// Enumerado de códigos de respuesta exclusivos del Core
        /// </summary>
        public enum CodigosRespuesta
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
            /// 
            /// </summary>
            MontoInvalido = 88
        }

        /// <summary>
        /// Tipo de log a escribir
        /// </summary>
        internal enum tipoLog
        {
            INFORMACION = 0,
            ALERTA = 1,
            ERROR = 2
        }
    }
}
