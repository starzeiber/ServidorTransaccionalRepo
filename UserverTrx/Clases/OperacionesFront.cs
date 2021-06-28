using CapaNegocio;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CapaPresentacion.Clases
{
    public static class OperacionesFront
    {

        /// <summary>
        /// Carga la configuración iniciar de bases de datos y otros parámetros importantes
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> CargarConfiguracion()
        {
            return await PrepararLog() != false && await ObtenerCadenasConexion() != false && await CargarPerfomanceCounter() != false;
        }

        /// <summary>
        /// Función que prepara el log del sistema
        /// </summary>
        /// <returns></returns>
        private static Task<bool> PrepararLog()
        {
            try
            {
                UtileriaVariablesGlobales.nombreLog = ConfigurationManager.AppSettings["Log"].ToString();
                UtileriaVariablesGlobales.instanciarLog();
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry("Error al inicializar el log: " + ex.Message, EventLogEntryType.Error);
                }
                return Task.FromResult(false);
            }            
        }

        /// <summary>
        /// Obtiene las cadenas de conexión del .config
        /// </summary>
        /// <returns></returns>
        private static Task<bool> ObtenerCadenasConexion()
        {
            try
            {
                UtileriaVariablesGlobales.cadenaConexionBO = ConfigurationManager.ConnectionStrings["cadenaConexionBO"].ToString();
                UtileriaVariablesGlobales.cadenaConexionTrx = ConfigurationManager.ConnectionStrings["cadenaConexionTransaccional"].ToString();
                
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Task.Run(() => Operaciones.EscribirLogInterno("Error al cargar las cadenas de conexión: " + ex.Message, Operaciones.TiposLog.error));
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Inicializa el performance counter del sistema
        /// </summary>
        /// <returns></returns>
        private static Task<bool> CargarPerfomanceCounter()
        {
            try
            {
                if (PerformanceCounterCategory.Exists("TN") == false)
                {
                    return Task.FromResult(false);
                }
                else
                {
                    UtileriaVariablesGlobales.peformancePeticionesEntrantes = new PerformanceCounter("TN", "PeticionesEntrantesUserver", false);
                    UtileriaVariablesGlobales.peformancePeticionesSalientes = new PerformanceCounter("TN", "wsPeticionesRespondidasUserver", false);
                    return Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                Task.Run(() => Operaciones.EscribirLogInterno("Error creando performance counter: " + ex.Message, Operaciones.TiposLog.error));                
                return Task.FromResult(false);
            }
        }
    }
}
