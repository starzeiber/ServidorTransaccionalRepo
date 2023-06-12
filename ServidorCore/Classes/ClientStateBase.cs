using System;
using System.Net.Sockets;
using System.Threading;

namespace ServerCore
{

    /// <summary>
    /// Clase contiene toda la información relevante de un cliente así como un socket
    /// que será el de trabajo para el envío y recepción de mensajes
    /// </summary>
    public class ClientStateBase : IClienteStateBase
    {
        /// <summary>
        /// Identificador único para un cliente
        /// </summary>
        public Guid UniqueId { get; set; }

        /// <summary>
        /// Referencia al servidor de socket principal
        /// </summary>
        public object SocketMainReference;

        /// <summary>
        /// SocketAsyncEventArgs que se utilizará en la recepción
        /// </summary>
        internal SocketAsyncEventArgs socketAsyncEventArgs;

        /// <summary>        
        /// trama de respuesta al cliente
        /// </summary>
        public string requestMessage;

        /// <summary>        
        /// evento para sincronización de procesos, con este manejador de evento controlo
        /// el flujo cuando el fin de un envío ocurre
        /// </summary>
        internal EventWaitHandle sentEventWaitHandle;

        /// <summary>
        /// Ip del cliente
        /// </summary>
        public string ClientIp { get; set; } = "127.0.0.0";

        /// <summary>
        /// Puerto del cliente
        /// </summary>
        public int ClientPort { get; set; } = 0;

        /// <summary>
        /// Socket asignado de trabajo sobre la conexión del cliente
        /// </summary>
        public Socket SocketToWork { get; set; }

        /// <summary>
        /// Codigo de respuesta sobre el proceso del cliente
        /// Solo debe ser utilizado en conjunto con el enumerado CodigosRespuesta exclusivo para el Core en la clase utileria
        /// </summary>
        public int responseCode;

        /// <summary>
        /// Codigo de autorización sobre el proceso del cliente
        /// </summary>
        public int authorizationCode;

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de petición de un cliente
        /// </summary>
        public object objectClientRequest;

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de respuesta de un cliente
        /// </summary>
        public object objectClientResponse;

        /// <summary>
        /// Cualquier instancia de clase que se haya creado como entidad de solicitud del proveedor
        /// </summary>
        public object objectProviderRequest;
        /// <summary>
        /// Cualquier instancia de clase que se haya creado como entidad de respuesta a una solicitud del proveedor
        /// </summary>
        public object objectProviderResponse;

        /// <summary>
        /// Fecha marcada como inicio de operaciones con el cliente
        /// </summary>
        internal DateTime DateTimeReceiveMessage { get; set; } = DateTime.Now;

        /// <summary>
        /// Tiempo de espera general del lado del cliente
        /// </summary>
        public int TimeOut { get; set; }

        ///// <summary>
        ///// Bandera  para indicar que el proceso de responder se ha concluido correctamente
        ///// </summary>
        //public bool seHaRespondido { get; set; } = false;

        internal bool responseInProcess;

        private readonly object objectToLock = new object();


        /// <summary>
        /// Constructor
        /// </summary>
        public ClientStateBase()
        {
            sentEventWaitHandle = new ManualResetEvent(true);
            // se separa del constructor debido a  que  la inicialización de puede usar nuevamente sin hacer una nueva instancia
            Initialize();
        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, sirve para limpiar e inicializar 
        /// todas las variables del info y socket de trabajo
        /// </summary>
        internal void Initialize()
        {
            UniqueId = Guid.NewGuid();
            SocketMainReference = null;
            requestMessage = "";
            sentEventWaitHandle.Set();
            SocketToWork = null;
            responseCode = 0;
            authorizationCode = 0;
            objectClientRequest = null;
            objectClientResponse = null;
            TimeOut = ServerConfiguration.clientTimeOut;
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
        public virtual void ProcessMessage(string mensajeCliente)
        {
            if (objectClientRequest == null)
            {
                throw new ArgumentNullException($"El objeto '{nameof(objectClientRequest)}' no debe ser nullo");
            }
            if (objectClientResponse== null)
            {
                throw new ArgumentNullException($"El objeto '{nameof(objectClientResponse)}' no debe ser nullo");
            }
        }

        /// <summary>
        /// Funcion en la que se va a indicar cuál fue el socket principal sobre el cual
        /// se inició toda la operación
        /// </summary>
        /// <param name="socketPrincipal"> proceso donde se encuentra el socket principal del cuál se desprende el socket de trabajo por cliente</param>
        internal void SetSocketMainReference(object socketPrincipal)
        {
            this.SocketMainReference = socketPrincipal;
        }

        /// <summary>
        /// Función para obtener la trama de respuesta al cliente dependiendo de su mensajería entrante
        /// </summary>
        public virtual void GetResponseMessage()
        {

        }

        /// <summary>
        /// Función que podrá guardar un registro en base de datos 
        /// o en cuyo caso, si está en modo router el servidor, actualizar un registro previamente guardado
        /// </summary>
        public virtual void SaveTransaction()
        {

        }

        /// <summary>
        /// Función que indica que hay una respuesta en proceso de envío al cliente
        /// </summary>
        internal void SetResponseInProcess()
        {
            lock (objectToLock)
                if (!responseInProcess) responseInProcess = true;
        }

        /// <summary>
        /// indica que no hay un proceso activo de envío de respuesta al cliente
        /// </summary>
        internal void SetResponseCompleted()
        {
            lock (objectToLock)
                if (responseInProcess) responseInProcess = false;
        }
    }
}
