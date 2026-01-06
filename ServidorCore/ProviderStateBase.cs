using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    /// <summary>
    /// Clase que contiene las propiedades de un proveedor en el flujo del servidor
    /// </summary>
    public class ProviderStateBase : IDisposable
    {
        /// <summary>
        /// Gets or sets the unique identifier for the provider.
        /// </summary>
        public string UniqueProviderId { get; set; }

        /// <summary>
        /// Referencia al servidor de socket principal
        /// </summary>
        public object mainSocketReference;

        /// <summary>
        /// SocketAsyncEventArgs que se utilizará en la recepción
        /// </summary>
        internal SocketAsyncEventArgs saeaSendReceive;

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
        public int authorizacionCode;

        /// <summary>
        /// Estado del cliente desde donde proviene la petición para un retorno
        /// </summary>
        public ClientStateBase clientStateSource { get; private set; }

        /// <summary>
        /// Trama de petición a un proveedor
        /// </summary>
        public string messageRequest;

        /// <summary>
        /// Trama de respuesta de un proveedor
        /// </summary>
        public string messageResponse;

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de petición de un proveedor
        /// </summary>
        public object objRequest;

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de respuesta de un proveedor
        /// </summary>
        public object objResponse;

        /// <summary>
        /// Indicates whether the resource is currently in use.
        /// </summary>
        /// <remarks>A value of 0 indicates that the resource is free, while a value of 1 indicates that
        /// the resource is in use.</remarks>
        public int InUse; // 0 = libre, 1 = en uso

        /// <summary>
        /// Bandera para indicar que hubo un vencimiento de TimeOut  y poder controlar la respuesta
        /// </summary>
        internal int wasTimeOutExpired;

        /// <summary>
        /// Represents the network endpoint, including the IP address and port, used for communication.
        /// </summary>
        /// <remarks>This field is intended for internal use and should not be accessed directly by
        /// external code. It specifies the endpoint to which the connection is bound or will be established.</remarks>
        internal IPEndPoint endPoint;

        /// <summary>
        /// Indicates whether the object has been disposed.
        /// </summary>
        /// <remarks>This field is used internally to track the disposal state of the object.  It should
        /// not be accessed directly outside of the class.</remarks>
        private bool disposed = false;

        /// <summary>
        /// The timestamp of the last recorded activity.
        /// </summary>
        /// <remarks>This field represents the date and time of the most recent activity.  It is expected
        /// to be in UTC format.</remarks>
        public DateTime lastActivityTime;

        internal EventWaitHandle waitSendingEvent;

        /// <summary>
        /// Tiempo de espera general del lado del cliente
        /// </summary>
        public int timeOut;



        /// <summary>
        /// Constructor
        /// </summary>
        public ProviderStateBase()
        {
            waitSendingEvent = new ManualResetEvent(true);
            // se separa del constructor debido a  que  la inicialización de puede usar nuevamente sin hacer una nueva instancia
            InitializeProviderStateBase();
        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, sirve para limpiar e inicializar 
        /// todas las variables del info y socket de trabajo
        /// </summary>
        public virtual void InitializeProviderStateBase()
        {

            // Limpiar el buffer del SAEA si aplica
            if (saeaSendReceive != null)
            {
                saeaSendReceive.AcceptSocket = null;
                //No puedo liberar el buffer porque lo administra el core
                //saeaDeEnvioRecepcion.UserToken = null;
                // El buffer se libera en el core con AdminBuffer.LiberarBuffer
            }

            waitSendingEvent.Set();
            mainSocketReference = null;
            responseCode = 0;
            authorizacionCode = 0;
            messageRequest = "";
            messageResponse = "";
            clientStateSource = null;
            objRequest = null;
            objResponse = null;
            endPoint = null;
            timeOut = ServerConfiguration.providerTimeout;
            UniqueProviderId = $"{Guid.NewGuid()}-{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}";
            TimeOutReset();
            InUse = 0;
            lastActivityTime = DateTime.Now;
        }

        /// <summary>
        /// Ingresa de forma segura el valor de la instancia de socket principal para un retorno de flujo
        /// </summary>
        /// <param name="objClient"></param>
        public virtual void SetObjClientRequest(object objClient)
        {

        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, en esta se controla
        /// toda la operación sobre el mensaje del cliente así como su mensaje de respuesta
        /// </summary>
        public virtual void ProcessMessage(string message)
        {
        }

        /// <summary>
        /// Funcion en la que se va a indicar cuál fue el socket principal sobre el cual
        /// se inició toda la operación
        /// </summary>
        /// <param name="mainSocket"> proceso donde se encuentra el socket principal del cuál se desprende el socket de trabajo por cliente</param>
        public void SetMainSocketReference(object mainSocket)
        {
            this.mainSocketReference = mainSocket;
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
        /// Función que guardará la operación con el proveedor
        /// </summary>
        public virtual void SaveTransaction()
        {

        }

        /// <summary>
        /// Marks the operation as timed out if it has not already been marked.
        /// </summary>
        /// <remarks>This method is thread-safe and ensures that the timeout state is set only
        /// once.</remarks>
        public void SetTimeOutExpired()
        {
            Interlocked.CompareExchange(ref wasTimeOutExpired, 1, 0);
        }

        /// <summary>
        /// Resets the timeout flag to indicate that the timeout condition is no longer met.
        /// </summary>
        /// <remarks>This method is thread-safe and ensures that the timeout flag is reset only when it
        /// has been set.  It should be called to clear the timeout state after handling a timeout condition.</remarks>
        public void TimeOutReset()
        {
            //lock (objetoDeBloqueo)
            //    if (seVencioElTimeOut) seVencioElTimeOut = false;
            Interlocked.CompareExchange(ref wasTimeOutExpired, 0, 1);
        }

        /// <summary>
        /// Releases the resources used by the current instance of the class.
        /// </summary>
        /// <remarks>Call this method when you are finished using the object to release both managed and
        /// unmanaged resources.  After calling <see cref="Dispose"/>, the object is in an unusable state and should not
        /// be used further.</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the resources used by the current instance of the class.
        /// </summary>
        /// <remarks>This method releases both managed and unmanaged resources. It is called by the public
        /// <see cref="Dispose()"/> method and the finalizer. When the <paramref name="disposing"/> parameter is <see
        /// langword="true"/>, this method releases all resources held by managed objects referenced by this instance.
        /// Override this method in a derived class to release additional resources.</remarks>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release
        /// only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Liberar recursos administrados
                    if (saeaSendReceive != null)
                    {
                        saeaSendReceive.Dispose();
                        saeaSendReceive = null;
                    }
                    if (SocketOfWork != null)
                    {
                        try { SocketOfWork.Shutdown(SocketShutdown.Both); } catch { }
                        SocketOfWork.Close();
                        SocketOfWork.Dispose();
                        SocketOfWork = null;
                    }
                }
                // Liberar recursos no administrados aquí si los hubiera
                disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the instance of the <see cref="ProviderStateBase"/> class.
        /// </summary>
        /// <remarks>This destructor ensures that unmanaged resources are released by calling the <see
        /// cref="Dispose(bool)"/> method.</remarks>
        ~ProviderStateBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Represents the <see cref="CancellationTokenSource"/> used to manage timeouts for operations.
        /// </summary>
        /// <remarks>This field is used internally to signal cancellation when a timeout occurs. It is not
        /// exposed publicly and should be properly disposed of to avoid resource leaks.</remarks>
        private CancellationTokenSource _timeoutCts;

        /// <summary>
        /// Occurs when the timeout period has expired.
        /// </summary>
        /// <remarks>This event is triggered to notify subscribers that the timeout period has elapsed. 
        /// Subscribers can handle this event to perform any necessary actions when the timeout occurs.</remarks>
        public event EventHandler<EventArgs> TimeOutExpired;

        /// <summary>
        /// Inicia una tarea asíncrona que espera X segundos y permite cancelación.
        /// </summary>
        internal async Task TimeOutCounterAsync(int timeOut)
        {
            _timeoutCts = new CancellationTokenSource();
            try
            {
                await Task.Delay(timeOut * 1000, _timeoutCts.Token);
                var sb = new System.Text.StringBuilder();
                sb.Append("Timeout expirado después de ");
                sb.Append(timeOut);
                sb.Append(" segundos, que es el tiempo restante para que la operación sea exitosa. ");
                sb.Append("ProveedorId: ");
                sb.Append(this.UniqueProviderId);
                sb.Append(". cliente: ");
                sb.Append(this.clientStateSource?.UniqueClientId.ToString() ?? "N/A");
                Utilities.Log(sb.ToString(), Utilities.LogType.Warning);
                SetTimeOutExpired();
                // Disparar el evento para notificar a ServidorTransaccional
                TimeOutExpired?.Invoke(this, EventArgs.Empty);
            }
            catch (TaskCanceledException)
            {
                TimeOutReset();
            }
        }

        /// <summary>
        /// Llama este método cuando recibas la respuesta antes del timeout.
        /// </summary>
        public void CancelTimeoutCounter()
        {
            try
            {
                _timeoutCts?.Cancel();
                var sb= new System.Text.StringBuilder();
                sb.Append("Timeout cancelado correctamente para la operación. ");
                sb.Append("ProveedorId: ");
                sb.Append(this.UniqueProviderId);
                sb.Append(". cliente: ");
                sb.Append(this.clientStateSource?.UniqueClientId.ToString() ?? "N/A");
                Utilities.Log(sb.ToString(), Utilities.LogType.Info);
            }
            catch (Exception ex)
            {
                Utilities.Log($"Error al cancelar el contador de timeout: {ex.Message}, para el cliente {clientStateSource.UniqueClientId}", Utilities.LogType.Error);
            }
        }

        /// <summary>
        /// Sets the current state of the client.
        /// </summary>
        /// <param name="clientState">The new state to assign to the client. Cannot be <see langword="null"/>.</param>
        public void SetClientState(ClientStateBase clientState)
        {
            this.clientStateSource = clientState;
        }

        /// <summary>
        /// Sets the HTTP response code for the current operation.
        /// </summary>
        /// <param name="code">The HTTP status code to set. Must be a valid HTTP status code (e.g., 200, 404, 500).</param>
        public void SetResponseCode(int code)
        {
            this.responseCode = code;
        }

        /// <summary>
        /// Sets the authorization code for the current instance.
        /// </summary>
        /// <param name="code">The authorization code to set. Must be a valid integer representing the desired authorization code.</param>
        public void SetAuthorizationCode(int code)
        {
            this.authorizacionCode = code;
        }

        /// <summary>
        /// Marks the current instance as being in use.
        /// </summary>
        /// <remarks>This method uses an atomic operation to ensure thread safety when updating the state.
        /// If the instance is already in use, the state remains unchanged.</remarks>
        public void SetInUse()
        {
            Interlocked.CompareExchange(ref InUse, 1, 0);
        }

        /// <summary>
        /// Marks the resource as free, allowing it to be reused.
        /// </summary>
        /// <remarks>This method uses an atomic operation to ensure thread safety when updating the
        /// resource's state.</remarks>
        public void SetFree()
        {
            Interlocked.CompareExchange(ref InUse, 0, 1);
        }

        public virtual void CompareResponseVsRequest(string message)
        {

        }


    }
}
