using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerCore
{

    /// <summary>
    /// Clase que contiene las propiedades de un proveedor en el flujo del servidor
    /// </summary>
    public class ProviderStateBase : IProviderStateBase
    {

        /// <summary>
        /// Referencia al servidor de socket principal
        /// </summary>
        internal object socketMainReference;

        /// <summary>
        /// SocketAsyncEventArgs que se utilizará en la recepción
        /// </summary>
        internal SocketAsyncEventArgs socketAsyncEventArgs;

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
        internal Socket SocketToWork { get; set; }

        /// <summary>
        /// Codigo de respuesta sobre el proceso del cliente
        /// </summary>
        public int responseCode;

        /// <summary>
        /// Codigo de autorización sobre el proceso del cliente
        /// </summary>
        public int authorizationCode;

        /// <summary>
        /// Estado del cliente desde donde proviene la petición para un retorno
        /// </summary>
        internal ClientStateBase ClientStateOriginal { get; set; }

        /// <summary>
        /// Trama de petición a un proveedor
        /// </summary>
        public string requestMessage;

        /// <summary>
        /// Trama de respuesta de un proveedor
        /// </summary>
        public string responseMessage;

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de petición de un proveedor
        /// </summary>
        public object objectRequest;

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de respuesta de un proveedor
        /// </summary>
        public object objectResponse;

        /// <summary>
        /// Timer del lado del proveedor para medir el tiempo de respuesta sobre una petición
        /// </summary>
        public Timer providerTimer;

        /// <summary>
        /// Bandera para indicar que hubo un vencimiento de TimeOut  y poder controlar la respuesta
        /// </summary>
        internal bool IsTimeOver { get; set; } = false;


        private readonly object objectToLock = new object();


        internal IPEndPoint endPoint;

        /// <summary>
        /// Constructor
        /// </summary>
        public ProviderStateBase()
        {
            // se separa del constructor debido a  que  la inicialización de puede usar nuevamente sin hacer una nueva instancia
            Initialize();
        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, sirve para limpiar e inicializar 
        /// todas las variables del info y socket de trabajo
        /// </summary>
        internal void Initialize()
        {
            socketMainReference = null;
            SocketToWork = null;
            responseCode = 0;
            authorizationCode = 0;
            requestMessage = "";
            responseMessage = "";
            ClientStateOriginal = null;
            objectRequest = null;
            objectResponse = null;
        }

        /// <summary>
        /// Ingresa de forma segura el valor de la instancia de socket principal para un retorno de flujo
        /// </summary>
        /// <param name="obj"></param>
        public virtual void SetObjectRequestClient(object obj)
        {

        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, en esta se controla
        /// toda la operación sobre el mensaje del cliente así como su mensaje de respuesta
        /// </summary>
        public virtual void ProcessProviderMessage(string message)
        {
        }

        /// <summary>
        /// Funcion en la que se va a indicar cuál fue el socket principal sobre el cual
        /// se inició toda la operación
        /// </summary>
        /// <param name="mainSocket"> proceso donde se encuentra el socket principal del cuál se desprende el socket de trabajo por cliente</param>
        internal void SetMainSocketReference(object mainSocket)
        {
            this.socketMainReference = mainSocket;
        }

        /// <summary>
        /// Función que obtiene la trama de petición al proveedor
        /// </summary>
        public virtual void GetRequestMessage()
        {

        }

        /// <summary>
        /// Función que obtiene la trama de respuesta de una proveedor
        /// </summary>
        public virtual void GetResponseMessage()
        {

        }

        /// <summary>
        /// Función que guardará un registro en base de datos cuando el servidor esté en modo router
        /// </summary>
        public virtual void SaveTransaction()
        {

        }

        /// <summary>
        /// Función que se utiliza para marcar un timeout de forma segura
        /// </summary>
        internal void SetTimeOver()
        {
            lock (objectToLock)
                if (!IsTimeOver) IsTimeOver = true;

        }

        /// <summary>
        /// Función que se utiliza para desmarcar la bandera de timeout de forma segura
        /// </summary>
        internal void RestartTimeOut()
        {
            lock (objectToLock)
                if (IsTimeOver) IsTimeOver = false;
        }
    }
}
