using System;
using System.Net.Sockets;
using System.Threading;

namespace ServerCore
{

    /// <summary>
    /// Clase contiene toda la información relevante de un cliente así como un socket
    /// que será el de trabajo para el envío y recepción de mensajes
    /// </summary>
    public class EstadoDelClienteBase : IEstadoDelClienteBase
    {
        /// <summary>
        /// Identificador único para un cliente
        /// </summary>
        public Guid IdUnicoCliente { get; set; }

        /// <summary>
        /// Referencia al servidor de socket principal
        /// </summary>
        public object ReferenciaSocketPrincipal;

        /// <summary>
        /// SocketAsyncEventArgs que se utilizará en la recepción
        /// </summary>
        internal SocketAsyncEventArgs saeaDeEnvioRecepcion;

        /// <summary>        
        /// trama de respuesta al cliente
        /// </summary>
        public string TramaRespuesta;

        /// <summary>        
        /// evento para sincronización de procesos, con este manejador de evento controlo
        /// el flujo cuando el fin de un envío ocurre
        /// </summary>
        internal EventWaitHandle esperandoEnvio;

        /// <summary>
        /// Ip del cliente
        /// </summary>
        public string IpCliente { get; set; } = "127.0.0.0";

        /// <summary>
        /// Puerto del cliente
        /// </summary>
        public int PuertoCliente { get; set; } = 0;

        /// <summary>
        /// Socket asignado de trabajo sobre la conexión del cliente
        /// </summary>
        public Socket SocketDeTrabajo { get; set; }

        /// <summary>
        /// Codigo de respuesta sobre el proceso del cliente
        /// Solo debe ser utilizado en conjunto con el enumerado CodigosRespuesta exclusivo para el Core en la clase utileria
        /// </summary>
        public int CodigoRespuesta;

        /// <summary>
        /// Codigo de autorización sobre el proceso del cliente
        /// </summary>
        public int CodigoAutorizacion;

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de petición de un cliente
        /// </summary>
        public object ObjSolicitud;

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de respuesta de un cliente
        /// </summary>
        public object ObjRespuesta;

        /// <summary>
        /// Cualquier instancia de clase que se haya creado como entidad de solicitud del proveedor
        /// </summary>
        public object ObjSolicitudProveedor;
        /// <summary>
        /// Cualquier instancia de clase que se haya creado como entidad de respuesta a una solicitud del proveedor
        /// </summary>
        public object ObjRespuestaProveedor;

        /// <summary>
        /// Fecha marcada como inicio de operaciones con el cliente
        /// </summary>
        internal DateTime FechaInicioTrx { get; set; } = DateTime.Now;

        /// <summary>
        /// Tiempo de espera general del lado del cliente
        /// </summary>
        public int TimeOut { get; set; }

        ///// <summary>
        ///// Bandera  para indicar que el proceso de responder se ha concluido correctamente
        ///// </summary>
        //public bool seHaRespondido { get; set; } = false;

        internal bool seEstaRespondiendo;

        private readonly object _objetoDeBloqueo = new object();


        /// <summary>
        /// Constructor
        /// </summary>
        public EstadoDelClienteBase()
        {
            esperandoEnvio = new ManualResetEvent(true);
            // se separa del constructor debido a  que  la inicialización de puede usar nuevamente sin hacer una nueva instancia
            InicializarEstadoDelClienteBase();
        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, sirve para limpiar e inicializar 
        /// todas las variables del info y socket de trabajo
        /// </summary>
        internal void InicializarEstadoDelClienteBase()
        {
            IdUnicoCliente = Guid.NewGuid();
            ReferenciaSocketPrincipal = null;
            TramaRespuesta = "";
            esperandoEnvio.Set();
            SocketDeTrabajo = null;
            CodigoRespuesta = 0;
            CodigoAutorizacion = 0;
            ObjSolicitud = null;
            ObjRespuesta = null;
            TimeOut = Configuracion.timeOutCliente;
            //este no porque hay una función con lock para hacerlo seEstaRespondiendo = false;
        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, en esta se controla
        /// toda el proceso del mensaje del cliente.
        /// Al final del proceso, la variable CodigoRespuesta, debe contener alguno de los valores
        /// del enumerado CodigosRespuesta exclusivo del Core en la clase utileria
        /// </summary>
        /// <param name="mensajeCliente">Mensaje que se recibe de un cliente sin formato</param>
        /// <remarks>El objeto objSolicitud debe contener la referencia de la clase utilizada como receptora del mensaje del cliente
        /// El objeto objRespuesa debe contener la referencia de la clase utilizada como respuesta al mensaje del cliente</remarks>
        public virtual void ProcesarTrama(string mensajeCliente)
        {
            if (ObjSolicitud == null)
            {
                throw new ArgumentNullException($"El objeto '{nameof(ObjSolicitud)}' no debe ser nullo");
            }
            if (ObjRespuesta== null)
            {
                throw new ArgumentNullException($"El objeto '{nameof(ObjRespuesta)}' no debe ser nullo");
            }
        }

        /// <summary>
        /// Funcion en la que se va a indicar cuál fue el socket principal sobre el cual
        /// se inició toda la operación
        /// </summary>
        /// <param name="socketPrincipal"> proceso donde se encuentra el socket principal del cuál se desprende el socket de trabajo por cliente</param>
        internal void IngresarReferenciaSocketPrincipal(object socketPrincipal)
        {
            this.ReferenciaSocketPrincipal = socketPrincipal;
        }

        /// <summary>
        /// Función para obtener la trama de respuesta al cliente dependiendo de su mensajería entrante
        /// </summary>
        public virtual void ObtenerTramaRespuesta()
        {

        }

        /// <summary>
        /// Función que podrá guardar un registro en base de datos 
        /// o en cuyo caso, si está en modo router el servidor, actualizar un registro previamente guardado
        /// </summary>
        public virtual void GuardarTransaccion()
        {

        }

        /// <summary>
        /// Función que indica que hay una respuesta en proceso de envío al cliente
        /// </summary>
        internal void SeEstaProcesandoRespuesta()
        {
            lock (_objetoDeBloqueo)
                if (!seEstaRespondiendo) seEstaRespondiendo = true;
        }

        /// <summary>
        /// indica que no hay un proceso activo de envío de respuesta al cliente
        /// </summary>
        internal void SeFinalizaProcesoRespuesta()
        {
            lock (_objetoDeBloqueo)
                if (seEstaRespondiendo) seEstaRespondiendo = false;
        }
    }
}
