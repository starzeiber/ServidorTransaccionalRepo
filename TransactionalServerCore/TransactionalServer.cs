using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static TransactionalServerCore.Utilities;

namespace TransactionalServerCore
{
    /// <summary>
    /// Clase principal sobre el core del servidor transaccional, contiene todas las propiedades 
    /// del servidor y los métodos de envío y recepción asincronos
    /// </summary>
    /// <typeparam name="T">Instancia sobre la clase que contiene la información de un cliente conectado y su
    /// socket de trabajo una vez asignado desde el pool</typeparam>
    /// <typeparam name="S">Instancia sobre la clase que contiene el estado de flujo de una operación en el servidor</typeparam>
    /// <typeparam name="X">Instancia sobre la clase que contiene la información de un cliente conectado y su
    /// socket de trabajo una vez asignado desde el pool</typeparam>
    public class TransactionalServer<T, S, X>
        where T : ClientStateBase
        where S : ServerStateBase
        where X : ProviderStateBase
    {
        /// <summary>
        /// Specifies the timeout duration, in milliseconds, to wait for acquiring a lock.
        /// </summary>
        const int milisecondsTimeOutLock = 500;

        /// <summary>
        /// A factory method used to create instances of type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>This delegate is expected to return a new or existing instance of <typeparamref
        /// name="T"/>  when invoked. Ensure that the factory method is properly configured to provide valid instances 
        /// of the required type.</remarks>
        private Func<T> clientFactory;

        /// <summary>
        /// A delegate that creates and returns an instance of type <typeparamref name="S"/>.
        /// </summary>
        /// <remarks>This factory function is used to generate instances of the specified type
        /// <typeparamref name="S"/>  on demand. Ensure that the delegate is properly initialized before use.</remarks>
        private Func<S> serverFactory;

        /// <summary>
        /// A delegate that provides a factory method for creating instances of type <see cref="X"/>.
        /// </summary>
        /// <remarks>This delegate is used to encapsulate the logic for creating instances of <see
        /// cref="X"/>.  It allows for deferred or customized instantiation of the type.</remarks>
        private Func<X> providerFactory;

        /// <summary>
        /// Represents a pool of reusable socket connections.
        /// </summary>
        /// <remarks>This field is intended to manage and reuse socket connections efficiently, reducing
        /// the overhead  of creating and disposing sockets repeatedly. It is typically used in scenarios where multiple
        /// network connections are required, such as in high-performance networking applications.</remarks>
        SocketPool socketPool;

        /// <summary>
        /// Instancia del performance counter de peticiones entrantes
        /// </summary>
        public PerformanceCounter incommigConnectionsPerformanceCounter;

        /// <summary>        
        /// nombre del servidor para identificarlo en una lista
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Descripción del servidor
        /// </summary>
        public string ServerDescription { get; set; }

        /// <summary>
        /// Puerto del servidor
        /// </summary>
        public Int32 ServerPort { get; set; }

        /// <summary>
        /// Obtiene una lista de clientes ordenados por un GUID
        /// </summary>
        public ConcurrentDictionary<String, T> clientsList;

        /// <summary>
        /// Represents a collection of providers, where each provider is identified by a unique identifier.
        /// </summary>
        /// <remarks>The dictionary maps a <see cref="Guid"/> to an instance of <typeparamref name="X"/>. 
        /// Use this collection to store and retrieve providers based on their unique identifiers.</remarks>
        public ConcurrentDictionary<String, X> providersList;

        /// <summary>        
        /// Obtiene o ingresa el número máximo de conexiones simultaneas de una misma IP del cliente (0=ilimitadas)
        /// </summary>
        public int maximumConnectionsPerClientIp { get; set; }

        /// <summary>
        /// Obtiene o ingresa el valor de que si el servidor está o no ejecutandose
        /// </summary>
        public bool InExecution { get; set; }

        /// <summary>
        /// Obtiene o ingresa el estado del socket del servidor
        /// </summary>
        public S ServerStateBase { get; set; }

        /// <summary>
        /// Obtiene o ingresa a la lista de clientes pendientes de desconexión, esta lista es para la verificación de que todos los cliente
        /// se desconectan adecuadamente, su uso es más para debug
        /// </summary>
        public List<T> ClientsPendingDisconnectionList { get; set; }

        /// <summary>
        /// Obtiene o ingresa a la lista de proveedores pendientes de desconexión, esta lista es para la verificación de que todos los proveedores
        /// se desconectan adecuadamente, su uso es más para debug pero queda para mejorar
        /// </summary>
        public List<X> ProvidersPendingDisconnectionList { get; set; }

        /// <summary>
        /// Obtiene el número de clientes conectados actualmente al servidor
        /// </summary>
        public int ClientsConnectedCounter
        {
            get
            {
                return clientsList.Values.Count;
            }
        }

        /// <summary>
        /// Gets the number of connected providers.
        /// </summary>
        public int ProvidersConnectedCounter
        {
            get
            {
                return providersList.Values.Count;
            }
        }

        /// <summary>
        /// Ip a la cual se apuntarán todas las transacciones del proveedor
        /// </summary>
        public string ProviderIp { get; set; }

        /// <summary>
        /// Gets the total number of client states currently managed.
        /// </summary>
        public int ClientStateCounter
        {
            get
            {
                return clientStateManager.ClientStateCounter;
            }
        }

        /// <summary>
        /// Gets the total count of supplier states.
        /// </summary>
        public int ProviderStateCounter
        {
            get
            {
                return providerStateManager.ProviderStateCounter;
            }
        }

        /// <summary>
        /// Gets the number of client sockets that are pending disconnection.
        /// </summary>
        public int SocketsClientsPendientsCounter
        {
            get
            {
                return ClientsPendingDisconnectionList.Count;
            }
        }

        /// <summary>
        /// Gets the number of socket providers that are pending disconnection.
        /// </summary>
        public int SocketsProvidersPendientsCounter
        {
            get
            {
                return ProvidersPendingDisconnectionList.Count;
            }
        }

        /// <summary>
        /// Gets the current count of available buffers in the stack buffer manager.
        /// </summary>
        public int StackBufferCounter
        {
            get
            {
                return bufferManager.AvailableBuffersCounter;
            }
        }

        /// <summary>
        /// Gets the number of sockets currently available in the socket pool.
        /// </summary>
        public int SocketInPoolCounter
        {
            get
            {
                return socketPool.SocketInPoolCounter;
            }
        }

        /// <summary>
        /// Gets or sets the list of port numbers associated with the provider.
        /// </summary>
        public List<int> ProviderPortsList { get; set; }

        /// <summary>
        /// Gets or sets the number of messages received per second.
        /// </summary>
        private int messagesReceivedPerSecond = 0;

        /// <summary>
        /// Gets or sets the number of messages processed per second.
        /// </summary>
        private int messagesPerSecond = 0;

        /// <summary>
        /// Represents the timestamp of the most recent second, initialized to the current UTC time.
        /// </summary>
        /// <remarks>This field is intended for internal use to track or compare time intervals.  The
        /// value is set to the current UTC time when the object is created.</remarks>
        private DateTime lastSecond = DateTime.UtcNow;

        /// <summary>
        /// A synchronization object used to ensure thread-safe access to shared resources.
        /// </summary>
        /// <remarks>This object is intended to be used as a lock for critical sections where multiple
        /// threads may attempt to access or modify shared data concurrently. Use the <c>lock</c> statement with this
        /// object to enforce mutual exclusion.</remarks>
        private readonly object lockMessages = new object();

        /// <summary>
        /// Gets the number of messages processed per second.
        /// </summary>
        public int MessagesPerSecond => messagesPerSecond;


        /// <summary>
        /// IP de escucha
        /// </summary>
        public string localIp;

        /// <summary>
        /// Número de conexiones simultaneas que podrá manejar el servidor por defecto
        /// </summary>
        private int numberOfSimultaneousClientConnections;
        /// <summary>
        /// Represents the maximum number of simultaneous connections allowed for the provider.
        /// </summary>
        /// <remarks>This field is used to define the limit on concurrent connections that the provider
        /// can handle. It is a read-only value and cannot be modified after initialization.</remarks>
        private int numberOfSimultaneousProviderConnections;

        /// <summary>
        /// Número sockest para lectura y escritura sin asignación de espacio del buffer para aceptar peticiones como default
        /// esto para tener siempre por lo menos sockects disponibles al inicio del servidor
        /// </summary>
        private const int preAvailableOperations = 3;

        /// <summary>
        /// instancia al administrador de estados de socket de trabajo
        /// </summary>
        private readonly ClientStateManager<T> clientStateManager;

        /// <summary>
        /// Instancia del administrador de estados del proveedor
        /// </summary>
        private readonly ProviderStateManager<X> providerStateManager;

        /// <summary>
        /// semáforo sobre las peticiones de clientes para controlar el número total que podrá soportar el servidor
        /// </summary>
        private readonly SemaphoreSlim ClientSemaphoreConnections;

        /// <summary>
        /// semáforo sobre las peticiones a proveedores para controlar el número total que podrá soportar el servidor
        /// </summary>
        private readonly SemaphoreSlim ProviderSemaphoreConnections;

        /// <summary>
        /// Indicates whether the response was triggered due to system saturation.
        /// </summary>
        private bool responseDueToSaturation = false;

        /// <summary>
        /// Sets the response code flag to indicate whether saturation has occurred.
        /// </summary>
        /// <param name="saturationExists">A value indicating whether saturation exists. Set to <see langword="true"/> if saturation is present;
        /// otherwise, <see langword="false"/>.</param>
        public void SetResponseCodeDueToSaturation(bool saturationExists)
        {
            responseDueToSaturation = saturationExists;
        }

        /// <summary>
        /// Representa un conjunto enorme de buffer reutilizables entre todos los sockects de trabajo
        /// </summary>
        private readonly BufferManager bufferManager;

        /// <summary>
        /// Socket de escucha para las conexiones de clientes
        /// </summary>
        private Socket mainListenSocket;

        /// <summary>
        /// Bandera para identificar que la conexión está bien establecida
        /// </summary>
        private bool disconnecting = false;

        /// <summary>
        /// Parámetros que  indica el máximo de pedidos que pueden encolarse simultáneamente en caso que el servidor 
        /// esté ocupado atendiendo una nueva conexión.
        /// </summary>
        private int backLog;

        /// <summary>
        /// Tamaño del buffer por petición
        /// </summary>
        private int sizeBufferPerRequest;

        /// <summary>
        /// Numero que muestra cuantas conexiones puede aún soportar el servidor del lado del cliente
        /// </summary>
        public int numberOfAvailableClientResources
        {
            get
            {
                return ClientSemaphoreConnections.CurrentCount;
            }
        }

        /// <summary>
        /// Numero que muestra cuantas conexiones puede aún soportar el servidor del lado del proveedor
        /// </summary>
        public int numberOfAvailableProviderResources
        {
            get
            {
                return ProviderSemaphoreConnections.CurrentCount;
            }
        }


        /// <summary>
        /// Mensaje de aviso
        /// </summary>
        private const string NOTPERMISSION = "No cuenta con permisos para usar la aplicación o falta el archivo de configuración";

        /// <summary>
        /// Represents the current count of ports being tracked.
        /// </summary>
        /// <remarks>This field is intended for internal use only and should not be accessed directly
        /// outside of the containing class.</remarks>
        internal int portCounter = 0;

        /// <summary>
        /// Número total de bytes recibido en el servidor, para uso estadístico
        /// </summary>
        private int totalBytesRead;

        /// <summary>
        /// Bytes que se han transmitido desde el inicio de la aplicación
        /// </summary>
        public int totalBytesTransferred
        {
            get
            {
                return totalBytesRead;
            }
        }

        /// <summary>
        /// Crea una instancia del administrador de sockets, posterior se tiene que llamar al método
        /// ConfigInicioServidor para iniciar el proceso de asignacion de recursos        
        /// </summary>
        /// <param name="clienteFactory">Función que crea una instancia de la clase EstadoDelClienteBase</param>
        /// <param name="providerFactory">Función que crea una instancia de la clase EstadoDelProveedorBase</param>
        /// <param name="servidorFactory">Función que crea una instancia de la clase EstadoDelServidorBase</param>
        /// <param name="concurrentConexionNumber">Maximo número de conexiones simultaneas a manejar en el servidor</param>
        /// <param name="bufferSizePerMessage">Tamaño del buffer por conexión, un parámetro standart es 1024</param>
        /// <param name="backlog">Parámetro TCP/IP backlog, el recomendable es 100</param>
        public TransactionalServer(Func<T> clienteFactory, Func<S> serverFactory, Func<X> providerFactory, Int32 concurrentConexionNumber, Int32 bufferSizePerMessage = 1024, int backlog = 100)
        {
            SetStatesFactories(clienteFactory, serverFactory, providerFactory);
            SetConcurrentConnections(concurrentConexionNumber, backlog);
            SetClientsAndProvidersLists();
            SetBufferSizePerMessage(bufferSizePerMessage);
            // establezco el proceso principal para referencia futura
            ServerStateBase.MainProcees = this;
            // indico que aún no está en funcionamiento, faltan parámetros
            InExecution = false;

            //Asignación de un buffer tomando en cuenta por lo menos los 3 sockets por defecto para lectura y escritura iniciales
            // es decir, el tamaño del buffer por operación por el número de conexiónes por el número de sockets iniciales no dará
            // el valor de buffer enorme en bytes, por ejemplo: tamanoBuffer= 1024 * 1000 * 3 =2048000 bytes
            this.bufferManager = new BufferManager(bufferSizePerMessage * numberOfSimultaneousClientConnections * preAvailableOperations, sizeBufferPerRequest);

            //se inicializan los estados de socket necesarios para el número simultaneo de conexiones, para tenerlos listos a usarse como una pila            
            clientStateManager = new ClientStateManager<T>(numberOfSimultaneousClientConnections);

            //se inicializan los estados de socket necesarios para el número simultaneo de conexiones hacia el proveedor, para tenerlos listos a usarse como una pila            
            providerStateManager = new ProviderStateManager<X>(numberOfSimultaneousProviderConnections);

            //Se inicializa el número inicial y maximo de conexiones simultaneas soportadas, será el semáforo quien indique que hay saturación.            
            ClientSemaphoreConnections = new SemaphoreSlim(numberOfSimultaneousClientConnections, numberOfSimultaneousClientConnections);
            ProviderSemaphoreConnections = new SemaphoreSlim(numberOfSimultaneousProviderConnections, numberOfSimultaneousProviderConnections);
        }

        /// <summary>
        /// Sets the buffer size for processing requests.
        /// </summary>
        /// <remarks>This method configures the buffer size used for handling individual requests.  Ensure
        /// that the specified <paramref name="bufferSize"/> is appropriate for the expected workload  to avoid
        /// performance issues.</remarks>
        /// <param name="bufferSize">The size of the buffer, in bytes. Must be a positive integer.</param>
        private void SetBufferSizePerMessage(int bufferSize)
        {
            sizeBufferPerRequest = bufferSize;
        }

        /// <summary>
        /// Initializes and resets the internal collections used to manage clients and providers.
        /// </summary>
        /// <remarks>This method clears and reinitializes the internal data structures, including the list
        /// of clients  pending disconnection. It should be called to ensure the collections are in a clean state before
        /// use.</remarks>
        private void SetClientsAndProvidersLists()
        {
            clientsList = new ConcurrentDictionary<String, T>();
            providersList = new ConcurrentDictionary<String, X>();
            ClientsPendingDisconnectionList = new List<T>();
            ProvidersPendingDisconnectionList = new List<X>();
        }

        /// <summary>
        /// Configures the maximum number of concurrent connections and the connection backlog.
        /// </summary>
        /// <param name="concurrentConexionNumber">The maximum number of simultaneous connections allowed for both clients and providers.  A value of 0
        /// indicates no limit.</param>
        /// <param name="backlog">The maximum number of pending connections that can be queued before being accepted.</param>
        private void SetConcurrentConnections(int concurrentConexionNumber, int backlog)
        {
            numberOfSimultaneousClientConnections = concurrentConexionNumber;
            numberOfSimultaneousProviderConnections = concurrentConexionNumber;
            //Se coloca ilimitado para fines no restrictivos
            maximumConnectionsPerClientIp = 0;
            backLog = backlog;
        }

        /// <summary>
        /// Configures the factories used to create instances of the client, server, and provider states.
        /// </summary>
        /// <remarks>This method initializes the server state using the provided <paramref
        /// name="serverFactory"/>.  If an error occurs during the creation of the server state, an error message is
        /// logged. Ensure that the class used for the server state has a parameterless constructor.</remarks>
        /// <param name="clientFactory">A factory method that creates an instance of the client state. Cannot be <see langword="null"/>.</param>
        /// <param name="serverFactory">A factory method that creates an instance of the server state. Cannot be <see langword="null"/>.</param>
        /// <param name="providerFactory">A factory method that creates an instance of the provider state. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="clientFactory"/>, <paramref name="serverFactory"/>, or <paramref
        /// name="providerFactory"/> is <see langword="null"/>.</exception>
        private void SetStatesFactories(Func<T> clientFactory, Func<S> serverFactory, Func<X> providerFactory)
        {
            this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            this.serverFactory = serverFactory ?? throw new ArgumentNullException(nameof(serverFactory));
            this.providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            try
            {
                this.ServerStateBase = serverFactory();
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error al crear la instancia del servidor, revise que la clase derivada de EstadoDelServidorBase tenga un constructor sin parámetros. ");
                sb.Append(ex.Message);
                sb.Append(" ServidorTransaccional");
                Log(sb.ToString(), LogType.Error);
            }
        }

        /// <summary>
        /// Inicializa el servidor con una pre asignación de buffers reusables y estados de sockets
        /// </summary>
        public void ServerConfiguration(int clientTimeout, int providerTimeout)
        {
            if (clientTimeout <= 0 || providerTimeout <= 0)
            {
                Log("El tiempo de espera del cliente y proveedor debe ser mayor a 0. ", LogType.Error);
                throw new Exception("El tiempo de espera del cliente y proveedor debe ser mayor a 0. ");
            }

            TransactionalServerCore.ServerConfiguration.clientTimeOut = clientTimeout;
            TransactionalServerCore.ServerConfiguration.providerTimeout = providerTimeout;
            SetPerformanceCounters();

            if (!ValidateParametersServer())
            {
                Log(NOTPERMISSION, LogType.Error);
                Environment.Exit(666);
            }

            //Se prepara un buffer suficientemente grande para todas las operaciones y poder reutilizarlo por secciones
            bufferManager.InitializeFullBuffer();
            SetClientStatePool();
            SetProviderStatePool();
        }

        /// <summary>
        /// Initializes and configures a pool of provider state objects for managing socket connections.
        /// </summary>
        /// <remarks>This method pre-allocates a set of provider state objects and their associated
        /// resources, such as  <see cref="SocketAsyncEventArgs"/> instances and buffers, to handle the maximum number
        /// of simultaneous  client connections. Each provider state object is initialized and added to the provider
        /// state pool for  efficient reuse during socket operations.</remarks>
        private void SetProviderStatePool()
        {
            try
            {
                //pre asignar un conjunto de estados de socket para usarlos inmediatamente en cada una
                // de la conexiones simultaneas que se pueden esperar
                for (Int32 i = 0; i < this.numberOfSimultaneousClientConnections; i++)
                {

                    //Ahora genero la pila de estados para el proveedor
                    X estadoDelProveedor = providerFactory();
                    estadoDelProveedor.InitializeProviderStateBase();

                    SocketAsyncEventArgs saeaDeEnvioRecepcionAlProveedor;
                    saeaDeEnvioRecepcionAlProveedor = new SocketAsyncEventArgs();
                    //El manejador de eventos para cada lectura de una peticion del proveedor
                    saeaDeEnvioRecepcionAlProveedor.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveSendOutgoingCallBack);
                    //SocketAsyncEventArgs necesita un objeto con la información de cada proveedor para su administración
                    saeaDeEnvioRecepcionAlProveedor.UserToken = estadoDelProveedor;
                    //Se establece el buffer que se utilizará en la operación de lectura del proveedor en el eventArgDeEnvioRecepcion
                    if (!bufferManager.SetBuffer(saeaDeEnvioRecepcionAlProveedor, estadoDelProveedor.clientStateSource?.UniqueClientId))
                        throw new Exception();
                    //Se establece el socket asincrono de EventArg a utilizar en las operaciones con el proveedor
                    estadoDelProveedor.saeaSendReceive = saeaDeEnvioRecepcionAlProveedor;

                    //Ya con los parametros establecidos para cada operacion, se ingresa en la pila
                    //de estados del proveedor y desde ahi administar su uso en cada petición
                    providerStateManager.AddProviderState(estadoDelProveedor);
                }
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error al inicializar los estados de socket del cliente, verifique que las clases derivadas de ");
                sb.Append(nameof(SetProviderStatePool));
                sb.Append(": ");
                sb.Append(ex.Message);
                Log(sb.ToString(), LogType.Error);
                throw;
            }
        }

