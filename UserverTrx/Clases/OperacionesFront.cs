using CapaNegocio;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Userver.Clases
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
                Utileria.nombreLog = ConfigurationManager.AppSettings["Log"].ToString();
                Utileria.InstanciarLog();
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
                Utileria.cadenaConexionBO = ConfigurationManager.ConnectionStrings["cadenaConexionBO"].ToString();
                Utileria.cadenaConexionTrx = ConfigurationManager.ConnectionStrings["cadenaConexionTransaccional"].ToString();

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion("Error al cargar las cadenas de conexión: " + ex.Message), Utileria.TiposLog.error));
                return Task.FromResult(false);
            }
        }

        private static Task<bool> CargarIpsYPuertos()
        {
            try
            {
                Utileria.puertoLocal = int.Parse(ConfigurationManager.AppSettings["puertoLocal"].ToString());
                Utileria.ipProveedor = ConfigurationManager.AppSettings["ipProveedor"].ToString();
                Utileria.puertoProveedor = int.Parse(ConfigurationManager.AppSettings["puertoProveedor"].ToString());

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
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
                    Utileria.peformancePeticionesEntrantes = new PerformanceCounter("TN", "PeticionesEntrantesUserver", false);
                    Utileria.peformancePeticionesSalientes = new PerformanceCounter("TN", "wsPeticionesRespondidasUserver", false);
                    return Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion("Error creando performance counter: " + ex.Message), Utileria.TiposLog.error));
                return Task.FromResult(false);
            }
        }
    }
}
