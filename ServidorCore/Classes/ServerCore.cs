using ServerCore.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static ServerCore.ServerConfiguration;
using static ServerCore.Constants.ServerCoreConstants;

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
    public class ServerCore<T, S, X> : IServerCore<T, S, X> where T : ClientStateBase, new()
        where S : ServerStateBase, new()
        where X : ProviderStateBase, new()
    {
        /// <summary>
        /// Instancia del performance counter de peticiones entrantes
        /// </summary>
        public PerformanceCounter peformanceConexionesEntrantes;

        #region Public properties

        /// <summary>
        /// Obtiene una lista de clientes ordenados por un GUID
        /// </summary>
        internal Dictionary<Guid, T> _connectedClients;

        ///// <summary>
        ///// Obtiene o ingresa una ip a bloquear
        ///// </summary>
        //public Dictionary<IPAddress, ClienteBloqueo> listaClientesBloqueados { get; set; }


        ///// <summary>
        ///// Ingresa u obtiene a la lista de ip permitidas, como simulando un firewall
        ///// </summary>
        //public List<Regex> listaClientesPermitidos { get; set; }

        /// <summary>        
        /// Obtiene o ingresa el número máximo de conexiones simultaneas de una misma IP del cliente (0=ilimitadas)
        /// </summary>
        internal int _maximumConnectionsPerClient;

        /// <summary>        
        /// Obtiene o ingresa el número máximo de conexiones simultaneas de una misma IP del cliente (0=ilimitadas)
        /// </summary>
        public int MaximumConnectionsPerClient
        {
            get
            {
                return _maximumConnectionsPerClient;
            }
            set
            {
                if (!_isRunning)
                {
                    _maximumConnectionsPerClient = MaximumConnectionsPerClient;
                }
            }
        }

        /// <summary>
        /// Obtiene o ingresa el valor de que si el servidor está o no ejecutandose
        /// </summary>
        internal bool _isRunning;

        /// <summary>
        /// Obtiene o ingresa el valor de que si el servidor está o no ejecutandose
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }
            set
            {
                if (!IsRunning)
                {
                    _isRunning = IsRunning;
                }
            }
        }

        /// <summary>
        /// Obtiene o ingresa el estado del socket del servidor
        /// </summary>
        public S ServerStateBase { get; set; }

        /// <summary>
        /// Obtiene o ingresa a la lista de clientes pendientes de desconexión, esta lista es para la verificación de que todos los cliente
        /// se desconectan adecuadamente, su uso es más para debug
        /// </summary>
        internal List<T> _pPendingClientsToDisconnect;

        /// <summary>
        /// Obtiene o ingresa a la lista de clientes pendientes de desconexión, esta lista es para la verificación de que todos los cliente
        /// se desconectan adecuadamente, su uso es más para debug
        /// </summary>
        public List<T> PendingClientsToDisconnect
        {
            get => _pPendingClientsToDisconnect is null ? null : _pPendingClientsToDisconnect;
            set
            {
                if (!_isRunning)
                    _pPendingClientsToDisconnect = value;
            }
        }

        /// <summary>
        /// Obtiene o ingresa a la lista de proveedores pendientes de desconexión, esta lista es para la verificación de que todos los proveedores
        /// se desconectan adecuadamente, su uso es más para debug pero queda para mejorar
        /// </summary>
        internal List<X> _pendingProvidersToDisconnect;

        /// <summary>
        /// Obtiene o ingresa a la lista de proveedores pendientes de desconexión, esta lista es para la verificación de que todos los proveedores
        /// se desconectan adecuadamente, su uso es más para debug pero queda para mejorar
        /// </summary>
        public List<X> PendingProvidersToDisconnect
        {
            get => _pendingProvidersToDisconnect;
            set
            {
                if (!_isRunning)
                {
                    _pendingProvidersToDisconnect = value;
                }
            }
        }

        /// <summary>
        /// Obtiene el número de clientes conectados actualmente al servidor
        /// </summary>
        public int ClientsConnectedCounter
        {
            get
            {
                return _connectedClients.Values.Count;
            }
        }

        /// <summary>
        /// Ip a la cual se apuntarán todas las transacciones del proveedor
        /// </summary>
        internal string _providerIp;

        /// <summary>
        /// Ip a la cual se apuntarán todas las transacciones del proveedor
        /// </summary>
        public string ProviderIp
        {
            get => _providerIp;
            set
            {
                if (!_isRunning)
                    _providerIp = value;
            }
        }

        /// <summary>
        /// Lista de puertos en la IP configurada al proveedor para el envío de la mensajería
        /// </summary>
        internal List<int> _providersPorts;

        /// <summary>
        /// Lista de puertos en la IP configurada al proveedor para el envío de la mensajería
        /// </summary>
        public List<int> ProvidersPorts
        {
            get => _providersPorts;
            set
            {
                if (!_isRunning)
                    _providersPorts = value;
            }
        }

        /// <summary>
        /// Bytes que se han transmitido desde el inicio de la aplicación
        /// </summary>
        public int TotalDeBytesTransferidos
        {
            get
            {
                return totalBytesReceived;
            }
        }

        internal string _serverListeningIp;
        /// <summary>
        /// IP de escucha de la aplicación  para recibir mensajes
        /// </summary>
        public string ServerListeningIp
        {
            get => _serverListeningIp;
            set
            {
                if (!_isRunning)
                    _serverListeningIp = value;
            }
        }

        /// <summary>
        /// Retraso en el envío, es para uso en Debug
        /// </summary>
        internal static int _maxRetrasoParaEnvio = 0;

        /// <summary>
        /// Numero que muestra cuantas conexiones puede aún soportar el servidor del lado del cliente
        /// </summary>
        public int ResourcesAvailableCounterPerClient
        {
            get
            {
                return semaphoreClientResourses.CurrentCount;
            }
        }

        /// <summary>
        /// Numero que muestra cuantas conexiones puede aún soportar el servidor del lado del proveedor
        /// </summary>
        public int ResourcesAvailableCounterPerProvider
        {
            get
            {
                return semaphoreProviderResourses.CurrentCount;
            }
        }

        /// <summary>
        /// Auxiliar para el conteo de puertos y su balanceo de salida
        /// </summary>
        internal int _RandomPorts = 0;

        #endregion

        #region Privates Properties

        /// <summary>
        /// Número de conexiones simultaneas que podrá manejar el servidor por defecto
        /// </summary>
        private readonly int maximumSimultaneousConnectionsClientSide;
        private readonly int maximumSimultaneousConnectionsProviderSide;

        /// <summary>
        /// Número sockest para lectura y escritura sin asignación de espacio del buffer para aceptar peticiones como default
        /// esto para tener siempre por lo menos sockects disponibles al inicio del servidor
        /// </summary>
        private const int PRE_SOCKETS_DEFAULT = 3;

        /// <summary>
        /// instancia al administrador de estados de socket de trabajo
        /// </summary>
        private readonly ClientStatesMananger<T> clientStatesManager;

        /// <summary>
        /// Instancia del administrador de estados del proveedor
        /// </summary>
        private readonly ProviderStateManager<X> providerStatesManager;

        /// <summary>
        /// semáforo sobre las peticiones de clientes para controlar el número total que podrá soportar el servidor
        /// </summary>
        private readonly SemaphoreSlim semaphoreClientResourses;

        /// <summary>
        /// semáforo sobre las peticiones a proveedores para controlar el número total que podrá soportar el servidor
        /// </summary>
        private readonly SemaphoreSlim semaphoreProviderResourses;

        /// <summary>
        /// Número total de bytes recibido en el servidor, para uso estadístico
        /// </summary>
        private int totalBytesReceived;

        /// <summary>
        /// Representa un conjunto enorme de buffer reutilizables entre todos los sockects de trabajo
        /// </summary>
        private readonly BufferManager bufferManager;

        /// <summary>
        /// Socket de escucha para las conexiones de clientes
        /// </summary>
        private Socket mainSocketListening;

        /// <summary>
        /// Bandera para identificar que la conexión está bien establecida
        /// </summary>
        private bool disconnecting = false;

        /// <summary>
        /// Parámetros que  indica el máximo de pedidos que pueden encolarse simultáneamente en caso que el servidor 
        /// esté ocupado atendiendo una nueva conexión.
        /// </summary>
        private readonly int backLog;

        /// <summary>
        /// Tamaño del buffer por petición
        /// </summary>
        private readonly int bufferSizePerRequest;

        /// <summary>
        /// Mensaje de aviso
        /// </summary>
        private const string PERMISSION_DENIED = "Permission denied to use the system";

        /// <summary>
        /// Log del sistema
        /// </summary>
        private static EventLogTraceListener logListener;

        private Security security;
        private PerformanceCounters PerformanceCounters;
        private LogTrace logTrace;

        #endregion

        #region Server Constructors

        /// <summary>
        /// Crea una instancia del administrador de sockets, posterior se tiene que llamar al método
        /// ConfigInicioServidor para iniciar el proceso de asignacion de recursos        
        /// </summary>
        /// <param name="maximumSimultaneousConnections">Maximo número de conexiones simultaneas a manejar en el servidor</param>
        /// <param name="BufferSize">Tamaño del buffer por conexión, un parámetro standart es 1024</param>
        /// <param name="backlog">Parámetro TCP/IP backlog, el recomendable es 100</param>
        public ServerCore(int maximumSimultaneousConnections, int BufferSize = 1024, int backlog = 100)
        {
            totalBytesReceived = 0;
            this.maximumSimultaneousConnectionsClientSide = maximumSimultaneousConnections;
            maximumSimultaneousConnectionsProviderSide = maximumSimultaneousConnections;
            //Se coloca ilimitado para fines no restrictivos
            //TODO crear la restricción
            MaximumConnectionsPerClient = 0;
            this.backLog = backlog;
            _connectedClients = new Dictionary<Guid, T>();
            //listaClientesBloqueados = new Dictionary<IPAddress, ClienteBloqueo>();
            //listaClientesPermitidos = new List<Regex>();
            _pPendingClientsToDisconnect = new List<T>();

            this.bufferSizePerRequest = BufferSize;


            try
            {
                ServerStateBase = new S();
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + ". ServerCore", LogType.Error);
            }

            // establezco el proceso principal para referencia futura
            ServerStateBase.MainProcess = this;
            // indico que aún no está en funcionamiento, faltan parámetros
            _isRunning = false;

            //Asignación de un buffer tomando en cuenta por lo menos los 3 sockets por defecto para lectura y escritura iniciales
            // es decir, el tamaño del buffer por operación por el número de conexiónes por el número de sockets iniciales no dará
            // el valor de buffer enorme en bytes, por ejemplo: tamanoBuffer= 1024 * 1000 * 3 =2048000 bytes
            this.bufferManager = new BufferManager(BufferSize * maximumSimultaneousConnectionsClientSide * PRE_SOCKETS_DEFAULT, bufferSizePerRequest);

            //se inicializan los estados de socket necesarios para el número simultaneo de conexiones, para tenerlos listos a usarse como una pila            
            clientStatesManager = new ClientStatesMananger<T>(maximumSimultaneousConnectionsClientSide);

            //se inicializan los estados de socket necesarios para el número simultaneo de conexiones hacia el proveedor, para tenerlos listos a usarse como una pila            
            providerStatesManager = new ProviderStateManager<X>(maximumSimultaneousConnectionsProviderSide);

            //Se inicializa el número inicial y maximo de conexiones simultaneas soportadas, será el semáforo quien indique que hay saturación.            
            semaphoreClientResourses = new SemaphoreSlim(maximumSimultaneousConnectionsClientSide, maximumSimultaneousConnectionsClientSide);
            semaphoreProviderResourses = new SemaphoreSlim(maximumSimultaneousConnectionsProviderSide, maximumSimultaneousConnectionsProviderSide);
        }

        /// <summary>
        /// Inicializa el servidor con una pre asignación de buffers reusables y estados de sockets
        /// </summary>
        /// <param name="timeOutClientSide">Tiempo en segundos de espera antes de cancelar una respuesta por tiempo excedido en el proceso</param>
        public void PreInitialize(int timeOutClientSide)
        {
            try
            {
                logTrace = new LogTrace("Core");
                if (!logTrace.CrearLog())
                    throw new Exception("Error al crear el log del sistema, la aplicación debe ejecutarse con privilegios de administrador");
                Trace.Listeners.Clear();
                logListener = new EventLogTraceListener("Core");

                if (!Trace.Listeners.Contains(logListener))
                {
                    Trace.Listeners.Add(logListener);
                }
                logTrace.EscribirLog("Comprobación de escritura de log", LogType.Information);


                ServerConfiguration.clientTimeOut = timeOutClientSide;
                peformanceConexionesEntrantes = new PerformanceCounter("TN", "conexionesEntrantesUserver", false);
                peformanceConexionesEntrantes.IncrementBy(1);
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + ", PreInitialize", LogType.Error);
                throw;
            }

            security = new Security(logTrace);
            if (!security.ValidatePermissions())
            {
                logTrace.EscribirLog(PERMISSION_DENIED, LogType.Error);
                throw new Exception(PERMISSION_DENIED);
            }

            PerformanceCounters = new PerformanceCounters();
            PerformanceCounters.BuildCounters();

            //objetos para operaciones asincronas en los sockets de los clientes
            SocketAsyncEventArgs saeaDeEnvioRecepcionCliente;
            //SocketAsyncEventArgs saeaDeEnvioForzadoAlCliente;

            //Se prepara un buffer suficientemente grande para todas las operaciones y poder reutilizarlo por secciones
            bufferManager.InitializeBuffer();

            //pre asignar un conjunto de estados de socket para usarlos inmediatamente en cada una
            // de la conexiones simultaneas que se pueden esperar
            for (Int32 i = 0; i < this.maximumSimultaneousConnectionsClientSide; i++)
            {
                T estadoDelCliente = new T();
                estadoDelCliente.Initialize();

                saeaDeEnvioRecepcionCliente = new SocketAsyncEventArgs();
                //El manejador de eventos para cada lectura de una peticion del cliente
                saeaDeEnvioRecepcionCliente.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveSendProcessCallBack);
                //SocketAsyncEventArgs necesita un objeto con la información de cada cliente para su administración
                saeaDeEnvioRecepcionCliente.UserToken = estadoDelCliente;
                //Se establece el buffer que se utilizará en la operación de lectura del cliente en el eventArgDeRecepcion
                bufferManager.SetBuffer(saeaDeEnvioRecepcionCliente);
                //Se establece el socket asincrono de EventArg a utilizar en la lectura del cliente
                estadoDelCliente.socketAsyncEventArgs = saeaDeEnvioRecepcionCliente;


                //saeaDeEnvioForzadoAlCliente = new SocketAsyncEventArgs();
                ////El manejador de eventos para cada envío a un cliente
                //saeaDeEnvioForzadoAlCliente.Completed += new EventHandler<SocketAsyncEventArgs>(RecepcionEnvioEntranteCallBack);
                ////SocketAsyncEventArgs necesita un objeto con la información de cada cliente para su administración
                //saeaDeEnvioForzadoAlCliente.UserToken = estadoDelCliente;
                ////Se establece el buffer que se utilizará en la operación de envío al cliente
                //administradorBuffer.asignarBuffer(saeaDeEnvioForzadoAlCliente);
                ////Se establece el socket asincrono de EventArg a utilizar en el envío al cliente
                //estadoDelCliente.saeaDeEnvioForzadoAlCliente = saeaDeEnvioForzadoAlCliente;

                //Ya con los parametros establecidos para cada operacion, se ingresa en la pila
                //de estados del cliente y desde ahi administar su uso en cada petición
                clientStatesManager.SetStackItem(estadoDelCliente);


                //Ahora genero la pila de estados para el proveedor
                X estadoDelProveedor = new X();
                estadoDelProveedor.Initialize();

                SocketAsyncEventArgs saeaDeEnvioRecepcionAlProveedor;
                saeaDeEnvioRecepcionAlProveedor = new SocketAsyncEventArgs();
                //El manejador de eventos para cada lectura de una peticion del proveedor
                saeaDeEnvioRecepcionAlProveedor.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveSendOutputProcessCallBack);
                //SocketAsyncEventArgs necesita un objeto con la información de cada proveedor para su administración
                saeaDeEnvioRecepcionAlProveedor.UserToken = estadoDelProveedor;
                //Se establece el buffer que se utilizará en la operación de lectura del proveedor en el eventArgDeEnvioRecepcion
                bufferManager.SetBuffer(saeaDeEnvioRecepcionAlProveedor);
                //Se establece el socket asincrono de EventArg a utilizar en las operaciones con el proveedor
                estadoDelProveedor.socketAsyncEventArgs = saeaDeEnvioRecepcionAlProveedor;

                //Ya con los parametros establecidos para cada operacion, se ingresa en la pila
                //de estados del proveedor y desde ahi administar su uso en cada petición
                providerStatesManager.SetStackItem(estadoDelProveedor);
            }
        }

        /// <summary>
        /// Se inicia el servidor de manera que esté escuchando solicitudes de conexión entrantes.
        /// </summary>
        /// <param name="listeningPort">Puerto de escucha para la recepeción de mensajes</param>
        /// <param name="providerPorts">Puertos del proveedor</param>
        /// <param name="testMode">Variable que indicará si el server entra en modo test.
        /// El modo Test, responderá a toda petición bien formada, con una código de autorización
        /// y respuesta simulado sin enviar la trama a un proveedor externo</param>
        /// <param name="routerMode">Activación para que el servidor pueda enviar mensajes a otro proveedor</param>
        /// <param name="providerIp">IP del proveedor a donde se enviarán mensajes en caso de que el modoRouter esté encendido</param>
        public void Start(int listeningPort, bool testMode = false, bool routerMode = true, string providerIp = "", List<int> providerPorts = null)
        {
            //Se inicializa la bandera de que no hay ningún cliente pendiente por desconectar
            disconnecting = false;
            ServerConfiguration.testMode = testMode;
            ServerConfiguration.routerMode = routerMode;
            //De acuerdo a las buenas practicas de manejo de operaciones asincronas, se debe ANUNCIAR el inicio
            //de un trabajo asincrono para ir controlando su avance por eventos si fuera necesario
            ServerStateBase.OnStart();

            if (!testMode && routerMode)
            {
                if (!IPAddress.TryParse(providerIp, out IPAddress address))
                    throw new Exception("IP del proveedor ingresada, es inválida");
                _providerIp = providerIp;
                if (providerPorts == null)
                    throw new Exception("Puertos del proveedor inválidos");
                _providersPorts = providerPorts;
                _RandomPorts = providerPorts.Count;
            }

            try
            {
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, listeningPort);

                // se crea el socket que se utilizará de escucha para las conexiones entrantes
                mainSocketListening = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // se asocia con el puerto de escucha el socket de escucha
                this.mainSocketListening.Bind(localEndPoint);

                _serverListeningIp = mainSocketListening.LocalEndPoint.ToString().Split(':')[0];

                // se inicia la escucha de conexiones con un backlog de 100 conexiones
                this.mainSocketListening.Listen(backLog);
            }
            catch (Exception ex)
            {
                throw ex;
            }


            // Se indica al sistema que se empiezan a aceptar conexiones, se envía una referencia a null para que se indique que es la primera vez
            this.StartsConnections(null);
            _isRunning = true;
        }

        ///// <summary>
        ///// Crea el log por defecto del sistema
        ///// </summary>
        ///// <returns></returns>
        //private bool CrearLog()
        //{
        //    string origenLog = nombreLog;
        //    // Create an EventLog instance and assign its source.
        //    EventLog myLog = new EventLog();
        //    try
        //    {
        //        // Create the source, if it does not already exist.
        //        if (!EventLog.SourceExists(nombreLog))
        //        {
        //            // An event log source should not be created and immediately used.
        //            // There is a latency time to enable the source, it should be created
        //            // prior to executing the application that uses the source.
        //            // Execute this sample a second time to use the new source.
        //            EventLog.CreateEventSource(origenLog, nombreLog);
        //            //Console.WriteLine("CreatingEventSource");
        //            //Console.WriteLine("Exiting, execute the application a second time to use the source.");
        //            // The source is created.  Exit the application to allow it to be registered.
        //            //return true;


        //            myLog.Source = origenLog;

        //            // Write an informational entry to the event log.
        //            myLog.WriteEntry("Se ha creado el log exitosamente");
        //        }

        //        myLog.Source = origenLog;

        //        // Write an informational entry to the event log.
        //        myLog.WriteEntry("Comprobando escritura de log");
        //        return true;
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}
        #endregion

        #region ClientSideProcess

        /// <summary>
        /// Se inicia la operación de aceptar solicitudes por parte de un cliente
        /// </summary>
        /// <param name="saeaStartsConnections">Objeto que se utilizará en cada aceptación de una solicitud</param>
        private void StartsConnections(SocketAsyncEventArgs saeaStartsConnections)
        {
            // de ser null quiere decir que no hay objeto instanciado y debe crearse desde cero. Es por el primer proceso de escucha
            if (saeaStartsConnections == null)
            {
                saeaStartsConnections = new SocketAsyncEventArgs();
                saeaStartsConnections.Completed += new EventHandler<SocketAsyncEventArgs>(StartsConnectionsCallBack);
            }
            else
            {
                // si ya existe instancia, se limpia el socket para trabajo. Esto se utiliza cuando se vuelve a colocar en escucha
                saeaStartsConnections.AcceptSocket = null;
            }
            // se comprueba el semáforo que nos indica que se tiene recursos para aceptar la conexión
            semaphoreClientResourses.Wait();
            try
            {
                peformanceConexionesEntrantes.IncrementBy(1);
                // se comienza asincronamente el proceso de aceptación y mediante un evento manual 
                // se verifica que haya sido exitoso. Cuando el proceso asincrono es exitoso devuelve false
                bool seHizoAsync = mainSocketListening.AcceptAsync(saeaStartsConnections);
                if (!seHizoAsync)
                    // se manda llamar a la función que procesa la solicitud, 
                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                    // en su evento callback
                    StartsConnectionsCallBack(mainSocketListening, saeaStartsConnections);
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + ". StartsConnections", LogType.Error);
                // se hace un último intento para volver a iniciar el servidor por si el error fue una excepción al empezar la aceptación
                StartsConnections(saeaStartsConnections);
            }
        }

        /// <summary>
        /// Procesa la solicitud por medio de socket principal de escucha
        /// </summary>
        /// <param name="sender">Objeto que se tomará como quien dispara el evento principal</param>
        /// <param name="saeaStartsConnections">SocketAsyncEventArg asociado al proceso asincrono.</param>
        private void StartsConnectionsCallBack(object sender, SocketAsyncEventArgs saeaStartsConnections)
        {
            // se valida que existan errores registrados
            if (saeaStartsConnections.SocketError != SocketError.Success)
            {
                // se comprueba que no hay un proceso de cierre del programa o desconexión en curso para no dejar sockets en segundo plano
                if (disconnecting)
                {
                    logTrace.EscribirLog("Socket de escucha desconectado porque el programa principal se está cerrando, AceptarConexionCallBack", LogType.Error);
                    // se le indica al semáforo que puede permitir la siguiente conexion....al final se cerrará pero no se bloqueará el proceso
                    semaphoreClientResourses.Release();
                    return;
                }
                // se le indica al semáforo que puede asignar otra conexion
                semaphoreClientResourses.Release();
                // aquí el truco en volver a iniciar el proceso de aceptación de solicitudes con el mismo
                // saea que tiene el cliente si hubo un error para re utilizarlo
                StartsConnections(saeaStartsConnections);
                return;
            }

            // si el cliente pasó todas las Utileria entonces se le asigna ya un estado de trabajo del pool de estados de cliente listo con todas sus propiedes
            T estadoDelCliente = clientStatesManager.GetStackItem();
            // limpio el estado para reutilizar solo el saea no los parámetros
            estadoDelCliente.Initialize();
            //por precaución se coloca que no se está procesando respuesta
            estadoDelCliente.SetResponseCompleted();
            // debo colocar la referencia del proceso principal donde genero el estado del cliente para tenerlo como referencia de retorno
            estadoDelCliente.SetSocketMainReference(this);
            // Del SAEA de aceptación de conexión, se recupera el socket para asignarlo al estado del cliente obtenido del pool de estados
            estadoDelCliente.SocketToWork = saeaStartsConnections.AcceptSocket;
            //  de la misma forma se ingresa la ip y puerto del cliente que se aceptó
            estadoDelCliente.ClientIp = (saeaStartsConnections.AcceptSocket.RemoteEndPoint as IPEndPoint).Address.ToString();
            estadoDelCliente.ClientPort = (saeaStartsConnections.AcceptSocket.RemoteEndPoint as IPEndPoint).Port;

            // con estas instrucciones puedo controlar las acciones en cada fase del proceso de recepción y envío de ser necesario
            ServerStateBase.OnAcept(estadoDelCliente);

            // se ingresa el cliente a la lista de clientes
            // Monitor proporciona un mecanismo que sincroniza el acceso a datos entre hilos
            bool seSincronzo = Monitor.TryEnter(_connectedClients, 1000);
            if (seSincronzo)
            {
                try
                {
                    if (!_connectedClients.ContainsKey(estadoDelCliente.UniqueId))
                    {
                        _connectedClients.Add(estadoDelCliente.UniqueId, estadoDelCliente);
                    }
                    else
                    {
                        logTrace.EscribirLog("Cliente ya registrado", LogType.warnning);
                    }
                }
                finally
                {
                    Monitor.Exit(_connectedClients);
                }
            }
            else
            {
                logTrace.EscribirLog("Timeout de 5 seg para obtener bloqueo en AceptarConexionCallBack, listaClientes", LogType.warnning);
                // si no puedo ingresarlo en la lista de clientes debo rechazarlo porque no tendría control para manipularlo en un futuro
                CloseSocketClient(estadoDelCliente);
                // coloco nuevamente el socket en proceso de aceptación con el mismo saea para un reintento de conexión
                this.StartsConnections(saeaStartsConnections);
                return;
            }

            // se inicia la recepción de datos del cliente si es que sigue conectado dicho cliente  
            if (estadoDelCliente.SocketToWork.Connected)
            {
                try
                {
                    // se ingresa la configuración del buffer para la recepción del mensaje                    
                    estadoDelCliente.socketAsyncEventArgs.SetBuffer(estadoDelCliente.socketAsyncEventArgs.Offset, bufferSizePerRequest);
                    // se procede a la recepción asincrona del mensaje,el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                    bool seHizoAsync = estadoDelCliente.SocketToWork.ReceiveAsync(estadoDelCliente.socketAsyncEventArgs);
                    if (!seHizoAsync)
                        // se llama a la función que completa el flujo de envío, 
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        ReceiveSendProcessCallBack(estadoDelCliente.SocketToWork, estadoDelCliente.socketAsyncEventArgs);
                }
                catch (Exception ex)
                {
                    logTrace.EscribirLog(ex.Message + ", AceptarConexionCallBack, recibiendo mensaje del cliente " + estadoDelCliente.UniqueId, LogType.Error);
                    CloseSocketClient(estadoDelCliente);
                }
            }
            else
            {
                logTrace.EscribirLog("AceptarConexiónCallBack.estadoDelCliente.socketDeTrabajo no conectado", LogType.warnning);
            }

            // se indica que puede aceptar más solicitudes con el mismo saea que es el principal
            this.StartsConnections(saeaStartsConnections);
        }

        /// <summary>
        /// Operación de callBack que se llama cuando se envía o se recibe de un socket de asincrono para completar la operación
        /// </summary>
        /// <param name="sender">Objeto principal para devolver la llamada</param>
        /// <param name="saea">SocketAsyncEventArg asociado a la operación de envío o recepción</param>        
        private void ReceiveSendProcessCallBack(object sender, SocketAsyncEventArgs saea)
        {
            // obtengo el estado del socket
            // se comprueba que el estado haya sido obtenido correctamente
            if (!(saea.UserToken is T estadoDelCliente))
            {
                logTrace.EscribirLog("estadoDelCliente recibido es inválido para la operacion", LogType.Error);
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
                            ProcessMessageReceiveClient(estadoDelCliente);
                        }
                        else
                        {
                            //logTrace.EscribirLog("No hay datos que recibir", tipoLog.ALERTA);
                            // si no hay datos por X razón, se cierra el cliente porque puede perdurar indefinidamente la conexión
                            CloseSocketClient(estadoDelCliente);
                        }
                    }
                    else
                    {
                        logTrace.EscribirLog("Error en el proceso de recepción, socket no conectado correctamente, cliente:" + estadoDelCliente.UniqueId + ", " + saea.SocketError.ToString(), LogType.warnning);
                        //se cierra el cliente porque puede perdurar indefinidamente la conexión
                        CloseSocketClient(estadoDelCliente);
                    }
                    break;
                case SocketAsyncOperation.Send:
                    //indico que estaré enviando algo al cliente para que otro proceso con la misma conexión no quiera enviar algo al mismo tiempo
                    estadoDelCliente.sentEventWaitHandle.Set();
                    // se comprueba que no hay errores con el socket
                    if (saea.SocketError == SocketError.Success)
                    {
                        //Intento colocar el socket de nuevo en escucha por si el cliente envía otra trama con la misma conexión
                        ReceiveSendCycleProcessClientSide(estadoDelCliente);
                    }
                    else
                    {
                        logTrace.EscribirLog("el socket no esta conectado correctamente para el cliente " + estadoDelCliente.UniqueId, LogType.warnning);
                        estadoDelCliente.SetResponseInProcess();
                        // si no hay datos por X razón, se cierra el cliente porque puede perdurar indefinidamente la conexión                        
                        CloseSocketClient(estadoDelCliente);

                    }
                    break;
                default:
                    // se da por errores de TCP/IP en alguna intermitencia
                    logTrace.EscribirLog("La ultima operación no se detecto como de recepcion o envío, RecepcionEnvioEntranteCallBack, " + saea.LastOperation.ToString(), LogType.Error);
                    CloseSocketClient(estadoDelCliente);
                    break;
            }
        }

        /// <summary>
        /// Este método se invoca cuando la operación de recepción asincrona se completa y si el cliente
        /// cierra la conexión el socket también se cierra y se libera
        /// </summary>
        /// <param name="clientState">Objeto que tiene la información y socket de trabajo del cliente</param>
        private void ProcessMessageReceiveClient(T clientState)
        {
            //por precaución se coloca que no se está procesando respuesta
            clientState.SetResponseCompleted();

            //Para ir midiendo el TO por cada recepción
            bool bloqueo = Monitor.TryEnter(clientState.DateTimeReceiveMessage, 1000);
            if (bloqueo)
            {
                clientState.DateTimeReceiveMessage = DateTime.Now;
            }
            else
            {
                logTrace.EscribirLog("Error al intentar hacer un interbloqueo, ProcesarRecepcion, para ingresar la fecha de inicio del cliente " + clientState.UniqueId, LogType.Error);
            }

            // se obtiene el SAEA de trabajo
            SocketAsyncEventArgs saeaDeEnvioRecepcion = clientState.socketAsyncEventArgs;
            // se obtienen los bytes que han sido recibidos
            Int32 bytesTransferred = saeaDeEnvioRecepcion.BytesTransferred;


            // se obtiene el mensaje y se decodifica para entenderlo
            string mensajeRecibido = Encoding.ASCII.GetString(saeaDeEnvioRecepcion.Buffer, saeaDeEnvioRecepcion.Offset, bytesTransferred);

            try
            {
                logTrace.EscribirLog("Mensaje recibido: " + mensajeRecibido.Trim() + " del cliente: " + clientState.UniqueId, LogType.Information);
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + ". Error al identificar si tiene encabezado el mensaje recibido, se intenta escribir pero se descarta " + mensajeRecibido.Trim() + " del cliente: " + clientState.UniqueId, LogType.Error);
                CloseSocketClient(clientState);
                return;
            }


            // incrementa el contador de bytes totales recibidos para tener estadísticas nada más
            // debido a que la variable está compartida entre varios procesos, se utiliza interlocked que ayuda a que no se revuelvan
            if (totalBytesReceived == LIMITE_BYTES_CONTADOR)
            {
                Interlocked.Exchange(ref this.totalBytesReceived, 0);
            }
            else
            {
                Interlocked.Add(ref this.totalBytesReceived, bytesTransferred);
            }

            // el mensaje recibido llevará un proceso, que no debe ser llevado por el core, se coloca en la función virtual
            // para que se consuma en otra capa, se procese y se entregue una respuesta

            // bloqueo los procesos sobre este mismo cliente hasta no terminar con esta petición para no tener revolturas de mensajes
            clientState.sentEventWaitHandle.Reset();

            try
            {
                // aquí se debe realizar lo necesario con la trama entrante para preparar la trama al proveedor en la variable tramaEnvioProveedor
                clientState.ProcessMessage(mensajeRecibido);
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog("Error al procesar la trama al llamar la función ProcessMessageReceiveClient" + ex.Message + ". Del cliente: " + clientState.UniqueId, LogType.Error);
            }

            if (IsTimeOver(clientState))
            {
                logTrace.EscribirLog("Se venció el TimeOut para el cliente " + clientState.UniqueId.ToString() + ". Después de procesar la trama", LogType.warnning);
                clientState.responseCode = (int)ProcessResponseCodes.InternalTimeOut;
                clientState.authorizationCode = 0;
                ResponseToClient(clientState);
                return;
            }

            try
            {
                // cuando haya terminado la clase estadoDelCliente de procesar la trama, se debe evaluar su éxito para enviar la solicitud al proveedor
                if (clientState.responseCode == (int)ProcessResponseCodes.ProcessSuccess)
                {
                    if (!testMode && routerMode)
                    {
                        // me espero a ver si tengo disponibilidad de SAEA para un proveedor
                        semaphoreProviderResourses.Wait();

                        //Se prepara el estado del proveedor que servirá como operador de envío y recepción de trama
                        SocketAsyncEventArgs saeaProveedor = new SocketAsyncEventArgs();
                        saeaProveedor.Completed += new EventHandler<SocketAsyncEventArgs>(ProviderConexionCallBack);

                        X estadoDelProveedor = providerStatesManager.GetStackItem();
                        // ingreso la información de peticion para llenar las clases al proveedor
                        estadoDelProveedor.SetObjectRequestClient(clientState.objectClientRequest);
                        estadoDelProveedor.ClientStateOriginal = clientState;
                        //por seguridad, se coloca la bandera de vencimiento por TimeOut en false
                        estadoDelProveedor.RestartTimeOut();

                        if (estadoDelProveedor.responseCode != (int)ProcessResponseCodes.ProcessSuccess)
                        {
                            estadoDelProveedor.authorizationCode = 0;
                            estadoDelProveedor.ClientStateOriginal.responseCode = estadoDelProveedor.responseCode;
                            estadoDelProveedor.ClientStateOriginal.authorizationCode = estadoDelProveedor.authorizationCode;
                            ResponseToClient((T)estadoDelProveedor.ClientStateOriginal);
                            // el SAEA del proveedor se ingresa nuevamente al pool para ser re utilizado
                            providerStatesManager.SetStackItem(estadoDelProveedor);
                            // se libera el semaforo por si otra petición está solicitando acceso
                            semaphoreProviderResourses.Release();
                            return;
                        }

                        saeaProveedor.UserToken = estadoDelProveedor;
                        IPAddress iPAddress = IPAddress.Parse(_providerIp);

                        bool seSincronzo = Monitor.TryEnter(_providersPorts, 1000);
                        IPEndPoint endPointProveedor;
                        if (seSincronzo)
                        {
                            try
                            {
                                if (_RandomPorts == 0)
                                {
                                    endPointProveedor = new IPEndPoint(iPAddress, _providersPorts.First());
                                }
                                else
                                {
                                    endPointProveedor = new IPEndPoint(iPAddress, _providersPorts[_RandomPorts - 1]);
                                }
                            }
                            catch
                            {
                                endPointProveedor = new IPEndPoint(iPAddress, _providersPorts.First());
                            }
                            finally
                            {
                                Monitor.Exit(_providersPorts);
                            }
                        }
                        else
                        {
                            logTrace.EscribirLog("Timeout de 5 seg para obtener un puerto de listaPuertosProveedor", LogType.Error);
                            endPointProveedor = new IPEndPoint(iPAddress, _providersPorts.First());
                        }


                        if (_RandomPorts == _providersPorts.Count)
                        {
                            Interlocked.Exchange(ref _RandomPorts, 0);
                        }
                        else
                        {
                            Interlocked.Increment(ref _RandomPorts);
                        }

                        saeaProveedor.RemoteEndPoint = endPointProveedor;
                        estadoDelProveedor.endPoint = endPointProveedor;
                        // se genera un socket que será usado en el envío y recepción
                        Socket socketDeTrabajoProveedor = new Socket(endPointProveedor.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                        try
                        {
                            saeaProveedor.AcceptSocket = socketDeTrabajoProveedor;
                            //Inicio el proceso de conexión                    
                            bool seHizoSync = socketDeTrabajoProveedor.ConnectAsync(saeaProveedor);
                            if (!seHizoSync)
                                // se llama a la función que completa el flujo de envío, 
                                // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                                // en su evento callback                    
                                ProviderConexionCallBack(socketDeTrabajoProveedor, saeaProveedor);
                        }
                        catch (Exception ex)
                        {
                            logTrace.EscribirLog(ex.Message + ",ProcesarRecepcion, ConnectAsync, " + saeaProveedor.RemoteEndPoint.ToString() + ", cliente " + clientState.UniqueId, LogType.Error);

                            socketDeTrabajoProveedor.Close();
                            estadoDelProveedor.responseCode = (int)ProcessResponseCodes.NetworkError;
                            estadoDelProveedor.authorizationCode = 0;
                            estadoDelProveedor.ClientStateOriginal.responseCode = estadoDelProveedor.responseCode;
                            estadoDelProveedor.ClientStateOriginal.authorizationCode = estadoDelProveedor.authorizationCode;
                            ResponseToClient((T)estadoDelProveedor.ClientStateOriginal);
                            // se libera el semaforo por si otra petición está solicitando acceso
                            semaphoreProviderResourses.Release();
                            // el SAEA del proveedor se ingresa nuevamente al pool para ser re utilizado
                            providerStatesManager.SetStackItem(estadoDelProveedor);
                        }
                    }
                    else
                    {
                        ResponseToClient(clientState);
                    }
                }
                // si el código de respuesta es 30(error en el formato) o 50 (Error en algún paso de evaluar la mensajería),
                // se debe responder al cliente, de lo contrario si es un codigo de los anteriores, no se puede responder porque no se tienen confianza en los datos
                else if (clientState.responseCode != (int)ProcessResponseCodes.FormatError && clientState.responseCode != (int)ProcessResponseCodes.ProcessError)
                {
                    logTrace.EscribirLog("Error en el proceso de validación de la trama del cliente " + clientState.UniqueId, LogType.warnning);
                    ResponseToClient(clientState);
                }
                else
                {
                    CloseSocketClient(clientState);
                }
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + ". " + ex.StackTrace + ". ProcessMessageReceiveClient, cliente " + clientState.UniqueId, LogType.Error);
                CloseSocketClient(clientState);
            }
        }

        /// <summary>
        /// Función que entrega una respuesta al cliente por medio del socket de conexión
        /// </summary>
        /// <param name="clientState">Estado del cliente con los valores de retorno</param>
        private void ResponseToClient(T clientState)
        {
            if (clientState == null || clientState.responseInProcess)
            {
                return;
            }

            clientState.SetResponseInProcess();

            // trato de obtener la trama que se le responderá al cliente
            clientState.GetResponseMessage();

            // Si ya se cuenta con una respuesta(s) para el cliente
            if (clientState.responseMessage != "")
            {
                if (!testMode)
                    clientState.SaveTransaction();

                // se obtiene el mensaje de respuesta que se enviará cliente
                string mensajeRespuesta = clientState.responseMessage;
                logTrace.EscribirLog("Mensaje de respuesta: " + mensajeRespuesta + " al cliente " + clientState.UniqueId, LogType.Information);

                // se obtiene la cantidad de bytes de la trama completa
                int numeroDeBytes = Encoding.ASCII.GetBytes(mensajeRespuesta, 0, mensajeRespuesta.Length, clientState.socketAsyncEventArgs.Buffer, clientState.socketAsyncEventArgs.Offset);
                // si el número de bytes es mayor al buffer que se tiene destinado a la recepción, no se puede proceder, no es válido el mensaje
                if (numeroDeBytes > bufferSizePerRequest)
                {
                    logTrace.EscribirLog("La respuesta es más grande que el buffer, cliente " + clientState.UniqueId, LogType.warnning);
                    CloseSocketClient(clientState);
                    return;
                }
                try
                {
                    // Se solicita el espacio de buffer para los bytes que se van a enviar                    
                    clientState.socketAsyncEventArgs.SetBuffer(clientState.socketAsyncEventArgs.Offset, numeroDeBytes);
                }
                catch (Exception ex)
                {
                    logTrace.EscribirLog("Error asignando buffer para la respuesta al cliente " + clientState.UniqueId + ". " + ex.Message, LogType.Error);
                    return;
                }

                try
                {

                    // se envía asincronamente por medio del socket copia de recepción que es
                    // con el que se está trabajando en esta operación, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                    bool seHizoAsync = clientState.SocketToWork.SendAsync(clientState.socketAsyncEventArgs);
                    if (!seHizoAsync)
                        // Si se tiene una respuesta False de que el proceso está pendiente, se completa el flujo,
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        ReceiveSendProcessCallBack(clientState.SocketToWork, clientState.socketAsyncEventArgs);
                }
                catch (Exception ex)
                {
                    logTrace.EscribirLog(ex.Message + ", ResponseToClient, cliente " + clientState.UniqueId, LogType.Error);
                    CloseSocketClient(clientState);
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
                        clientState.socketAsyncEventArgs.SetBuffer(clientState.socketAsyncEventArgs.Offset, bufferSizePerRequest);
                        // se solicita un proceso de recepción asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                        // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                        bool seHizoAsync = clientState.SocketToWork.ReceiveAsync(clientState.socketAsyncEventArgs);
                        if (!seHizoAsync)
                            // si el evento indica que el proceso asincrono está pendiente, se completa el flujo,
                            // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                            // en su evento callback
                            ReceiveSendProcessCallBack(clientState.SocketToWork, clientState.socketAsyncEventArgs);
                    }
                    catch (Exception ex)
                    {
                        logTrace.EscribirLog(ex.Message + ", ResponseToClient, Desconectando cliente " + clientState.UniqueId, LogType.warnning);
                        CloseSocketClient(clientState);
                    }
                }
            }
        }

        /// <summary>
        /// Función callback que se utiliza cuando en un proceso ciclico de envio y recepción
        /// </summary>
        /// <param name="clientState">Objeto con la información y socket de trabajo de cliente</param>
        private void ReceiveSendCycleProcessClientSide(T clientState)
        {

            // Una vez terminado el envio, se continua escuchando por el Socket de trabajo
            try
            {
                // se asigna el buffer para continuar el envío
                clientState.socketAsyncEventArgs.SetBuffer(clientState.socketAsyncEventArgs.Offset, bufferSizePerRequest);
                // se inicia el proceso de recepción asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                bool seHizoAsync = clientState.SocketToWork.ReceiveAsync(clientState.socketAsyncEventArgs);
                if (!seHizoAsync)
                    // si el evento indica que el proceso está pendiente, se completa el flujo,
                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                    // en su evento callback
                    ReceiveSendProcessCallBack(clientState.SocketToWork, clientState.socketAsyncEventArgs);
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + ", ReceiveSendCycleProcessClientSide, cliente " + clientState.UniqueId, LogType.Error);
                CloseSocketClient(clientState);
            }
        }

        /// <summary>
        /// Cierra el socket asociado a un cliente y retira al cliente de la lista de clientes conectados
        /// </summary>
        /// <param name="clientState">Instancia del cliente a cerrar</param>
        public void CloseSocketClient(T clientState)
        {
            // Se comprueba que la información del socket de trabajo sea null, ya que podría ser invocado como resultado 
            // de una operación de E / S sin valores
            if (clientState == null)
            {
                logTrace.EscribirLog("estadoDelCliente null", LogType.Error);
                return;
            }

            // proporciona un mecanismo de sincronización de acceso a datos donde un hilo solo puede tener acceso a un
            // bloque de código a la vez, en este caso en ingresar al listado de clientes, de lo contrario habría 
            // cross threading y provocaría error
            bool bloqueo = Monitor.TryEnter(_connectedClients, 1000);
            if (bloqueo)
            {
                try
                {
                    // se busca en la lista el cliente y se remueve porque se va a desconectar
                    if (_connectedClients.ContainsKey(clientState.UniqueId))
                    {
                        _connectedClients.Remove(clientState.UniqueId);
                    }
                    else
                    {
                        logTrace.EscribirLog("el cliente " + clientState.UniqueId.ToString() + " no se encuentra en listaClientes a desconectar, ya ha sido desconectado en otro proceso", LogType.warnning);
                        return;     // quiere decir que ya está desconectado
                    }
                }
                catch (Exception ex)
                {
                    logTrace.EscribirLog(ex.Message + " en CerrarSocketCliente, listaClientes, cliente " + clientState.UniqueId, LogType.Error);
                }
                finally
                {
                    Monitor.Exit(_connectedClients);
                }
            }
            else
            {
                logTrace.EscribirLog("Error obteniendo el bloqueo, CerrarSocketCliente, listaClientes", LogType.Error);
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
                logTrace.EscribirLog(ex.Message + " en CloseSocketClient, shutdown de envío en el socket de trabajo del cliente " + clientState.UniqueId, LogType.warnning);
            }

            try
            {
                socketDeTrabajoACerrar.Close();
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + " en CloseSocketClient, close en el socket de trabajo del cliente " + clientState.UniqueId, LogType.Error);
            }

            // se llama a la secuencia de cerrando para tener un flujo de eventos
            ServerStateBase.OnClientClosed(clientState);

            // se libera la instancia de socket de trabajo para reutilizarlo
            clientStatesManager.SetStackItem(clientState);
            // se marca el semáforo de que puede aceptar otro cliente

            if (semaphoreClientResourses.CurrentCount < maximumSimultaneousConnectionsClientSide)
            {
                //logTrace.EscribirLog("Se libera semaforoParaAceptarClientes " + semaforoParaAceptarClientes.CurrentCount.ToString() + ", para el cliente " + estadoDelCliente.IdUnicoCliente, tipoLog.ALERTA);
                semaphoreClientResourses.Release();
            }
        }

        #endregion

        #region ProcesoDePeticionesProveedor

        /// <summary>
        /// Funcion callback para la conexión al proveedor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="socketAsyncEventArgs"></param>
        private void ProviderConexionCallBack(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            // Si hay errores, debo regresar el estado del proveedor que se está usando a la pila de estados para ser reutilizado
            //X estadoDelProveedor = e.UserToken as X;
            if (socketAsyncEventArgs == null)
            {
                logTrace.EscribirLog("estadoDelProveedor recibido es nulo", LogType.Error);
                return;
            }

            if (!(socketAsyncEventArgs.UserToken is X estadoDelProveedor))
            {
                logTrace.EscribirLog("estadoDelProveedor recibido es inválido para la operacion", LogType.Error);
                return;
            }

            // se valida que existan errores registrados
            if (socketAsyncEventArgs.SocketError != SocketError.Success && socketAsyncEventArgs.SocketError != SocketError.IsConnected)
            {
                logTrace.EscribirLog("Error en la conexión, ConexionProveedorCallBack " + estadoDelProveedor.endPoint + ", cliente " + estadoDelProveedor.ClientStateOriginal.UniqueId, LogType.Error);

                estadoDelProveedor.responseCode = (int)ProcessResponseCodes.NetworkError;
                estadoDelProveedor.authorizationCode = 0;
                estadoDelProveedor.ClientStateOriginal.responseCode = estadoDelProveedor.responseCode;
                estadoDelProveedor.ClientStateOriginal.authorizationCode = estadoDelProveedor.authorizationCode;

                estadoDelProveedor.SaveTransaction();

                ResponseToClient((T)estadoDelProveedor.ClientStateOriginal);
                providerStatesManager.SetStackItem(estadoDelProveedor);
                semaphoreProviderResourses.Release();
                return;
            }

            // si todo va bien
            estadoDelProveedor.SetMainSocketReference(this);
            // se le indica al estado del proveedor el socket de trabajo
            try
            {
                estadoDelProveedor.SocketToWork = socketAsyncEventArgs.AcceptSocket;
                if (estadoDelProveedor.SocketToWork == null)
                {
                    throw new Exception("estadoDelProveedor.socketDeTrabajo recibido es inválido para la operacion");
                }
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + "  ProviderConexionCallBack, obteniendo el socket de trabajo, cliente " + estadoDelProveedor.ClientStateOriginal.UniqueId, LogType.Error);
                estadoDelProveedor.responseCode = (int)ProcessResponseCodes.NetworkError;
                estadoDelProveedor.authorizationCode = 0;
                estadoDelProveedor.ClientStateOriginal.responseCode = estadoDelProveedor.responseCode;
                estadoDelProveedor.ClientStateOriginal.authorizationCode = estadoDelProveedor.authorizationCode;

                estadoDelProveedor.SaveTransaction();

                ResponseToClient((T)estadoDelProveedor.ClientStateOriginal);
                providerStatesManager.SetStackItem(estadoDelProveedor);
                semaphoreProviderResourses.Release();
                return;
            }

            estadoDelProveedor.socketAsyncEventArgs.UserToken = estadoDelProveedor;

            // obtengo las tramas para considerar cualquier evento antes de enviar la petición al proveedor.
            // se puede actualizar más adelante
            // solo por precaución se inicializan los valores
            estadoDelProveedor.responseCode = 0;
            estadoDelProveedor.authorizationCode = 0;
            estadoDelProveedor.GetRequestMessage();

            //Si todas la validaciones hasta ahora fueron exitosas
            // Se guarda  la transacción para posterior actualizarla
            estadoDelProveedor.SaveTransaction();

            try
            {
                if (estadoDelProveedor.SocketToWork.Connected)
                {
                    string mensajeAlProveedor = estadoDelProveedor.requestMessage;
                    logTrace.EscribirLog("Mensaje enviado del proveedor: " + estadoDelProveedor.requestMessage + " para el cliente: " + estadoDelProveedor.ClientStateOriginal.UniqueId, LogType.Information);

                    // se obtiene la cantidad de bytes de la trama completa
                    int numeroDeBytes = Encoding.Default.GetBytes(mensajeAlProveedor, 0, mensajeAlProveedor.Length, estadoDelProveedor.socketAsyncEventArgs.Buffer, estadoDelProveedor.socketAsyncEventArgs.Offset);
                    // si el número de bytes es mayor al buffer que se tiene destinado a la recepción, no se puede proceder, no es válido el mensaje
                    if (numeroDeBytes > bufferSizePerRequest)
                    {
                        logTrace.EscribirLog("El mensaje al proveedor es más grande que el buffer, cliente: " + estadoDelProveedor.ClientStateOriginal.UniqueId, LogType.warnning);
                        CloseSocketProvider(estadoDelProveedor);
                        return;
                    }

                    // Se prepara el buffer del SAEA con el tamaño predefinido                         
                    estadoDelProveedor.socketAsyncEventArgs.SetBuffer(estadoDelProveedor.socketAsyncEventArgs.Offset, numeroDeBytes);

                    estadoDelProveedor.providerTimer = new Timer(new TimerCallback(TickTimer), estadoDelProveedor, 1000, 1000);

                    // se procede a la recepción asincrona del mensaje,el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                    bool seHizoAsync = estadoDelProveedor.SocketToWork.SendAsync(estadoDelProveedor.socketAsyncEventArgs);
                    if (!seHizoAsync)
                        // se llama a la función que completa el flujo de envío, 
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        ReceiveSendOutputProcessCallBack(estadoDelProveedor.SocketToWork, estadoDelProveedor.socketAsyncEventArgs);
                }
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + "ProviderConexionCallBack, iniciando conexión con el proveedor, cliente " + estadoDelProveedor.ClientStateOriginal.UniqueId, LogType.Error);
                estadoDelProveedor.responseCode = (int)ProcessResponseCodes.SocketCriticalError;
                estadoDelProveedor.authorizationCode = 0;
                estadoDelProveedor.ClientStateOriginal.responseCode = estadoDelProveedor.responseCode;
                estadoDelProveedor.ClientStateOriginal.authorizationCode = estadoDelProveedor.authorizationCode;
                ResponseToClient((T)estadoDelProveedor.ClientStateOriginal);
                CloseSocketProvider(estadoDelProveedor);
            }

        }

        ///// <summary>
        ///// Timer que controla el TO de conexión asincrona  al proveedor
        ///// </summary>
        ///// <param name="info">Estado con información útil</param>
        //private void TimerConexionTimeOutProveedor(object info)
        //{
        //    X estadoProveedor = info as X;
        //    AutoResetEvent autoResetConexionProveedor = estadoProveedor.autoResetConexionProveedor;

        //    //Si pasado el tiempo de espera, el socket  no se ha conectado, entonces se elimina
        //    if (estadoProveedor.socketDeTrabajo == null)
        //    {
        //        ResponderAlCliente((T)estadoProveedor.estadoDelClienteOrigen, 70, 0);
        //    }
        //    // instrucción para dejar de esperar en proceso principal
        //    autoResetConexionProveedor.Set();
        //}

        /// <summary>
        /// Función call back para el evento de envío y recepción al proveedor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="socketAsyncEventArgs"></param>
        private void ReceiveSendOutputProcessCallBack(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            // obtengo el estado del proveedor
            // se comprueba que el estado haya sido obtenido correctamente
            if (!(socketAsyncEventArgs.UserToken is X estadoDelProveedor))
            {
                logTrace.EscribirLog("estadoDelProveedor recibido es inválido para la operacion", LogType.Error);
                return;
            }

            // se determina que operación se está llevando a cabo para indicar que manejador de eventos se ejecuta
            switch (socketAsyncEventArgs.LastOperation)
            {
                case SocketAsyncOperation.Send:

                    // se comprueba que no hay errores con el socket
                    if (socketAsyncEventArgs.SocketError == SocketError.Success)
                    {
                        // se procesa el envío
                        ReceiveSendOutputCicleProcessProvider(estadoDelProveedor);
                    }
                    else
                    {
                        logTrace.EscribirLog("Error en el envío a " + estadoDelProveedor.socketAsyncEventArgs.RemoteEndPoint + ", RecepcionEnvioSalienteCallBack" + socketAsyncEventArgs.SocketError.ToString() +
                            ", cliente " + estadoDelProveedor.ClientStateOriginal.UniqueId, LogType.Error);
                        estadoDelProveedor.responseCode = (int)ProcessResponseCodes.NoResponseProvider;
                        estadoDelProveedor.authorizationCode = 0;
                        estadoDelProveedor.ClientStateOriginal.responseCode = estadoDelProveedor.responseCode;
                        estadoDelProveedor.ClientStateOriginal.authorizationCode = estadoDelProveedor.authorizationCode;
                        ResponseToClient((T)estadoDelProveedor.ClientStateOriginal);
                        if (!estadoDelProveedor.IsTimeOver)
                            CloseSocketProvider(estadoDelProveedor);
                    }
                    break;
                case SocketAsyncOperation.Receive:

                    // se comprueba que exista información
                    if (socketAsyncEventArgs.BytesTransferred > 0 && socketAsyncEventArgs.SocketError == SocketError.Success)
                    {
                        // se procesa la solicitud
                        ProcessMessageReceiveProvider(estadoDelProveedor);
                    }
                    else
                    {
                        estadoDelProveedor.responseCode = (int)ProcessResponseCodes.NoResponseProvider;
                        estadoDelProveedor.authorizationCode = 0;
                        estadoDelProveedor.ClientStateOriginal.responseCode = estadoDelProveedor.responseCode;
                        estadoDelProveedor.ClientStateOriginal.authorizationCode = estadoDelProveedor.authorizationCode;
                        ResponseToClient((T)estadoDelProveedor.ClientStateOriginal);
                        if (!estadoDelProveedor.IsTimeOver)
                            CloseSocketProvider(estadoDelProveedor);
                    }
                    break;
                default:
                    logTrace.EscribirLog("La ultima operación no se detecto como de recepcion o envío, RecepcionEnvioSalienteCallBack, " + socketAsyncEventArgs.LastOperation.ToString(), LogType.warnning);
                    estadoDelProveedor.responseCode = (int)ProcessResponseCodes.NetworkError;
                    estadoDelProveedor.authorizationCode = 0;
                    estadoDelProveedor.ClientStateOriginal.responseCode = estadoDelProveedor.responseCode;
                    estadoDelProveedor.ClientStateOriginal.authorizationCode = estadoDelProveedor.authorizationCode;
                    ResponseToClient((T)estadoDelProveedor.ClientStateOriginal);
                    //CerrarSocketProveedor(estadoDelProveedor);
                    break;
            }
        }

        /// <summary>
        /// Función para procesar el envío al proveedor y dejar de nuevo en escucha al socket
        /// </summary>
        /// <param name="providerState">Estado del proveedor con la información de conexión</param>
        private void ReceiveSendOutputCicleProcessProvider(X providerState)
        {
            if (providerState == null)
            {
                logTrace.EscribirLog("estadoDelProveedor es inválido para la operacion", LogType.Error);
                return;
            }

            // Una vez terminado el envio, se continua escuchando por el Socket de trabajo
            try
            {
                // se asigna el buffer para continuar el envío
                providerState.socketAsyncEventArgs.SetBuffer(providerState.socketAsyncEventArgs.Offset, bufferSizePerRequest);
                // se inicia el proceso de recepción asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                bool seHizoAsync = providerState.SocketToWork.ReceiveAsync(providerState.socketAsyncEventArgs);
                if (!seHizoAsync)
                    // si el evento indica que el proceso está pendiente, se completa el flujo,
                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                    // en su evento callback
                    ReceiveSendOutputProcessCallBack(providerState.SocketToWork, providerState.socketAsyncEventArgs);
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog("Error al ponerse en espera de respuesta del proveedor: " + ex.Message + ", ReceiveSendOutputCicleProcessProvider, cliente " + providerState.ClientStateOriginal.UniqueId, LogType.Error);
                providerState.responseCode = (int)ProcessResponseCodes.SocketCriticalError;
                providerState.authorizationCode = 0;
                providerState.ClientStateOriginal.responseCode = providerState.responseCode;
                providerState.ClientStateOriginal.authorizationCode = providerState.authorizationCode;
                ResponseToClient((T)providerState.ClientStateOriginal);
                CloseSocketProvider(providerState);
            }

        }

        /// <summary>
        /// Función que realiza la recepción del mensaje y lo procesa
        /// </summary>
        /// <param name="providerState">Estado del proveedor con la información de conexión</param>
        private void ProcessMessageReceiveProvider(X providerState)
        {
            if (providerState == null)
            {
                logTrace.EscribirLog("estadoDelProveedor es inválido para la operacion", LogType.Error);
                return;
            }

            // se obtiene el SAEA de recepción
            SocketAsyncEventArgs saeaRecepcion = providerState.socketAsyncEventArgs;
            // se obtienen los bytes que han sido recibidos
            int bytesTransferred = saeaRecepcion.BytesTransferred;

            // se obtiene el mensaje y se decodifica
            string mensajeRecibido = Encoding.ASCII.GetString(saeaRecepcion.Buffer, saeaRecepcion.Offset, bytesTransferred);

            // el mensaje recibido llevará un proceso, que no debe ser llevado por el core, se coloca en la función virtual
            // para que se consuma en otra capa, se procese y se entregue una respuesta
            try
            {
                logTrace.EscribirLog("Mensaje recibido del proveedor: " + mensajeRecibido.Substring(2) + " para el cliente: " + providerState.ClientStateOriginal.UniqueId, LogType.Information);
                providerState.ProcessProviderMessage(mensajeRecibido);
                providerState.GetResponseMessage();
            }
            catch (Exception ex)
            {
                providerState.responseCode = (int)ProcessResponseCodes.ProcessError;
                providerState.authorizationCode = 0;
                logTrace.EscribirLog(ex.Message + ", procesando trama del proveedor, ProcessMessageReceiveProvider, cliente " + providerState.ClientStateOriginal.UniqueId, LogType.Error);
                return;
            }

            if (providerState.responseMessage == "")
            {
                providerState.responseCode = (int)ProcessResponseCodes.ProcessError;
                providerState.authorizationCode = 0;
            }
            providerState.ClientStateOriginal.responseCode = providerState.responseCode;
            providerState.ClientStateOriginal.authorizationCode = providerState.authorizationCode;
            ResponseToClient((T)providerState.ClientStateOriginal);
            CloseSocketProvider(providerState);
        }

        /// <summary>
        /// Cierra el socket asociado a un proveedor y retira al proveedor de la lista de conectados
        /// </summary>
        public void CloseSocketProvider(X providerState)
        {
            // Se comprueba que la información del socket de trabajo sea null, ya que podría ser invocado como resultado 
            // de una operación de E / S sin valores
            if (providerState == null) return;

            if (providerState.SocketToWork == null) return;

            // se obtiene el socket específico del cliente en cuestión
            Socket socketDeTrabajoACerrar = providerState.SocketToWork;

            if (socketDeTrabajoACerrar.Connected)
            {
                // se inhabilita y se cierra dicho socket
                try
                {
                    socketDeTrabajoACerrar.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex)
                {
                    logTrace.EscribirLog(ex.Message + " en CloseSocketProvider, shutdown de envio el socket de trabajo del proveedor " + providerState.ClientStateOriginal.UniqueId, LogType.Error);
                }

                try
                {
                    socketDeTrabajoACerrar.Close();
                }
                catch (Exception ex)
                {
                    logTrace.EscribirLog(ex.Message + " en CloseSocketProvider, close el socket de trabajo del proveedor " + providerState.ClientStateOriginal.UniqueId, LogType.Error);
                }
            }

            // se libera la instancia de socket de trabajo para reutilizarlo
            providerStatesManager.SetStackItem(providerState);
            // se marca el semáforo de que puede aceptar otro cliente
            if (this.semaphoreProviderResourses.CurrentCount < this.maximumSimultaneousConnectionsProviderSide)
            {
                //logTrace.EscribirLog("Se libera semaforoParaAceptarProveedores " + semaforoParaAceptarProveedores.CurrentCount.ToString() + ", para el cliente " + estadoDelProveedor.estadoDelClienteOrigen.IdUnicoCliente, tipoLog.ALERTA);
                this.semaphoreProviderResourses.Release();
            }
        }

        ///// <summary>
        ///// Función que realiza el envío de la trama de respuesta al cliente desde los estado del proveedor
        ///// </summary>
        ///// <param name="estadoDelProveedor">EstadoDelProveedor</param>
        //private void ResponderAlCliente(X estadoDelProveedor)
        //{
        //    if (estadoDelProveedor == null)
        //    {
        //        return;
        //    }
        //    T estadoDelCliente = (T)estadoDelProveedor.estadoDelClienteOrigen;
        //    if (estadoDelCliente == null || estadoDelCliente.seHaRespondido || estadoDelCliente.seEstaRespondiendo)
        //    {
        //        return;
        //    }

        //    estadoDelCliente.SetProcessingResponse();

        //    estadoDelCliente.codigoAutorizacion = estadoDelProveedor.codigoAutorizacion;
        //    estadoDelCliente.codigoRespuesta = estadoDelProveedor.codigoRespuesta;

        //    // trato de obtener la trama que se le responderá al cliente
        //    estadoDelCliente.ObtenerTramaRespuesta();

        //    // Si ya se cuenta con una respuesta(s) para el cliente
        //    if (estadoDelCliente.tramaRespuesta != "")
        //    {
        //        // se guarda la transacción sin importar si pudiera existir un error porque al cliente siempre se le debe responder
        //        estadoDelProveedor.GuardarTransaccion();

        //        // se obtiene el mensaje de respuesta que se enviará cliente
        //        string mensajeRespuesta = estadoDelCliente.tramaRespuesta;
        //        try
        //        {
        //            if (int.TryParse(mensajeRespuesta.Substring(0, 2), out int encabezado))
        //            {
        //                logTrace.EscribirLog("Mensaje de respuesta: " + mensajeRespuesta + " al cliente " + estadoDelCliente.idUnicoCliente, tipoLog.INFORMACION);
        //            }
        //            else
        //            {
        //                logTrace.EscribirLog("Mensaje de respuesta: " + mensajeRespuesta.Substring(2) + " al cliente " + estadoDelCliente.idUnicoCliente, tipoLog.INFORMACION);
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            logTrace.EscribirLog("Mensaje de respuesta: " + mensajeRespuesta + " al cliente " + estadoDelCliente.idUnicoCliente, tipoLog.INFORMACION);
        //        }

        //        // se obtiene la cantidad de bytes de la trama completa
        //        int numeroDeBytes = Encoding.ASCII.GetBytes(mensajeRespuesta, 0, mensajeRespuesta.Length, estadoDelCliente.saeaDeEnvioRecepcion.Buffer, estadoDelCliente.saeaDeEnvioRecepcion.Offset);
        //        // si el número de bytes es mayor al buffer que se tiene destinado a la recepción, no se puede proceder, no es válido el mensaje
        //        if (numeroDeBytes > tamanoBufferPorPeticion)
        //        {
        //            logTrace.EscribirLog("La respuesta es más grande que el buffer para el cliente " + estadoDelCliente.idUnicoCliente, tipoLog.ALERTA);
        //            CerrarSocketCliente(estadoDelCliente);
        //            return;
        //        }
        //        try
        //        {
        //            // Se solicita el espacio de buffer para los bytes que se van a enviar                    
        //            estadoDelCliente.saeaDeEnvioRecepcion.SetBuffer(estadoDelCliente.saeaDeEnvioRecepcion.Offset, numeroDeBytes);
        //        }
        //        catch (Exception)
        //        {
        //            // cuando se presente que se deba responder al cliente con el saea del lado CLIENTE y se solicite responderle también desde el lado del proveedor
        //            // es muy común cuando se presenta un timeout en el cliente pero el proveedor aún sigue su curso
        //            //logTrace.EscribirLog(ex.Message + ", en estadoDelCliente.saeaDeEnvioRecepcion.SetBuffer, ResponderAlCliente(estadoDelProveedor), cliente " + estadoDelCliente.idUnicoCliente, tipoLog.ALERTA);
        //            return;
        //        }

        //        try
        //        {
        //            // se envía asincronamente por medio del socket copia de recepción que es
        //            // con el que se está trabajando en esta operación, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
        //            // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
        //            bool seHizoAsync = estadoDelCliente.socketDeTrabajo.SendAsync(estadoDelCliente.saeaDeEnvioRecepcion);
        //            if (!seHizoAsync)
        //                // Si se tiene una respuesta False de que el proceso está pendiente, se completa el flujo,
        //                // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
        //                // en su evento callback
        //                RecepcionEnvioEntranteCallBack(estadoDelCliente.socketDeTrabajo, estadoDelCliente.saeaDeEnvioRecepcion);

        //            estadoDelCliente.SetFinishedProcessingResponse();
        //            estadoDelCliente.SetResponsed();
        //        }
        //        catch (Exception ex)
        //        {
        //            logTrace.EscribirLog("ResponderAlCliente del lado proveedor: " + ex.Message + ". Del cliente " + estadoDelCliente.idUnicoCliente, tipoLog.ERROR);
        //            CerrarSocketCliente(estadoDelCliente);
        //            return;
        //        }
        //    }
        //    else  // Si el proceso no tuvo una respuesta o se descartó por error, se procede a volver a escuchar para recibir la siguiente trama del mismo cliente
        //    {
        //        if (estadoDelCliente.socketDeTrabajo.Connected)
        //        {
        //            try
        //            {
        //                // se solicita el espacio de buffer para la recepción del mensaje
        //                estadoDelCliente.saeaDeEnvioRecepcion.SetBuffer(estadoDelCliente.saeaDeEnvioRecepcion.Offset, tamanoBufferPorPeticion);
        //                // se solicita un proceso de recepción asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
        //                // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
        //                bool seHizoAsync = estadoDelCliente.socketDeTrabajo.ReceiveAsync(estadoDelCliente.saeaDeEnvioRecepcion);
        //                if (!seHizoAsync)
        //                    // si el evento indica que el proceso asincrono está pendiente, se completa el flujo,
        //                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
        //                    // en su evento callback
        //                    RecepcionEnvioEntranteCallBack(estadoDelCliente.socketDeTrabajo, estadoDelCliente.saeaDeEnvioRecepcion);
        //                estadoDelCliente.SetFinishedProcessingResponse();
        //                estadoDelCliente.SetResponsed();
        //            }
        //            catch (Exception ex)
        //            {
        //                logTrace.EscribirLog(ex.Message + ", procesarRecepcion, Desconectando cliente " + estadoDelCliente.idUnicoCliente, tipoLog.ALERTA);
        //                CerrarSocketCliente(estadoDelCliente);
        //            }
        //        }
        //    }
        //}

        #endregion

        #region Timeout Validation

        /// <summary>
        /// Evento asincrono del timer para medir el timeout del servidor
        /// </summary>
        /// <param name="state"></param>
        private void TickTimer(object state)
        {
            try
            {
                X estadoDelProveedor = (X)state;
                bool seSincronzo = Monitor.TryEnter(estadoDelProveedor, 500);
                if (seSincronzo)
                {
                    if (estadoDelProveedor.ClientStateOriginal.responseInProcess)
                    {
                        estadoDelProveedor.providerTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        estadoDelProveedor.providerTimer.Dispose();
                    }
                    else if (IsTimeOver((T)estadoDelProveedor.ClientStateOriginal))
                    {
                        TimeSpan timeSpan = DateTime.Now - estadoDelProveedor.ClientStateOriginal.DateTimeReceiveMessage;
                        logTrace.EscribirLog("Se venció el TimeOut para el proveedor: " +
                                estadoDelProveedor.ClientStateOriginal.UniqueId +
                                ", TickTimer, fecha hora inicial " + estadoDelProveedor.ClientStateOriginal.DateTimeReceiveMessage +
                                ", segundos transcurridos " + timeSpan.Seconds +
                                ", TimeOut configurado " + estadoDelProveedor.ClientStateOriginal.TimeOut, LogType.warnning);

                        estadoDelProveedor.providerTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        estadoDelProveedor.providerTimer.Dispose();

                        estadoDelProveedor.responseCode = (int)ProcessResponseCodes.NoResponseProvider;
                        estadoDelProveedor.authorizationCode = 0;
                        estadoDelProveedor.ClientStateOriginal.responseCode = estadoDelProveedor.responseCode;
                        estadoDelProveedor.ClientStateOriginal.authorizationCode = estadoDelProveedor.authorizationCode;
                        estadoDelProveedor.SetTimeOver();
                        CloseSocketProvider(estadoDelProveedor);
                    }
                    Monitor.Exit(estadoDelProveedor);
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Verificación del tiempo de la transacción sobre el proceso del clente
        /// </summary>
        /// <param name="estadoDelCliente">instancia del estado del cliente</param>
        /// <returns></returns>
        private bool IsTimeOver(T estadoDelCliente)
        {
            try
            {
                TimeSpan timeSpan = DateTime.Now - estadoDelCliente.DateTimeReceiveMessage;
                return timeSpan.Seconds > estadoDelCliente.TimeOut;
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + ", IsTimeOver, cliente " + estadoDelCliente.UniqueId, LogType.warnning);
                return true;
            }

        }

        #endregion

        #region FuncionesDeAyuda     

        /// <summary>
        /// Envía un mensaje sincronamente (Discontinuado porque ya se puede hacer asincrono)
        /// </summary>
        /// <param name="mensaje">mensaje a enviar</param>
        /// <param name="e">A client's SocketAsyncEventArgs</param>
        public void SendMessageSync(string mensaje, SocketAsyncEventArgs e)
        {
            T socketDeTrabajoInfoCliente = e.UserToken as T;
            Byte[] bufferEnvio;
            bufferEnvio = Encoding.ASCII.GetBytes(mensaje);

            if (socketDeTrabajoInfoCliente.SocketToWork.Connected)
            {
                socketDeTrabajoInfoCliente.SocketToWork.Send(bufferEnvio);
            }
        }

        ///// <summary>
        ///// Adiciona IP como bloqueada
        ///// </summary>
        ///// <param name="ip">IP a agregar</param>
        ///// <param name="razon">la razón del bloquedo</param>
        ///// <param name="segundosBloqueo">Número de segundos que estaba bloqueada</param>
        //public void AgregarListaBloqueados(IPAddress ip, string razon, int segundosBloqueo)
        //{
        //    // se hace un bloqueo sobre la lista para que no exista choque de procesos
        //    // en el ingreso y obtención
        //    lock (listaClientesBloqueados)
        //    {
        //        if (!listaClientesBloqueados.ContainsKey(ip))
        //        {
        //            listaClientesBloqueados.Add(ip, new ClienteBloqueo(ip, razon, segundosBloqueo, true));
        //        }
        //    }
        //}

        ///// <summary>
        ///// Remueve una ip como bloqueada
        ///// </summary>
        ///// <param name="ip">Ip a remover de la lista</param>
        //public void RemoverListaBloqueados(IPAddress ip)
        //{
        //    // se hace un bloqueo sobre la lista para que no exista choque de procesos
        //    // en el ingreso y obtención
        //    lock (listaClientesBloqueados)
        //    {
        //        if (listaClientesBloqueados.ContainsKey(ip))
        //            listaClientesBloqueados.Remove(ip);
        //    }
        //}

        ///// <summary>
        ///// Valida que una ip esté bloqueada
        ///// </summary>
        ///// <param name="ip">ip a validar</param>
        //public bool ClienteEstaBloqueado(IPAddress ip)
        //{
        //    return listaClientesBloqueados.ContainsKey(ip);
        //}


        /// <summary>
        /// Se detiene el servidor
        /// </summary>
        public void StopServer()
        {
            // se indica que se está ejecutando el proceso de desconexión de los clientes
            disconnecting = true;
            List<T> listaDeClientesEliminar = new List<T>();

            // Primero se detiene y se cierra el socket de escucha
            try
            {
                this.mainSocketListening.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + " en StopServer.Shutdown", LogType.Error);
            }

            try
            {
                mainSocketListening.Close();
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + " en StopServer.Close", LogType.Error);
            }


            // se recorre la lista de clientes conectados y se adiciona a la lista de clientes para desconectar
            foreach (T socketDeTrabajoPorCliente in _connectedClients.Values)
            {
                listaDeClientesEliminar.Add(socketDeTrabajoPorCliente);
            }

            // luego se cierran las conexiones de los clientes en la lista anterior
            foreach (T socketDeTrabajoPorCliente in listaDeClientesEliminar)
            {
                CloseSocketClient(socketDeTrabajoPorCliente);
            }
            // se limpia la lista
            listaDeClientesEliminar.Clear();
            _connectedClients.Clear();

            _isRunning = false;
            disconnecting = false;
        }

        #endregion


    }
}
