using System;
using System.IO;
using System.Linq;
using System.Management;
using static ServerCore.Utileria;

namespace ServerCore.Clases
{
    internal class Seguridad
    {
        /// <summary>
        /// Nombre del programa
        /// </summary>
        private const string PROGRAM = "UServer";

        /// <summary>
        /// Id del procesador del equipo
        /// </summary>
        private string _processorId = "";

        /// <summary>
        /// Producto que se ejecuta
        /// </summary>
        private string _product = "";

        /// <summary>
        /// información del fabricante
        /// </summary>
        private string _manufacturer = "";

        /// <summary>
        /// Toda la licencia
        /// </summary>
        private string _licence = "";

        public ILogTrace LogTrace { get; }

        public Seguridad(ILogTrace logTrace)
        {
            LogTrace = logTrace;
        }

        /// <summary>
        /// Información de la licencia
        /// </summary>
        private enum Licence
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
        public bool ValidacionPermisos()
        {
            try
            {
                if (!ObtenerConfiguracionPermisos())
                    return false;

                if (!DesencriptarParametrosConfiguracion(out string programa, out string procesador, out string producto, out string manufactura))
                    return false;

                if (!ObtenerInformacionDelEquipo())
                    return false;

                return string.Compare(PROGRAM, programa) == 0
                        //&& DateTime.Compare(localValidity, DateTime.Parse(encrypter.DesEncrypterText(licence.Split('|')[(int)Licence.Validity]))) <= 0
                        && (string.Compare(_processorId, procesador) == 0)
                        && (string.Compare(_product, producto) == 0)
                        && (string.Compare(_manufacturer, manufactura) == 0);
            }
            catch (Exception ex)
            {
                LogTrace.EscribirLog(ex.Message + ",ValidacionPermisos", tipoLog.ERROR);
                return false;
            }
        }

        /// <summary>
        /// Desencripta los valores obtenidos del archivo de licencia
        /// </summary>
        /// <param name="programa">nombre del programa a validar</param>
        /// <param name="procesador">id del procesador del equipo</param>
        /// <param name="producto">numero de serie o producto del equipo</param>
        /// <param name="manufactura">nombre de manufactura</param>
        /// <returns></returns>
        private bool DesencriptarParametrosConfiguracion(out string programa, out string procesador, out string producto, out string manufactura)
        {
            try
            {
                Encrypter.Encrypter encrypter = new Encrypter.Encrypter("AdmindeServicios");
                programa = encrypter.DesEncrypterText(_licence.Split('|')[(int)Licence.Program]);
                procesador = encrypter.DesEncrypterText(_licence.Split('|')[(int)Licence.ProcessorId]);
                producto = encrypter.DesEncrypterText(_licence.Split('|')[(int)Licence.Product]);
                manufactura = encrypter.DesEncrypterText(_licence.Split('|')[(int)Licence.Manufacturer]);
                return true;
            }
            catch (Exception ex)
            {
                LogTrace.EscribirLog(ex.Message + ". " + ex.StackTrace + ", DescriptarParametrosConfiguracion", tipoLog.ERROR);
                programa = "invalido";
                procesador = "invalido";
                producto = "invalido";
                manufactura = "invalido";
                return false;
            }
        }

        /// <summary>
        /// Obtiene el archivo de licencia de la ubicación de la aplicación
        /// </summary>
        /// <returns></returns>
        private bool ObtenerConfiguracionPermisos()
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
                            _licence = streamReader.ReadLine();
                        }
                    }
                }
                return _licence.Length > 0;
            }
            catch (Exception ex)
            {
                LogTrace.EscribirLog(ex.Message + ", ObtenerConfiguracionPermisos", tipoLog.ERROR);
                return false;
            }
        }

        /// <summary>
        /// Obtiene la información de la PC que se requiere para el funcionamiento del server
        /// </summary>
        /// <returns></returns>
        private bool ObtenerInformacionDelEquipo()
        {
            try
            {
                _processorId = RunQuery("Processor", "ProcessorId").ToUpper();

                _product = RunQuery("BaseBoard", "Product").ToUpper();

                _manufacturer = RunQuery("BaseBoard", "Manufacturer").ToUpper();

                return true;
            }
            catch (Exception ex)
            {
                LogTrace.EscribirLog(ex.Message + ", ObtenerInformacionDelEquipo", tipoLog.ERROR);
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
