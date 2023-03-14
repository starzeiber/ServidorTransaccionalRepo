using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerCore
{
    /// <summary>
    /// Clase que contiene las propiedades de un proveedor en el flujo del servidor
    /// </summary>
    public class EstadoDelProveedorBase
    {

        /// <summary>
        /// Referencia al servidor de socket principal
        /// </summary>
        public object referenciaSocketPrincipal;

        /// <summary>
        /// SocketAsyncEventArgs que se utilizará en la recepción
        /// </summary>
        internal SocketAsyncEventArgs saeaDeEnvioRecepcion;

        /// <summary>
        /// Ip del proveedor
        /// </summary>
        public string ipProveedor { get; set; } = "127.0.0.0";

        /// <summary>
        /// Puerto del proveedor
        /// </summary>
        public Int32 puertoProveedor { get; set; } = 0;

        /// <summary>
        /// Socket asignado de trabajo sobre la conexión del cliente
        /// </summary>
        public Socket socketDeTrabajo { get; set; }

        /// <summary>
        /// Codigo de respuesta sobre el proceso del cliente
        /// </summary>
        public int codigoRespuesta;

        /// <summary>
        /// Codigo de autorización sobre el proceso del cliente
        /// </summary>
        public int codigoAutorizacion;

        /// <summary>
        /// Estado del cliente desde donde proviene la petición para un retorno
        /// </summary>
        public EstadoDelClienteBase estadoDelClienteOrigen { get; set; }

        /// <summary>
        /// Trama de petición a un proveedor
        /// </summary>
        public string tramaSolicitud;

        /// <summary>
        /// Trama de respuesta de un proveedor
        /// </summary>
        public string tramaRespuesta;

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de petición de un proveedor
        /// </summary>
        public object objSolicitud;

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de respuesta de un proveedor
        /// </summary>
        public object objRespuesta;



        public Timer providerTimer;

        /// <summary>
        /// Bandera para indicar que hubo un vencimiento de TimeOut  y poder controlar la respuesta
        /// </summary>
        internal bool seVencioElTimeOut { get; set; } = false;


        private readonly object objetoDeBloqueo = new object();


        internal IPEndPoint endPoint;

        /// <summary>
        /// Constructor
        /// </summary>
        public EstadoDelProveedorBase()
        {
            // se separa del constructor debido a  que  la inicialización de puede usar nuevamente sin hacer una nueva instancia
            InicializarEstadoDelProveedorBase();
        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, sirve para limpiar e inicializar 
        /// todas las variables del info y socket de trabajo
        /// </summary>
        public virtual void InicializarEstadoDelProveedorBase()
        {
            referenciaSocketPrincipal = null;
            socketDeTrabajo = null;
            codigoRespuesta = 0;
            codigoAutorizacion = 0;
            tramaSolicitud = "";
            tramaRespuesta = "";
            estadoDelClienteOrigen = null;
            objSolicitud = null;
            objRespuesta = null;
        }

        /// <summary>
        /// Ingresa de forma segura el valor de la instancia de socket principal para un retorno de flujo
        /// </summary>
        /// <param name="obj"></param>
        public virtual void IngresarObjetoPeticionCliente(object obj)
        {

        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, en esta se controla
        /// toda la operación sobre el mensaje del cliente así como su mensaje de respuesta
        /// </summary>
        public virtual void ProcesarTramaDelProveeedor(string trama)
        {
        }

        /// <summary>
        /// Funcion en la que se va a indicar cuál fue el socket principal sobre el cual
        /// se inició toda la operación
        /// </summary>
        /// <param name="socketPrincipal"> proceso donde se encuentra el socket principal del cuál se desprende el socket de trabajo por cliente</param>
        internal void IngresarReferenciaSocketPrincipal(object socketPrincipal)
        {
            this.referenciaSocketPrincipal = socketPrincipal;
        }

        /// <summary>
        /// Función que obtiene la trama de petición al proveedor
        /// </summary>
        public virtual void ObtenerTramaPeticion()
        {

        }

        /// <summary>
        /// Función que obtiene la trama de respuesta de una proveedor
        /// </summary>
        public virtual void ObtenerTramaRespuesta()
        {

        }

        /// <summary>
        /// Función que guardará la operación con el proveedor
        /// </summary>
        public virtual void GuardarTransaccion()
        {

        }

        public void IndicarVencimientoPorTimeOut()
        {
            lock (objetoDeBloqueo)
                if (!seVencioElTimeOut) seVencioElTimeOut = true;

        }

        public void ReinicioBanderaTimeOut()
        {
            lock (objetoDeBloqueo)
                if (seVencioElTimeOut) seVencioElTimeOut = false;
        }
    }
}
