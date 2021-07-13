using CapaNegocio;
using System;
using System.Configuration;
using System.Diagnostics;
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
            return await PrepararLog() != false && 
                await ObtenerCadenasConexion() != false && 
                await CargarIpsYPuertos() != false;
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
                UtileriaVariablesGlobales.InstanciarLog();
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
                Task.Run(() => UtileriaVariablesGlobales.Log(UtileriaVariablesGlobales.ObtenerNombreFuncion("Error al cargar las cadenas de conexión: " + ex.Message), UtileriaVariablesGlobales.TiposLog.error));
                return Task.FromResult(false);
            }
        }

        private static Task<bool> CargarIpsYPuertos()
        {
            try
            {
                UtileriaVariablesGlobales.puertoLocal = int.Parse(ConfigurationManager.AppSettings["puertoLocal"].ToString());
                UtileriaVariablesGlobales.ipProveedor = ConfigurationManager.AppSettings["ipProveedor"].ToString();
                UtileriaVariablesGlobales.puertoProveedor = int.Parse(ConfigurationManager.AppSettings["puertoProveedor"].ToString());

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Task.Run(() => UtileriaVariablesGlobales.Log(UtileriaVariablesGlobales.ObtenerNombreFuncion(ex.Message), UtileriaVariablesGlobales.TiposLog.error));
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
                Task.Run(() => UtileriaVariablesGlobales.Log(UtileriaVariablesGlobales.ObtenerNombreFuncion("Error creando performance counter: " + ex.Message), UtileriaVariablesGlobales.TiposLog.error));
                return Task.FromResult(false);
            }
        }
    }
}
