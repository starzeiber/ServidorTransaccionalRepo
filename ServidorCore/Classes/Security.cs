using System;
using System.IO;
using System.Linq;
using System.Management;
using static ServerCore.Constants.ServerCoreConstants;

namespace ServerCore.Classes
{
    internal class Security
    {
        /// <summary>
        /// Nombre del programa
        /// </summary>
        private const string PROGRAM = "UServer";

        /// <summary>
        /// Id del procesador del equipo
        /// </summary>
        private string processorId = "";

        /// <summary>
        /// Producto que se ejecuta
        /// </summary>
        private string product = "";

        /// <summary>
        /// información del fabricante
        /// </summary>
        private string manufacturer = "";

        /// <summary>
        /// Toda la licencia
        /// </summary>
        private string licence = "";

        /// <summary>
        /// Instancia para utilizar el log
        /// </summary>
        public ILogTrace LogTrace { get; }

        /// <summary>
        /// Constructor de la clase
        /// </summary>
        /// <param name="logTrace">Instancia de ServerCore.Classes.LogTrace</param>
        public Security(ILogTrace logTrace)
        {
            LogTrace = logTrace;
        }

        /// <summary>
        /// Información de la licencia
        /// </summary>
        private enum LicencePropiertiesParse
        {
            Program = 0,
            Validity = 2,
            ProcessorId = 4,
            Product = 6,
            Manufacturer = 8
        }



        /// <summary>
        /// Valida que la licencia esté vigente
        /// </summary>
        /// <returns></returns>
        public bool ValidatePermissions()
        {
            try
            {
                if (!GetParametersServerFile() ||
                    !DesencryptConfirgurationParameters(out string program, out string processorId, out string product, out string manufacturer) ||
                    !GetMachineInformation())
                    return false;
                return string.Compare(PROGRAM, program) == 0
                        //&& DateTime.Compare(localValidity, DateTime.Parse(encrypter.DesEncrypterText(licence.Split('|')[(int)Licence.Validity]))) <= 0
                        && (string.Compare(this.processorId, processorId) == 0)
                        && (string.Compare(this.product, product) == 0)
                        && (string.Compare(this.manufacturer, manufacturer) == 0);
            }
            catch (Exception ex)
            {
                LogTrace.EscribirLog(ex.Message + ",ValidatePermissions", LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// Desencripta los valores obtenidos del archivo de licencia
        /// </summary>
        /// <param name="program">nombre del programa a validar</param>
        /// <param name="processorId">id del procesador del equipo</param>
        /// <param name="product">numero de serie o producto del equipo</param>
        /// <param name="manufacturer">nombre de manufactura</param>
        /// <returns></returns>
        private bool DesencryptConfirgurationParameters(out string program, out string processorId, out string product, out string manufacturer)
        {
            try
            {
                Encrypter.Encrypter encrypter = new Encrypter.Encrypter("AdmindeServicios");
                program = encrypter.DesEncrypterText(licence.Split('|')[(int)LicencePropiertiesParse.Program]);
                processorId = encrypter.DesEncrypterText(licence.Split('|')[(int)LicencePropiertiesParse.ProcessorId]);
                product = encrypter.DesEncrypterText(licence.Split('|')[(int)LicencePropiertiesParse.Product]);
                manufacturer = encrypter.DesEncrypterText(licence.Split('|')[(int)LicencePropiertiesParse.Manufacturer]);
                return true;
            }
            catch (Exception ex)
            {
                LogTrace.EscribirLog(ex.Message + ". " + ex.StackTrace + ", DesencryptConfirgurationParameters", LogType.Error);
                program = "invalido";
                processorId = "invalido";
                product = "invalido";
                manufacturer = "invalido";
                return false;
            }
        }

        /// <summary>
        /// Obtiene el archivo de licencia de la ubicación de la aplicación
        /// </summary>
        /// <returns></returns>
        private bool GetParametersServerFile()
        {
            FileStream fileStream;
            try
            {
                using (fileStream = File.OpenRead(Environment.CurrentDirectory + "\\" + PROGRAM + ".txt"))
                {
                    using (StreamReader streamReader = new StreamReader(fileStream))
                    {
                        while (streamReader.EndOfStream == false)
                        {
                            licence = streamReader.ReadLine();
                        }
                    }
                }
                return licence.Length > 0;
            }
            catch (Exception ex)
            {
                LogTrace.EscribirLog(ex.Message + ", GetParametersServerFile", LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// Obtiene la información de la PC que se requiere para el funcionamiento del server
        /// </summary>
        /// <returns></returns>
        private bool GetMachineInformation()
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
                LogTrace.EscribirLog(ex.Message + ", GetMachineInformation", LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// Ejecuta una consulta al sistema
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="MethodName"></param>
        /// <returns></returns>
        private string RunQuery(string TableName, string MethodName)
        {
            ManagementObjectSearcher MOS =
              new ManagementObjectSearcher("Select * from Win32_" + TableName);
            foreach (ManagementObject MO in MOS.Get().Cast<ManagementObject>())
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
