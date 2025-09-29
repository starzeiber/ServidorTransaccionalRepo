using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using static ServerCore.Utilities;

namespace ServerCore
{
    /// <summary>
    /// Provides functionality for retrieving and managing system information required for the server's operation.
    /// </summary>
    /// <remarks>The <see cref="Security"/> class is responsible for gathering hardware and system details,
    /// such as processor ID, product information, and manufacturer details, which are necessary for the server to
    /// function correctly. This class includes methods for querying system information and handling license-related
    /// data.</remarks>
    internal class Security
    {
        /// <summary>
        /// Información de la licencia
        /// </summary>
        internal enum Licence
        {
            Program = 0,
            Validity = 2,
            ProcessorId = 4,
            Product = 6,
            Manufacturer = 8
        }

        /// <summary>
        /// Nombre del programa
        /// </summary>
        internal const string PROGRAM = "UServer";

        /// <summary>
        /// Id del procesador del equipo
        /// </summary>
        internal string processorId { get; set; }

        /// <summary>
        /// Producto que se ejecuta
        /// </summary>
        internal string product { get; set; }

        /// <summary>
        /// información del fabricante
        /// </summary>
        internal string manufacturer { get; set; }

        /// <summary>
        /// Toda la licencia
        /// </summary>
        internal string licence { get; set; }

        /// <summary>
        /// Obtiene la información de la PC que se requiere para el funcionamiento del server
        /// </summary>
        /// <returns></returns>
        internal bool GetInfoPc()
        {
            try
            {
                processorId = RunQuery("Processor", "ProcessorId").ToUpper();

                product = RunQuery("BaseBoard", "Product").ToUpper();

                manufacturer = RunQuery("BaseBoard", "Manufacturer").ToUpper();

                return true;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("No se pudo obtener la información de la PC, ");
                sb.Append(ex.Message);
                EscribirLog(sb.ToString(), tipoLog.ERROR);
                return false;
            }
        }

        /// <summary>
        /// Ejecuta una consulta al sistema
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="MethodName"></param>
        /// <returns></returns>
        internal string RunQuery(string TableName, string MethodName)
        {
            ManagementObjectSearcher MOS =
              new ManagementObjectSearcher("Select * from Win32_" + TableName);
            foreach (ManagementObject MO in MOS.Get())
            {
                try
                {
                    return MO[MethodName].ToString();
                }
                catch (Exception)
                {
                    return "";
                }
            }
            return "";
        }
    }
}
