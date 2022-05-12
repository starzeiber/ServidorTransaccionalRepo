using System;
using System.Net.Sockets;
using System.Threading;

namespace UServerCore
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
        public string ipProveedor { get; set; }

        /// <summary>
        /// Puerto del proveedor
        /// </summary>
        public Int32 puertoProveedor { get; set; }

        /// <summary>
        /// Socket asignado de trabajo sobre la conexión del cliente
        /// </summary>
        public Socket socketDeTrabajo { get; set; }

        /// <summary>
        /// Codigo de respuesta sobre el proceso del cliente
        /// </summary>
        public int codigoRespuesta { get; set; }

        /// <summary>
        /// Codigo de autorización sobre el proceso del cliente
        /// </summary>
        public int codigoAutorizacion { get; set; }

        /// <summary>
        /// Estado del cliente desde donde proviene la petición para un retorno
        /// </summary>
        public EstadoDelClienteBase estadoDelClienteOrigen { get; set; }

        /// <summary>
        /// Trama de petición a un proveedor
        /// </summary>
        public string tramaSolicitud { get; set; }

        /// <summary>
        /// Trama de respuesta de un proveedor
        /// </summary>
        public string tramaRespuesta { get; set; }

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de petición de un proveedor
        /// </summary>
        public object objSolicitud;

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de respuesta de un proveedor
        /// </summary>
        public object objRespuesta;

        /// <summary>
        /// Tiempo de espera general del lado del proveedor
        /// </summary>
        public int timeOut { get; set; }

        /// <summary>
        /// Fecha marcada como inicio de operaciones con el proveedor
        /// </summary>
        public DateTime fechaInicioTrx { get; set; }


        public System.Threading.Timer providerTimer;


        /// <summary>
        /// Constructor
        /// </summary>
        public EstadoDelProveedorBase()
        {
            // waitSend = new AutoResetEvent(true);
            //esperandoEnvio = new ManualResetEvent(true);
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
            puertoProveedor = 0;
            ipProveedor = "";
            socketDeTrabajo = null;
            estadoDelClienteOrigen = null;
            timeOut = Configuracion.timeOutProveedor;
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
        public void IngresarReferenciaSocketPrincipal(object socketPrincipal)
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

        public void InitializeTimer()
        {
            ManualResetEvent waitHandle = new ManualResetEvent(false);
            providerTimer.Dispose(waitHandle);
            waitHandle.WaitOne();
            providerTimer = null;
        }
    }
}
