using System.Net.Sockets;

namespace TransactionalServerCore
{

    /// <summary>
    /// Clase contiene toda la información relevante de un cliente así como un socket
    /// que será el de trabajo para el envío y recepción de mensajes
    /// </summary>
    public class ClientStateBase : IDisposable
    {
        /// <summary>
        /// Identificador único para un cliente
        /// </summary>
        public string UniqueClientId { get; set; } = "";

        /// <summary>
        /// Referencia al servidor de socket principal
        /// </summary>
        public object mainSocketReference;

        /// <summary>
        /// SocketAsyncEventArgs que se utilizará en la recepción
        /// </summary>
        internal SocketAsyncEventArgs saeaOfSendReceive;

        /// <summary>        
        /// trama de respuesta al cliente
        /// </summary>
        public string messageResponse;

        /// <summary>        
        /// evento para sincronización de procesos, con este manejador de evento controlo
        /// el flujo cuando el fin de un envío ocurre
        /// </summary>
        internal EventWaitHandle waitSendingEvent;

        /// <summary>
        /// Ip del cliente
        /// </summary>
        public string IpClient { get; set; } = "127.0.0.0";

        /// <summary>
        /// Puerto del cliente
        /// </summary>
        public Int32 PortClient { get; set; } = 0;

        /// <summary>
        /// Socket asignado de trabajo sobre la conexión del cliente
        /// </summary>
        public Socket SocketOfWork { get; set; }

        /// <summary>
        /// Codigo de respuesta sobre el proceso del cliente
        /// </summary>
        public int responseCode;

        /// <summary>
        /// Codigo de autorización sobre el proceso del cliente
        /// </summary>
        public int authorizationCode;

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de petición de un cliente
        /// </summary>
        public object objRequest;

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de respuesta de un cliente
        /// </summary>
        public object objResponse;

        /// <summary>
        /// Represents the supplier request object.
        /// </summary>
        /// <remarks>This field is intended to store data related to a supplier request.  Ensure that the
        /// object assigned to this field is of the expected type and structure.</remarks>
        public object objRequestToProvider;

        /// <summary>
        /// Represents the response object from a provider.
        /// </summary>
        /// <remarks>This property is intended to store the result or data returned by an external
        /// provider.  The specific type and structure of the object depend on the provider's implementation.</remarks>
        public object objResponseFromProvider;

        /// <summary>
        /// Fecha marcada como inicio de operaciones con el cliente
        /// </summary>
        public DateTime StartDateTrx { get; set; } = DateTime.Now;

        /// <summary>
        /// Tiempo de espera general del lado del cliente
        /// </summary>
        public int timeOut;

        /// <summary>
        /// Bandera para identificar si el proceso solo es de consulta sobre una transacción
        /// </summary>
        public bool isQuery;

        /// <summary>
        /// Indicates whether the system is in use.
        /// </summary>
        public int inUse;

        /// <summary>
        /// Represents the unique identifier for a database transaction.
        /// </summary>
        /// <remarks>This field is intended to store the transaction ID associated with a specific
        /// database operation.</remarks>
        public int idTrxBD;

        /// <summary>
        /// Represents a message string. This field is intended to store a textual message.
        /// </summary>
        public string msg210 = "";

        /// <summary>
        /// Represents a message string. This field is intended to store a textual message.
        /// </summary>
        public string msg230 = "";

        /// <summary>
        /// Indicates whether the object has been disposed.
        /// </summary>
        /// <remarks>This field is used internally to track the disposal state of the object.  It should
        /// not be accessed directly by external code.</remarks>
        private bool disposed = false;


        /// <summary>
        /// Initializes a new instance of the <see cref="ClientStateBase"/> class.
        /// </summary>
        /// <remarks>The constructor initializes the internal state of the <see cref="ClientStateBase"/>
        /// instance and prepares it for use. The initialization logic is separated into the  <see
        /// cref="InitializeClientStateBase"/> method to allow reinitialization without creating a new
        /// instance.</remarks>
        public ClientStateBase()
        {
            waitSendingEvent = new ManualResetEvent(true);
            // se separa del constructor debido a  que  la inicialización de puede usar nuevamente sin hacer una nueva instancia
            InitializeClientStateBase();
        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, sirve para limpiar e inicializar 
        /// todas las variables del info y socket de trabajo
        /// </summary>
        public virtual void InitializeClientStateBase()
        {
            // Liberar y limpiar el socket si existe
            if (SocketOfWork != null)
            {
                try { SocketOfWork.Shutdown(SocketShutdown.Both); } catch { }
                try { SocketOfWork.Close(); } catch { }
                try { SocketOfWork.Dispose(); } catch { }
                SocketOfWork = null;
            }

            // Limpiar el buffer del SAEA si aplica
            if (saeaOfSendReceive != null)
            {
                saeaOfSendReceive.AcceptSocket = null;
            }

            // Limpiar o inicializar otros datos de sesión
            SetClientId();
            waitSendingEvent.Set();
            messageResponse = "";
            objRequest = null;
            objResponse = null;
            objRequestToProvider = null;
            objResponseFromProvider = null;
            responseCode = 0;
            authorizationCode = 0;
            StartDateTrx = DateTime.Now;
            timeOut = ServerConfiguration.clientTimeOut;
            isQuery = false;
            //seEstaRespondiendo = false;
            inUse = 0;
            idTrxBD = 0;
            msg210 = "";
            msg230 = "";
            mainSocketReference = null;
            //por precaución se coloca que no se está procesando respuesta
            SetFree();
        }

        public bool SetClientId()
        {
            bool isLock = false;
            try
            {
                isLock = Monitor.TryEnter(this, Utilities.milisecondsTimeOutLock);
                if (isLock)
                {
                    UniqueClientId = $"{Guid.NewGuid()}-{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}";
                }
                else
                {
                    throw new TimeoutException("Timeout al intentar obtener el lock para generar el UniqueClientId");
                }
                return true;
            }
            catch (Exception ex)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("Error en ");
                sb.Append(nameof(SetClientId));
                sb.Append(", ");
                sb.Append(ex.Message);
                Utilities.Log(sb.ToString(), Utilities.LogType.Error);
                return false;
            }
            finally
            {
                if (isLock)
                {
                    Monitor.Exit(this);
                }
            }
        }


