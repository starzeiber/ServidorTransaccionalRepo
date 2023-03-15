using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerCore
{

    /// <summary>
    /// Clase que contiene las propiedades de un proveedor en el flujo del servidor
    /// </summary>
    public class EstadoDelProveedorBase : IEstadoDelProveedorBase
    {

        /// <summary>
        /// Referencia al servidor de socket principal
        /// </summary>
        internal object referenciaSocketPrincipal;

        /// <summary>
        /// SocketAsyncEventArgs que se utilizará en la recepción
        /// </summary>
        internal SocketAsyncEventArgs saeaDeEnvioRecepcion;

        ///// <summary>
        ///// Ip del proveedor
        ///// </summary>
        //public string ipProveedor { get; set; } = "127.0.0.0";

        ///// <summary>
        ///// Puerto del proveedor
        ///// </summary>
        //public int puertoProveedor { get; set; } = 0;

        /// <summary>
        /// Socket asignado de trabajo sobre la conexión del cliente
        /// </summary>
        internal Socket SocketDeTrabajo { get; set; }

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
        internal EstadoDelClienteBase EstadoDelClienteOrigen { get; set; }

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

        /// <summary>
        /// Timer del lado del proveedor para medir el tiempo de respuesta sobre una petición
        /// </summary>
        public Timer providerTimer;

        /// <summary>
        /// Bandera para indicar que hubo un vencimiento de TimeOut  y poder controlar la respuesta
        /// </summary>
        internal bool SeVencioElTimeOut { get; set; } = false;


        private readonly object _objetoDeBloqueo = new object();


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
        internal void InicializarEstadoDelProveedorBase()
        {
            referenciaSocketPrincipal = null;
            SocketDeTrabajo = null;
            codigoRespuesta = 0;
            codigoAutorizacion = 0;
            tramaSolicitud = "";
            tramaRespuesta = "";
            EstadoDelClienteOrigen = null;
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
        /// Función que guardará un registro en base de datos cuando el servidor esté en modo router
        /// </summary>
        public virtual void GuardarTransaccion()
        {

        }

        /// <summary>
        /// Función que se utiliza para marcar un timeout de forma segura
        /// </summary>
        internal void IndicarVencimientoPorTimeOut()
        {
            lock (_objetoDeBloqueo)
                if (!SeVencioElTimeOut) SeVencioElTimeOut = true;

        }

        /// <summary>
        /// Función que se utiliza para desmarcar la bandera de timeout de forma segura
        /// </summary>
        internal void ReinicioBanderaTimeOut()
        {
            lock (_objetoDeBloqueo)
                if (SeVencioElTimeOut) SeVencioElTimeOut = false;
        }
    }
}
