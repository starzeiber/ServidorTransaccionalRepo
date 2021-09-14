﻿using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CapaNegocio
{
    /// <summary>
    /// Clase que contiene varias funciones y variables que se pueden utilizar en todo el proyecto
    /// </summary>
    public static class Utileria
    {
        /// <summary>
        /// Encabezado de un mensaje 13
        /// </summary>
        public static String ENCABEZADO_SOLICITUD_TAE_PX = "13";

        /// <summary>
        /// Encabezado de un mensaje 14
        /// </summary>
        public static String ENCABEZADO_RESPUESTA_TAE_PX = "14";

        /// <summary>
        /// Encabezado de un mensaje 17
        /// </summary>
        public static String ENCABEZADO_CONSULTA_TAE_PX = "17";

        /// <summary>
        /// Encabezado de un mensaje 18
        /// </summary>
        public static String ENCABEZADO_RESPUESTA_CONSULTA_TAE_PX = "18";

        /// <summary>
        /// Encabezado de un mensaje 25
        /// </summary>
        public static String ENCABEZADO_SOLICITUD_DATOS_PX = "25";

        /// <summary>
        /// Encabezado de un mensaje 26
        /// </summary>
        public static String ENCABEZADO_RESPUESTA_DATOS_PX = "26";

        /// <summary>
        /// Encabezado de un mensaje 27
        /// </summary>
        public static String ENCABEZADO_CONSULTA_DATOS_PX = "27";

        /// <summary>
        /// Encabezado de un mensaje 28
        /// </summary>
        public static String ENCABEZADO_RESPUESTA_CONSULTA_DATOS_PX = "28";

        /// <summary>
        /// Valor del timeout sobre una compra
        /// </summary>
        public static int timeOut;

        /// <summary>
        /// Nombre del log en el sistema
        /// </summary>
        public static string nombreLog;

        /// <summary>
        /// cadena de conexión al BO
        /// </summary>
        public static string cadenaConexionBO;

        /// <summary>
        /// Cadena de conexión a la base administrativa del ws
        /// </summary>
        public static string cadenaConexionTrx;

        /// <summary>
        /// Instancias del performance counter de peticiones entrantes
        /// </summary>
        public static PerformanceCounter peformancePeticionesEntrantes;
        /// <summary>
        /// Instancias del performance counter de peticiones entrantes
        /// </summary>
        public static PerformanceCounter peformancePeticionesSalientes;

        /// <summary>
        /// Log del sistema
        /// </summary>
        private static EventLogTraceListener logListener;

        public static string ipLocal;
        /// <summary>
        /// Puerto local asignado al servidor
        /// </summary>
        public static int puertoLocal;

        /// <summary>
        /// ip del switch del proveedor
        /// </summary>
        public static string ipProveedor;
        /// <summary>
        /// puerto del switch del proveedor
        /// </summary>
        public static int puertoProveedor;

        [ThreadStatic]
        static int semillaAleatorio;

        /// <summary>
        /// Número aleatorio para seleccionar un puerto disponible
        /// </summary>
        private static Random random;

        /// <summary>
        /// listado de códigos de respuesta del sistema
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
        /// Enumerado con los tipos de log
        /// </summary>
        public enum TiposLog
        {
            /// <summary>
            /// Informativo
            /// </summary>
            info = 0,
            /// <summary>
            /// Alerta
            /// </summary>
            warnning = 1,
            /// <summary>
            /// Error
            /// </summary>
            error
        }

        /// <summary>
        /// Configura e inicializa el log del sistema
        /// </summary>
        public static void InstanciarLog()
        {
            logListener = new EventLogTraceListener(nombreLog);
            Trace.Listeners.Add(logListener);
        }

        /// <summary>
        /// Función que graba en un log interno desde afuera de la capa de negocio
        /// </summary>
        /// <param name="mensaje"></param>
        /// <param name="tipoLog"></param>
        public static void Log(string mensaje, TiposLog tipoLog)
        {
            switch (tipoLog)
            {
                case TiposLog.info:
                    Trace.TraceInformation(mensaje);
                    break;
                case TiposLog.warnning:
                    Trace.TraceWarning(mensaje);
                    break;
                case TiposLog.error:
                    Trace.TraceError(mensaje);
                    break;
                default:
                    Trace.WriteLine(mensaje);
                    break;
            }
        }

        /// <summary>
        /// Obtiene la descripción sobre un código del sistema
        /// </summary>
        /// <param name="codigo">código del sistema</param>
        /// <returns></returns>
        public static String ObtenerDescripcionCodigoRespuesta(int codigo)
        {
            String descripcion;
            switch (codigo)
            {
                case 0:
                    descripcion = "TransaccionAprobada";
                    break;
                case 2:
                    descripcion = "TerminalInvalida";
                    break;
                case 4:
                    descripcion = "Denegada";
                    break;
                case 5:
                    descripcion = "ErrorEnRed";
                    break;
                case 6:
                    descripcion = "TimeOut";
                    break;
                case 7:
                    descripcion = "ErrorGuardandoDB";
                    break;
                case 9:
                    descripcion = "NoExisteOriginal";
                    break;
                case 15:
                    descripcion = "ErrorTELCELTablaLLena";
                    break;
                case 16:
                    descripcion = "ErrorAccesoDB";
                    break;
                case 30:
                    descripcion = "ErrorDeFormato";
                    break;
                case 35:
                    descripcion = "NumeroInvalido";
                    break;
                case 50:
                    descripcion = "ErrorProceso";
                    break;
                case 65:
                    descripcion = "ClienteBloqueado";
                    break;
                case 66:
                    descripcion = "SinCreditoDisponible";
                    break;
                case 67:
                    descripcion = "ErrorObteniendoCredito";
                    break;
                case 70:
                    descripcion = "ErrorConexionServer";
                    break;
                case 71:
                    descripcion = "SinRespuestaCarrier";
                    break;
                case 73:
                    descripcion = "CarrierAbajo";
                    break;
                case 87:
                    descripcion = "TelefonoNoSuceptibleARecargas";
                    break;
                case 88:
                    descripcion = "MontoInvalido";
                    break;
                default:
                    descripcion = "Respuesta del carrier";
                    break;
            }

            return descripcion;

        }

        /// <summary>
        /// Obtiene el nombre de la función en conjunto con la clase a la que pertenece y todas sus propiedades con su valor en una cadena de texto
        /// </summary>
        /// <param name="nombreFuncion">opcional con el nombre de la funcion, de lo contrario se obtiene del objeto instanciaDeUnaClase </param>
        /// <returns></returns>
        public static string ObtenerNombreFuncion([System.Runtime.CompilerServices.CallerMemberName] string nombreFuncion = "")
        {
            try
            {
                StackTrace st = new StackTrace(new StackFrame(1));
                string infoMetodo = st.GetFrame(0).GetMethod().DeclaringType.FullName + "." +
                    nombreFuncion + ".";
                return infoMetodo;
            }
            catch (Exception ex)
            {
                return "Error al obtener el nombre de la funcion: " + ex.Message;
            }

        }

        /// <summary>
        /// Obtiene el nombre de la función en conjunto con la clase a la que pertenece y todas sus propiedades con su valor en una cadena de texto
        /// </summary>
        /// <param name="propiedadesConValores">Si se desea ingrear propiedad:valor como cadena para darle formato de salida</param>
        /// <param name="nombreFuncion">opcional con el nombre de la funcion, de lo contrario se obtiene del objeto instanciaDeUnaClase </param>
        /// <returns></returns>
        public static string ObtenerNombreFuncion(string propiedadesConValores = "", [System.Runtime.CompilerServices.CallerMemberName] string nombreFuncion = "")
        {
            try
            {
                StackTrace st = new StackTrace(new StackFrame(1));
                string infoMetodoConArgumentos = st.GetFrame(0).GetMethod().DeclaringType.FullName + "." +
                    nombreFuncion + ". " + propiedadesConValores;
                return infoMetodoConArgumentos;
            }
            catch (Exception ex)
            {
                return "Error al obtener todas las propiedades de entrada: " + ex.Message;
            }

        }

        /// <summary>
        /// Función para obtener un número aleatorio confiable
        /// </summary>
        /// <returns></returns>
        public static int ObtenerNumeroResultadoAleatorio(int numElementos)
        {
            try
            {
                Thread.Sleep(70);
                semillaAleatorio = (int)DateTime.Now.Ticks & 0x0000FFFF;
                random = new Random(semillaAleatorio);
                Monitor.Enter(random);
                int aleatorio = random.Next(1, numElementos);
                Monitor.Exit(random);
                return aleatorio;
            }
            catch (Exception ex)
            {
                Task.Run(() => Log(ObtenerNombreFuncion(ex.Message), TiposLog.error));
                return 0;
            }
        }

        /// <summary>
        /// Función que realiza la adecuación de una cadena de caracteres dependiendo el tipo de formato establecido
        /// </summary>
        /// <param name="cadena">Cadena de caracteres a realizar un formato</param>
        /// <param name="tipoFormato">Tipo de formato a otorgar</param>
        /// <param name="longitud">Longitud final de la cadena</param>
        /// <returns></returns>
        public static object formatoValor(String cadena, TipoFormato tipoFormato, int longitud)
        {
            try
            {
                switch (tipoFormato)
                {
                    case TipoFormato.N:
                        while (cadena.Length < longitud)
                        {
                            cadena = "0" + cadena;
                        }
                        break;
                    case TipoFormato.AN:
                        while (cadena.Length < longitud)
                        {
                            cadena += " ";
                        }
                        break;
                    default:
                        while (cadena.Length < longitud)
                        {
                            cadena += " ";
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log(ObtenerNombreFuncion(ex.Message), TiposLog.error);
                return String.Empty;
            }
            return cadena;
        }

        /// <summary>
        /// función que valida si la entrada tiene carateres especiales
        /// </summary>
        /// <param name="valor"></param>
        /// <returns></returns>
        public static bool RevisionCaracteresEspeciales(String valor)
        {
            var regex = new Regex(@"[^a-zA-Z0-9:/ ]");
            if (regex.IsMatch(valor))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Funciona para convertir los bytes a kb, gb etc en formato string
        /// </summary>
        /// <param name="bytes">cantidad de bytes a formatear</param>
        /// <returns></returns>
        public static string ConvertirBytesFormato(int bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return String.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }
    }
}