        /// <summary>
        /// Initializes and pre-allocates a pool of client state objects and their associated resources for managing
        /// simultaneous client connections.
        /// </summary>
        /// <remarks>This method prepares a set of client state objects, each configured with the
        /// necessary asynchronous socket operation resources, to handle the maximum number of simultaneous client
        /// connections specified by <c>numeroConexionesSimultaneasCliente</c>. Each client state object is initialized
        /// using the provided factory method and added to the client state pool for reuse during client requests.  If
        /// an error occurs during initialization, an error message is logged, and the exception is rethrown.</remarks>
        private void SetClientStatePool()
        {
            try
            {
                //pre asignar un conjunto de estados de socket para usarlos inmediatamente en cada una
                // de la conexiones simultaneas que se pueden esperar
                for (Int32 i = 0; i < this.numberOfSimultaneousClientConnections; i++)
                {
                    T clientState = clientFactory();
                    clientState.InitializeClientStateBase();
                    //objetos para operaciones asincronas en los sockets de los clientes
                    SocketAsyncEventArgs saeaDeEnvioRecepcionCliente;
                    saeaDeEnvioRecepcionCliente = new SocketAsyncEventArgs();
                    //El manejador de eventos para cada lectura de una peticion del cliente
                    saeaDeEnvioRecepcionCliente.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveSendIncomingProcessCallBack);
                    //SocketAsyncEventArgs necesita un objeto con la información de cada cliente para su administración
                    saeaDeEnvioRecepcionCliente.UserToken = clientState;
                    //Se establece el buffer que se utilizará en la operación de lectura del cliente en el eventArgDeRecepcion
                    if (!bufferManager.SetBuffer(saeaDeEnvioRecepcionCliente, clientState.UniqueClientId))
                        throw new Exception();
                    //Se establece el socket asincrono de EventArg a utilizar en la lectura del cliente
                    clientState.saeaOfSendReceive = saeaDeEnvioRecepcionCliente;

                    //Ya con los parametros establecidos para cada operacion, se ingresa en la pila
                    //de estados del cliente y desde ahi administar su uso en cada petición
                    clientStateManager.AddClientState(clientState);
                }
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error al inicializar los estados de socket del cliente, verifique que las clases derivadas de ");
                sb.Append(nameof(T));
                sb.Append(" ");
                sb.Append(ex.Message);
                sb.Append(" ConfigInicioServidor ");
                Log(sb.ToString(), LogType.Error);
                throw;
            }
        }

        /// <summary>
        /// Initializes and increments the performance counter for tracking incoming connections.
        /// </summary>
        /// <remarks>This method attempts to create and increment a performance counter named 
        /// "conexionesEntrantesUserver" within the "TN" category. If the performance counter  or category does not
        /// exist, an exception is logged and rethrown.</remarks>
        private void SetPerformanceCounters()
        {
            try
            {
                incommigConnectionsPerformanceCounter = new PerformanceCounter("TN", "conexionesEntrantesUserver", false);
                incommigConnectionsPerformanceCounter.IncrementBy(1);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error al crear el performance counter, verifique que exista la categoría TN y el contador conexionesEntrantesUserver. ");
                sb.Append(ex.Message);
                sb.Append(" ");
                sb.Append(ex.StackTrace);
                sb.Append(" ConfigInicioServidor ");
                Log(sb.ToString(), LogType.Error);
                throw;
            }
        }

        /// <summary>
        /// Initializes the socket pool with the configured number of simultaneous connections.
        /// </summary>
        /// <remarks>This method sets up the socket pool for managing connections. If an error occurs
        /// during initialization,  the exception is logged and rethrown for the caller to handle.</remarks>
        private void SetSocketPool()
        {
            try
            {
                socketPool = new SocketPool(numberOfSimultaneousProviderConnections);
            }
            catch (Exception ex)
            {
                Log("Error al inicializar el pool de sockets para el proveedor. " + ex.Message + " ConfigInicioServidor ", LogType.Error);
                throw;
            }
        }

        /// <summary>
        /// Retrieves an <see cref="IPEndPoint"/> from the provider's list of ports, cycling through the list in a
        /// thread-safe manner.
        /// </summary>
        /// <remarks>This method ensures thread safety when accessing the list of ports by using a
        /// synchronization mechanism. If the list of ports cannot be accessed within the specified timeout, the first
        /// port in the list is used as a fallback. The method also increments the port counter, cycling back to the
        /// beginning of the list when the end is reached.</remarks>
        /// <returns>An <see cref="IPEndPoint"/> representing the provider's IP address and the selected port.</returns>
        private IPEndPoint GetIPEndPointFromProviderPortsList()
        {
            IPAddress iPAddress = IPAddress.Parse(ProviderIp);
            IPEndPoint endPointProveedor;
            bool isLock = Monitor.TryEnter(ProviderPortsList, milisecondsTimeOutLock);
            if (isLock)
            {
                try
                {
                    if (portCounter == 0)
                    {
                        endPointProveedor = new IPEndPoint(iPAddress, ProviderPortsList.First());
                    }
                    else
                    {
                        endPointProveedor = new IPEndPoint(iPAddress, ProviderPortsList[portCounter - 1]);
                    }
                }
                catch
                {
                    endPointProveedor = new IPEndPoint(iPAddress, ProviderPortsList.First());
                }
                finally
                {
                    if (isLock)
                        Monitor.Exit(ProviderPortsList);
                }
            }
            else
            {
                endPointProveedor = new IPEndPoint(iPAddress, ProviderPortsList.First());
            }

            if (portCounter == ProviderPortsList.Count)
            {
                Interlocked.Exchange(ref portCounter, 0);
            }
            else
            {
                Interlocked.Increment(ref portCounter);
            }
            return endPointProveedor;
        }

        /// <summary>
        /// Se inicia el servidor de manera que esté escuchando solicitudes de conexión entrantes.
        /// </summary>
        /// <param name="localPort">Puerto de escucha del servidor</param>
        /// <param name="providerIp">Ip del servidor del proveedor</param>
        /// <param name="providerPortsList">Puertos del proveedor</param>
        /// <param name="testMode">Modo pruebas, no guarda las transacciones y solo responde con el eco de los datos de entrada en forma exitosa,, si está activo
        ///  se omite el parámetro routerMode</param>
        /// <param name="routerMode">Indicador de que el servidor tendrá la función de enviar mensajes a otro proveedor, de lo contrario solo se usuaria como servidor de procesamiento local</param>
        public void Start(Int32 localPort, string providerIp, List<int> providerPortsList, bool testMode, bool routerMode)
        {
            //Se inicializa la bandera de que no hay ningún cliente pendiente por desconectar
            disconnecting = false;
            TransactionalServerCore.ServerConfiguration.testMode = testMode;
            TransactionalServerCore.ServerConfiguration.routerMode = routerMode;
            //De acuerdo a las buenas practicas de manejo de operaciones asincronas, se debe ANUNCIAR el inicio
            //de un trabajo asincrono para ir controlando su avance por eventos si fuera necesario
            ServerStateBase.OnStart();

            this.ProviderIp = providerIp;
            this.ProviderPortsList = providerPortsList;
            SetSocketPool();

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, localPort);

            // se crea el socket que se utilizará de escucha para las conexiones entrantes
            mainListenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // se asocia con el puerto de escucha el socket de escucha
            this.mainListenSocket.Bind(localEndPoint);

            localIp = mainListenSocket.LocalEndPoint.ToString().Split(':')[0];

            // se inicia la escucha de conexiones con un backlog de 100 conexiones
            this.mainListenSocket.Listen(backLog);

            portCounter = providerPortsList.Count;

            // Se indica al sistema que se empiezan a aceptar conexiones, se envía una referencia a null para que se indique que es la primera vez
            this.StartAccepting(null);
            InExecution = true;
        }



        #region ProcesoDePeticionesCliente

        /// <summary>
        /// Se inicia la operación de aceptar solicitudes por parte de un cliente
        /// </summary>
        /// <param name="saeaConnectionAccept">Objeto que se utilizará en cada aceptación de una solicitud</param>
        private void StartAccepting(SocketAsyncEventArgs saeaConnectionAccept)
        {
            // de ser null quiere decir que no hay objeto instanciado y debe crearse desde cero. Es por el primer proceso de escucha
            if (saeaConnectionAccept == null)
            {
                saeaConnectionAccept = new SocketAsyncEventArgs();
                saeaConnectionAccept.Completed += new EventHandler<SocketAsyncEventArgs>(StartAcceptingCallBack);
            }
            else
            {
                // si ya existe instancia, se limpia el socket para trabajo. Esto se utiliza cuando se vuelve a colocar en escucha
                saeaConnectionAccept.AcceptSocket = null;
            }
            // se comprueba el semáforo que nos indica que se tiene recursos para aceptar la conexión
            ClientSemaphoreConnections.Wait();
            IncrementPerformanceCounterIn();

            try
            {

                // se comienza asincronamente el proceso de aceptación y mediante un evento manual 
                // se verifica que haya sido exitoso. Cuando el proceso asincrono es exitoso devuelve false
                bool seHizoAsync = mainListenSocket.AcceptAsync(saeaConnectionAccept);
                if (!seHizoAsync)
                    // se manda llamar a la función que procesa la solicitud, 
                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                    // en su evento callback
                    StartAcceptingCallBack(mainListenSocket, saeaConnectionAccept);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error al iniciar las aceptaciones, verifique que el puerto no esté en uso. ");
                sb.Append(ex.Message);
                sb.Append(" ");
                sb.Append(nameof(StartAccepting));
                Log(sb.ToString(), LogType.Error);
                // se hace un último intento para volver a iniciar el servidor por si el error fue una excepción al empezar la aceptación
                StartAccepting(saeaConnectionAccept);
            }
        }

        /// <summary>
        /// Safely increments the performance counter for incoming connections.
        /// </summary>
        /// <remarks>This method attempts to increment the performance counter in a thread-safe manner by
        /// using a synchronization mechanism. If the lock cannot be acquired within the  specified timeout, the
        /// increment operation is skipped. Exceptions during the operation  are caught and suppressed, as they are not
        /// considered critical.</remarks>
        private void IncrementPerformanceCounterIn()
        {
            bool isLock = false;
            try
            {
                isLock = Monitor.TryEnter(incommigConnectionsPerformanceCounter, milisecondsTimeOutLock);
                if (isLock)
                {
                    incommigConnectionsPerformanceCounter.IncrementBy(1);
                }
            }
            catch (Exception)
            {
                //Solo se coloca para evitar el error pero no genera una tarea crítica
            }
            finally
            {
                if (isLock)
                    Monitor.Exit(incommigConnectionsPerformanceCounter);
            }
        }

        /// <summary>
        /// Procesa la solicitud por medio de socket principal de escucha
        /// </summary>
        /// <param name="sender">Objeto que se tomará como quien dispara el evento principal</param>
        /// <param name="saea">SocketAsyncEventArg asociado al proceso asincrono.</param>
        private void StartAcceptingCallBack(object sender, SocketAsyncEventArgs saea)
        {
            // se valida que existan errores registrados
            if (saea.SocketError != SocketError.Success)
            {
                // se comprueba que no hay un proceso de cierre del programa o desconexión en curso para no dejar sockets en segundo plano
                if (disconnecting)
                {
                    var sb = new StringBuilder();
                    sb.Append("Socket de escucha desconectado porque el programa principal se está cerrando, ");
                    sb.Append(nameof(StartAcceptingCallBack));
                    Log(sb.ToString(), LogType.Error);
                    // se le indica al semáforo que puede permitir la siguiente conexion....al final se cerrará pero no se bloqueará el proceso
                    ClientSemaphoreConnections.Release();
                    return;
                }
                // se le indica al semáforo que puede asignar otra conexion
                ClientSemaphoreConnections.Release();
                // aquí el truco en volver a iniciar el proceso de aceptación de solicitudes con el mismo
                // saea que tiene el cliente si hubo un error para re utilizarlo
                StartAccepting(saea);
                return;
            }

            // si el cliente pasó todas las Utileria entonces se le asigna ya un estado de trabajo del pool de estados de cliente listo con todas sus propiedes
            T clientState = clientStateManager.GetClientState();
            if (clientState == null || !clientState.SetClientId())
            {
                Log("No se pudo obtener un estado de cliente del pool o asignar su UniqueClientId, se cerrará la conexión. " + nameof(StartAcceptingCallBack), LogType.Error);
                ClientSocketClose(clientState);
                ClientSemaphoreConnections.Release();
                // coloco nuevamente el socket en proceso de aceptación con el mismo saea para un reintento de conexión
                StartAccepting(saea);
                return;
            }
            // debo colocar la referencia del proceso principal donde genero el estado del cliente para tenerlo como referencia de retorno
            clientState.SetMainSocketReference(this);
            // Del SAEA de aceptación de conexión, se recupera el socket para asignarlo al estado del cliente obtenido del pool de estados
            clientState.SocketOfWork = saea.AcceptSocket;
            //indico que el estado de cliente está en uso incluyendo el saea
            clientState.SetInUse();

            //Se establece el buffer que se utilizará en la operación de lectura del cliente en el eventArgDeRecepcion
            if (clientState.saeaOfSendReceive.Buffer == null)
            {
                if (!bufferManager.SetBuffer(clientState.saeaOfSendReceive, clientState.UniqueClientId))
                {
                    ClientSocketClose(clientState);
                    ClientSemaphoreConnections.Release();
                    StartAccepting(saea);
                    return;
                }
            }

            //  de la misma forma se ingresa la ip y puerto del cliente que se aceptó
            clientState.IpClient = (saea.AcceptSocket.RemoteEndPoint as IPEndPoint).Address.ToString();
            clientState.PortClient = (saea.AcceptSocket.RemoteEndPoint as IPEndPoint).Port;

            // con estas instrucciones puedo controlar las acciones en cada fase del proceso de recepción y envío de ser necesario
            ServerStateBase.OnAccept(clientState);
            if (!AddClientToClientList(clientState))
            {
                // si no puedo ingresarlo en la lista de clientes debo rechazarlo porque no tendría control para manipularlo en un futuro                
                ClientSocketClose(clientState);
                ClientSemaphoreConnections.Release();
                // coloco nuevamente el socket en proceso de aceptación con el mismo saea para un reintento de conexión
                StartAccepting(saea);
                return;
            }

            // se inicia la recepción de datos del cliente si es que sigue conectado dicho cliente  
            if (clientState.SocketOfWork.Connected)
            {
                try
                {
                    // se ingresa la configuración del buffer para la recepción del mensaje                    
                    clientState.saeaOfSendReceive.SetBuffer(clientState.saeaOfSendReceive.Offset, sizeBufferPerRequest);
                    // se procede a la recepción asincrona del mensaje,el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                    bool seHizoAsync = clientState.SocketOfWork.ReceiveAsync(clientState.saeaOfSendReceive);
                    if (!seHizoAsync)
                        // se llama a la función que completa el flujo de envío, 
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        ReceiveSendIncomingProcessCallBack(clientState.SocketOfWork, clientState.saeaOfSendReceive);
                }
                catch (Exception ex)
                {
                    var sb = new StringBuilder();
                    sb.Append("Error al intentar recibir el mensaje del cliente, se cerrará la conexión. ");
                    sb.Append(nameof(StartAcceptingCallBack));
                    sb.Append(", cliente: ");
                    sb.Append(clientState.UniqueClientId);
                    sb.Append(", ");
                    sb.Append(ex.Message);
                    Log(sb.ToString(), LogType.Error);
                    ClientSocketClose(clientState);
                }
            }
            else
            {
                var sb = new StringBuilder();
                sb.Append("No se aceptó la conexión porque el socket no está conectado, ");
                sb.Append(nameof(StartAcceptingCallBack));
                sb.Append(", cliente: ");
                sb.Append(clientState.UniqueClientId);
                sb.Append(", ");
                sb.Append(saea.SocketError.ToString());
                Log(sb.ToString(), LogType.Warning);
            }

            // se indica que puede aceptar más solicitudes con el mismo saea que es el principal
            this.StartAccepting(saea);
        }

        /// <summary>
        /// Attempts to add a client to the client list in a thread-safe manner.
        /// </summary>
        /// <remarks>This method uses a synchronization mechanism to ensure thread-safe access to the
        /// client list.  If the client list already contains a client with the same unique identifier, the client is
        /// not added again.</remarks>
        /// <param name="clientState">The client state object to be added. The object must have a unique identifier.</param>
        /// <returns><see langword="true"/> if the client was successfully added to the list; otherwise, <see langword="false"/>.</returns>
        private bool AddClientToClientList(T clientState)
        {
            // se ingresa el cliente a la lista de clientes
            // Monitor proporciona un mecanismo que sincroniza el acceso a datos entre hilos
            bool isLock = Monitor.TryEnter(clientsList, milisecondsTimeOutLock);
            try
            {
                if (isLock)
                {
                    if (!clientsList.ContainsKey(clientState.UniqueClientId))
                    {
                        clientsList.TryAdd(clientState.UniqueClientId, clientState);
                    }
                }
                else
                {
                    throw new Exception();
                }
                return true;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error al intentar ingresar el cliente a la lista de clientes, se cerrará la conexión, ");
                sb.Append(nameof(AddClientToClientList));
                sb.Append(", cliente: ");
                sb.Append(clientState.UniqueClientId);
                sb.Append(", ");
                sb.Append(ex.Message);
                Log(sb.ToString(), LogType.Error);
                return false;
            }
            finally
            {
                if (isLock)
                    Monitor.Exit(clientsList);
            }
        }

        /// <summary>
        /// Operación de callBack que se llama cuando se envía o se recibe de un socket de asincrono para completar la operación
        /// </summary>
        /// <param name="sender">Objeto principal para devolver la llamada</param>
        /// <param name="saea">SocketAsyncEventArg asociado a la operación de envío o recepción</param>        
        private void ReceiveSendIncomingProcessCallBack(object sender, SocketAsyncEventArgs saea)
        {
            var sb = new StringBuilder();
            // obtengo el estado del socket
            // se comprueba que el estado haya sido obtenido correctamente
            if (!(saea.UserToken is T clientState))
            {
                sb.Append("No se pudo obtener el estado del cliente, ");
                sb.Append(nameof(ReceiveSendIncomingProcessCallBack));
                Log(sb.ToString(), LogType.Error);
                return;
            }

            //Aquí ya puedo liberar el SAEA y el estado del cliente dado que concluyó si flujo callback y no está en espera sin completarse. Con esta marca puedo liberar el buffer del saea y meterlo en el pool al terminar sin tener errores
            clientState.SetFree();

            // se determina que operación se está llevando a cabo para indicar que manejador de eventos se ejecuta
            switch (saea.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    if (saea.SocketError == SocketError.Success)
                    {
                        // se comprueba que exista información y que el socket no refleje errores
                        if (saea.BytesTransferred > 0)
                        {
                            // se procesa la solicitud
                            ReceivingProcess(clientState);
                        }
                        else
                        {
                            //EscribirLog("No hay datos que recibir", tipoLog.ALERTA);
                            // si no hay datos por X razón, se cierra el cliente porque puede perdurar indefinidamente la conexión                            
                            ClientSocketClose(clientState);
                        }
                    }
                    else
                    {
                        sb.Append("Error en el proceso de recepción, socket no conectado correctamente, cliente:");
                        sb.Append(clientState.UniqueClientId);
                        sb.Append(", ");
                        sb.Append(saea.SocketError.ToString());
                        Log(sb.ToString(), LogType.Warning);
                        //se cierra el cliente porque puede perdurar indefinidamente la conexión
                        ClientSocketClose(clientState);
                    }
                    break;
                case SocketAsyncOperation.Send:
                    //indico que estaré enviando algo al cliente para que otro proceso con la misma conexión no quiera enviar algo al mismo tiempo
                    clientState.waitSendingEvent.Set();

                    // se comprueba que no hay errores con el socket
                    if (saea.SocketError == SocketError.Success)
                    {
                        //Intento colocar el socket de nuevo en escucha por si el cliente envía otra trama con la misma conexión
                        ReceiveIncomingProcessCiclicToClient(clientState);
                    }
                    else
                    {
                        sb.Append("Error en el proceso de envío, socket no conectado correctamente, cliente:");
                        sb.Append(clientState.UniqueClientId);
                        Log(sb.ToString(), LogType.Warning);
                        // si no hay datos por X razón, se cierra el cliente porque puede perdurar indefinidamente la conexión                        
                        ClientSocketClose(clientState);
                    }
                    break;
                default:
                    // se da por errores de TCP/IP en alguna intermitencia
                    sb.Append("La ultima operación no se detecto como de recepcion o envío, RecepcionEnvioEntranteCallBack, ");
                    sb.Append(clientState.UniqueClientId);
                    sb.Append(", ");
                    sb.Append(saea.SocketError.ToString());
                    sb.Append(", ");
                    sb.Append(saea.LastOperation.ToString());
                    Log(sb.ToString(), LogType.Error);
                    ClientSocketClose(clientState);
                    break;
            }
        }

        /// <summary>
        /// Este método se invoca cuando la operación de recepción asincrona se completa y si el cliente
        /// cierra la conexión el socket también se cierra y se libera
        /// </summary>
        /// <param name="clientState">Objeto que tiene la información y socket de trabajo del cliente</param>
        private void ReceivingProcess(T clientState)
        {
            // Actualizo el contador de mensajes por segundo
            MessagePerSecond();

            clientState.waitSendingEvent.WaitOne();

            // se ingresa el cliente a la lista de clientes
            if (!SetStartDateTrxReceive(clientState))
            {
                clientState.responseCode = (int)CodigosRespuesta.ErrorProceso;
                clientState.authorizationCode = 0;
                ResponseToClient(clientState);
                return;
            }

            // se obtiene el objeto que tiene la información del socket y el buffer
            SocketAsyncEventArgs saeaOfSendReceive = clientState.saeaOfSendReceive;
            string message = GetMessageFromClient(clientState, saeaOfSendReceive);
            if (string.IsNullOrEmpty(message))
            {
                ClientSocketClose(clientState);
                return;
            }
            SetTotalBytesCounter(saeaOfSendReceive.BytesTransferred);

            // el mensaje recibido llevará un proceso, que no debe ser llevado por el core, se coloca en la función virtual
            // para que se consuma en otra capa, se procese y se entregue una respuesta

            // bloqueo los procesos sobre este mismo cliente hasta no terminar con esta petición para no tener revolturas de mensajes
            clientState.waitSendingEvent.Reset();
            try
            {
                // aquí se debe realizar lo necesario con la trama entrante para preparar la trama al proveedor en la variable tramaEnvioProveedor
                clientState.ProcessMessage(message);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error en ");
                sb.Append(nameof(ReceivingProcess));
                sb.Append(", ");
                sb.Append(ex.Message);
                sb.Append(" Error al procesar la trama, se descarta el mensaje del cliente: ");
                sb.Append(clientState.UniqueClientId);
                Log(sb.ToString(), LogType.Error);
            }

            //Verifico si se venció el TO mientras procesaba la trama
            if (responseDueToSaturation)
            {
                var sb = new StringBuilder();
                sb.Append("Respuesta forzada por saturación de componentes para el cliente ");
                sb.Append(clientState.UniqueClientId.ToString());
                sb.Append(". No se enviará la solicitud al proveedor.");
                Log(sb.ToString(), LogType.Warning);
                clientState.responseCode = (int)CodigosRespuesta.ErrorProcesoSockets;
                clientState.authorizationCode = 0;
                ResponseToClient(clientState);
                return;
            }

            if (ValidateTimeOutExpired(clientState))
            {
                var sb = new StringBuilder();
                sb.Append("Se venció el TimeOut para el cliente ");
                sb.Append(clientState.UniqueClientId.ToString());
                sb.Append(", durante el procesamiento de la trama. ");
                sb.Append("No se enviará la solicitud al proveedor.");
                Log(sb.ToString(), LogType.Warning);
                clientState.responseCode = (int)CodigosRespuesta.TimeOutInterno;
                clientState.authorizationCode = 0;
                ResponseToClient(clientState);
                return;
            }

            //Verifico que sea una consulta y que no haya sido con código 71, porque si tuviera ese código, tengo que formar el 220 al proveedor
            if (clientState.isQuery && clientState.responseCode != (int)CodigosRespuesta.SinRespuestaCarrier)
            {
                //regresa la respuesta de la base
                ResponseToClient(clientState);
                return;
            }
            else if (clientState.isQuery && clientState.responseCode == (int)CodigosRespuesta.SinRespuestaCarrier)
            {
                //Fue 71, entonces tengo que dejar seguir el flujo y solamente cuando guarde la respuesta del proveedor, actualizar el registro
                clientState.responseCode = (int)CodigosRespuesta.TransaccionExitosa;
                clientState.isQuery = false;
            }

            try
            {
                // cuando haya terminado la clase de procesar la trama, se debe evaluar su éxito para enviar la solicitud al proveedor
                if (clientState.responseCode == (int)CodigosRespuesta.TransaccionExitosa)
                {
                    if (!TransactionalServerCore.ServerConfiguration.testMode)
                    {
                        if (TransactionalServerCore.ServerConfiguration.routerMode)
                        {
                            //Verifico nuevamente si se venció el TO mientras procesaba la trama
                            if (ValidateTimeOutExpired(clientState))
                            {
                                var sb = new StringBuilder();
                                sb.Append("Se venció el TimeOut para el cliente ");
                                sb.Append(clientState.UniqueClientId.ToString());
                                sb.Append(", durante el procesamiento de la trama. ");
                                sb.Append("No se enviará la solicitud al proveedor.");
                                Log(sb.ToString(), LogType.Warning);
                                clientState.responseCode = (int)CodigosRespuesta.TimeOutInterno;
                                clientState.authorizationCode = 0;
                                ResponseToClient(clientState);
                                return;
                            }
                            StartProviderProcess(clientState);
                        }
                        else
                        {
                            ResponseToClient(clientState);
                        }
                    }
                    else
                    {
                        ResponseToClient(clientState);
                    }
                }
                // si el código de respuesta es 30(error en el formato) o 50 (Error en algún paso de evaluar la mensajería),
                // se debe responder al cliente, de lo contrario si es un codigo de los anteriores, no se puede responder porque no se tienen confianza en los datos
                else if (clientState.responseCode != (int)CodigosRespuesta.ErrorFormato && clientState.responseCode != (int)CodigosRespuesta.ErrorProceso)
                {
                    var sb = new StringBuilder();
                    sb.Append("Error en el proceso de lectura del mensaje entrante, cliente ");
                    sb.Append(clientState.UniqueClientId);
                    Log(sb.ToString(), LogType.Warning);
                    ResponseToClient(clientState);
                }
                else
                {
                    ClientSocketClose(clientState);
                }
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error en ");
                sb.Append(nameof(ReceivingProcess));
                sb.Append(", ");
                sb.Append(ex.Message);
                sb.Append(", ");
                sb.Append(ex.StackTrace);
                sb.Append(" Error en el procesamiento de la trama, se cierra la conexión del cliente ");
                sb.Append(clientState.UniqueClientId);
                Log(sb.ToString(), LogType.Error);
                ClientSocketClose(clientState);
            }
        }

        /// <summary>
        /// Updates the count of messages received per second.
        /// </summary>
        /// <remarks>This method calculates the number of messages received in the current second and
        /// updates the  corresponding count. It ensures thread safety by locking access to the shared state.</remarks>
        private void MessagePerSecond()
        {
            try
            {
                lock (lockMessages)
                {
                    //messagesReceivedPerSecond++;
                    //var ahora = DateTime.UtcNow;
                    //if ((ahora - lastSecond).TotalSeconds >= 1)
                    //{
                    //    messagesPerSecond = messagesReceivedPerSecond;
                    //    messagesReceivedPerSecond = 0;
                    //    lastSecond = ahora;
                    //}
                    //else
                    //{
                    //    messagesReceivedPerSecond++;
                    //    messagesPerSecond = messagesReceivedPerSecond;
                    //}
                    var dateTimeNow = DateTime.UtcNow;
                    if ((dateTimeNow - lastSecond).TotalSeconds >= 1)
                    {
                        messagesPerSecond = messagesReceivedPerSecond;
                        messagesReceivedPerSecond = 1;
                        lastSecond = dateTimeNow;
                    }
                    else
                    {
                        messagesReceivedPerSecond++;
                    }
                }
            }
            catch (Exception)
            {
                // cualquier error se ignora porque no es crítico
            }
        }

        /// <summary>
        /// Updates the total bytes counter with the specified number of bytes transferred.
        /// </summary>
        /// <remarks>This method ensures thread-safe updates to the total bytes counter using interlocked
        /// operations. If the total bytes counter reaches the predefined limit, it is reset to zero.</remarks>
        /// <param name="bytesTransferred">The number of bytes to add to the total bytes counter. Must be a non-negative value.</param>
        internal void SetTotalBytesCounter(int bytesTransferred)
        {
            try
            {
                // incrementa el contador de bytes totales recibidos para tener estadísticas nada más
                // debido a que la variable está compartida entre varios procesos, se utiliza interlocked que ayuda a que no se revuelvan
                if (totalBytesRead == TransactionalServerCore.ServerConfiguration.LIMITE_BYTES_CONTADOR)
                {
                    Interlocked.Exchange(ref this.totalBytesRead, 0);
                }
                else
                {
                    Interlocked.Add(ref this.totalBytesRead, bytesTransferred);
                }
            }
            catch (Exception)
            {
                // cualquier error se ignora porque no es crítico
            }

        }

        /// <summary>
        /// Retrieves and decodes a message received from the specified client.
        /// </summary>
        /// <remarks>This method extracts the received bytes from the provided <paramref
        /// name="saeaDeEnvioRecepcion"/>, decodes them using ASCII encoding,  and logs the message along with the
        /// client's unique identifier. If the message includes a header, it is processed accordingly.</remarks>
        /// <param name="clientState">The state object representing the client from which the message was received.</param>
        /// <param name="saeaDeEnvioRecepcion">The <see cref="SocketAsyncEventArgs"/> instance containing the data received from the client.</param>
        /// <returns>The decoded message as a <see cref="string"/>. Returns an empty string if an error occurs during processing.</returns>
        private string GetMessageFromClient(T clientState, SocketAsyncEventArgs saeaDeEnvioRecepcion)
        {
            try
            {
                // se obtienen los bytes que han sido recibidos
                Int32 bytesTransferred = saeaDeEnvioRecepcion.BytesTransferred;

                // se obtiene el mensaje y se decodifica para entenderlo
                string message = Encoding.ASCII.GetString(saeaDeEnvioRecepcion.Buffer, saeaDeEnvioRecepcion.Offset, bytesTransferred);
                if ((message.Trim().Length > 4) && int.TryParse(message.Trim().Substring(0, 2), out int encabezado) == true)
                {
                    var sb = new StringBuilder();
                    sb.Append("Mensaje recibido: ");
                    sb.Append(message.Trim());
                    sb.Append(" del cliente: ");
                    sb.Append(clientState.UniqueClientId);
                    Log(sb.ToString(), LogType.Info);
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.Append("Mensaje recibido: ");
                    sb.Append(message.Trim().Substring(2));
                    sb.Append(" del cliente: ");
                    sb.Append(clientState.UniqueClientId);
                    Log(sb.ToString(), LogType.Info);
                }
                return message;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append(ex.Message);
                sb.Append(" Error al identificar si tiene encabezado el mensaje recibido, se intenta escribir pero se descarta ");
                sb.Append(" del cliente: ");
                sb.Append(clientState.UniqueClientId);
                Log(sb.ToString(), LogType.Error);
                return string.Empty;
            }
        }

        /// <summary>
        /// Updates the transaction start time for the specified client and logs the operation.
        /// </summary>
        /// <remarks>This method attempts to acquire a lock on the client's transaction start time to
        /// ensure thread safety while updating it. If the lock cannot be acquired within the specified timeout, an
        /// exception is thrown and logged. The method logs both the updated transaction start time and any errors
        /// encountered during the operation.</remarks>
        /// <param name="clientState">The client state object containing the unique client identifier and other transaction-related data.</param>
        private bool SetStartDateTrxReceive(T clientState)
        {
            bool isLock = false;
            try
            {
                //Para ir midiendo el TO por cada recepción
                isLock = Monitor.TryEnter(clientState, milisecondsTimeOutLock);
                if (isLock)
                {
                    clientState.StartDateTrx = DateTime.Now;
                    return true;
                }
                else
                {
                    throw new Exception("No se pudo bloquear el proceso para establecer la fecha de inicio de la transacción por lo que no se debe continuar con el proceso");
                }
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error en ");
                sb.Append(nameof(SetStartDateTrxReceive));
                sb.Append(", ");
                sb.Append(ex.Message);
                sb.Append(" para el cliente: ");
                sb.Append(clientState.UniqueClientId);
                Log(sb.ToString(), LogType.Error);
                return false;
            }
            finally
            {
                if (isLock)
                    Monitor.Exit(clientState);
            }
        }

        /// <summary>
        /// Función que entrega una respuesta al cliente por medio del socket de conexión
        /// </summary>
        /// <param name="clientState">Estado del cliente con los valores de retorno</param>
        private void ResponseToClient(T clientState)
        {
            if (clientState == null || clientState.inUse == 1)
            {
                return;
            }

            // trato de obtener la trama que se le responderá al cliente
            clientState.GetResponseMessage();

            // Si ya se cuenta con una respuesta(s) para el cliente
            if (clientState.messageResponse != "")
            {
                if (!TransactionalServerCore.ServerConfiguration.testMode)
                    clientState.UpdateTransaction();

                string responseMessage = GetResponseMessage(clientState);
                int bytesCounter;
                if (!GetBytesCounter(clientState, responseMessage, out bytesCounter))
                {
                    ClientSocketClose(clientState);
                    return;
                }

                try
                {
                    // Se solicita el espacio de buffer para los bytes que se van a enviar                    
                    clientState.saeaOfSendReceive.SetBuffer(clientState.saeaOfSendReceive.Offset, bytesCounter);
                }
                catch (Exception ex)
                {
                    var sb = new StringBuilder();
                    sb.Append("Error en ");
                    sb.Append(nameof(ResponseToClient));
                    sb.Append(". ");
                    sb.Append(ex.Message);
                    sb.Append(". Asignando buffer para la respuesta al cliente ");
                    sb.Append(clientState.UniqueClientId);
                    Log(sb.ToString(), LogType.Error);
                    ClientSocketClose(clientState);
                    return;
                }

                try
                {
                    clientState.SetInUse();
                    // se envía asincronamente por medio del socket copia de recepción que es
                    // con el que se está trabajando en esta operación, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                    bool seHizoAsync = clientState.SocketOfWork.SendAsync(clientState.saeaOfSendReceive);
                    if (!seHizoAsync)
                        // Si se tiene una respuesta False de que el proceso está pendiente, se completa el flujo,
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        ReceiveSendIncomingProcessCallBack(clientState.SocketOfWork, clientState.saeaOfSendReceive);
                }
                catch (Exception ex)
                {
                    var sb = new StringBuilder();
                    sb.Append("Error en ");
                    sb.Append(nameof(ResponseToClient));
                    sb.Append(". ");
                    sb.Append(ex.Message);
                    sb.Append(", enviando la respuesta al cliente ");
                    sb.Append(clientState.UniqueClientId);
                    Log(sb.ToString(), LogType.Error);
                    ClientSocketClose(clientState);
                    return;
                }
            }
            else  // Si el proceso no tuvo una respuesta o se descartó por error, se procede a volver a escuchar para recibir la siguiente trama del mismo cliente
            {
                if (clientState.SocketOfWork.Connected)
                {
                    try
                    {
                        // se solicita el espacio de buffer para la recepción del mensaje
                        clientState.saeaOfSendReceive.SetBuffer(clientState.saeaOfSendReceive.Offset, sizeBufferPerRequest);
                        // se solicita un proceso de recepción asincrona, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                        // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                        bool seHizoAsync = clientState.SocketOfWork.ReceiveAsync(clientState.saeaOfSendReceive);
                        if (!seHizoAsync)
                            // si el evento indica que el proceso asincrono está pendiente, se completa el flujo,
                            // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                            // en su evento callback
                            ReceiveSendIncomingProcessCallBack(clientState.SocketOfWork, clientState.saeaOfSendReceive);
                    }
                    catch (Exception ex)
                    {
                        var sb = new StringBuilder();
                        sb.Append("Error en ");
                        sb.Append(nameof(ResponseToClient));
                        sb.Append(". ");
                        sb.Append(ex.Message);
                        sb.Append(". Al intentar recibir el mensaje del cliente, se cerrará la conexión, cliente ");
                        sb.Append(clientState.UniqueClientId);
                        Log(sb.ToString(), LogType.Error);
                        ClientSocketClose(clientState);
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to calculate the number of bytes required to encode the specified response message and determines
        /// whether it fits within the allocated buffer size.
        /// </summary>
        /// <remarks>If the response message exceeds the buffer size, the method logs a warning and
        /// returns <see langword="false"/>.</remarks>
        /// <param name="clientState">The state object representing the client, which contains the buffer and offset for encoding.</param>
        /// <param name="responseMessage">The response message to be encoded into the buffer.</param>
        /// <param name="bytesCounter">When this method returns, contains the number of bytes required to encode the response message, or 0 if the
        /// operation fails.</param>
        /// <returns><see langword="true"/> if the response message fits within the allocated buffer size;  otherwise, <see
        /// langword="false"/>.</returns>
        private bool GetBytesCounter(T clientState, string responseMessage, out int bytesCounter)
        {
            try
            {
                // se obtiene la cantidad de bytes de la trama completa
                bytesCounter = Encoding.ASCII.GetBytes(responseMessage, 0, responseMessage.Length, clientState.saeaOfSendReceive.Buffer, clientState.saeaOfSendReceive.Offset);
                // si el número de bytes es mayor al buffer que se tiene destinado a la recepción, no se puede proceder, no es válido el mensaje
                if (bytesCounter > sizeBufferPerRequest)
                {
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                var sb = new StringBuilder();
                sb.Append("La respuesta es más grande que el buffer asignado, no se puede procesar, cliente ");
                sb.Append(clientState.UniqueClientId);
                Log(sb.ToString(), LogType.Warning);
                bytesCounter = 0;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Retrieves the response message to be sent to the client.
        /// </summary>
        /// <remarks>This method processes the response message contained in the <paramref
        /// name="clientState"/> object and logs relevant information about the message and the client. If an error
        /// occurs during processing, the exception details are logged, but the original response message is still
        /// returned.</remarks>
        /// <param name="clientState">The client state object containing the response data and client-specific information.</param>
        /// <returns>The response message as a string, which will be sent to the client.</returns>
        private static string GetResponseMessage(T clientState)
        {
            // se obtiene el mensaje de respuesta que se enviará cliente
            string responseMessage = clientState.messageResponse;
            try
            {
                if (int.TryParse(responseMessage.Substring(0, 2), out int encabezado))
                {
                    var sb = new StringBuilder();
                    sb.Append("Mensaje de respuesta: ");
                    sb.Append(responseMessage);
                    sb.Append(" al cliente ");
                    sb.Append(clientState.UniqueClientId);
                    Log(sb.ToString(), LogType.Info);
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.Append("Mensaje de respuesta: ");
                    sb.Append(responseMessage.Substring(2));
                    sb.Append(" al cliente ");
                    sb.Append(clientState.UniqueClientId);
                    Log(sb.ToString(), LogType.Info);
                }
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error en ");
                sb.Append(nameof(GetResponseMessage));
                sb.Append(", ");
                sb.Append(ex.Message);
                sb.Append(" Error al identificar si tiene encabezado el mensaje de respuesta, se intenta escribir pero se descarta ");
                sb.Append(responseMessage);
                sb.Append(" al cliente ");
                sb.Append(clientState.UniqueClientId);
                Log(sb.ToString(), LogType.Info);
            }

            return responseMessage;
        }

        /// <summary>
        /// Función callback que se utiliza cuando en un proceso ciclico de envio y recepción
        /// </summary>
        /// <param name="clientState">Objeto con la información y socket de trabajo de cliente</param>
        private void ReceiveIncomingProcessCiclicToClient(T clientState)
        {
            // Una vez terminado el envio, se continua escuchando por el Socket de trabajo
            try
            {
                clientState.SetInUse();
                // se asigna el buffer para continuar el envío
                clientState.saeaOfSendReceive.SetBuffer(clientState.saeaOfSendReceive.Offset, sizeBufferPerRequest);
                // se inicia el proceso de recepción asincrona, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                bool seHizoAsync = clientState.SocketOfWork.ReceiveAsync(clientState.saeaOfSendReceive);
                if (!seHizoAsync)
                    // si el evento indica que el proceso está pendiente, se completa el flujo,
                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                    // en su evento callback
                    ReceiveSendIncomingProcessCallBack(clientState.SocketOfWork, clientState.saeaOfSendReceive);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error en ");
                sb.Append(nameof(ReceiveIncomingProcessCiclicToClient));
                sb.Append(". ");
                sb.Append(ex.Message);
                sb.Append(". Al intentar recibir el mensaje del cliente, se cerrará la conexión, cliente ");
                sb.Append(clientState.UniqueClientId);
                Log(sb.ToString(), LogType.Error);
                ClientSocketClose(clientState);
            }
        }

        /// <summary>
        /// Cierra el socket asociado a un cliente y retira al cliente de la lista de clientes conectados
        /// </summary>
        /// <param name="clientState">Instancia del cliente a cerrar</param>
        public void ClientSocketClose(T clientState)
        {
            // Se comprueba que la información del socket de trabajo sea null, ya que podría ser invocado como resultado 
            // de una operación de E / S sin valores
            if (clientState == null)
            {
                var sb = new StringBuilder();
                sb.Append("No se pudo obtener el estado del cliente, ");
                sb.Append(nameof(ClientSocketClose));
                Log(sb.ToString(), LogType.Error);
                return;
            }


            // se obtiene el socket específico del cliente en cuestión
            Socket socketOfWork = clientState.SocketOfWork;

            // se inhabilita y se cierra dicho socket
            try
            {
                socketOfWork.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error en ");
                sb.Append(nameof(ClientSocketClose));
                sb.Append(", ");
                sb.Append(ex.Message);
                sb.Append(", shutdown de envío en el socket de trabajo del cliente ");
                sb.Append(clientState.UniqueClientId);
                Log(sb.ToString(), LogType.Warning);
            }

            try
            {
                socketOfWork.Close();
                socketOfWork.Dispose();
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error en ");
                sb.Append(nameof(ClientSocketClose));
                sb.Append(", ");
                sb.Append(ex.Message);
                sb.Append(", al cerrar y liberar el socket de trabajo del cliente ");
                sb.Append(clientState.UniqueClientId);
                Log(sb.ToString(), LogType.Warning);
            }

            // se llama a la secuencia de cerrando para tener un flujo de eventos
            ServerStateBase.OnClientClose(clientState);


            // se libera la instancia de socket de trabajo para reutilizarlo
            // Antes de liberar el cliente al pool, libera el buffer
            if (clientState.saeaOfSendReceive != null && clientState.inUse == 0)
            {
                if (!RemoveClientToClientList(clientState))
                {
                    AddClientToPendientDesconnetionList(clientState);
                }
                else
                {
                    bufferManager.FreeBuffer(clientState.saeaOfSendReceive, clientState.UniqueClientId);
                    clientState.saeaOfSendReceive.AcceptSocket = null;
                    clientStateManager.AddClientState(clientState);
                }
            }
            else
            {
                AddClientToPendientDesconnetionList(clientState);
            }

            // se marca el semáforo de que puede aceptar otro cliente
            if (ClientSemaphoreConnections.CurrentCount < numberOfSimultaneousClientConnections)
            {
                try
                {
                    ClientSemaphoreConnections.Release();
                }
                catch (SemaphoreFullException)
                {
                    // Ya estaba en el máximo, no pasa nada
                }
            }
        }

        /// <summary>
        /// Adds a client to the list of clients pending disconnection if it is not already present.
        /// </summary>
        /// <remarks>This method attempts to acquire a lock on the pending disconnection list to ensure
        /// thread safety. If the lock cannot be acquired within the configured timeout, an exception is
        /// thrown.</remarks>
        /// <param name="clientState">The client state to be added to the pending disconnection list.</param>
        private void AddClientToPendientDesconnetionList(T clientState)
        {
            bool isLock = false;
            try
            {
                isLock = Monitor.TryEnter(ClientsPendingDisconnectionList, Utilities.milisecondsTimeOutLock);
                if (isLock)
                {
                    // se agrega el cliente a la lista de pendientes de desconexión
                    if (!ClientsPendingDisconnectionList.Contains(clientState))
                    {
                        ClientsPendingDisconnectionList.Add(clientState);
                    }
                }
                else
                {
                    throw new Exception("No se pudo bloquear el proceso para agregar el cliente a la lista de pendientes de desconexión");
                }
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error en ");
                sb.Append(nameof(AddClientToPendientDesconnetionList));
                sb.Append(", ");
                sb.Append(ex.Message);
                sb.Append(" al agregar el cliente a la lista de pendientes de desconexión ");
                sb.Append(clientState.UniqueClientId);
                Log(sb.ToString(), LogType.Error);
            }
            finally
            {
                if (isLock)
                    Monitor.Exit(ClientsPendingDisconnectionList);
            }
        }

        /// <summary>
        /// Cierra el socket asociado a un cliente y retira al cliente de la lista de clientes conectados
        /// </summary>
        /// <param name="clientState">Instancia del cliente a cerrar</param>
        public bool RemoveForcedClient(T clientState)
        {
            // Se comprueba que la información del socket de trabajo sea null, ya que podría ser invocado como resultado 
            // de una operación de E / S sin valores
            if (clientState == null)
            {
                var sb = new StringBuilder();
                sb.Append("No se pudo obtener el estado del cliente, ");
                sb.Append(nameof(RemoveForcedClient));
                Log(sb.ToString(), LogType.Error);
                return false;
            }


            // se obtiene el socket específico del cliente en cuestión
            Socket socketDeTrabajoACerrar = clientState.SocketOfWork;

            // se inhabilita y se cierra dicho socket
            try
            {
                socketDeTrabajoACerrar.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append(ex.Message);
                sb.Append(", ");
                sb.Append(nameof(RemoveForcedClient));
                sb.Append(", shutdown de envío en el socket de trabajo del cliente ");
                sb.Append(clientState.UniqueClientId);
                Log(sb.ToString(), LogType.Warning);
            }

            try
            {
                socketDeTrabajoACerrar.Close();
                socketDeTrabajoACerrar.Dispose();
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append(ex.Message);
                sb.Append(", ");
                sb.Append(nameof(RemoveForcedClient));
                sb.Append(", close en el socket de trabajo del cliente ");
                sb.Append(clientState.UniqueClientId);
                Log(sb.ToString(), LogType.Error);
            }

            // se llama a la secuencia de cerrando para tener un flujo de eventos
            ServerStateBase.OnClientClose(clientState);

            try
            {
                // se libera la instancia de socket de trabajo para reutilizarlo
                // Antes de liberar el cliente al pool, libera el buffer
                if (clientState.saeaOfSendReceive != null && clientState.inUse == 0)
                {
                    if (!RemoveClientToClientList(clientState))
                        return false;
                    bufferManager.FreeBuffer(clientState.saeaOfSendReceive, clientState.UniqueClientId);
                    clientState.saeaOfSendReceive.AcceptSocket = null;
                    clientStateManager.AddClientState(clientState);
                }
                return true;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append(ex.Message);
                sb.Append(", ");
                sb.Append(nameof(RemoveForcedClient));
                sb.Append(", liberando buffer del cliente ");
                sb.Append(clientState.UniqueClientId);
                Log(sb.ToString(), LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// Removes the specified client from the client list.
        /// </summary>
        /// <remarks>This method ensures thread-safe access to the client list by using a synchronization
        /// mechanism.  If the client is not found in the list, a log entry is created to indicate that the client has
        /// already been disconnected.</remarks>
        /// <param name="clientState">The client state object representing the client to be removed. The <see cref="UniqueClientId"/> property
        /// must uniquely identify the client.</param>
        /// <returns><see langword="true"/> if the client was successfully removed from the list;  otherwise, <see
        /// langword="false"/> if an error occurred during the removal process.</returns>
        public bool RemoveClientToClientList(T clientState)
        {
            bool isLock = false;
            try
            {
                // proporciona un mecanismo de sincronización de acceso a datos donde un hilo solo puede tener acceso a un
                // bloque de código a la vez, en este caso en ingresar al listado de clientes, de lo contrario habría 
                // cross threading y provocaría error
                isLock = Monitor.TryEnter(clientsList, milisecondsTimeOutLock);
                if (isLock)
                {
                    // se busca en la lista el cliente y se remueve porque se va a desconectar
                    if (clientsList.ContainsKey(clientState.UniqueClientId))
                    {
                        clientsList.TryRemove(clientState.UniqueClientId, out _);
                    }
                    else
                    {
                        // quiere decir que ya está desconectado
                        var sb = new StringBuilder();
                        sb.Append("No se encontró el cliente ");
                        sb.Append(clientState.UniqueClientId.ToString());
                        sb.Append(" en listaClientes a desconectar, ya ha sido desconectado en otro proceso");
                        Log(sb.ToString(), LogType.Warning);
                    }
                }
                else
                {
                    throw new Exception("No se pudo bloquear el proceso para remover el cliente de la lista de clientes");
                }
                return true;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append(ex.Message);
                sb.Append(" Error removiendo el cliente de la listaClientes, cliente ");
                sb.Append(clientState.UniqueClientId.ToString());
                Log(sb.ToString(), LogType.Error);
                return false;
            }
            finally
            {
                if (isLock)
                    Monitor.Exit(clientsList);
            }
        }

        #endregion


        #region ProcesoDePeticionesProveedor

        /// <summary>
        /// Initiates the process of establishing a connection with a provider for the specified client state.
        /// </summary>
        /// <remarks>This method manages the connection process to a provider, including acquiring
        /// necessary resources such as sockets  and handling connection callbacks. It ensures thread-safe access to
        /// shared resources using synchronization mechanisms.  If an error occurs during the connection process, the
        /// client's response state is updated to indicate the failure,  and the semaphore controlling provider access
        /// is released to allow other requests to proceed.</remarks>
        /// <param name="clientState">The state of the client for which the connection process is being initiated. This parameter cannot be null.</param>
        private void StartProviderProcess(ClientStateBase clientState)
        {
            // me espero a ver si tengo disponibilidad de SAEA para un proveedor
            ProviderSemaphoreConnections.Wait();

            //Se prepara el estado del proveedor que servirá como operador de envío y recepción de trama
            SocketAsyncEventArgs saeaProveedor = new SocketAsyncEventArgs();
            //TODO si ya se va a obtener un socket conectado, creo que no es necesario
            //saeaProveedor.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectionAcceptProviderCallBack);

            IPEndPoint endPointProveedor = GetIPEndPointFromProviderPortsList();
            saeaProveedor.UserToken = clientState;
            try
            {
                Socket providerSocket = socketPool.GetSocket(endPointProveedor, clientState.UniqueClientId);

                if (providerSocket == null)
                {
                    throw new Exception("No se pudo obtener un socket del pool");
                }
                if (saeaProveedor == null)
                {
                    throw new Exception("SocketAsyncEventArgs del proveedor es nulo");
                }
                if (endPointProveedor == null)
                {
                    throw new Exception("EndPoint del proveedor es nulo");
                }
                saeaProveedor.RemoteEndPoint = endPointProveedor;
                saeaProveedor.AcceptSocket = providerSocket;

                ////Inicio el proceso de conexión                    
                //bool seHizoSync = socketDelProveedor.ConnectAsync(saeaProveedor);
                //if (!seHizoSync)
                //    // se llama a la función que completa el flujo de envío, 
                //    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                //    // en su evento callback                    
                //    AceptarConexionProveedorCallBack(socketDelProveedor, saeaProveedor);

                //Al tener un socket ya conectado, se llama directamente al callback
                ConnectionAcceptProviderCallBack(providerSocket, saeaProveedor);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error al intentar conectar con el proveedor: ");
                sb.Append(endPointProveedor);
                sb.Append(", se cerrará la conexión, cliente ");
                sb.Append(clientState.UniqueClientId);
                sb.Append(", ");
                sb.Append(ex.Message);
                sb.Append(",  ");
                sb.Append(ex.StackTrace);
                sb.Append(",  ");
                sb.Append(nameof(StartProviderProcess));
                Log(sb.ToString(), LogType.Error);

                // se libera el semaforo por si otra petición está solicitando acceso
                ProviderSemaphoreConnections.Release();
                clientState.authorizationCode = 0;
                clientState.responseCode = (int)CodigosRespuesta.ErrorEnRed;
                ResponseToClient((T)clientState);
            }
        }

        /// <summary>
        /// Funcion callback para la conexión al proveedor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="saea"></param>
        private void ConnectionAcceptProviderCallBack(object sender, SocketAsyncEventArgs saea)
        {
            var sb = new StringBuilder();
            if (saea == null)
            {
                sb.Append("SocketAsyncEventArgs es nulo en ");
                sb.Append(nameof(ConnectionAcceptProviderCallBack));
                Log(sb.ToString(), LogType.Error);
                return;
            }

            T clientState = saea.UserToken as T;
            X providerState = providerStateManager.GetProviderState();
            if (providerState == null)
            {
                sb.Append("No se pudo obtener el estado del proveedor en ");
                sb.Append(nameof(ConnectionAcceptProviderCallBack));
                sb.Append(" se cerrará la conexión");
                sb.Append(", cliente ");
                sb.Append(clientState.UniqueClientId);
                Log(sb.ToString(), LogType.Error);
                clientState.SetResponseCode((int)CodigosRespuesta.ErrorProcesoSockets);
                clientState.SetAuthorizationCode(0);
                ResponseToClient((T)clientState);
                // se libera el semaforo por si otra petición está solicitando acceso
                ProviderSemaphoreConnections.Release();
                return;
            }
            providerState.SetObjClientRequest(clientState.objRequest);
            providerState.SetClientState(clientState);
            providerState.endPoint = (IPEndPoint)saea.RemoteEndPoint;
            providerState.TimeOutExpired -= ProviderTimeOutExpired;
            providerState.TimeOutExpired += ProviderTimeOutExpired;


            //Se establece el buffer que se utilizará en la operación de lectura del cliente en el eventArgDeRecepcion
            if (providerState.saeaSendReceive.Buffer == null)
                if (!bufferManager.SetBuffer(providerState.saeaSendReceive, providerState.clientStateSource.UniqueClientId))
                {
                    providerState.SetResponseCode((int)CodigosRespuesta.ErrorProcesoSockets);
                    providerState.SetAuthorizationCode(0);
                    providerState.clientStateSource.SetResponseCode(providerState.responseCode);
                    providerState.clientStateSource.SetAuthorizationCode(providerState.authorizacionCode);

                    providerState.SaveTransaction();

                    ResponseToClient((T)providerState.clientStateSource);
                    ReleaseResourceWithOutCloseSocket(providerState);
                    // se libera el semaforo por si otra petición está solicitando acceso
                    ProviderSemaphoreConnections.Release();
                    return;
                }

            if (providerState.responseCode != (int)CodigosRespuesta.TransaccionExitosa)
            {
                providerState.SetAuthorizationCode(0);
                providerState.clientStateSource.SetResponseCode(providerState.responseCode);
                providerState.clientStateSource.SetAuthorizationCode(providerState.authorizacionCode);

                providerState.SaveTransaction();

                ResponseToClient((T)providerState.clientStateSource);
                ReleaseResourceWithOutCloseSocket(providerState);
                // se libera el semaforo por si otra petición está solicitando acceso
                ProviderSemaphoreConnections.Release();
                return;
            }

            // se valida que existan errores registrados
            if (saea.SocketError != SocketError.Success && saea.SocketError != SocketError.IsConnected)
            {
                sb.Clear();
                sb.Append("Error en la conexión al proveedor, ");
                sb.Append(saea.SocketError.ToString());
                sb.Append(", ");
                sb.Append(nameof(ConnectionAcceptProviderCallBack));
                sb.Append(", endpoint ");
                sb.Append(providerState.endPoint.ToString());
                sb.Append(", cliente ");
                sb.Append(providerState.clientStateSource.UniqueClientId);
                Log(sb.ToString(), LogType.Error);

                providerState.SetResponseCode((int)CodigosRespuesta.ErrorEnRed);
                providerState.SetAuthorizationCode(0);
                providerState.clientStateSource.SetResponseCode(providerState.responseCode);
                providerState.clientStateSource.SetAuthorizationCode(providerState.authorizacionCode);

                providerState.SaveTransaction();

                ResponseToClient((T)providerState.clientStateSource);
                ReleaseResourceWithOutCloseSocket(providerState);
                ProviderSemaphoreConnections.Release();
                return;
            }

            // si todo va bien
            providerState.SetMainSocketReference(this);
            // se le indica al estado del proveedor el socket de trabajo
            try
            {
                providerState.SocketOfWork = saea.AcceptSocket;
                if (providerState.SocketOfWork == null)
                {
                    throw new Exception();
                }
            }
            catch (Exception ex)
            {
                sb.Clear();
                sb.Append(nameof(ConnectionAcceptProviderCallBack));
                sb.Append("SocketOfWork recibido es inválido para la operacion, ");
                sb.Append(ex.Message);
                sb.Append(", cliente ");
                sb.Append(providerState.clientStateSource.UniqueClientId);
                Log(sb.ToString(), LogType.Error);

                providerState.SetResponseCode((int)CodigosRespuesta.ErrorEnRed);
                providerState.SetAuthorizationCode(0);
                providerState.clientStateSource.SetResponseCode(providerState.responseCode);
                providerState.clientStateSource.SetAuthorizationCode(providerState.authorizacionCode);

                providerState.SaveTransaction();

                ResponseToClient((T)providerState.clientStateSource);
                ReleaseResourceWithOutCloseSocket(providerState);
                ProviderSemaphoreConnections.Release();
                return;
            }

            // obtengo las tramas para considerar cualquier evento antes de enviar la petición al proveedor.
            // se puede actualizar más adelante
            // solo por precaución se inicializan los valores
            providerState.SetResponseCode(0);
            providerState.SetAuthorizationCode(0);

            if (ValidateTimeOutExpired((T)providerState.clientStateSource))
            {
                sb.Clear();
                sb.Append("Se venció el TimeOut para el cliente ");
                sb.Append(providerState.clientStateSource.UniqueClientId.ToString());
                sb.Append(", durante el procesamiento de la trama antes de enviarla al proveedor. ");
                sb.Append("No se enviará la solicitud.");
                Log(sb.ToString(), LogType.Warning);
                providerState.SetResponseCode((int)CodigosRespuesta.TimeOutInterno);
                providerState.SetAuthorizationCode(0);
                providerState.clientStateSource.SetResponseCode(providerState.responseCode);
                providerState.clientStateSource.SetAuthorizationCode(providerState.authorizacionCode);

                providerState.SaveTransaction();

                ResponseToClient((T)providerState.clientStateSource);
                ReleaseResourceWithOutCloseSocket(providerState);
                ProviderSemaphoreConnections.Release();
                return;
            }



            try
            {
                if (!AddProviderToProvidersList(providerState))
                {
                    throw new Exception("No se pudo ingresar el proveedor a la lista de proveedores en curso, de debe excluir y anular la operación. ");
                }

                //Si todo está correcto se obtiene a enviar
                providerState.GetRequestMessage();

                // Se guarda  la transacción para posterior actualizarla
                providerState.SaveTransaction();

                providerState.SetInUse();
                providerState.saeaSendReceive.UserToken = providerState;
                if (providerState.SocketOfWork.Connected)
                {
                    string messageToProvider = providerState.messageRequest;
                    LogMessageToProvider(providerState);
                    // se obtiene la cantidad de bytes de la trama completa
                    int bytesCounter = GetMessageBytes(providerState, messageToProvider);
                    if (bytesCounter == 0)
                    {
                        providerState.SetResponseCode((int)CodigosRespuesta.ErrorFormato);
                        providerState.SetAuthorizationCode(0);
                        providerState.clientStateSource.SetResponseCode(providerState.responseCode);
                        providerState.clientStateSource.SetAuthorizationCode(providerState.authorizacionCode);

                        providerState.SaveTransaction();

                        ResponseToClient((T)providerState.clientStateSource);
                        ReleaseResourceWithOutCloseSocket(providerState);
                        ProviderSemaphoreConnections.Release();
                        return;
                    }

                    // Se prepara el buffer del SAEA con el tamaño predefinido                         
                    providerState.saeaSendReceive.SetBuffer(providerState.saeaSendReceive.Offset, bytesCounter);

                    //140824 se valida que exista tiempo suficiente para que el proveedor (procesa) realice la tarea
                    if (!ValidateTimeRemainingToProvider(providerState, out int timeremaining))
                    {
                        providerState.SetResponseCode((int)CodigosRespuesta.ErrorEnElProceso);
                        providerState.SetAuthorizationCode(0);
                        providerState.clientStateSource.SetResponseCode(providerState.responseCode);
                        providerState.clientStateSource.SetAuthorizationCode(providerState.authorizacionCode);

                        providerState.SaveTransaction();

                        ResponseToClient((T)providerState.clientStateSource);
                        ReleaseResourceWithOutCloseSocket(providerState);
                        ProviderSemaphoreConnections.Release();
                        return;
                    }
                    //Se inicializa el timer para el timeout de la operación
                    var _ = providerState.TimeOutCounterAsync(timeremaining);
                    providerState.SetInUse();


                    // se procede al envío asincrono del mensaje,el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                    bool seHizoAsync = providerState.SocketOfWork.SendAsync(providerState.saeaSendReceive);
                    if (!seHizoAsync)
                        // se llama a la función que completa el flujo de envío, 
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        ReceiveSendOutgoingCallBack(providerState.SocketOfWork, providerState.saeaSendReceive);
                }
                else
                {
                    throw new Exception("El socket no está conectado al proveedor");
                }
            }
            catch (Exception ex)
            {
                sb.Clear();
                sb.Append("Error enviando la solicitud al proveedor, se cerrará la conexión, cliente ");
                sb.Append(providerState.clientStateSource.UniqueClientId);
                sb.Append(", ");
                sb.Append(ex.Message);
                Log(sb.ToString(), LogType.Error);
                providerState.SetFree();
                providerState.SetResponseCode((int)CodigosRespuesta.ErrorProcesoSockets);
                providerState.SetAuthorizationCode(0);
                providerState.clientStateSource.SetResponseCode(providerState.responseCode);
                providerState.clientStateSource.SetAuthorizationCode(providerState.authorizacionCode);

                providerState.SaveTransaction();

                ResponseToClient((T)providerState.clientStateSource);
                providerState.SetFree();
                ProviderSocketClose(providerState);
            }
        }

        /// <summary>
        /// Attempts to add a provider to the list of providers if it does not already exist.
        /// </summary>
        /// <remarks>This method uses a synchronization mechanism to ensure thread safety when accessing
        /// the shared list of providers.  If the operation cannot acquire a lock within the specified timeout, an
        /// exception is thrown, and the method logs the error.</remarks>
        /// <param name="providerState">The state of the provider to be added. This must include a unique client identifier.</param>
        /// <returns><see langword="true"/> if the provider was successfully added to the list or already exists;  otherwise,
        /// <see langword="false"/> if an error occurred during the operation.</returns>
        private bool AddProviderToProvidersList(X providerState)
        {
            StringBuilder sb = new StringBuilder();
            bool isLock = false;
            try
            {
                isLock = Monitor.TryEnter(providersList, milisecondsTimeOutLock);
                if (isLock)
                {
                    if (!providersList.ContainsKey(providerState.UniqueProviderId))
                    {
                        providersList.TryAdd(providerState.UniqueProviderId, providerState);
                    }
                }
                else
                {
                    throw new Exception("No se pudo bloquear el proceso para ingresar el proveedor a la lista de proveedores");
                }
                return true;
            }
            catch (Exception ex)
            {
                sb.Clear();
                sb.Append("Error al intentar ingresar el proveedor a la lista de proveedores, se cerrará la conexión, ");
                sb.Append(nameof(AddProviderToProvidersList));
                sb.Append(", cliente: ");
                sb.Append(providerState.clientStateSource.UniqueClientId);
                sb.Append(", ");
                sb.Append(ex.Message);
                Log(sb.ToString(), LogType.Error);
                return false;
            }
            finally
            {
                if (isLock)
                    Monitor.Exit(providersList);
            }
        }

        /// <summary>
        /// Converts the specified message to a byte array and writes it to the buffer of the given provider state.
        /// </summary>
        /// <remarks>If the size of the message exceeds the allocated buffer size, the method logs a
        /// warning and the message is considered invalid. In the event of an error during the conversion process, the
        /// method logs the exception details along with the message and client information.</remarks>
        /// <param name="providerState">The state object representing the provider, which contains the buffer and offset for writing the message
        /// bytes.</param>
        /// <param name="messageToProvider">The message to be converted to bytes and written to the provider's buffer.</param>
        /// <returns>The number of bytes written to the buffer.</returns>
        private int GetMessageBytes(X providerState, string messageToProvider)
        {
            int bytesCounter = 0;
            var sb = new StringBuilder();
            try
            {
                bytesCounter = Encoding.Default.GetBytes(messageToProvider, 0, messageToProvider.Length, providerState.saeaSendReceive.Buffer, providerState.saeaSendReceive.Offset);
                // si el número de bytes es mayor al buffer que se tiene destinado a la recepción, no se puede proceder, no es válido el mensaje
                if (bytesCounter > sizeBufferPerRequest)
                {
                    sb.Append("El mensaje al proveedor es más grande que el buffer asignado. Se anula la operación, cliente: ");
                    sb.Append(providerState.clientStateSource.UniqueClientId);
                    Log(sb.ToString(), LogType.Warning);
                }
                return bytesCounter;
            }
            catch (Exception ex)
            {
                sb.Clear();
                sb.Append(ex.Message);
                sb.Append(" Error obteniendo los Bytes para enviar el mensaje al proveedor: ");
                sb.Append(providerState.messageRequest);
                sb.Append(", cliente: ");
                sb.Append(providerState.clientStateSource.UniqueClientId);
                Log(sb.ToString(), LogType.Error);
                return bytesCounter;
            }
        }

        /// <summary>
        /// Logs a message related to the provider's state and the associated client information.
        /// </summary>
        /// <remarks>This method constructs a log message based on the provider's request data and the
        /// unique identifier of the originating client. If an exception occurs during the process, the exception
        /// message is included in the log.</remarks>
        /// <param name="providerState">An object representing the state of the provider, including the request data and the originating client's
        /// state.</param>
        private static void LogMessageToProvider(X providerState)
        {
            var sb = new StringBuilder();
            try
            {
                sb.Append("Mensaje que se envia al proveedor: ");
                sb.Append(providerState.messageRequest.Trim().Substring(2));
                sb.Append(" para el cliente: ");
                sb.Append(providerState.clientStateSource.UniqueClientId);
                Log(sb.ToString(), LogType.Info);
            }
            catch (Exception ex)
            {
                sb.Append(ex.Message);
                sb.Append("Mensaje que se envia al proveedor: ");
                sb.Append(providerState.messageRequest);
                sb.Append(" para el cliente: ");
                sb.Append(providerState.clientStateSource.UniqueClientId);
                Log(sb.ToString(), LogType.Info);
            }
        }

        /// <summary>
        /// Releases resources associated with the specified provider state.
        /// </summary>
        /// <remarks>This method ensures that the buffer associated with the provider's socket is
        /// released, the socket is reset, and the provider state is returned to the pool for reuse. If an error occurs
        /// during the release process, the error is logged.</remarks>
        /// <param name="providerState">The state of the provider whose resources are to be released. This includes the associated socket and client
        /// state information.</param>
        private void ReleaseResourceWithOutCloseSocket(X providerState)
        {
            try
            {
                bufferManager.FreeBuffer(providerState.saeaSendReceive, providerState.clientStateSource.UniqueClientId);
                providerState.saeaSendReceive.AcceptSocket = null;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append(ex.Message);
                sb.Append(" Error liberando recursos del proveedor, cliente ");
                sb.Append(providerState.clientStateSource.UniqueClientId);
                Log(sb.ToString(), LogType.Error);
            }
            finally
            {
                // el SAEA del proveedor se ingresa nuevamente al pool para ser re utilizado y cuando se tome de nuevo se limpiará
                providerStateManager.AddProviderState(providerState);
            }
        }

        /// <summary>
        /// Función call back para el evento de envío y recepción al proveedor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReceiveSendOutgoingCallBack(object sender, SocketAsyncEventArgs e)
        {
            var sb = new StringBuilder();
            // obtengo el estado del proveedor
            // se comprueba que el estado haya sido obtenido correctamente
            if (!(e.UserToken is X providerState))
            {
                sb.Append("No se pudo obtener el estado del proveedor en ");
                sb.Append(nameof(ReceiveSendOutgoingCallBack));
                Log(sb.ToString(), LogType.Error);
                return;
            }

            //Al entrar al callback se libera el estado del proveedor para que pueda ser usado en otro proceso si fuera necesario
            providerState.SetFree();

            // se determina que operación se está llevando a cabo para indicar que manejador de eventos se ejecuta
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Send:

                    // se comprueba que no hay errores con el socket
                    if (e.SocketError == SocketError.Success)
                    {
                        // se procesa el envío
                        ReceiveIncomingProcessCiclicToProvider(providerState);
                    }
                    else
                    {
                        sb.Clear();
                        sb.Append("Error en el envío a ");
                        sb.Append(providerState.saeaSendReceive.RemoteEndPoint);
                        sb.Append(", ");
                        sb.Append(nameof(ReceiveSendOutgoingCallBack));
                        sb.Append(", ");
                        sb.Append(e.SocketError.ToString());
                        sb.Append(", cliente ");
                        sb.Append(providerState.clientStateSource.UniqueClientId);
                        Log(sb.ToString(), LogType.Error);

                        providerState.SetResponseCode((int)CodigosRespuesta.SinRespuestaCarrier);
                        providerState.SetAuthorizationCode(0);
                        providerState.clientStateSource.SetResponseCode(providerState.responseCode);
                        providerState.clientStateSource.SetAuthorizationCode(providerState.authorizacionCode);
                        ResponseToClient((T)providerState.clientStateSource);
                        ProviderSocketClose(providerState);
                    }
                    break;
                case SocketAsyncOperation.Receive:
                    providerState.waitSendingEvent.Set();
                    // se comprueba que exista información
                    if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                    {
                        // se procesa la solicitud
                        ReceiveProcess(providerState);
                    }
                    else
                    {
                        providerState.SetResponseCode((int)CodigosRespuesta.SinRespuestaCarrier);
                        providerState.SetAuthorizationCode(0);
                        providerState.clientStateSource.SetResponseCode(providerState.responseCode);
                        providerState.clientStateSource.SetAuthorizationCode(providerState.authorizacionCode);
                        ResponseToClient((T)providerState.clientStateSource);
                        ProviderSocketClose(providerState);
                    }
                    break;
                default:
                    sb.Append("La ultima operación no se detecto como de recepcion o envío, ");
                    sb.Append(nameof(ReceiveSendOutgoingCallBack));
                    sb.Append(", ultima operación del socket fue: ");
                    sb.Append(e.LastOperation.ToString());
                    sb.Append(", cliente ");
                    sb.Append(providerState.clientStateSource.UniqueClientId);
                    Log(sb.ToString(), LogType.Warning);

                    providerState.SetResponseCode((int)CodigosRespuesta.ErrorEnRed);
                    providerState.SetAuthorizationCode(0);
                    providerState.clientStateSource.SetResponseCode(providerState.responseCode);
                    providerState.clientStateSource.SetAuthorizationCode(providerState.authorizacionCode);
                    ResponseToClient((T)providerState.clientStateSource);
                    break;
            }
        }

        /// <summary>
        /// Función para procesar el envío al proveedor y dejar de nuevo en escucha al socket
        /// </summary>
        /// <param name="providerState">Estado del proveedor con la información de conexión</param>
        private void ReceiveIncomingProcessCiclicToProvider(X providerState)
        {
            if (providerState == null)
            {
                var sb = new StringBuilder();
                sb.Append("estadoDelProveedor es inválido para la operacion");
                Log(sb.ToString(), LogType.Error);
                return;
            }
            providerState.waitSendingEvent.WaitOne();

            // Una vez terminado el envio, se continua escuchando por el Socket de trabajo
            try
            {
                providerState.SetInUse();
                providerState.waitSendingEvent.Reset();
                // se asigna el buffer para continuar el envío
                providerState.saeaSendReceive.SetBuffer(providerState.saeaSendReceive.Offset, sizeBufferPerRequest);
                // se inicia el proceso de recepción asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                bool seHizoAsync = providerState.SocketOfWork.ReceiveAsync(providerState.saeaSendReceive);
                if (!seHizoAsync)
                    // si el evento indica que el proceso está pendiente, se completa el flujo,
                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                    // en su evento callback
                    ReceiveSendOutgoingCallBack(providerState.SocketOfWork, providerState.saeaSendReceive);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error al intentar recibir el mensaje del proveedor, se cerrará la conexión, cliente ");
                sb.Append(providerState.clientStateSource.UniqueClientId);
                sb.Append(", ");
                sb.Append(ex.Message);
                sb.Append(", ");
                sb.Append(nameof(ReceiveIncomingProcessCiclicToProvider));
                Log(sb.ToString(), LogType.Error);

                providerState.SetResponseCode((int)CodigosRespuesta.ErrorProcesoSockets);
                providerState.SetAuthorizationCode(0);
                providerState.clientStateSource.SetResponseCode(providerState.responseCode);
                providerState.clientStateSource.SetAuthorizationCode(providerState.authorizacionCode);
                ResponseToClient((T)providerState.clientStateSource);
                providerState.SetFree();
                ProviderSocketClose(providerState);
            }
        }

        /// <summary>
        /// Función que realiza la recepción del mensaje y lo procesa
        /// </summary>
        /// <param name="providerState">Estado del proveedor con la información de conexión</param>
        private void ReceiveProcess(X providerState)
        {
            //Al recibir respuesta se cancela el timeout
            providerState.CancelTimeoutCounter();
            if (providerState == null)
            {
                var sb = new StringBuilder();
                sb.Append(nameof(providerState));
                sb.Append(" es inválido para la operacion");
                Log(sb.ToString(), LogType.Error);
                return;
            }
            // se obtiene el SAEA de recepción
            SocketAsyncEventArgs saeaReceive = providerState.saeaSendReceive;
            // se obtienen los bytes que han sido recibidos
            int bytesTransferred = saeaReceive.BytesTransferred;

            //si aún no expira el timeout
            if (providerState.wasTimeOutExpired == 0)
            {
                // el mensaje recibido llevará un proceso, que no debe ser llevado por el core, se coloca en la función virtual
                // para que se consuma en otra capa, se procese y se entregue una respuesta
                try
                {
                    if (saeaReceive.Buffer == null)
                    {
                        throw new Exception("Buffer nulo en ReceiveProcess para el cliente: " + providerState.clientStateSource.UniqueClientId);
                    }
                    // se obtiene el mensaje y se decodifica
                    string messageReceive = Encoding.ASCII.GetString(saeaReceive.Buffer, saeaReceive.Offset, bytesTransferred);

                    var sb = new StringBuilder();
                    sb.Append("Mensaje recibido del proveedor: ");
                    sb.Append(messageReceive.Trim().Substring(2));
                    sb.Append(" cliente: ");
                    sb.Append(providerState.clientStateSource.UniqueClientId);
                    Log(sb.ToString(), LogType.Info);

                    providerState.ProcessMessage(messageReceive);
                    providerState.CompareResponseVsRequest(messageReceive);
                    providerState.GetResponseMessage();
                }
                catch (Exception ex)
                {
                    providerState.SetResponseCode((int)CodigosRespuesta.ErrorProceso);
                    providerState.SetAuthorizationCode(0);
                    var sb = new StringBuilder();
                    sb.Append(ex.Message);
                    sb.Append(", procesando trama del proveedor");
                    sb.Append(" , ");
                    sb.Append(nameof(ReceiveProcess));
                    sb.Append(", cliente ");
                    sb.Append(providerState.clientStateSource.UniqueClientId);
                    Log(sb.ToString(), LogType.Error);
                    return;
                }

                if (providerState.messageResponse == "")
                {
                    providerState.SetResponseCode((int)CodigosRespuesta.ErrorProceso);
                    providerState.SetAuthorizationCode(0);
                }
                providerState.clientStateSource.msg210 = providerState.messageResponse;
                providerState.clientStateSource.msg230 = providerState.messageResponse;
                providerState.clientStateSource.SetResponseCode(providerState.responseCode);
                providerState.clientStateSource.SetAuthorizationCode(providerState.authorizacionCode);
                ResponseToClient((T)providerState.clientStateSource);
                ProviderSocketClose(providerState);
            }
            else
            {
                var sb = new StringBuilder();
                sb.Append("Se venció por timeout por lo que probablemente se haya ya respondido al cliente: ");
                sb.Append(providerState.clientStateSource.UniqueClientId.ToString());
                Log(sb.ToString(), LogType.Info);
                ResponseToClient((T)providerState.clientStateSource);
                ProviderSocketClose(providerState);
            }
        }

        /// <summary>
        /// Cierra el socket asociado a un proveedor y retira al proveedor de la lista de conectados
        /// </summary>
        public void ProviderSocketClose(X providerState)
        {
            var sb = new StringBuilder();
            try
            {
                // se desuscribe del evento de timeout
                providerState.TimeOutExpired -= ProviderTimeOutExpired;
                // Se comprueba que la información del socket de trabajo sea null, ya que podría ser invocado como resultado 
                // de una operación de E / S sin valores
                if (providerState == null) return;
                if (providerState.SocketOfWork == null) return;

                // se obtiene el socket específico del proveedor en cuestión
                Socket socketDeTrabajoACerrar = providerState.SocketOfWork;
                socketPool.ReturnSocket(socketDeTrabajoACerrar, providerState.clientStateSource.UniqueClientId);

                // se libera la instancia de socket de trabajo para reutilizarlo
                if (providerState.saeaSendReceive != null && providerState.InUse == 0 && providerState.wasTimeOutExpired == 0)
                {
                    if (!RemoveProviderToProviderList(providerState))
                    {
                        AddProvidersPendingDisconnectionList(providerState);
                    }
                    else
                    {
                        //bufferManager.FreeBuffer(providerState.saeaSendReceive, providerState.clientStateSource.UniqueClientId);
                        //providerState.saeaSendReceive.AcceptSocket = null;
                        //providerStateManager.AddProviderState(providerState);
                        ReleaseResourceWithOutCloseSocket(providerState);
                    }
                }
                else
                {
                    AddProvidersPendingDisconnectionList(providerState);
                }

                // se marca el semáforo de que puede aceptar otro cliente
                if (this.ProviderSemaphoreConnections.CurrentCount < this.numberOfSimultaneousProviderConnections)
                {
                    try
                    {
                        this.ProviderSemaphoreConnections.Release();
                    }
                    catch (SemaphoreFullException)
                    {
                        // Ya estaba en el máximo, no pasa nada
                    }
                }
            }
            catch (Exception ex)
            {
                sb.Append(ex.Message);
                sb.Append(", ");
                sb.Append(nameof(ProviderSocketClose));
                sb.Append(", cliente ");
                if (providerState != null && providerState.clientStateSource != null)
                {
                    sb.Append(providerState.clientStateSource.UniqueClientId);
                }
                Log(sb.ToString(), LogType.Error);
                if (!ProvidersPendingDisconnectionList.Contains(providerState))
                    ProvidersPendingDisconnectionList.Add(providerState);
            }
        }

        /// <summary>
        /// Adds the specified provider state to the list of providers pending disconnection,  ensuring thread-safe
        /// access to the list.
        /// </summary>
        /// <remarks>This method attempts to acquire a lock on the pending disconnection list to ensure 
        /// thread safety. If the provider state is already in the list, it will not be added again.  Any errors
        /// encountered during the operation are logged.</remarks>
        /// <param name="providerState">The provider state to add to the pending disconnection list.  This parameter must not be null.</param>
        private void AddProvidersPendingDisconnectionList(X providerState)
        {
            bool isLock = false;
            try
            {
                isLock = Monitor.TryEnter(ProvidersPendingDisconnectionList, Utilities.milisecondsTimeOutLock);
                if (isLock)
                {
                    if (!ProvidersPendingDisconnectionList.Contains(providerState))
                        ProvidersPendingDisconnectionList.Add(providerState);
                }
            }
            catch (Exception)
            {
                var sb = new StringBuilder();
                sb.Append("Error agregando el proveedor a la lista de desconexiones pendientes, ");
                sb.Append(nameof(AddProvidersPendingDisconnectionList));
                sb.Append(", cliente ");
                if (providerState != null && providerState.clientStateSource != null)
                {
                    sb.Append(providerState.clientStateSource.UniqueClientId);
                }
                Log(sb.ToString(), LogType.Error);
            }
            finally
            {
                if (isLock)
                    Monitor.Exit(ProvidersPendingDisconnectionList);
            }

        }

        /// <summary>
        /// Forces the closure of a provider's socket and releases associated resources.
        /// </summary>
        /// <remarks>This method ensures that the provider's socket is properly closed and its resources
        /// are released  for reuse. If the socket is no longer in use and can be removed from the pending list, the
        /// associated  buffer is released, and the state object is returned to the state manager. If the socket has
        /// been  inactive for more than 10 minutes, it is forcibly closed.   Any exceptions encountered during the
        /// operation are logged, including the unique client identifier  if available.</remarks>
        /// <param name="providerState">The state object representing the provider's socket and its associated resources.  This parameter cannot be
        /// null.</param>
        public bool RemoveForcedProvider(X providerState)
        {
            var sb = new StringBuilder();
            try
            {
                // Se comprueba que la información del socket de trabajo sea null, ya que podría ser invocado como resultado 
                // de una operación de E / S sin valores
                if (providerState == null) return false;

                // se libera la instancia de socket de trabajo para reutilizarlo
                if (providerState.saeaSendReceive != null && providerState.InUse == 0)
                {
                    if (!RemoveProviderToProviderPendingList(providerState))
                    {
                        return false;
                    }
                    else
                    {
                        //bufferManager.FreeBuffer(providerState.saeaSendReceive, providerState.clientStateSource.UniqueClientId);
                        //providerState.saeaSendReceive.AcceptSocket = null;
                        //providerStateManager.AddProviderState(providerState);
                        ReleaseResourceWithOutCloseSocket(providerState);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                sb.Append(ex.Message);
                sb.Append(", ");
                sb.Append(nameof(RemoveForcedProvider));
                sb.Append(", cliente ");
                if (providerState != null && providerState.clientStateSource != null)
                {
                    sb.Append(providerState.clientStateSource.UniqueClientId);
                }
                Log(sb.ToString(), LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// Removes a provider from the list of active providers.
        /// </summary>
        /// <remarks>This method ensures thread-safe access to the provider list by using a
        /// synchronization mechanism.  If the provider is not found in the list, a log entry is created to indicate
        /// that the provider  has already been removed in another process. If the synchronization lock cannot be
        /// acquired,  an exception is thrown.</remarks>
        /// <param name="providerState">The state of the provider to be removed, including its unique client identifier.</param>
        /// <returns><see langword="true"/> if the provider was successfully removed from the list;  otherwise, <see
        /// langword="false"/> if an error occurred during the removal process.</returns>
        public bool RemoveProviderToProviderList(X providerState)
        {
            bool isLock = false;
            try
            {
                // proporciona un mecanismo de sincronización de acceso a datos donde un hilo solo puede tener acceso a un
                // bloque de código a la vez, en este caso en ingresar al listado de clientes, de lo contrario habría 
                // cross threading y provocaría error
                isLock = Monitor.TryEnter(providersList, milisecondsTimeOutLock);
                if (isLock)
                {
                    // se busca en la lista el cliente y se remueve porque se va a desconectar
                    if (providersList.ContainsKey(providerState.UniqueProviderId))
                    {
                        providersList.TryRemove(providerState.UniqueProviderId, out _);
                    }
                    else
                    {
                        // quiere decir que ya está desconectado
                        var sb = new StringBuilder();
                        sb.Append("No se encontró el proveedor ");
                        sb.Append(providerState.UniqueProviderId);
                        sb.Append(" en lista de proveedores a desconectar, ya ha sido desconectado en otro proceso. ");
                        sb.Append("cliente:");
                        sb.Append(providerState.clientStateSource.UniqueClientId);
                        Log(sb.ToString(), LogType.Warning);
                    }
                }
                else
                {
                    throw new Exception("No se pudo bloquear el proceso para remover el cliente de la lista de proveedores");
                }
                return true;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append(ex.Message);
                sb.Append(" Error removiendo el proveedor de la lista de proveedores, cliente ");
                sb.Append(providerState.clientStateSource?.UniqueClientId.ToString() ?? "N/A");
                Log(sb.ToString(), LogType.Error);
                return false;
            }
            finally
            {
                if (isLock)
                    Monitor.Exit(providersList);
            }
        }

        /// <summary>
        /// Removes a provider from the list of active providers.
        /// </summary>
        /// <remarks>This method ensures thread-safe access to the provider list by using a
        /// synchronization mechanism.  If the provider is not found in the list, a log entry is created to indicate
        /// that the provider  has already been removed in another process. If the synchronization lock cannot be
        /// acquired,  an exception is thrown.</remarks>
        /// <param name="providerState">The state of the provider to be removed, including its unique client identifier.</param>
        /// <returns><see langword="true"/> if the provider was successfully removed from the list;  otherwise, <see
        /// langword="false"/> if an error occurred during the removal process.</returns>
        private bool RemoveProviderToProviderPendingList(X providerState)
        {
            bool isLock = false;
            try
            {
                // proporciona un mecanismo de sincronización de acceso a datos donde un hilo solo puede tener acceso a un
                // bloque de código a la vez, en este caso en ingresar al listado de clientes, de lo contrario habría 
                // cross threading y provocaría error
                isLock = Monitor.TryEnter(ProvidersPendingDisconnectionList, milisecondsTimeOutLock);
                if (isLock)
                {
                    // se busca en la lista el cliente y se remueve porque se va a desconectar
                    if (ProvidersPendingDisconnectionList.Contains(providerState))
                    {
                        ProvidersPendingDisconnectionList.Remove(providerState);
                    }
                    else
                    {
                        // quiere decir que ya está desconectado
                        var sb = new StringBuilder();
                        sb.Append("No se encontró el proveedor ");
                        sb.Append(providerState.clientStateSource.UniqueClientId);
                        sb.Append(" en lista de proveedores a desconectar, ya ha sido desconectado en otro proceso");
                        Log(sb.ToString(), LogType.Warning);
                    }
                }
                else
                {
                    throw new Exception("No se pudo bloquear el proceso para remover el proveedor de la lista de proveedores conectados");
                }
                return true;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append(ex.Message);
                sb.Append(" Error removiendo el proveedor de la lista de proveedores, cliente ");
                sb.Append(providerState.clientStateSource.UniqueClientId.ToString());
                Log(sb.ToString(), LogType.Error);
                return false;
            }
            finally
            {
                if (isLock)
                    Monitor.Exit(ProvidersPendingDisconnectionList);
            }
        }

        /// <summary>
        /// valida que exista tiempo suficiente para que el proveedor (procesa) realice la tarea, el tiempo por defecto es 25 seg
        /// </summary>
        /// <param name="providerState"></param>
        /// <param name="timeRemaining"></param>
        /// <param name="estadoDelProveedor"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private bool ValidateTimeRemainingToProvider(X providerState, out int timeRemaining)
        {
            bool isLock = false;
            try
            {
                bool hasEnoughTime = true;
                isLock = Monitor.TryEnter(providerState, milisecondsTimeOutLock);
                if (isLock)
                {
                    TimeSpan timeSpan = DateTime.Now - providerState.clientStateSource.StartDateTrx;
                    var sb = new StringBuilder();
                    sb.Append(nameof(ValidateTimeRemainingToProvider));
                    sb.Append(", a la fechaHoraDeComprobacion: ");
                    sb.Append(DateTime.Now);
                    sb.Append(" se le resta la fechaHoraInicioTrx: ");
                    sb.Append(providerState.clientStateSource.StartDateTrx);
                    sb.Append(" el resultado es: ");
                    sb.Append(timeSpan.Seconds);
                    sb.Append(" segundos transcurridos desde que llegó la petición. El timeout establecido del proveedor es de: ");
                    sb.Append(providerState.timeOut);
                    sb.Append(" segundos.");
                    sb.Append(" Por lo tanto si se le suma el tiempo transcurrido al timeout completo del proveedor, el resultado es: ");
                    int expectedElapsedTime = providerState.timeOut + timeSpan.Seconds;
                    sb.Append(expectedElapsedTime);
                    sb.Append(" segundos que pudiera tomar la transacción y si el timeout del cliente es: ");
                    sb.Append(providerState.clientStateSource.timeOut);
                    sb.Append(" segundos. ");


                    if (expectedElapsedTime < providerState.clientStateSource.timeOut)
                    {
                        sb.Append("Hay tiempo suficiente para procesar la solicitud con el proveedor, ");
                        hasEnoughTime = true;
                    }
                    else
                    {
                        sb.Append("No hay tiempo suficiente para procesar la solicitud con el proveedor, ");
                        hasEnoughTime = false;
                    }
                    sb.Append("clienteId: ");
                    sb.Append(providerState.clientStateSource.UniqueClientId);
                    Log(sb.ToString(), LogType.Info);
                    timeRemaining = providerState.clientStateSource.timeOut - timeSpan.Seconds;
                    return hasEnoughTime;
                }
                else
                {
                    throw new Exception("No se pudo bloquear el proceso para validar el tiempo restante para el proveedor");
                }

            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error en ");
                sb.Append(nameof(ValidateTimeRemainingToProvider));
                sb.Append(": ");
                sb.Append(ex.Message);
                sb.Append(". cliente ");
                sb.Append(providerState.clientStateSource.UniqueClientId);
                Log(sb.ToString(), LogType.Error);
                timeRemaining = 0;
                return false;
            }
            finally
            {
                if (isLock)
                    Monitor.Exit(providerState);
            }
        }

        /// <summary>
        /// Handles the event triggered when the provider's timeout expires.
        /// </summary>
        /// <remarks>This method processes the provider's state upon a timeout, updates response and
        /// authorization codes,  sends a response to the client, and closes the provider's socket connection.</remarks>
        /// <param name="sender">The source of the event, representing the provider's state.</param>
        /// <param name="e">The event data associated with the timeout expiration.</param>
        private void ProviderTimeOutExpired(object sender, EventArgs e)
        {
            X providerState = (X)sender;
            providerState.SetResponseCode((int)CodigosRespuesta.SinRespuestaCarrier);
            providerState.SetAuthorizationCode(0);
            providerState.clientStateSource.SetResponseCode(providerState.responseCode);
            providerState.clientStateSource.SetAuthorizationCode(providerState.authorizacionCode);

            var sb = new StringBuilder();
            sb.Append("Se venció el timeout para la solicitud al endpoint: ");
            sb.Append(providerState.endPoint);
            sb.Append(", mensaje al proveedor que fue enviado: ");
            sb.Append(providerState.messageRequest);
            sb.Append(", respuesta del proveedor: ");
            sb.Append(providerState.messageResponse);
            sb.Append(", código de respuesta al cliente: ");
            sb.Append(providerState.clientStateSource.responseCode);
            sb.Append(", código de autorización al cliente: ");
            sb.Append(providerState.clientStateSource.authorizationCode);
            sb.Append(", código de respuesta del proveedor: ");
            sb.Append(providerState.responseCode);
            sb.Append(", código de autorización del proveedor: ");
            sb.Append(providerState.authorizacionCode);
            sb.Append(", cliente ");
            sb.Append(providerState.clientStateSource.UniqueClientId);
            Log(sb.ToString(), LogType.Warning);

            // Forzar cierre del socket si sigue abierto para desencadenar su callback
            try
            {
                if (providerState.SocketOfWork != null)
                {
                    if (IsSocketConnected(providerState.SocketOfWork))
                    {
                        providerState.SocketOfWork.Shutdown(SocketShutdown.Both);
                        providerState.SocketOfWork.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                sb.Clear();
                sb.Append(" Error forzando cierre de socket por timeout: " + ex.Message);
                sb.Append(", cliente ");
                sb.Append(providerState.clientStateSource.UniqueClientId);
                Log(sb.ToString(), LogType.Error);
                providerState.CancelTimeoutCounter();

            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="Socket"/> is currently connected.
        /// </summary>
        /// <remarks>This method checks the connectivity of the socket by polling its state. A return
        /// value of  <see langword="false"/> indicates that the socket is either closed or no longer
        /// connected.</remarks>
        /// <param name="socket">The <see cref="Socket"/> instance to check for connectivity.</param>
        /// <returns><see langword="true"/> if the socket is connected; otherwise, <see langword="false"/>.</returns>
        private bool IsSocketConnected(Socket socket)
        {
            var sb = new StringBuilder();
            try
            {
                if (socket == null)
                {
                    return false;
                }
                //sb.Append("verificando conectividad del socket.");
                //sb.Append(socket.RemoteEndPoint.ToString());
                //sb.Append(" para poder reutilizarlo.");
                //Utilities.Log(sb.ToString(), Utilities.LogType.Info);
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException sex)
            {
                sb.Append("SocketException al verificar la conectividad del socket. ");
                sb.Append(sex.Message);
                Utilities.Log(sb.ToString(), Utilities.LogType.Warning);
                return false;
            }
        }

        #endregion

        /// <summary>
        /// Se detiene el servidor
        /// </summary>
        public void Stop()
        {
            #region codigo pendiente
            //// se indica que se está ejecutando el proceso de desconexión de los clientes
            //desconectando = true;
            //List<T> listaDeClientesEliminar = new List<T>();

            ////// Primero se detiene y se cierra el socket de escucha
            ////try
            ////{
            ////    if (this.socketDeEscucha != null && this.socketDeEscucha.Connected)
            ////    {
            ////        // Solo se apaga si está conectado y no está enviando datos
            ////        if (!this.socketDeEscucha.Poll(0, SelectMode.SelectWrite))
            ////        {
            ////            this.socketDeEscucha.Shutdown(SocketShutdown.Send);
            ////        }
            ////    }
            ////}
            ////catch (Exception ex)
            ////{
            ////    EscribirLog(ex.Message + " en detenerServidor.Shutdown", tipoLog.ERROR);
            ////}

            ////try
            ////{
            ////    if (this.socketDeEscucha != null)
            ////    {
            ////        this.socketDeEscucha.Close();
            ////    }
            ////}
            ////catch (Exception ex)
            ////{
            ////    EscribirLog(ex.Message + " en detenerServidor.Close", tipoLog.ERROR);                
            ////}
            //// Refactorización del bloque para simplificar y mejorar la robustez del cierre del socket de escucha
            //try
            //{
            //    if (this.socketDeEscucha != null)
            //    {
            //        if (this.socketDeEscucha.Connected)
            //        {
            //            // Solo se apaga si está conectado y no está enviando datos
            //            if (!this.socketDeEscucha.Poll(0, SelectMode.SelectWrite))
            //            {
            //                this.socketDeEscucha.Shutdown(SocketShutdown.Send);
            //            }
            //        }
            //        this.socketDeEscucha.Close();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    EscribirLog(ex.Message + " en detenerServidor (Shutdown/Close)", tipoLog.ERROR);
            //}

            //// se recorre la lista de clientes conectados y se adiciona a la lista de clientes para desconectar
            //foreach (T socketDeTrabajoPorCliente in listaClientes.Values)
            //{
            //    listaDeClientesEliminar.Add(socketDeTrabajoPorCliente);
            //}

            //// luego se cierran las conexiones de los clientes en la lista anterior
            //foreach (T socketDeTrabajoPorCliente in listaDeClientesEliminar)
            //{
            //    CerrarConexionForzadaCliente(socketDeTrabajoPorCliente.socketDeTrabajo);
            //}
            //// se limpia la lista
            //listaDeClientesEliminar.Clear();
            //listaClientes.Clear();
            //enEjecucion = false;
            //desconectando = false;

            #endregion
            try
            {
                ClientSemaphoreConnections.Wait();
                disconnecting = true;
            }
            catch (Exception)
            {
                throw;
            }

            Monitor.Enter(clientsList);
            // Cerrar y liberar todos los clientes
            foreach (T cliente in clientsList.Values)
            {
                try
                {
                    // Cerrar socket
                    cliente.SocketOfWork?.Shutdown(SocketShutdown.Both);
                    cliente.SocketOfWork?.Close();

                    // Liberar buffer y referencias
                    if (cliente.saeaOfSendReceive != null)
                    {
                        bufferManager.FreeBuffer(cliente.saeaOfSendReceive, cliente.UniqueClientId);
                        cliente.saeaOfSendReceive.UserToken = null;
                        cliente.saeaOfSendReceive.AcceptSocket = null;
                        // Si no se reutiliza, puedes llamar a Dispose()
                        cliente.saeaOfSendReceive.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    var sb = new StringBuilder();
                    sb.Append("Error al liberar cliente en Stop, cliente ");
                    sb.Append(cliente.UniqueClientId);
                    sb.Append(" , ");
                    sb.Append(ex.Message);
                    Log(sb.ToString(), LogType.Error);
                }
            }
            clientsList.Clear();
            Monitor.Exit(clientsList);


            Monitor.Enter(ClientsPendingDisconnectionList);
            // Cerrar y liberar todos los clientes
            foreach (T cliente in ClientsPendingDisconnectionList)
            {
                try
                {
                    // Cerrar socket
                    cliente.SocketOfWork?.Shutdown(SocketShutdown.Both);
                    cliente.SocketOfWork?.Close();

                    // Liberar buffer y referencias
                    if (cliente.saeaOfSendReceive != null)
                    {
                        bufferManager.FreeBuffer(cliente.saeaOfSendReceive, cliente.UniqueClientId);
                        cliente.saeaOfSendReceive.UserToken = null;
                        cliente.saeaOfSendReceive.AcceptSocket = null;
                        // Si no se reutiliza, puedes llamar a Dispose()
                        cliente.saeaOfSendReceive.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    var sb = new StringBuilder();
                    sb.Append("Error al liberar cliente en Stop, cliente ");
                    sb.Append(cliente.UniqueClientId);
                    sb.Append(" , ");
                    sb.Append(ex.Message);
                    Log(sb.ToString(), LogType.Error);
                }
            }
            ClientsPendingDisconnectionList.Clear();
            Monitor.Exit(ClientsPendingDisconnectionList);


            Monitor.Enter(providersList);
            // Cerrar y liberar todos los proveedores (si tienes una lista)
            if (providersList != null)
            {
                foreach (X proveedor in providersList.Values)
                {
                    try
                    {
                        proveedor.SocketOfWork?.Shutdown(SocketShutdown.Both);
                        proveedor.SocketOfWork?.Close();

                        if (proveedor.saeaSendReceive != null)
                        {
                            bufferManager.FreeBuffer(proveedor.saeaSendReceive, proveedor.clientStateSource.UniqueClientId);
                            proveedor.saeaSendReceive.UserToken = null;
                            proveedor.saeaSendReceive.AcceptSocket = null;
                            // proveedor.saeaDeEnvioRecepcion.Dispose();
                        }

                        //proveedor.providerTimer?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        var sb = new StringBuilder();
                        sb.Append("Error al liberar proveedor en DetenerServidor, proveedor del cliente ");
                        sb.Append(proveedor.clientStateSource.UniqueClientId);
                        sb.Append(" , ");
                        sb.Append(ex.Message);
                        Log(sb.ToString(), LogType.Error);
                    }
                }
            }
            providersList.Clear();
            Monitor.Exit(providersList);


            Monitor.Enter(ProvidersPendingDisconnectionList);
            // Cerrar y liberar todos los proveedores (si tienes una lista)
            if (ProvidersPendingDisconnectionList != null)
            {
                foreach (X proveedor in ProvidersPendingDisconnectionList)
                {
                    try
                    {
                        proveedor.SocketOfWork?.Shutdown(SocketShutdown.Both);
                        proveedor.SocketOfWork?.Close();

                        if (proveedor.saeaSendReceive != null)
                        {
                            bufferManager.FreeBuffer(proveedor.saeaSendReceive, proveedor.clientStateSource.UniqueClientId);
                            proveedor.saeaSendReceive.UserToken = null;
                            proveedor.saeaSendReceive.AcceptSocket = null;
                            // proveedor.saeaDeEnvioRecepcion.Dispose();
                        }

                        //proveedor.providerTimer?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        var sb = new StringBuilder();
                        sb.Append("Error al liberar proveedor en DetenerServidor, proveedor del cliente ");
                        sb.Append(proveedor.clientStateSource.UniqueClientId);
                        sb.Append(" , ");
                        sb.Append(ex.Message);
                        Log(sb.ToString(), LogType.Error);
                    }
                }
            }
            ProvidersPendingDisconnectionList.Clear();
            Monitor.Exit(ProvidersPendingDisconnectionList);

            // Liberar el socket de escucha
            try
            {
                mainListenSocket?.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error al detener socket de escucha en Stop, ");
                sb.Append(ex.Message);
                Log(sb.ToString(), LogType.Error);
            }

            // Liberar PerformanceCounter
            incommigConnectionsPerformanceCounter?.Dispose();

            bufferManager.ClearFullBuffer();
            bufferManager.ClearStackBuffer();


            InExecution = false;
            disconnecting = false;
        }

        /// <summary>
        /// Verificación del tiempo de la transacción sobre el proceso del clente
        /// </summary>
        /// <param name="clientState">instancia del estado del cliente</param>
        /// <returns></returns>
        private bool ValidateTimeOutExpired(T clientState)
        {
            bool isLock = false;
            try
            {
                if (clientState == null)
                {
                    return true;
                }
                isLock = Monitor.TryEnter(clientState, milisecondsTimeOutLock);
                if (isLock)
                {
                    TimeSpan timeSpan = DateTime.Now - clientState.StartDateTrx;
                    return timeSpan.Seconds > clientState.timeOut;
                }
                else
                {
                    throw new Exception("No se pudo bloquear el proceso para validar el timeout del cliente");
                }
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error en ");
                sb.Append(nameof(ValidateTimeOutExpired));
                sb.Append(", ");
                sb.Append(ex.Message);
                sb.Append(". cliente ");
                sb.Append(clientState.UniqueClientId);
                Log(sb.ToString(), LogType.Warning);
                return true;
            }
            finally
            {
                if (isLock)
                    Monitor.Exit(clientState);
            }

        }

        /// <summary>
        /// Valida que la licencia esté vigente
        /// </summary>
        /// <returns></returns>
        private bool ValidateParametersServer()
        {
            var sb = new StringBuilder();
            try
            {
                sb.Append("Validando parámetros del servidor");
                Log(sb.ToString(), Utilities.LogType.Info);
                Telstock.Encrypter.Encrypter encrypter = new Telstock.Encrypter.Encrypter("AdmindeServicios");
                Security security = new Security();

                if (!GetParametersFile(security))
                    return false;

                if (!security.GetInfoPc())
                    return false;

                return string.Compare(Security.PROGRAM, encrypter.DecryptText(security.Licence.Split('|')[(int)Security.eLicence.Program])) == 0
                        //&& DateTime.Compare(localValidity, DateTime.Parse(encrypter.DesEncrypterText(licence.Split('|')[(int)Licence.Validity]))) <= 0
                        && (string.Compare(security.ProcessorId, encrypter.DecryptText(security.Licence.Split('|')[(int)Security.eLicence.ProcessorId])) == 0)
                        && (string.Compare(security.Product, encrypter.DecryptText(security.Licence.Split('|')[(int)Security.eLicence.Product])) == 0)
                        && (string.Compare(security.Manufacturer, encrypter.DecryptText(security.Licence.Split('|')[(int)Security.eLicence.Manufacturer])) == 0);
            }
            catch (Exception ex)
            {
                sb.Clear();
                sb.Append("Error en ");
                sb.Append(nameof(ValidateParametersServer));
                sb.Append(", ");
                sb.Append(ex.Message);
                Log(sb.ToString(), LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// Obtiene el archivo de licencia de la ubicación de la aplicación
        /// </summary>
        /// <returns></returns>
        private bool GetParametersFile(Security security)
        {
            FileStream fileStream;
            try
            {
                using (fileStream = File.OpenRead(Environment.CurrentDirectory + "\\" + Security.PROGRAM + ".txt"))
                {
                    using (StreamReader streamReader = new StreamReader(fileStream))
                    {

                        while (streamReader.EndOfStream == false)
                        {
                            security.Licence = streamReader.ReadLine();
                        }
                    }
                }
                return security.Licence.Length > 0;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("No se pudo leer el archivo de configuración en la ruta ");
                sb.Append(Environment.CurrentDirectory + "\\" + Security.PROGRAM + ".txt");
                sb.Append(", ");
                sb.Append(ex.Message);
                Log(sb.ToString(), LogType.Error);
                return false;
            }
        }
    }
}
