using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static ServerCore.Constants.ServerCoreConstants;

namespace ServerCore.Classes
{
    internal class LogTrace : ILogTrace
    {
        public string NombreLog { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nombreLog">Nombre del event log en el sistema. Nota: se debe ejecutar con privilegios de administrador la aplicación</param>
        public LogTrace(string nombreLog)
        {
            NombreLog = nombreLog;
        }

        public LogTrace()
        {
        }

        /// <summary>
        /// Crea el log por defecto del sistema
        /// </summary>
        /// <returns></returns>
        internal bool CrearLog()
        {
            string origenLog = NombreLog;
            // Create an EventLog instance and assign its source.
            EventLog myLog = new EventLog();
            try
            {
                // Create the source, if it does not already exist.
                if (!EventLog.SourceExists(NombreLog))
                {
                    // An event log source should not be created and immediately used.
                    // There is a latency time to enable the source, it should be created
                    // prior to executing the application that uses the source.
                    // Execute this sample a second time to use the new source.
                    EventLog.CreateEventSource(origenLog, NombreLog);
                    //Console.WriteLine("CreatingEventSource");
                    //Console.WriteLine("Exiting, execute the application a second time to use the source.");
                    // The source is created.  Exit the application to allow it to be registered.
                    //return true;


                    myLog.Source = origenLog;

                    // Write an informational entry to the event log.
                    myLog.WriteEntry("Se ha creado el log exitosamente");
                }

                myLog.Source = origenLog;

                // Write an informational entry to the event log.
                myLog.WriteEntry("Comprobando escritura de log");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Funcion que el guardado de logs en el event log de windows
        /// </summary>
        /// <param name="mensaje"></param>
        /// <param name="tipoLog"></param>
        public void EscribirLog(string mensaje, LogType tipoLog)
        {
            switch (tipoLog)
            {
                case LogType.Information:
                    Task.Run(() => Trace.TraceInformation(DateTime.Now.ToString() + ". " + mensaje));
                    break;
                case LogType.warnning:
                    Task.Run(() => Trace.TraceWarning(DateTime.Now.ToString() + ". " + mensaje));
                    break;
                case LogType.Error:
                    Task.Run(() => Trace.TraceError(DateTime.Now.ToString() + ". " + mensaje));
                    break;
                default:
                    Task.Run(() => Trace.WriteLine(DateTime.Now.ToString() + ". " + mensaje));
                    break;
            }
        }

        
    }
}
