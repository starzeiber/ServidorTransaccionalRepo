using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static ServerCore.ServerConfiguration;
using static ServerCore.Utilities;

namespace ServerCore
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
        private readonly Func<T> clientFactory;
        private readonly Func<S> serverFactory;
        private readonly Func<X> providerFactory;

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

        #region Propiedades públicas

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
        public Dictionary<Guid, T> clientsList;

        /// <summary>
        /// Represents a collection of providers, where each provider is identified by a unique identifier.
        /// </summary>
        /// <remarks>The dictionary maps a <see cref="Guid"/> to an instance of <typeparamref name="X"/>. 
        /// Use this collection to store and retrieve providers based on their unique identifiers.</remarks>
        public Dictionary<Guid, X> providersList;

        /// <summary>        
        /// Obtiene o ingresa el número máximo de conexiones simultaneas de una misma IP del cliente (0=ilimitadas)
        /// </summary>
        public int maximumConnectionsPerClientIp { get; set; }

        /// <summary>
        /// Obtiene o ingresa el valor de que si el servidor está o no ejecutandose
        /// </summary>
        public bool inExecution { get; set; }

        /// <summary>
        /// Obtiene o ingresa el estado del socket del servidor
        /// </summary>
        public S serverStateBase { get; set; }

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
                return clientStateManager.contadorElementos;
            }
        }

        /// <summary>
        /// Gets the total count of supplier states.
        /// </summary>
        public int ProviderStateCounter
        {
            get
            {
                return providerStateManager.contadorElementos;
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
                return bufferManager.ContadorDeBuffersDisponibles;
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
        /// IP de escucha
        /// </summary>
        public string localIp;

        #endregion

        #region Propiedades privadas

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
        /// Número total de bytes recibido en el servidor, para uso estadístico
        /// </summary>
        private int totalBytesRead;

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
        /// Información de la licencia
        /// </summary>
        private enum Licence
        {
            Program = 0,
            Validity = 2,
            ProcessorId = 4,
            Product = 6,
            Manufacturer = 8
        }

        /// <summary>
        /// Nombre del programa
        /// </summary>
        private const string PROGRAM = "UServer";

        /// <summary>
        /// Id del procesador del equipo
        /// </summary>
        private string processorId = "";

        /// <summary>
        /// Producto que se ejecuta
        /// </summary>
        private string product = "";

        /// <summary>
        /// información del fabricante
        /// </summary>
        private string manufacturer = "";

        /// <summary>
        /// Toda la licencia
        /// </summary>
        private string licence = "";

        /// <summary>
        /// Mensaje de aviso
        /// </summary>
        private const string NOTPERMISSION = "No cuenta con permisos para usar la aplicación o falta el archivo de configuración";

        /// <summary>
        /// Represents the current count of ports being tracked.
        /// </summary>
        /// <remarks>This field is intended for internal use only and should not be accessed directly
        /// outside of the containing class.</remarks>
        internal int contadorPuertos = 0;

        #endregion

        /// <summary>
        /// Crea una instancia del administrador de sockets, posterior se tiene que llamar al método
        /// ConfigInicioServidor para iniciar el proceso de asignacion de recursos        
        /// </summary>
        /// <param name="clienteFactory">Función que crea una instancia de la clase EstadoDelClienteBase</param>
        /// <param name="proveedorFactory">Función que crea una instancia de la clase EstadoDelProveedorBase</param>
        /// <param name="servidorFactory">Función que crea una instancia de la clase EstadoDelServidorBase</param>
        /// <param name="numeroConexSimultaneas">Maximo número de conexiones simultaneas a manejar en el servidor</param>
        /// <param name="tamanoBuffer">Tamaño del buffer por conexión, un parámetro standart es 1024</param>
        /// <param name="backlog">Parámetro TCP/IP backlog, el recomendable es 100</param>
        /// <param name="conLogsParaDepuracion">Se habilita para escribir más a logs y tener un mejor rastreo</param>
        public TransactionalServer(Func<T> clienteFactory, Func<S> servidorFactory, Func<X> proveedorFactory, Int32 numeroConexSimultaneas, Int32 tamanoBuffer = 1024, int backlog = 100)
        {
            SetStatesFactories(clienteFactory, servidorFactory, proveedorFactory);
            SetConcurrentConnections(numeroConexSimultaneas, backlog);
            SetClientsAndProvidersLists();
            SetBuffer(tamanoBuffer);
            // establezco el proceso principal para referencia futura
            serverStateBase.procesoPrincipal = this;
            // indico que aún no está en funcionamiento, faltan parámetros
            inExecution = false;

            //Asignación de un buffer tomando en cuenta por lo menos los 3 sockets por defecto para lectura y escritura iniciales
            // es decir, el tamaño del buffer por operación por el número de conexiónes por el número de sockets iniciales no dará
            // el valor de buffer enorme en bytes, por ejemplo: tamanoBuffer= 1024 * 1000 * 3 =2048000 bytes
            this.bufferManager = new BufferManager(tamanoBuffer * numberOfSimultaneousClientConnections * preAvailableOperations, sizeBufferPerRequest);

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
        private void SetBuffer(int bufferSize)
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
            clientsList = new Dictionary<Guid, T>();
            providersList = new Dictionary<Guid, X>();
            ClientsPendingDisconnectionList = new List<T>();
            ProvidersPendingDisconnectionList = new List<X>();
        }

        /// <summary>
        /// Configures the maximum number of concurrent connections and the connection backlog.
        /// </summary>
        /// <param name="numeroConexSimultaneas">The maximum number of simultaneous connections allowed for both clients and providers.  A value of 0
        /// indicates no limit.</param>
        /// <param name="backlog">The maximum number of pending connections that can be queued before being accepted.</param>
        private void SetConcurrentConnections(int numeroConexSimultaneas, int backlog)
        {
            numberOfSimultaneousClientConnections = numeroConexSimultaneas;
            numberOfSimultaneousProviderConnections = numeroConexSimultaneas;
            //Se coloca ilimitado para fines no restrictivos
            maximumConnectionsPerClientIp = 0;
            backLog = backlog;
        }

        /// <summary>
        /// Configures the factories used to create instances of the client, server, and provider states.
        /// </summary>
        /// <remarks>This method initializes the server state using the provided <paramref
        /// name="servidorFactory"/>.  If an error occurs during the creation of the server state, an error message is
        /// logged. Ensure that the class used for the server state has a parameterless constructor.</remarks>
        /// <param name="clienteFactory">A factory method that creates an instance of the client state. Cannot be <see langword="null"/>.</param>
        /// <param name="servidorFactory">A factory method that creates an instance of the server state. Cannot be <see langword="null"/>.</param>
        /// <param name="proveedorFactory">A factory method that creates an instance of the provider state. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="clienteFactory"/>, <paramref name="servidorFactory"/>, or <paramref
        /// name="proveedorFactory"/> is <see langword="null"/>.</exception>
        private void SetStatesFactories(Func<T> clienteFactory, Func<S> servidorFactory, Func<X> proveedorFactory)
        {
            clienteFactory = clienteFactory ?? throw new ArgumentNullException(nameof(clienteFactory));
            servidorFactory = servidorFactory ?? throw new ArgumentNullException(nameof(servidorFactory));
            proveedorFactory = proveedorFactory ?? throw new ArgumentNullException(nameof(proveedorFactory));
            try
            {
                serverStateBase = servidorFactory();
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error al crear la instancia del servidor, revise que la clase derivada de EstadoDelServidorBase tenga un constructor sin parámetros. ");
                sb.Append(ex.Message);
                sb.Append(" ServidorTransaccional");
                EscribirLog(sb.ToString(), tipoLog.ERROR);
            }
        }

        /// <summary>
        /// Inicializa el servidor con una pre asignación de buffers reusables y estados de sockets
        /// </summary>
        public void ServerConfiguration(int timeOut)
        {
            ServerCore.ServerConfiguration.timeOutCliente = timeOut;
            SetPerformanceCounters();

            if (!ValidateParametersServer())
            {
                EscribirLog(NOTPERMISSION, tipoLog.ERROR);
                Environment.Exit(666);
            }

            //Se prepara un buffer suficientemente grande para todas las operaciones y poder reutilizarlo por secciones
            bufferManager.inicializarBuffer();
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
                    bufferManager.asignarBuffer(saeaDeEnvioRecepcionAlProveedor);
                    //Se establece el socket asincrono de EventArg a utilizar en las operaciones con el proveedor
                    estadoDelProveedor.saeaSendReceive = saeaDeEnvioRecepcionAlProveedor;

                    //Ya con los parametros establecidos para cada operacion, se ingresa en la pila
                    //de estados del proveedor y desde ahi administar su uso en cada petición
                    providerStateManager.ingresarUnElemento(estadoDelProveedor);
                }
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error al inicializar los estados de socket del cliente, verifique que las clases derivadas de ");
                sb.Append(nameof(X));
                sb.Append(" ");
                sb.Append(ex.Message);
                sb.Append(" ConfigInicioServidor ");
                EscribirLog(sb.ToString(), tipoLog.ERROR);
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
                    //T estadoDelCliente = new T();
                    T estadoDelCliente = clientFactory();
                    estadoDelCliente.InitializeClientStateBase();

                    //objetos para operaciones asincronas en los sockets de los clientes
                    SocketAsyncEventArgs saeaDeEnvioRecepcionCliente;
                    saeaDeEnvioRecepcionCliente = new SocketAsyncEventArgs();
                    //El manejador de eventos para cada lectura de una peticion del cliente
                    saeaDeEnvioRecepcionCliente.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveSendIncomingProcessCallBack);
                    //SocketAsyncEventArgs necesita un objeto con la información de cada cliente para su administración
                    saeaDeEnvioRecepcionCliente.UserToken = estadoDelCliente;
                    //Se establece el buffer que se utilizará en la operación de lectura del cliente en el eventArgDeRecepcion
                    bufferManager.asignarBuffer(saeaDeEnvioRecepcionCliente);
                    //Se establece el socket asincrono de EventArg a utilizar en la lectura del cliente
                    estadoDelCliente.saeaOfSendReceive = saeaDeEnvioRecepcionCliente;

                    //Ya con los parametros establecidos para cada operacion, se ingresa en la pila
                    //de estados del cliente y desde ahi administar su uso en cada petición
                    clientStateManager.ingresarUnElemento(estadoDelCliente);
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
                EscribirLog(sb.ToString(), tipoLog.ERROR);
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
                EscribirLog(sb.ToString(), tipoLog.ERROR);
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
                //var maxPoolSize = (numeroConexionesSimultaneasProveedor * 10) / 100;
                socketPool = new SocketPool(numberOfSimultaneousProviderConnections);
            }
            catch (Exception ex)
            {
                EscribirLog("Error al inicializar el pool de sockets para el proveedor. " + ex.Message + " ConfigInicioServidor ", tipoLog.ERROR);
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
            bool seSincronzo = Monitor.TryEnter(ProviderPortsList, 1000);
            IPEndPoint endPointProveedor;
            if (seSincronzo)
            {
                try
                {
                    //192.168.69.91
                    if (contadorPuertos == 0)
                    {
                        endPointProveedor = new IPEndPoint(iPAddress, ProviderPortsList.First());
                    }
                    else
                    {
                        endPointProveedor = new IPEndPoint(iPAddress, ProviderPortsList[contadorPuertos - 1]);
                    }
                }
                catch
                {
                    endPointProveedor = new IPEndPoint(iPAddress, ProviderPortsList.First());
                }
                finally
                {
                    Monitor.Exit(ProviderPortsList);
                }
            }
            else
            {
                endPointProveedor = new IPEndPoint(iPAddress, ProviderPortsList.First());
            }
            if (contadorPuertos == ProviderPortsList.Count)
            {
                Interlocked.Exchange(ref contadorPuertos, 0);
            }
            else
            {
                Interlocked.Increment(ref contadorPuertos);
            }

            return endPointProveedor;
        }

        /// <summary>
        /// Se inicia el servidor de manera que esté escuchando solicitudes de conexión entrantes.
        /// </summary>
        /// <param name="puertoLocal">Puerto de escucha del servidor</param>
        /// <param name="ipProveedor">Ip del servidor del proveedor</param>
        /// <param name="listaPuertosProveedor">Puertos del proveedor</param>
        /// <param name="modoTest">Modo pruebas</param>
        /// <param name="modoRouter">Indicador de que el servidor tendrá la función de enviar mensajes a otro proveedor</param>
        public void Start(Int32 puertoLocal, string ipProveedor, List<int> listaPuertosProveedor, bool modoTest, bool modoRouter)
        {
            //Se inicializa la bandera de que no hay ningún cliente pendiente por desconectar
            disconnecting = false;
            ServerCore.ServerConfiguration.modoTest = modoTest;
            ServerCore.ServerConfiguration.modoRouter = modoRouter;
            //De acuerdo a las buenas practicas de manejo de operaciones asincronas, se debe ANUNCIAR el inicio
            //de un trabajo asincrono para ir controlando su avance por eventos si fuera necesario
            serverStateBase.OnInicio();

            this.ProviderIp = ipProveedor;
            this.ProviderPortsList = listaPuertosProveedor;
            SetSocketPool();

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, puertoLocal);

            // se crea el socket que se utilizará de escucha para las conexiones entrantes
            mainListenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // se asocia con el puerto de escucha el socket de escucha
            this.mainListenSocket.Bind(localEndPoint);

            localIp = mainListenSocket.LocalEndPoint.ToString().Split(':')[0];

            // se inicia la escucha de conexiones con un backlog de 100 conexiones
            this.mainListenSocket.Listen(backLog);

            contadorPuertos = listaPuertosProveedor.Count;

            // Se indica al sistema que se empiezan a aceptar conexiones, se envía una referencia a null para que se indique que es la primera vez
            this.StartAccepting(null);
            inExecution = true;
            //IPAddress iPAddress = IPAddress.Parse(ipProveedor);
            //socketDelProveedor = new Socket(new IPEndPoint(iPAddress, listaPuertosProveedor.First()).AddressFamily, SocketType.Stream, ProtocolType.Tcp);

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
            try
            {
                incommigConnectionsPerformanceCounter.IncrementBy(1);
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
                EscribirLog(sb.ToString(), tipoLog.ERROR);
                // se hace un último intento para volver a iniciar el servidor por si el error fue una excepción al empezar la aceptación
                StartAccepting(saeaConnectionAccept);
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
                    EscribirLog(sb.ToString(), tipoLog.ERROR);
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
            T estadoDelCliente = clientStateManager.obtenerUnElemento();
            // limpio el estado para reutilizar solo el saea no los parámetros
            estadoDelCliente.InitializeClientStateBase();
            // debo colocar la referencia del proceso principal donde genero el estado del cliente para tenerlo como referencia de retorno
            estadoDelCliente.SetMainSocketReference(this);
            // Del SAEA de aceptación de conexión, se recupera el socket para asignarlo al estado del cliente obtenido del pool de estados
            estadoDelCliente.SocketToWork = saea.AcceptSocket;

            //Se establece el buffer que se utilizará en la operación de lectura del cliente en el eventArgDeRecepcion
            if (estadoDelCliente.saeaOfSendReceive.Buffer == null)
                bufferManager.asignarBuffer(estadoDelCliente.saeaOfSendReceive);

            //  de la misma forma se ingresa la ip y puerto del cliente que se aceptó
            estadoDelCliente.ClientIp = (saea.AcceptSocket.RemoteEndPoint as IPEndPoint).Address.ToString();
            estadoDelCliente.ClientPort = (saea.AcceptSocket.RemoteEndPoint as IPEndPoint).Port;

            // con estas instrucciones puedo controlar las acciones en cada fase del proceso de recepción y envío de ser necesario
            serverStateBase.OnAceptacion(estadoDelCliente);
            if (!AddClientToClientList(estadoDelCliente))
            {
                // si no puedo ingresarlo en la lista de clientes debo rechazarlo porque no tendría control para manipularlo en un futuro                
                ClientSocketClose(estadoDelCliente);
                // coloco nuevamente el socket en proceso de aceptación con el mismo saea para un reintento de conexión
                this.StartAccepting(saea);
                return;
            }

            // se inicia la recepción de datos del cliente si es que sigue conectado dicho cliente  
            if (estadoDelCliente.SocketToWork.Connected)
            {
                try
                {
                    // se ingresa la configuración del buffer para la recepción del mensaje                    
                    estadoDelCliente.saeaOfSendReceive.SetBuffer(estadoDelCliente.saeaOfSendReceive.Offset, sizeBufferPerRequest);
                    // se procede a la recepción asincrona del mensaje,el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                    bool seHizoAsync = estadoDelCliente.SocketToWork.ReceiveAsync(estadoDelCliente.saeaOfSendReceive);
                    if (!seHizoAsync)
                        // se llama a la función que completa el flujo de envío, 
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        ReceiveSendIncomingProcessCallBack(estadoDelCliente.SocketToWork, estadoDelCliente.saeaOfSendReceive);
                }
                catch (Exception ex)
                {
                    var sb = new StringBuilder();
                    sb.Append("Error al intentar recibir el mensaje del cliente, se cerrará la conexión, ");
                    sb.Append(nameof(StartAcceptingCallBack));
                    sb.Append(", cliente: ");
                    sb.Append(estadoDelCliente.UniqueClientId);
                    sb.Append(", ");
                    sb.Append(ex.Message);
                    EscribirLog(sb.ToString(), tipoLog.ERROR);
                    ClientSocketClose(estadoDelCliente);
                }
            }
            else
            {
                var sb = new StringBuilder();
                sb.Append("No se aceptó la conexión porque el socket no está conectado, AceptarConexionCallBack, cliente: ");
                sb.Append(estadoDelCliente.UniqueClientId);
                sb.Append(", ");
                sb.Append(saea.SocketError.ToString());
                EscribirLog(sb.ToString(), tipoLog.ALERTA);
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
            bool seSincronzo = Monitor.TryEnter(clientsList, 1000);
            try
            {
                if (seSincronzo)
                {
                    if (!clientsList.ContainsKey(clientState.UniqueClientId))
                    {
                        clientsList.Add(clientState.UniqueClientId, clientState);
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
                EscribirLog(sb.ToString(), tipoLog.ERROR);
                return false;
            }
            finally
            {
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
            if (!(saea.UserToken is T estadoDelCliente))
            {
                sb.Append("No se pudo obtener el estado del cliente, RecepcionEnvioEntranteCallBack, ");
                EscribirLog(sb.ToString(), tipoLog.ERROR);
                return;
            }

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
                            ReceivingProcess(estadoDelCliente);
                        }
                        else
                        {
                            //EscribirLog("No hay datos que recibir", tipoLog.ALERTA);
                            // si no hay datos por X razón, se cierra el cliente porque puede perdurar indefinidamente la conexión                            
                            ClientSocketClose(estadoDelCliente);
                        }
                    }
                    else
                    {
                        sb.Append("Error en el proceso de recepción, socket no conectado correctamente, cliente:");
                        sb.Append(estadoDelCliente.UniqueClientId);
                        sb.Append(", ");
                        sb.Append(saea.SocketError.ToString());
                        EscribirLog(sb.ToString(), tipoLog.ALERTA);
                        //se cierra el cliente porque puede perdurar indefinidamente la conexión
                        ClientSocketClose(estadoDelCliente);
                    }
                    break;
                case SocketAsyncOperation.Send:
                    //indico que estaré enviando algo al cliente para que otro proceso con la misma conexión no quiera enviar algo al mismo tiempo
                    //TODO  ver si es necesario porque no hay un wait
                    estadoDelCliente.esperandoEnvio.Set();

                    // se comprueba que no hay errores con el socket
                    if (saea.SocketError == SocketError.Success)
                    {
                        //Intento colocar el socket de nuevo en escucha por si el cliente envía otra trama con la misma conexión
                        ReceiveIncomingProcessCiclicToClient(estadoDelCliente);
                    }
                    else
                    {
                        sb.Append("Error en el proceso de envío, socket no conectado correctamente, cliente:");
                        sb.Append(estadoDelCliente.UniqueClientId);
                        EscribirLog(sb.ToString(), tipoLog.ALERTA);
                        estadoDelCliente.SetResponseProcess();
                        // si no hay datos por X razón, se cierra el cliente porque puede perdurar indefinidamente la conexión                        
                        ClientSocketClose(estadoDelCliente);
                    }
                    break;
                default:
                    // se da por errores de TCP/IP en alguna intermitencia
                    sb.Append("La ultima operación no se detecto como de recepcion o envío, RecepcionEnvioEntranteCallBack, ");
                    sb.Append(estadoDelCliente.UniqueClientId);
                    sb.Append(", ");
                    sb.Append(saea.SocketError.ToString());
                    sb.Append(", ");
                    sb.Append(saea.LastOperation.ToString());
                    EscribirLog(sb.ToString(), tipoLog.ERROR);
                    ClientSocketClose(estadoDelCliente);
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
            //por precaución se coloca que no se está procesando respuesta
            clientState.ReleaseResponseProcess();

            // se ingresa el cliente a la lista de clientes
            AddClientToList(clientState);

            // se obtiene el objeto que tiene la información del socket y el buffer
            SocketAsyncEventArgs saeaDeEnvioRecepcion = clientState.saeaOfSendReceive;
            string message = GetMessageFromClient(clientState, saeaDeEnvioRecepcion);
            if (string.IsNullOrEmpty(message))
            {
                ClientSocketClose(clientState);
                return;
            }
            ValidateTotalBytesCounter(saeaDeEnvioRecepcion.BytesTransferred);

            // el mensaje recibido llevará un proceso, que no debe ser llevado por el core, se coloca en la función virtual
            // para que se consuma en otra capa, se procese y se entregue una respuesta

            // bloqueo los procesos sobre este mismo cliente hasta no terminar con esta petición para no tener revolturas de mensajes
            clientState.esperandoEnvio.Reset();
            clientState.msg210 = "";
            clientState.msg230 = "";
            clientState.isQuery = false;
            try
            {
                // aquí se debe realizar lo necesario con la trama entrante para preparar la trama al proveedor en la variable tramaEnvioProveedor
                clientState.ProcessMessage(message);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append(ex.Message);
                sb.Append(" Error al procesar la trama, se descarta el mensaje del cliente: ");
                sb.Append(clientState.UniqueClientId);
                EscribirLog(sb.ToString(), tipoLog.ERROR);
            }

            //Verifico si se venció el TO mientras procesaba la trama
            if (ValidateTimeOutExpired(clientState))
            {
                var sb = new StringBuilder();
                sb.Append("Se venció el TimeOut para el cliente ");
                sb.Append(clientState.UniqueClientId.ToString());
                sb.Append(", durante el procesamiento de la trama");
                EscribirLog(sb.ToString(), tipoLog.ALERTA);
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
                // cuando haya terminado la clase estadoDelCliente de procesar la trama, se debe evaluar su éxito para enviar la solicitud al proveedor
                if (clientState.responseCode == (int)CodigosRespuesta.TransaccionExitosa)
                {
                    if (!modoTest)
                    {
                        if (modoRouter)
                        {
                            StartProcessProvider(clientState);
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
                    sb.Append("Código de respuesta inválido para continuar el proceso, cliente ");
                    sb.Append(clientState.UniqueClientId);
                    EscribirLog(sb.ToString(), tipoLog.ALERTA);
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
                sb.Append(ex.Message);
                sb.Append(", ");
                sb.Append(ex.StackTrace);
                sb.Append(" Error en el procesamiento de la trama, se cierra la conexión del cliente ");
                sb.Append(clientState.UniqueClientId);
                EscribirLog(sb.ToString(), tipoLog.ERROR);
                ClientSocketClose(clientState);
            }
        }

        /// <summary>
        /// Updates the total bytes counter with the specified number of bytes transferred.
        /// </summary>
        /// <remarks>This method ensures thread-safe updates to the total bytes counter using interlocked
        /// operations. If the total bytes counter reaches the predefined limit, it is reset to zero.</remarks>
        /// <param name="bytesTransferred">The number of bytes to add to the total bytes counter. Must be a non-negative value.</param>
        private void ValidateTotalBytesCounter(int bytesTransferred)
        {
            // incrementa el contador de bytes totales recibidos para tener estadísticas nada más
            // debido a que la variable está compartida entre varios procesos, se utiliza interlocked que ayuda a que no se revuelvan
            if (totalBytesRead == LIMITE_BYTES_CONTADOR)
            {
                Interlocked.Exchange(ref this.totalBytesRead, 0);
            }
            else
            {
                Interlocked.Add(ref this.totalBytesRead, bytesTransferred);
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
                    EscribirLog(sb.ToString(), tipoLog.INFORMACION);
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.Append("Mensaje recibido: ");
                    sb.Append(message.Trim().Substring(2));
                    sb.Append(" del cliente: ");
                    sb.Append(clientState.UniqueClientId);
                    EscribirLog(sb.ToString(), tipoLog.INFORMACION);
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
                EscribirLog(sb.ToString(), tipoLog.ERROR);
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
        private static void AddClientToList(T clientState)
        {
            try
            {
                //Para ir midiendo el TO por cada recepción
                bool blocking = Monitor.TryEnter(clientState.StartDateTrx, 1000);
                if (blocking)
                {
                    clientState.StartDateTrx = DateTime.Now;
                    var sb = new StringBuilder();
                    sb.Append("Fecha de recepción ");
                    sb.Append(clientState.StartDateTrx);
                    sb.Append(" del cliente: ");
                    sb.Append(clientState.UniqueClientId);
                    EscribirLog(sb.ToString(), tipoLog.INFORMACION, true);
                }
                else
                {
                    throw new Exception("No se pudo bloquear el proceso para establecer la fecha de inicio de la transacción");
                }
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append(ex.Message);
                sb.Append(" para el cliente: ");
                sb.Append(clientState.UniqueClientId);
                EscribirLog(sb.ToString(), tipoLog.ERROR);
            }
            finally
            {
                Monitor.Exit(clientState.StartDateTrx);
            }
        }

        /// <summary>
        /// Función que entrega una respuesta al cliente por medio del socket de conexión
        /// </summary>
        /// <param name="clientState">Estado del cliente con los valores de retorno</param>
        private void ResponseToClient(T clientState)
        {
            if (clientState == null || clientState.isResponding == 1)
            {
                return;
            }

            clientState.SetResponseProcess();

            // trato de obtener la trama que se le responderá al cliente
            clientState.GetResponseMessage();

            // Si ya se cuenta con una respuesta(s) para el cliente
            if (clientState.messageResponse != "")
            {
                if (!modoTest)
                    clientState.UpdateTransaction();
                string responseMessage = GetResponseMessage(clientState);
                int numeroDeBytes;
                if (!GetBytesCounter(clientState, responseMessage, out numeroDeBytes))
                {
                    ClientSocketClose(clientState);
                    return;
                }

                try
                {
                    // Se solicita el espacio de buffer para los bytes que se van a enviar                    
                    clientState.saeaOfSendReceive.SetBuffer(clientState.saeaOfSendReceive.Offset, numeroDeBytes);
                }
                catch (Exception ex)
                {
                    var sb = new StringBuilder();
                    sb.Append("Error asignando buffer para la respuesta al cliente ");
                    sb.Append(clientState.UniqueClientId);
                    sb.Append(". ");
                    sb.Append(ex.Message);
                    EscribirLog(sb.ToString(), tipoLog.ERROR);
                    ClientSocketClose(clientState);
                    return;
                }

                try
                {

                    // se envía asincronamente por medio del socket copia de recepción que es
                    // con el que se está trabajando en esta operación, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                    bool seHizoAsync = clientState.SocketToWork.SendAsync(clientState.saeaOfSendReceive);
                    if (!seHizoAsync)
                        // Si se tiene una respuesta False de que el proceso está pendiente, se completa el flujo,
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        ReceiveSendIncomingProcessCallBack(clientState.SocketToWork, clientState.saeaOfSendReceive);
                }
                catch (Exception ex)
                {
                    var sb = new StringBuilder();
                    sb.Append("Error enviando la respuesta al cliente ");
                    sb.Append(clientState.UniqueClientId);
                    sb.Append(". ");
                    sb.Append(ex.Message);
                    sb.Append(" ResponderAlCliente. ");
                    EscribirLog(sb.ToString(), tipoLog.ERROR);
                    ClientSocketClose(clientState);
                    return;
                }
            }
            else  // Si el proceso no tuvo una respuesta o se descartó por error, se procede a volver a escuchar para recibir la siguiente trama del mismo cliente
            {
                if (clientState.SocketToWork.Connected)
                {
                    try
                    {
                        // se solicita el espacio de buffer para la recepción del mensaje
                        clientState.saeaOfSendReceive.SetBuffer(clientState.saeaOfSendReceive.Offset, sizeBufferPerRequest);
                        // se solicita un proceso de recepción asincrona, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                        // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                        bool seHizoAsync = clientState.SocketToWork.ReceiveAsync(clientState.saeaOfSendReceive);
                        if (!seHizoAsync)
                            // si el evento indica que el proceso asincrono está pendiente, se completa el flujo,
                            // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                            // en su evento callback
                            ReceiveSendIncomingProcessCallBack(clientState.SocketToWork, clientState.saeaOfSendReceive);
                    }
                    catch (Exception ex)
                    {
                        var sb = new StringBuilder();
                        sb.Append("Error al intentar recibir el mensaje del cliente, se cerrará la conexión, cliente ");
                        sb.Append(clientState.UniqueClientId);
                        sb.Append(", ");
                        sb.Append(ex.Message);
                        sb.Append(" ResponderAlCliente. ");
                        EscribirLog(sb.ToString(), tipoLog.ERROR);
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
                    var sb = new StringBuilder();
                    sb.Append("La respuesta es más grande que el buffer, cliente ");
                    sb.Append(clientState.UniqueClientId);
                    EscribirLog(sb.ToString(), tipoLog.ALERTA);
                    return false;
                }
            }
            catch (Exception)
            {
                var sb = new StringBuilder();
                sb.Append("La respuesta es más grande que el buffer, cliente ");
                sb.Append(clientState.UniqueClientId);
                EscribirLog(sb.ToString(), tipoLog.ALERTA);
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
                    EscribirLog(sb.ToString(), tipoLog.INFORMACION);
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.Append("Mensaje de respuesta: ");
                    sb.Append(responseMessage.Substring(2));
                    sb.Append(" al cliente ");
                    sb.Append(clientState.UniqueClientId);
                    EscribirLog(sb.ToString(), tipoLog.INFORMACION);
                }
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append(ex.Message);
                sb.Append(" Error al identificar si tiene encabezado el mensaje de respuesta, se intenta escribir pero se descarta ");
                sb.Append(responseMessage);
                sb.Append(" al cliente ");
                sb.Append(clientState.UniqueClientId);
                EscribirLog(sb.ToString(), tipoLog.INFORMACION);
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
                // se asigna el buffer para continuar el envío
                clientState.saeaOfSendReceive.SetBuffer(clientState.saeaOfSendReceive.Offset, sizeBufferPerRequest);
                // se inicia el proceso de recepción asincrona, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                bool seHizoAsync = clientState.SocketToWork.ReceiveAsync(clientState.saeaOfSendReceive);
                if (!seHizoAsync)
                    // si el evento indica que el proceso está pendiente, se completa el flujo,
                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                    // en su evento callback
                    ReceiveSendIncomingProcessCallBack(clientState.SocketToWork, clientState.saeaOfSendReceive);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error al intentar recibir el mensaje del cliente, se cerrará la conexión, cliente ");
                sb.Append(clientState.UniqueClientId);
                sb.Append(", ");
                sb.Append(ex.Message);
                sb.Append(" ");
                sb.Append(nameof(ReceiveIncomingProcessCiclicToClient));
                EscribirLog(sb.ToString(), tipoLog.ERROR);
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
                sb.Append("No se pudo obtener el estado del cliente, CerrarSocketCliente ");
                EscribirLog(sb.ToString(), tipoLog.ERROR);
                return;
            }

            if (!RemoveClientToClientList(clientState))
            {
                ClientsPendingDisconnectionList.Add(clientState); // se agrega a una lista de pendientes por desconectar
            }

            // se obtiene el socket específico del cliente en cuestión
            Socket socketDeTrabajoACerrar = clientState.SocketToWork;

            // se inhabilita y se cierra dicho socket
            try
            {
                socketDeTrabajoACerrar.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append(ex.Message);
                sb.Append(" en CerrarSocketCliente, shutdown de envío en el socket de trabajo del cliente ");
                sb.Append(clientState.UniqueClientId);
                EscribirLog(sb.ToString(), tipoLog.ALERTA);
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
                sb.Append(" en CerrarSocketCliente, close en el socket de trabajo del cliente ");
                sb.Append(clientState.UniqueClientId);
                EscribirLog(sb.ToString(), tipoLog.ERROR);
            }

            // se llama a la secuencia de cerrando para tener un flujo de eventos
            serverStateBase.OnClienteCerrado(clientState);

            // se libera la instancia de socket de trabajo para reutilizarlo
            // Antes de liberar el cliente al pool, libera el buffer
            if (clientState.saeaOfSendReceive != null)
            {
                EscribirLog("Liberando buffer del cliente " + clientState.UniqueClientId.ToString(), tipoLog.INFORMACION);
                bufferManager.LiberarBuffer(clientState.saeaOfSendReceive, clientState.UniqueClientId);
                clientState.saeaOfSendReceive.AcceptSocket = null;
            }
            clientStateManager.ingresarUnElemento(clientState);
            // se marca el semáforo de que puede aceptar otro cliente

            if (ClientSemaphoreConnections.CurrentCount < numberOfSimultaneousClientConnections)
            {
                ClientSemaphoreConnections.Release();
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
        private bool RemoveClientToClientList(T clientState)
        {            
            try
            {
                // proporciona un mecanismo de sincronización de acceso a datos donde un hilo solo puede tener acceso a un
                // bloque de código a la vez, en este caso en ingresar al listado de clientes, de lo contrario habría 
                // cross threading y provocaría error
                bool bloqueo = Monitor.TryEnter(clientsList, 1000);
                if (bloqueo)
                {
                    // se busca en la lista el cliente y se remueve porque se va a desconectar
                    if (clientsList.ContainsKey(clientState.UniqueClientId))
                    {
                        clientsList.Remove(clientState.UniqueClientId);
                    }
                    else
                    {
                        // quiere decir que ya está desconectado
                        var sb = new StringBuilder();
                        sb.Append("No se encontró el cliente ");
                        sb.Append(clientState.UniqueClientId.ToString());
                        sb.Append(" en listaClientes a desconectar, ya ha sido desconectado en otro proceso");
                        EscribirLog(sb.ToString(), tipoLog.ALERTA);
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
                EscribirLog(sb.ToString(), tipoLog.ERROR);
                return false;
            }
            finally
            {
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
        private void StartProcessProvider(ClientStateBase clientState)
        {
            // me espero a ver si tengo disponibilidad de SAEA para un proveedor
            ProviderSemaphoreConnections.Wait();

            //TODO si ya se va a obtener un socket conectado, creo que no es necesario
            //Se prepara el estado del proveedor que servirá como operador de envío y recepción de trama
            SocketAsyncEventArgs saeaProveedor = new SocketAsyncEventArgs();
            saeaProveedor.Completed += new EventHandler<SocketAsyncEventArgs>(ConnectionAcceptProviderCallBack);

            IPEndPoint endPointProveedor = GetIPEndPointFromProviderPortsList();
            saeaProveedor.UserToken = clientState;
            try
            {
                Socket providerSocket = socketPool.GetSocket(endPointProveedor);
                if (providerSocket == null)
                {
                    throw new Exception("No se pudo obtener un socket del pool");
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
                ConnectionAcceptProviderCallBack(providerSocket, saeaProveedor);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error al intentar conectar con el proveedor, se cerrará la conexión, cliente ");
                sb.Append(clientState.UniqueClientId);
                sb.Append(", ");
                sb.Append(ex.Message);
                EscribirLog(sb.ToString(), tipoLog.ERROR);

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
            // Si hay errores, debo regresar el estado del proveedor que se está usando a la pila de estados para ser reutilizado            
            if (saea == null)
            {
                sb.Append("SocketAsyncEventArgs es nulo en ");
                sb.Append(nameof(ConnectionAcceptProviderCallBack));
                EscribirLog(sb.ToString(), tipoLog.ERROR);
                return;
            }

            T clientState = saea.UserToken as T;
            X providerState = providerStateManager.obtenerUnElemento();
            // ingreso la información de peticion para llenar las clases al proveedor
            providerState.InitializeProviderStateBase();
            providerState.SetObjClientRequest(clientState.objRequest);
            providerState.SetClientState(clientState);
            providerState.endPoint = (IPEndPoint)saea.RemoteEndPoint;
            providerState.TimeOutExpired += ProviderTimeOutExpired;

            //Se establece el buffer que se utilizará en la operación de lectura del cliente en el eventArgDeRecepcion
            if (providerState.saeaSendReceive.Buffer == null)
                bufferManager.asignarBuffer(providerState.saeaSendReceive);

            if (providerState.responseCode != (int)CodigosRespuesta.TransaccionExitosa)
            {
                providerState.SetAuthorizationCode(0);
                providerState.clientStateSource.SetResponseCode(providerState.responseCode);
                providerState.clientStateSource.SetAuthorizationCode(providerState.authorizacionCode);
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
                sb.Append(", AceptarConexionProveedorCallBack ");
                sb.Append(providerState.endPoint.ToString());
                sb.Append(", cliente ");
                sb.Append(providerState.clientStateSource.UniqueClientId);
                EscribirLog(sb.ToString(), tipoLog.ERROR);

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
                sb.Append("estadoDelProveedor.socketDeTrabajo recibido es inválido para la operacion, ");
                sb.Append(ex.Message);
                sb.Append(" AceptarConexionProveedorCallBack, obteniendo el socket de trabajo, cliente ");
                sb.Append(providerState.clientStateSource.UniqueClientId);
                EscribirLog(sb.ToString(), tipoLog.ERROR);

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
            providerState.GetRequestMessage();

            // Se guarda  la transacción para posterior actualizarla
            providerState.SaveTransaction();

            try
            {
                if (!AddProviderToProvidersList(providerState))
                {
                    throw new Exception();
                }
                providerState.SetInUse();
                providerState.saeaSendReceive.UserToken = providerState;
                if (providerState.SocketOfWork.Connected)
                {
                    string messageToProvider = providerState.messageRequest;
                    LogMessageToProvider(providerState);
                    // se obtiene la cantidad de bytes de la trama completa
                    int numeroDeBytes = GetMessageBytes(providerState, messageToProvider);
                    if (numeroDeBytes == 0)
                    {
                        ProviderSocketClose(providerState);
                        return;
                    }

                    // Se prepara el buffer del SAEA con el tamaño predefinido                         
                    providerState.saeaSendReceive.SetBuffer(providerState.saeaSendReceive.Offset, numeroDeBytes);

                    //140824 se valida que exista tiempo suficiente para que el proveedor (procesa) realice la tarea, el tiempo por defecto es 25 seg
                    if (!ValidateTimeRemaining(providerState, out int timeremaining))
                    {
                        throw new Exception("No hay tiempo restante para enviar la trama al proveedor");
                    }
                    var _ = providerState.TimeOutCounterAsync(timeremaining);

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
                EscribirLog(sb.ToString(), tipoLog.ERROR);
                providerState.SetFree();
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
            bool isSync = Monitor.TryEnter(providersList, 1000);
            try
            {
                if (isSync)
                {
                    if (!providersList.ContainsKey(providerState.clientStateSource.UniqueClientId))
                    {
                        providersList.Add(providerState.clientStateSource.UniqueClientId, providerState);
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
                sb.Clear();
                sb.Append("Error al intentar ingresar el proveedor a la lista de proveedores, se cerrará la conexión, ");
                sb.Append(nameof(AddProviderToProvidersList));
                sb.Append(", cliente: ");
                sb.Append(providerState.clientStateSource.UniqueClientId);
                sb.Append(", ");
                sb.Append(ex.Message);
                EscribirLog(sb.ToString(), tipoLog.ERROR);
                return false;
            }
            finally
            {
                Monitor.Exit(providersList);
            }
        }

        /// <summary>
        /// Converts the specified message to a byte array and writes it to the buffer of the given provider state.
        /// </summary>
        /// <remarks>If the size of the message exceeds the allocated buffer size, the method logs a
        /// warning and the message is considered invalid. In the event of an error during the conversion process, the
        /// method logs the exception details along with the message and client information.</remarks>
        /// <param name="estadoDelProveedor">The state object representing the provider, which contains the buffer and offset for writing the message
        /// bytes.</param>
        /// <param name="messageToProvider">The message to be converted to bytes and written to the provider's buffer.</param>
        /// <returns>The number of bytes written to the buffer.</returns>
        private int GetMessageBytes(X estadoDelProveedor, string messageToProvider)
        {
            int numeroDeBytes = 0;
            var sb = new StringBuilder();
            try
            {
                numeroDeBytes = Encoding.Default.GetBytes(messageToProvider, 0, messageToProvider.Length, estadoDelProveedor.saeaSendReceive.Buffer, estadoDelProveedor.saeaSendReceive.Offset);
                // si el número de bytes es mayor al buffer que se tiene destinado a la recepción, no se puede proceder, no es válido el mensaje
                if (numeroDeBytes > sizeBufferPerRequest)
                {
                    sb.Append("El mensaje al proveedor es más grande que el buffer, cliente: ");
                    sb.Append(estadoDelProveedor.clientStateSource.UniqueClientId);
                    EscribirLog(sb.ToString(), tipoLog.ALERTA);
                }
            }
            catch (Exception ex)
            {
                sb.Clear();
                sb.Append(ex.Message);
                sb.Append(" Error obteniendo Bytes. Mensaje enviado al proveedor: ");
                sb.Append(estadoDelProveedor.messageRequest);
                sb.Append(" para el cliente: ");
                sb.Append(estadoDelProveedor.clientStateSource.UniqueClientId);
                EscribirLog(sb.ToString(), tipoLog.ERROR);
            }
            return numeroDeBytes;
        }

        /// <summary>
        /// Logs a message related to the provider's state and the associated client information.
        /// </summary>
        /// <remarks>This method constructs a log message based on the provider's request data and the
        /// unique identifier of the originating client. If an exception occurs during the process, the exception
        /// message is included in the log.</remarks>
        /// <param name="estadoDelProveedor">An object representing the state of the provider, including the request data and the originating client's
        /// state.</param>
        private static void LogMessageToProvider(X estadoDelProveedor)
        {
            var sb = new StringBuilder();
            try
            {
                sb.Append("Mensaje enviado del proveedor: ");
                sb.Append(estadoDelProveedor.messageRequest.Trim().Substring(2));
                sb.Append(" para el cliente: ");
                sb.Append(estadoDelProveedor.clientStateSource.UniqueClientId);
                EscribirLog(sb.ToString(), tipoLog.INFORMACION);
            }
            catch (Exception ex)
            {
                sb.Append(ex.Message);
                sb.Append(" Mensaje enviado del proveedor: ");
                sb.Append(estadoDelProveedor.messageRequest);
                sb.Append(" para el cliente: ");
                sb.Append(estadoDelProveedor.clientStateSource.UniqueClientId);
                EscribirLog(sb.ToString(), tipoLog.INFORMACION);
            }
        }

        /// <summary>
        /// Releases resources associated with the specified provider state.
        /// </summary>
        /// <remarks>This method ensures that the buffer associated with the provider's socket is
        /// released, the socket is reset, and the provider state is returned to the pool for reuse. If an error occurs
        /// during the release process, the error is logged.</remarks>
        /// <param name="estadoDelProveedor">The state of the provider whose resources are to be released. This includes the associated socket and client
        /// state information.</param>
        private void ReleaseResourceWithOutCloseSocket(X estadoDelProveedor)
        {
            try
            {
                bufferManager.LiberarBuffer(estadoDelProveedor.saeaSendReceive, estadoDelProveedor.clientStateSource.UniqueClientId);
                estadoDelProveedor.saeaSendReceive.AcceptSocket = null;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append(ex.Message);
                sb.Append(" Error liberando recursos del proveedor, cliente ");
                sb.Append(estadoDelProveedor.clientStateSource.UniqueClientId);
                EscribirLog(sb.ToString(), tipoLog.ERROR);
            }
            finally
            {
                // el SAEA del proveedor se ingresa nuevamente al pool para ser re utilizado
                providerStateManager.ingresarUnElemento(estadoDelProveedor);
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
                sb.Append("No se pudo obtener el estado del proveedor en RecepcionEnvioSalienteCallBack");
                EscribirLog(sb.ToString(), tipoLog.ERROR);
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
                        sb.Append("Error en el envío a ");
                        sb.Append(providerState.saeaSendReceive.RemoteEndPoint);
                        sb.Append(", RecepcionEnvioSalienteCallBack ");
                        sb.Append(e.SocketError.ToString());
                        sb.Append(", cliente ");
                        sb.Append(providerState.clientStateSource.UniqueClientId);
                        EscribirLog(sb.ToString(), tipoLog.ERROR);

                        providerState.SetResponseCode((int)CodigosRespuesta.SinRespuestaCarrier);
                        providerState.SetAuthorizationCode(0);
                        providerState.clientStateSource.SetResponseCode(providerState.responseCode);
                        providerState.clientStateSource.SetAuthorizationCode(providerState.authorizacionCode);
                        ResponseToClient((T)providerState.clientStateSource);
                        ProviderSocketClose(providerState);
                    }
                    break;
                case SocketAsyncOperation.Receive:

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
                    sb.Append("La ultima operación no se detecto como de recepcion o envío, RecepcionEnvioSalienteCallBack, ");
                    sb.Append(e.LastOperation.ToString());
                    EscribirLog(sb.ToString(), tipoLog.ALERTA);

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
                EscribirLog(sb.ToString(), tipoLog.ERROR);
                return;
            }

            // Una vez terminado el envio, se continua escuchando por el Socket de trabajo
            try
            {
                providerState.SetInUse();
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
                EscribirLog(sb.ToString(), tipoLog.ERROR);

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
            providerState.CancelTimeout();
            if (providerState == null)
            {
                var sb = new StringBuilder();
                sb.Append(nameof(providerState));
                sb.Append(" es inválido para la operacion");
                EscribirLog(sb.ToString(), tipoLog.ERROR);
                return;
            }

            // se obtiene el SAEA de recepción
            SocketAsyncEventArgs saeaReceive = providerState.saeaSendReceive;
            // se obtienen los bytes que han sido recibidos
            int bytesTransferred = saeaReceive.BytesTransferred;

            // se obtiene el mensaje y se decodifica
            string messageReceive = Encoding.ASCII.GetString(saeaReceive.Buffer, saeaReceive.Offset, bytesTransferred);

            // el mensaje recibido llevará un proceso, que no debe ser llevado por el core, se coloca en la función virtual
            // para que se consuma en otra capa, se procese y se entregue una respuesta
            try
            {
                var sb = new StringBuilder();
                sb.Append("Mensaje recibido del proveedor: ");
                sb.Append(messageReceive.Trim().Substring(2));
                sb.Append(" para el cliente: ");
                sb.Append(providerState.clientStateSource.UniqueClientId);
                EscribirLog(sb.ToString(), tipoLog.INFORMACION);

                providerState.ProcessMessage(messageReceive);
                providerState.GetResponseMessage();
            }
            catch (Exception ex)
            {
                providerState.SetResponseCode((int)CodigosRespuesta.ErrorProceso);
                providerState.SetAuthorizationCode(0);
                var sb = new StringBuilder();
                sb.Append(ex.Message);
                sb.Append(", procesando trama del proveedor, ProcesarRecepcion, cliente ");
                sb.Append(providerState.clientStateSource.UniqueClientId);
                EscribirLog(sb.ToString(), tipoLog.ERROR);
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

        /// <summary>
        /// Cierra el socket asociado a un proveedor y retira al proveedor de la lista de conectados
        /// </summary>
        public void ProviderSocketClose(X estadoDelProveedor)
        {
            var sb = new StringBuilder();
            try
            {
                // Se comprueba que la información del socket de trabajo sea null, ya que podría ser invocado como resultado 
                // de una operación de E / S sin valores
                if (estadoDelProveedor == null) return;
                if (estadoDelProveedor.SocketOfWork == null) return;


                // se obtiene el socket específico del cliente en cuestión
                Socket socketDeTrabajoACerrar = estadoDelProveedor.SocketOfWork;
                socketPool.ReturnSocket(socketDeTrabajoACerrar);


                // se libera la instancia de socket de trabajo para reutilizarlo
                if (estadoDelProveedor.saeaSendReceive != null && estadoDelProveedor.InUse == 0)
                {
                    EscribirLog("Liberando buffer del proveedor para el cliente: " + estadoDelProveedor.clientStateSource.UniqueClientId, tipoLog.INFORMACION);
                    bufferManager.LiberarBuffer(estadoDelProveedor.saeaSendReceive, estadoDelProveedor.clientStateSource.UniqueClientId);
                    estadoDelProveedor.saeaSendReceive.AcceptSocket = null;
                    providerStateManager.ingresarUnElemento(estadoDelProveedor);
                    if(!RemoveProviderToProviderList(estadoDelProveedor))
                    {
                        ProvidersPendingDisconnectionList.Add(estadoDelProveedor);
                    }
                }
                else
                {
                    ProvidersPendingDisconnectionList.Add(estadoDelProveedor);
                }

                // se marca el semáforo de que puede aceptar otro cliente
                if (this.ProviderSemaphoreConnections.CurrentCount < this.numberOfSimultaneousProviderConnections)
                {
                    this.ProviderSemaphoreConnections.Release();
                }
            }
            catch (Exception ex)
            {
                sb.Append(ex.Message);
                sb.Append(" en cerrarSocketProveedor, cliente ");
                if (estadoDelProveedor != null && estadoDelProveedor.clientStateSource != null)
                {
                    sb.Append(estadoDelProveedor.clientStateSource.UniqueClientId);
                }
                EscribirLog(sb.ToString(), tipoLog.ERROR);
                ProvidersPendingDisconnectionList.Add(estadoDelProveedor);
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
        private bool RemoveProviderToProviderList(X providerState)
        {            
            try
            {
                // proporciona un mecanismo de sincronización de acceso a datos donde un hilo solo puede tener acceso a un
                // bloque de código a la vez, en este caso en ingresar al listado de clientes, de lo contrario habría 
                // cross threading y provocaría error
                bool bloqueo = Monitor.TryEnter(providersList, 1000);
                if (bloqueo)
                {
                    // se busca en la lista el cliente y se remueve porque se va a desconectar
                    if (providersList.ContainsKey(providerState.clientStateSource.UniqueClientId))
                    {
                        providersList.Remove(providerState.clientStateSource.UniqueClientId);
                    }
                    else
                    {
                        // quiere decir que ya está desconectado
                        var sb = new StringBuilder();
                        sb.Append("No se encontró el proveedor ");
                        sb.Append(providerState.clientStateSource.UniqueClientId.ToString());
                        sb.Append(" en lista de proveedores a desconectar, ya ha sido desconectado en otro proceso");
                        EscribirLog(sb.ToString(), tipoLog.ALERTA);
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
                sb.Append(" Error removiendo el cliente de la lista de proveedores, cliente ");
                sb.Append(providerState.clientStateSource.UniqueClientId.ToString());
                EscribirLog(sb.ToString(), tipoLog.ERROR);
                return false;
            }
            finally
            {
                Monitor.Exit(providersList);
            }
        }

        /// <summary>
        /// valida que exista tiempo suficiente para que el proveedor (procesa) realice la tarea, el tiempo por defecto es 25 seg
        /// </summary>
        /// <param name="estadoDelProveedor"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private bool ValidateTimeRemaining(X providerState, out int timeremaining)
        {
            try
            {
                bool hasEnoughTime = true;
                bool seSincronzo = Monitor.TryEnter(providerState, 500);
                if (seSincronzo)
                {
                    TimeSpan timeSpan = DateTime.Now - providerState.clientStateSource.StartDateTrx;
                    var sb = new StringBuilder();
                    sb.Append("ValidateTimeRemaining, fechaDeComprobacion: ");
                    sb.Append(DateTime.Now);
                    sb.Append(" - fechaInicioTrx: ");
                    sb.Append(providerState.clientStateSource.StartDateTrx);
                    sb.Append(", resultado: ");
                    sb.Append(timeSpan.Seconds);
                    sb.Append(" segundos de transcurridos. El timeout establecido es de: ");
                    sb.Append(providerState.clientStateSource.timeOut);
                    sb.Append(" - ");
                    sb.Append(timeSpan.Seconds);
                    sb.Append(" segundos transcurridos: ");
                    sb.Append(providerState.clientStateSource.timeOut - timeSpan.Seconds);
                    sb.Append(" segundos restantes. cliente:");
                    sb.Append(providerState.clientStateSource.UniqueClientId);
                    EscribirLog(sb.ToString(), tipoLog.ALERTA, false);


                    timeremaining = providerState.clientStateSource.timeOut - timeSpan.Seconds;
                    if (timeremaining > 25)
                        hasEnoughTime = true;
                    else
                        hasEnoughTime = false;
                }
                else
                {
                    timeremaining = 0;
                    hasEnoughTime = false;
                }
                return hasEnoughTime;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error en ValidateTimeRemaining, ");
                sb.Append(ex.Message);
                sb.Append(". cliente ");
                sb.Append(providerState.clientStateSource.UniqueClientId);
                EscribirLog(sb.ToString(), tipoLog.ERROR, true);
                timeremaining = 0;
                return false;
            }
            finally
            {
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
            X estadoDelProveedor = (X)sender;
            estadoDelProveedor.SetResponseCode((int)CodigosRespuesta.SinRespuestaCarrier);
            estadoDelProveedor.SetAuthorizationCode(0);
            estadoDelProveedor.clientStateSource.SetResponseCode(estadoDelProveedor.responseCode);
            estadoDelProveedor.clientStateSource.SetAuthorizationCode(estadoDelProveedor.authorizacionCode);
            ResponseToClient((T)estadoDelProveedor.clientStateSource);
            ProviderSocketClose(estadoDelProveedor);
        }

        #endregion


        /// <summary>
        /// Envía un mensaje sincronamente (Discontinuado porque ya se puede hacer asincrono)
        /// </summary>
        /// <param name="mensaje">mensaje a enviar</param>
        /// <param name="e">A client's SocketAsyncEventArgs</param>
        public void EnvioInfoSincro(string mensaje, SocketAsyncEventArgs e)
        {
            T socketDeTrabajoInfoCliente = e.UserToken as T;
            Byte[] bufferEnvio;
            bufferEnvio = Encoding.ASCII.GetBytes(mensaje);

            if (socketDeTrabajoInfoCliente.SocketToWork.Connected)
            {
                socketDeTrabajoInfoCliente.SocketToWork.Send(bufferEnvio);
            }
        }

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

            disconnecting = true;

            // Cerrar y liberar todos los clientes
            foreach (T cliente in clientsList.Values)
            {
                try
                {
                    // Cerrar socket
                    cliente.SocketToWork?.Shutdown(SocketShutdown.Both);
                    cliente.SocketToWork?.Close();

                    // Liberar buffer y referencias
                    if (cliente.saeaOfSendReceive != null)
                    {
                        bufferManager.LiberarBuffer(cliente.saeaOfSendReceive, cliente.UniqueClientId);
                        cliente.saeaOfSendReceive.UserToken = null;
                        cliente.saeaOfSendReceive.AcceptSocket = null;
                        // Si no se reutiliza, puedes llamar a Dispose()
                        cliente.saeaOfSendReceive.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    var sb = new StringBuilder();
                    sb.Append("Error al liberar cliente en DetenerServidor, cliente ");
                    sb.Append(cliente.UniqueClientId);
                    sb.Append(" , ");
                    sb.Append(ex.Message);
                    EscribirLog(sb.ToString(), tipoLog.ERROR);
                }
            }
            clientsList.Clear();

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
                            bufferManager.LiberarBuffer(proveedor.saeaSendReceive, proveedor.clientStateSource.UniqueClientId);
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
                        EscribirLog(sb.ToString(), tipoLog.ERROR);
                    }
                }
                ProvidersPendingDisconnectionList.Clear();
            }

            // Liberar el socket de escucha
            try
            {
                mainListenSocket?.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error al detener socket de escucha en DetenerServidor, ");
                sb.Append(ex.Message);
                EscribirLog(sb.ToString(), tipoLog.ERROR);
            }

            // Liberar PerformanceCounter
            incommigConnectionsPerformanceCounter?.Dispose();

            bufferManager.LimpiarBufferCompleto();
            bufferManager.LimpiarPilaDeIndices();


            inExecution = false;
            disconnecting = false;
        }

        /// <summary>
        /// Verificación del tiempo de la transacción sobre el proceso del clente
        /// </summary>
        /// <param name="clientState">instancia del estado del cliente</param>
        /// <returns></returns>
        private bool ValidateTimeOutExpired(T clientState)
        {
            try
            {
                if (clientState == null)
                {
                    return true;
                }
                TimeSpan timeSpan = DateTime.Now - clientState.StartDateTrx;
                return timeSpan.Seconds > clientState.timeOut;
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
                EscribirLog(sb.ToString(), tipoLog.ALERTA);
                return true;
            }

        }

        /// <summary>
        /// Valida que la licencia esté vigente
        /// </summary>
        /// <returns></returns>
        private bool ValidateParametersServer()
        {
            try
            {
                Encrypter.Encrypter encrypter = new Encrypter.Encrypter("AdmindeServicios");

                if (!GetParametersFile())
                    return false;

                if (!GetInfoPc())
                    return false;

                return string.Compare(PROGRAM, encrypter.DesEncrypterText(licence.Split('|')[(int)Licence.Program])) == 0
                        //&& DateTime.Compare(localValidity, DateTime.Parse(encrypter.DesEncrypterText(licence.Split('|')[(int)Licence.Validity]))) <= 0
                        && (string.Compare(processorId, encrypter.DesEncrypterText(licence.Split('|')[(int)Licence.ProcessorId])) == 0)
                        && (string.Compare(product, encrypter.DesEncrypterText(licence.Split('|')[(int)Licence.Product])) == 0)
                        && (string.Compare(manufacturer, encrypter.DesEncrypterText(licence.Split('|')[(int)Licence.Manufacturer])) == 0);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error en ValidateParametersServer, ");
                sb.Append(ex.Message);
                EscribirLog(sb.ToString(), tipoLog.ERROR);
                return false;
            }
        }

        /// <summary>
        /// Obtiene el archivo de licencia de la ubicación de la aplicación
        /// </summary>
        /// <returns></returns>
        private bool GetParametersFile()
        {
            FileStream fileStream;
            try
            {
                using (fileStream = File.OpenRead(Environment.CurrentDirectory + "\\" + PROGRAM + ".txt"))
                {
                    using (StreamReader streamReader = new StreamReader(fileStream))
                    {

                        while (streamReader.EndOfStream == false)
                        {
                            licence = streamReader.ReadLine();
                        }
                    }
                }
                return licence.Length > 0;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("No se pudo leer el archivo de configuración en la ruta ");
                sb.Append(Environment.CurrentDirectory + "\\" + PROGRAM + ".txt");
                sb.Append(", ");
                sb.Append(ex.Message);
                EscribirLog(sb.ToString(), tipoLog.ERROR);
                return false;
            }
        }

        /// <summary>
        /// Obtiene la información de la PC que se requiere para el funcionamiento del server
        /// </summary>
        /// <returns></returns>
        private bool GetInfoPc()
        {
            try
            {
                processorId = RunQuery("Processor", "ProcessorId").ToUpper();

                product = RunQuery("BaseBoard", "Product").ToUpper();

                manufacturer = RunQuery("BaseBoard", "Manufacturer").ToUpper();

                return true;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("No se pudo obtener la información de la PC, ");
                sb.Append(ex.Message);
                EscribirLog(sb.ToString(), tipoLog.ERROR);
                return false;
            }
        }

        /// <summary>
        /// Ejecuta una consulta al sistema
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="MethodName"></param>
        /// <returns></returns>
        private string RunQuery(string TableName, string MethodName)
        {
            ManagementObjectSearcher MOS =
              new ManagementObjectSearcher("Select * from Win32_" + TableName);
            foreach (ManagementObject MO in MOS.Get())
            {
                try
                {
                    return MO[MethodName].ToString();
                }
                catch (Exception)
                {
                    return "";
                }
            }
            return "";
        }

    }
}
