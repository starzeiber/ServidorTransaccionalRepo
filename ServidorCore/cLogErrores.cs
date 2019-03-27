using System;
using System.Diagnostics;
using System.Threading;

namespace ServidorCore
{
    /// <summary>
    /// Clase que crea, lee y escribe en un log de event viewer
    /// </summary>
    public static class cLogErrores
    {
        static public  String sNombreOrigen;
        static public String sNombreLog;

        static private EventLog oMylog;

        //public cLogErrores()
        //{
        //    Crear_Log();
        //}
        /// <summary>
        /// Función que crea el log de errores en el event viewer
        /// </summary>
        public static void Crear_Log()
        {
            try
            {                
                // compruebo que no exista la instancia a un nueva fuente y si no existe entonces lo creo
                if (!EventLog.SourceExists(sNombreOrigen))
                {
                    EventLog.CreateEventSource(sNombreOrigen, sNombreLog);
                }
                oMylog = new EventLog();
                oMylog.Source = sNombreOrigen;
            }
            catch (Exception)
            {
                //MessageBox.Show(" error al crear el log: " + ex.Message);
            }   
        }

        /// <summary>
        /// Función que escribe en un log con categoria de información
        /// </summary>
        /// <param name="sNombreLog"></param>
        /// <param name="sEvento"></param>
        public static void Escribir_Log_Evento(string sEvento)
        {
            //EventLog oMylog = new EventLog();
            //oMylog.Source = sNombreOrigen;
            bool noBloqueo = Monitor.TryEnter(oMylog, 5000);
            if (noBloqueo)
            {
                oMylog.WriteEntry(sEvento, EventLogEntryType.Information);
                Monitor.Exit(oMylog);
            }            
        }

        /// <summary>
        /// Función que escribe en un log con categoria de Error 
        /// </summary>
        /// <param name="sNombreLog"></param>
        /// <param name="sEvento"></param>
        public static void Escribir_Log_Error(string sEvento)
        {
            //EventLog oMylog = new EventLog();
            //oMylog.Source = sNombreOrigen;
            bool noBloqueo = Monitor.TryEnter(oMylog, 5000);
            if (noBloqueo)
            {
                oMylog.WriteEntry(sEvento, EventLogEntryType.Error);
                Monitor.Exit(oMylog);
            }            
        }

        public static void Escribir_Log_Advertencia(string sEvento)
        {
            //EventLog oMylog = new EventLog();
            //oMylog.Source = sNombreOrigen;
            bool noBloqueo = Monitor.TryEnter(oMylog, 5000);
            if (noBloqueo)
            {
                oMylog.WriteEntry(sEvento, EventLogEntryType.Warning);
                Monitor.Exit(oMylog);
            }
            
        }
    }
}