        /// <summary>
        /// Función virtual para poder sobre escribirla, en esta se controla
        /// toda la operación sobre el mensaje del cliente así como su mensaje de respuesta
        /// </summary>
        /// <param name="clientMessage">Mensaje que se recibe de un cliente</param>
        public virtual void ProcessMessage(string clientMessage)
        {
        }

        /// <summary>
        /// Funcion en la que se va a indicar cuál fue el socket principal sobre el cual
        /// se inició toda la operación
        /// </summary>
        /// <param name="mainSocket"> proceso donde se encuentra el socket principal del cuál se desprende el socket de trabajo por cliente</param>
        public void SetMainSocketReference(object mainSocket)
        {
            mainSocketReference = mainSocket;
        }

        /// <summary>
        /// Función para obtener la trama de respuesta al cliente dependiendo de su mensajería entrante
        /// </summary>
        public virtual void GetResponseMessage()
        {

        }

        /// <summary>
        /// Función que guardará el resultado de la transacción
        /// </summary>
        public virtual void UpdateTransaction()
        {

        }

        /// <summary>
        /// Marks the current instance as processing a response, ensuring thread-safe access.
        /// </summary>
        /// <remarks>This method uses an atomic operation to update the internal state, ensuring that 
        /// only one thread can mark the instance as processing a response at a time.  Subsequent calls from other
        /// threads will have no effect if the instance is already marked.</remarks>
        public void SetInUse()
        {
            Interlocked.CompareExchange(ref inUse, 1, 0);
        }

        /// <summary>
        /// Marks the resource as free, allowing it to be reused.
        /// </summary>
        /// <remarks>This method uses an atomic operation to ensure thread safety when updating the
        /// resource's state.</remarks>
        public void SetFree()
        {
            Interlocked.CompareExchange(ref inUse, 0, 1);
        }

        /// <summary>
        /// Releases the resources used by the current instance of the class.
        /// </summary>
        /// <remarks>This method should be called when the instance is no longer needed to free up
        /// resources.  After calling this method, the instance should not be used.</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the object and, optionally, releases the managed resources.
        /// </summary>
        /// <remarks>This method is called by the public <c>Dispose</c> method and the finalizer. When
        /// <paramref name="disposing"/> is <see langword="true"/>, this method releases all resources held by managed
        /// objects that the object references. Override this method in a derived class to release additional
        /// resources.</remarks>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release
        /// only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    saeaOfSendReceive?.Dispose();
                    saeaOfSendReceive = null;

                    if (SocketOfWork != null)
                    {
                        try { SocketOfWork.Shutdown(SocketShutdown.Both); } catch { }
                        SocketOfWork.Close();
                        SocketOfWork.Dispose();
                        SocketOfWork = null;
                    }
                }
                disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the instance of the <see cref="ClientStateBase"/> class.
        /// </summary>
        /// <remarks>This destructor ensures that unmanaged resources are released by calling the <see
        /// cref="Dispose(bool)"/> method.</remarks>
        ~ClientStateBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Sets the HTTP response code for the current operation.
        /// </summary>
        /// <remarks>The response code is used to indicate the result of the operation. Ensure that the
        /// provided code adheres to the standard HTTP status code conventions.</remarks>
        /// <param name="code">The HTTP status code to set. Must be a valid HTTP status code (e.g., 200, 404, 500).</param>
        public void SetResponseCode(int code)
        {
            responseCode = code;
        }

        /// <summary>
        /// Sets the authorization code used for authentication or access control.
        /// </summary>
        /// <param name="code">The authorization code to set. Must be a valid integer representing the required authorization.</param>
        public void SetAuthorizationCode(int code)
        {
            authorizationCode = code;
        }
    }
}
