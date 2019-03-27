using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;

namespace ServidorCore
{
    /// <summary>
    /// Clase principal sobre el core del servidor transaccional, contiene todas las propiedades 
    /// del servidor y los métodos de envío y recepción asincronos
    /// </summary>
    /// <typeparam name="T">Instancia sobre la clase que contiene la información de un cliente conectado y su
    /// socket de trabajo una vez asignado desde el pool</typeparam>
    /// <typeparam name="S">Instancia sobre la clase que contiene el estado de flujo de un socket de trabajo</typeparam>
    public class servidorTransaccionalMain<T,S>
        where T : infoYSocketDeTrabajo, new()
        where S : estadoSocketDeTrabajo, new()
    {
        #region Propiedades públicas

        /// <summary>        
        /// nombre del servidor para identificarlo en una lista
        /// </summary>
        public string nombreServidor { get; set; }

        /// <summary>
        /// Descripción del servidor
        /// </summary>
        public string descripciónServidor { get; set; }

        /// <summary>
        /// Puerto del servidor
        /// </summary>
        public Int32 puertoServidor { get; set; }

        /// <summary>
        /// Obtiene una lista de clientes ordenados por un GUID
        /// </summary>
        public Dictionary<Guid, T> listaClientes;

        /// <summary>
        /// Obtiene o ingresa una ip a bloquear como prohibida
        /// </summary>
        public Dictionary<IPAddress, clientesBloqueados> listaClientesBloqueados { get; set; }

        /// <summary>
        /// Ingresa u obtiene a la lista de ip permitidas, como un firewall
        /// </summary>
        public List<Regex> listaClientesPermitidos { get; set; }

        /// <summary>        
        /// Obtiene o ingresa el número máximo de conexiones simultaneas de una misma IP del cliente (0=ilimitadas)
        /// </summary>
        public int numeroMaximoConexionesPorIpCliente { get; set; }

        /// <summary>
        /// Obtiene o ingresa el valor de que si el servidor iniciará automaticamente solo con la lista de clientes permitidos
        /// </summary>
        public bool inicioAutomaticoPorDefecto { get; set; }

        /// <summary>
        /// Obtiene o ingresa el valor de que si el servidor está o no ejecutandose
        /// </summary>
        public bool enEjecucion { get; set; }

        /// <summary>
        /// Obtiene o ingresa el estado del socket de trabajo
        /// </summary>
        public S estadoDelSocketDeTrabajo { get; set; }

        /// <summary>
        /// Obtiene o ingresa a la lista de clientes pendientes de desconexión, esta lista es para la verificación de que todos los cliente
        /// se desconectan adecuadamente, su uso es más para debug
        /// </summary>
        public List<T> listaClientesPendientesDesconexion { get; set; }

        /// <summary>
        /// Obtiene el número de clientes conectados actualmente al servidor
        /// </summary>
        public int numeroclientesConectados
        {
            get
            {
                return listaClientes.Values.Count;
            }
        }

        #endregion

        #region Propiedades privadas
        /// <summary>
        /// Número de conexiones simultaneas que podrá manejar el servidor por defecto
        /// </summary>
        private int numeroConexionesSimultaneas;
        /// <summary>
        /// Número sockest para lectura y escritura sin asignación de espacio del buffer para aceptar peticiones como default
        /// esto para tener siempre por lo menos dos sockects disponibles al inicio del servidor
        /// </summary>
        private const int opsToPreAlloc = 2;
        /// <summary>
        /// instancia al administrador de estados de socket de trabajo
        /// </summary>
        private administradorEstadosDeSockets<T> administradorDeEstadosDeSockets;
        /// <summary>
        /// semáforo sobre las peticiones de clientes para controlar el número total de clientes que podrá soportar el servidor
        /// </summary>
        private Semaphore semaforoClientesAceptados;
        /// <summary>
        /// Número total de bytes recibido en el servidor, para uso estadístico
        /// </summary>
        private int totalBytesLeidos;
        /// <summary>
        /// Representa un conjunto enorme de buffer reutilizables entre todos los sockects de trabajo
        /// </summary>
        private administradorBuffer administradorBuffer;
        /// <summary>
        /// Socket de escucha para las conexiones de clientes
        /// </summary>
        private Socket socketDeEscucha;
        /// <summary>
        /// Bandera para identificar alguna desconexión remota
        /// </summary>
        private bool desconectando = false;
        /// <summary>
        /// Parámetros que  indica el máximo de pedidos que pueden encolarse simultáneamente en caso que el servidor 
        /// esté ocupado atendiendo una nueva conexión.
        /// </summary>
        private int backLog;
        /// <summary>
        /// Tamaño del buffer de recepción
        /// </summary>
        private int tamanoBufferRecepcion;
        /// <summary>
        /// Retraso en el envío, es para uso en Debug
        /// </summary>
        internal static int MaxDelaySend = 0;

        #endregion

        #region Constructor

        /// <summary>
        /// Crea una instancia del administrador de sockets, posterior se tiene que llamar al método
        /// inicializar para iniciar el proceso de asignacion de socket        
        /// </summary>
        /// <param name="numeroConexionesSimultaneas">Maximo número de conexiones simultaneas a manejar en el servidor</param>
        /// <param name="tamanoBuffer">Tamaño del buffer para la operación</param>
        /// <param name="backLog">Parámetro TCP/IP backlog</param>
        public servidorTransaccionalMain(Int32 numeroConexionesSimultaneas, Int32 tamanoBuffer, int backLog)
        {
            //Se inicializan los parámetros
            totalBytesLeidos = 0;
            this.numeroConexionesSimultaneas = numeroConexionesSimultaneas;
            //Se coloca ilimitado para fines no restrictivos
            numeroMaximoConexionesPorIpCliente = 0;            
            this.backLog = backLog;
            listaClientes = new Dictionary<Guid, T>();
            listaClientesBloqueados = new Dictionary<IPAddress, clientesBloqueados>();
            listaClientesPermitidos = new List<Regex>();
            listaClientesPendientesDesconexion = new List<T>();
            tamanoBufferRecepcion = tamanoBuffer;

            try
            {
                estadoDelSocketDeTrabajo = new S();
            }
            catch (Exception ex)
            {                
                InvokeAppendLog(ex.Message);
            }

            estadoDelSocketDeTrabajo.procesoPrincipal = this;
            enEjecucion = false;
                        
            //Asignación de un buffer tomando en cuenta por lo menos los 2 sockets por defecto para lectura y escritura iniciales
            // es decir, el tamaño del buffer por operación por el número de conexiónes por el número de sockets iniciales no dará
            // el valor de buffer enorme
            this.administradorBuffer = new administradorBuffer(tamanoBuffer * numeroConexionesSimultaneas * opsToPreAlloc, tamanoBuffer);

            //se inicializan los estados de socket necesarios para el número simultaneo de conexiones            
            administradorDeEstadosDeSockets = new administradorEstadosDeSockets<T>(numeroConexionesSimultaneas);

            //inicializa el número inicial y maximo de entradas simultaneas, será el semáforo para saber si hay saturación
            this.semaforoClientesAceptados = new Semaphore(numeroConexionesSimultaneas, numeroConexionesSimultaneas);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Inicializa el servidor con un pre asignación de buffers reusables y estados de sockets
        /// </summary>
        public void inicializarServidor()
        {
            //objetos para operaciones asincronas en los sockets
            SocketAsyncEventArgs eventArgDeLectura, eventArgDeEnvio;
                        
            //Asigna un buffer suficientemente grande para todas las operaciones y poder reutilizarlo por secciones
            this.administradorBuffer.inicializarBuffer();
                        
            //pre asignar un conjunto de estados de socket para usarlos inmediatamente en cada una
            // de la conexiones simultaneas que se pueden esperar
            for (Int32 i = 0; i < this.numeroConexionesSimultaneas; i++)
            {                
                T infoYSocketDeTrabajoCliente = new T();
                //Se inicializa en cada asignación
                infoYSocketDeTrabajoCliente.inicializarInfoYSocketDeTrabajoCliente();
                
                
                eventArgDeLectura = new SocketAsyncEventArgs();
                //El manejador de eventos para cada lectura de una peticion del cliente
                eventArgDeLectura.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                //SocketAsyncEventArgs necesita un objeto con la información de cada cliente para su administración
                eventArgDeLectura.UserToken = infoYSocketDeTrabajoCliente;
                //Se establece el buffer que se utilizará en la operación de lectura del cliente
                administradorBuffer.asignarBuffer(eventArgDeLectura);
                //Se establece el socket asincrono de EventArg a utilizar en la lectura del cliente
                infoYSocketDeTrabajoCliente.saeaDeRecepcion = eventArgDeLectura;

                
                eventArgDeEnvio = new SocketAsyncEventArgs();
                //El manejador de eventos para cada envío a un cliente
                eventArgDeEnvio.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                //SocketAsyncEventArgs necesita un objeto con la información de cada cliente para su administración
                eventArgDeEnvio.UserToken = infoYSocketDeTrabajoCliente;
                //Se establece el buffer que se utilizará en la operación de envío al cliente
                administradorBuffer.asignarBuffer(eventArgDeEnvio);
                //Se establece el socket asincrono de EventArg a utilizar en el envío al cliente
                infoYSocketDeTrabajoCliente.saeaDeEnvio = eventArgDeEnvio;

                //Ya con los parametros establecidos para cada operaciones, se ingresa en la pila
                //de estados de socket para desde ahi administar su uso en cada petición
                administradorDeEstadosDeSockets.ingresarUnElemento(infoYSocketDeTrabajoCliente);
            }
        }

        /// <summary>
        /// Se inicia el servidor de manera que esté escuchando solicitudes de conexión entrantes.
        /// </summary>
        /// <param name="puerto">Puerto de escucha del servidor</param>
        public void iniciarServidor(Int32 puerto)
        {
            //Se inicializa la bandera de que no hay ningún cliente pendiente por desconectar
            desconectando = false;
            //De acuerdo a las buenas practicas de manejo de operaciones asincronas, se debe ANUNCIAR el inicio
            //de un trabajo asincrono para ir controlando su avance por eventos
            estadoDelSocketDeTrabajo.OnInicio();
            listaClientes = new Dictionary<Guid, T>();
            listaClientesBloqueados = new Dictionary<IPAddress, clientesBloqueados>();

            Int32 puertoLocal = puerto;
            // Se obtiene información relacionada con el servidor
            IPAddress[] addressList = Dns.GetHostEntry(Environment.MachineName).AddressList;
            
            // se obtiene el punto de entrada para escuchar
            // IPEndPoint localEndPoint = new IPEndPoint(addressList[addressList.Length - 1], port);
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, puertoLocal);
                        
            // se crea el socket que se utilizará de escucha para las conexiones entrantes
            this.socketDeEscucha = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // cuando en lugar del protocolo ip4 es 6 pero casi no se utiliza
            if (localEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // Set dual-mode (IPv4 & IPv6) for the socket listener.
                // 27 is equivalent to IPV6_V6ONLY socket option in the winsock snippet below,
                // based on http://blogs.msdn.com/wndp/archive/2006/10/24/creating-ip-agnostic-applications-part-2-dual-mode-sockets.aspx
                this.socketDeEscucha.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                this.socketDeEscucha.Bind(new IPEndPoint(IPAddress.IPv6Any, localEndPoint.Port));
            }
            else
            {
                // se asocia con el puerto de escucha el socket de escucha
                this.socketDeEscucha.Bind(localEndPoint);
            }

            // se inicia la escucha de conexiones con un backlog de 100 conexiones
            this.socketDeEscucha.Listen(backLog);

            // Se indica al sistema que se empiezan a aceptar conexiones
            this.iniciarAceptaciones(null);
            enEjecucion = true;

            // se marca en pantalla que se inicia el servidor            
            //InvokeAppendLog("Se inicia el servidor");
        }

        /// <summary>
        /// Se detiene el servidor
        /// </summary>
        public void detenerServidor()
        {
            // se indica que se está ejecutando el proceso de desconexión de los clientes
            desconectando = true;
            List<T> listaDeClientesEliminar = new List<T>();

            // Primero se detiene y se cierra el socket de escucha
            try
            {
                this.socketDeEscucha.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {                
                InvokeAppendLog(ex.Message + " en detenerServidor");
            }
            socketDeEscucha.Close();

            // se recorre la lista de clientes conectados y se adiciona a la lista de clientes para desconectar
            foreach (T socketDeTrabajoPorCliente in listaClientes.Values)
            {
                listaDeClientesEliminar.Add(socketDeTrabajoPorCliente);
            }

            // luego se cierran las conexiones de los clientes en la lista anterior
            foreach (T socketDeTrabajoPorCliente in listaDeClientesEliminar)
            {
                CloseClientSocket(socketDeTrabajoPorCliente);
            }
            // se limpia la lista
            listaDeClientesEliminar.Clear();

            enEjecucion = false;
            desconectando = false;

            // Se marca en log que se detiene el servidor            
        }

        /// <summary>
        /// Cierra el socket asociado a un cliente y retira al cliente de la lista de clientes conectados
        /// </summary>
        /// <param name="e">SocketAsyncEventArg asociado en la operacion de envío y recepción con el cliente</param>
        public void CloseClientSocket(T socketDeTrabajoInfoCliente)
        {            
            // Se comprueba que la información del socket de trabajo sea null, ya que podría ser invocado como resultado 
            // de una operación de E / S sin valores
            if (socketDeTrabajoInfoCliente == null) return;

            // proporciona un mecanismo de sincronización de acceso a datos donde un hilo solo puede tener acceso a un
            // bloque de código a la vez, en este caso en ingresar al listado de clientes, de lo contrario habría 
            // cross threading y provocaría error
            bool gotLock = Monitor.TryEnter(listaClientes, 5000);
            if (gotLock)
            {
                try
                {
                    // se busca en la lista el cliente y se remueve porque se va a desconectar
                    if (listaClientes.ContainsKey(socketDeTrabajoInfoCliente.user_Guid))
                        listaClientes.Remove(socketDeTrabajoInfoCliente.user_Guid);
                    else
                        return;     // quiere decir que ya está desconectado
                }
                catch(Exception ex)
                {                    
                    InvokeAppendLog(ex.Message + " en CloseClientSocket, listaClientes, remove");
                }
                finally
                {
                    Monitor.Exit(listaClientes);
                }
            }
            else
            {                
                InvokeAppendLog("Error obteniendo el bloqueo, CloseClientSocket, listaClientes, remove");
            }
                        
            // se limpia la cola de envío de datos para cada socket, para así forzar la detención de respuestas
            lock (socketDeTrabajoInfoCliente.colaEnvio)
            {
                socketDeTrabajoInfoCliente.colaEnvio.Clear();
            }

            // se obtiene el socket específico del cliente en cuestión
            Socket socketDeTrabajoACerrar = socketDeTrabajoInfoCliente.socketDeTrabajo;

            // se inhabilita y se cierra dicho socket
            try
            {
                socketDeTrabajoACerrar.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                InvokeAppendLog(ex.Message + " en CloseClientSocket, shutdown el socket de trabajo");
            }
            socketDeTrabajoACerrar.Close();

            string tmpIP = socketDeTrabajoInfoCliente.ipCliente;
            Guid tmpGuid = socketDeTrabajoInfoCliente.user_Guid;

            // se llama a la secuencia de cerrando para tener un flujo de eventos
            estadoDelSocketDeTrabajo.OnClienteCerrado(socketDeTrabajoInfoCliente);

            // se espera a que un cliente acaba de recibir la última información y se cierra
            int tiempoInicial, tiempoFinal;
            tiempoInicial = Environment.TickCount;            
            bool receivedSignal = socketDeTrabajoInfoCliente.waitSend.WaitOne(10000, false);
            tiempoFinal = Environment.TickCount;
            // la señal debe ser false si excedió el TO
            if (!receivedSignal)
            {
                // se mide el tiempo de espera para el envío de la información y si supera el establecido es un error
                if (tiempoFinal - tiempoInicial > MaxDelaySend)
                {
                    MaxDelaySend = tiempoFinal - tiempoInicial;
                    InvokeAppendLog("maximo tiempo de espera para el envío de información: " + MaxDelaySend + " ms, CloseClientSocket");                    
                }
                InvokeAppendLog("No se puede esperar para terminar el envío de información al cliente, CloseClientSocket");                
                // como no puedo esperar tanto al envío se deja pendiente, esto para no perder ninguna transacción
                listaClientesPendientesDesconexion.Add(socketDeTrabajoInfoCliente);
                return;
            }
                        
            // se libera la instancia de socket de trabajo para reutilizarlo
            administradorDeEstadosDeSockets.ingresarUnElemento(socketDeTrabajoInfoCliente);
            // se marca el semáforo de que puede aceptar otro cliente
            this.semaforoClientesAceptados.Release();
        }

        /// <summary>
        /// Manda el mensaje al cliente sobre el socket de trabajo
        /// </summary>
        /// <param name="mensaje">Mensaje a enviar por el socket</param>
        /// <param name="socketDeTrabajoInfoCliente">la información del cliente y su socket de trabajo</param>
        public void envioInfo(string mensaje, T socketDeTrabajoInfoCliente)
        {            
            int longitudColaEnvio;
            if (mensaje == "")
            {
                InvokeAppendLog("Mensaje vacío, no se puede enviar, envioInfo");                
                return;
            }
            // si está desconectado se marca como error
            if (!socketDeTrabajoInfoCliente.socketDeTrabajo.Connected)
            {
                InvokeAppendLog("Socket desconectado, envioInfo");                
                // se cierra el socket para reutilizarlo
                CloseClientSocket(socketDeTrabajoInfoCliente);
                return;
            }
             
            // se utiliza un bloqueo sobre la cola de envío al cliente y no
            // choquen procesos de ingreso y obtención
            lock (socketDeTrabajoInfoCliente.colaEnvio)
            {
                // se ingresa el mensaje que se enviará al cliente al final de la cola
                socketDeTrabajoInfoCliente.colaEnvio.Enqueue(mensaje);
                // se obtiene la longitud de cola de envío
                longitudColaEnvio = socketDeTrabajoInfoCliente.colaEnvio.Count();
            }

            // TODO: Este límite hasta ahora funciona, en un cambio se podría mejorar para hacerlo dinámico
            // la cola de mensajes no puede ser mayor a 500, es demasiado pero será nuestro límite
            if (longitudColaEnvio > 499)
            {                
                InvokeAppendLog("Cola de mensaje superó el límite de 500 elementos, envioInfo");
                // TODO: revisar porque no se puede obtener el GUID del cliente para el log anterior
                //InvokeAppendLog(socketDeTrabajoInfoCliente.user_Guid + " send queue " + longitudColaEnvio + " exceeded limit");
                // es mejor cerrar el socket porque existe un sobre encolamiento de transacciones
                CloseClientSocket(socketDeTrabajoInfoCliente);
                return;
            }

            // se llama la función que procesará la cola de envío
            colaDeProcesamiento(socketDeTrabajoInfoCliente);
        }

        /// <summary>
        /// Envía un mensaje sincronamente (Discontinuado porque ya se puede hacer asincrono)
        /// </summary>
        /// <param name="mensaje">mensaje a enviar</param>
        /// <param name="e">A client's SocketAsyncEventArgs</param>
        public void envioInfoSincro(string mensaje, SocketAsyncEventArgs e)
        {
            T socketDeTrabajoInfoCliente = e.UserToken as T;            
            Byte[] bufferEnvio;
            bufferEnvio = Encoding.ASCII.GetBytes(mensaje);

            if (socketDeTrabajoInfoCliente.socketDeTrabajo.Connected)
            {
                socketDeTrabajoInfoCliente.socketDeTrabajo.Send(bufferEnvio);
            }
        }

        /// <summary>
        /// Adiciona IP como bloqueada
        /// </summary>
        /// <param name="ip">IP a agregar</param>
        /// <param name="razon">la razón del bloquedo</param>
        /// <param name="segundosBloqueo">Número de segundos que estaba bloqueada</param>
        public void agregarListaBloqueados(IPAddress ip, string razon, int segundosBloqueo)
        {
            // se hace un bloqueo sobre la lista para que no exista choque de procesos
            // en el ingreso y obtención
            lock (listaClientesBloqueados)
            {
                if (!listaClientesBloqueados.ContainsKey(ip))
                {
                    listaClientesBloqueados.Add(ip, new clientesBloqueados(ip, razon, segundosBloqueo, true));
                }
            }
        }

        /// <summary>
        /// Remueve una ip como bloqueada
        /// </summary>
        /// <param name="ip">Ip a remover de la lista</param>
        public void removerListaBloqueados(IPAddress ip)
        {
            // se hace un bloqueo sobre la lista para que no exista choque de procesos
            // en el ingreso y obtención
            lock (listaClientesBloqueados)
            {
                if (listaClientesBloqueados.ContainsKey(ip))
                    listaClientesBloqueados.Remove(ip);
            }
        }

        /// <summary>
        /// Valida que una ip esté bloqueada
        /// </summary>
        /// <param name="ip">ip a validar</param>
        public bool clienteEstaBloqueado(IPAddress ip)
        {
            return listaClientesBloqueados.ContainsKey(ip);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Se inicia la operación de aceptar solicitudes por parte de un cliente
        /// </summary>
        /// <param name="acceptEventArg">Objeto que se utilizará en cada aceptación de una solicitud</param>
        private void iniciarAceptaciones(SocketAsyncEventArgs acceptEventArg)
        {
            // de ser null quiere decir que no hay objeto instanciado y debe crearse desde cero
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(procesarSolicitudDeAceptacionDeConexion);
            }
            else
            {
                // si ya existe instancia, se limpia el socket para trabajo
                acceptEventArg.AcceptSocket = null;
            }

            // se comprueba el semáforo que nos indica que se tiene recursos para aceptar la conexión
            this.semaforoClientesAceptados.WaitOne();
            try
            {
                // se comienza asincronamente el proceso de aceptación y mediante un evento manual 
                // se verifica que haya sido exitoso. Cuando el proceso asincrono es exitoso devuelve false
                bool willRaiseEvent = socketDeEscucha.AcceptAsync(acceptEventArg);
                if (!willRaiseEvent)
                    // se manda llamar a la función que procesa la solicitud, 
                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                    // en su evento callback
                    procesarSolicitudDeAceptacionDeConexion(socketDeEscucha, acceptEventArg);
            }
            catch (Exception ex)
            {
                InvokeAppendLog(ex.Message + "en iniciarAceptaciones");                
                // se hace un último intento para volver a iniciar el servidor por si el error fue una excepción no controlada
                iniciarAceptaciones(acceptEventArg);
            }
        }

        /// <summary>
        /// Cierra el socket asociado al cliente pero este método no retira de la lista de clientes conectados al cliente actual
        /// </summary>
        /// <param name="socketCliente">The socket to close</param>
        private void CloseClientSocketConnection(Socket socketCliente)
        {
            try
            {
                socketCliente.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                InvokeAppendLog(ex.Message + ", CloseClientSocketConnection, shutdown");
            }
            socketCliente.Close();

            this.semaforoClientesAceptados.Release();
        }

        /// <summary>
        /// Procesa la solicitud por medio de socket principal de escucha
        /// </summary>
        /// <param name="sender">Objeto que se tomará como quien dispara el evento principal</param>
        /// <param name="e">SocketAsyncEventArg asociado al proceso asincrono.</param>
        private void procesarSolicitudDeAceptacionDeConexion(object sender, SocketAsyncEventArgs e)
        {
            // se valida que exista errores registrados
            if (e.SocketError != SocketError.Success)
            {
                // se comprueba que no hay un procesa de desconexión en curso
                if (desconectando)
                {
                    InvokeAppendLog("Socket de escucha desconectado, procesarSolicitudDeAceptacionDeConexion");                    
                    return;
                }

                // se dispara el evento manual para liberarlo
                semaforoClientesAceptados.Release();
                
                // aquí el truco en volver a iniciar el proceso de aceptación de solicitudes con el mismo
                // objeto de socket que tiene el cliente
                iniciarAceptaciones(e);
                return;
            }

            // como no hay errores declaro un objeto de trabajo al cliente
            T infoSocketTrabajoCliente = null;
            IPAddress ip = (e.AcceptSocket.RemoteEndPoint as IPEndPoint).Address;
            Int32 puerto = (e.AcceptSocket.RemoteEndPoint as IPEndPoint).Port;

            // Se valida que la ip no esté bloqueada
            if (clienteEstaBloqueado(ip))
            {
                CloseClientSocketConnection(e.AcceptSocket);
                iniciarAceptaciones(e);
                return;
            }
                                   
            // Se valida que la ip sea una de las permitidas
            // TODO: Colocar la expresión regular adecuada
            if (listaClientesPermitidos.Count > 0)
            {
                bool hayCoincidencia = false;
                // con la expresión regular se comprueba que la ip esté permitida
                foreach (Regex ip_exp in listaClientesPermitidos)
                {
                    if (ip_exp.IsMatch(ip.ToString()))
                    {
                        hayCoincidencia = true; break;
                    }
                }
                if (!hayCoincidencia)
                {
                    InvokeAppendLog("Client IP " + ip.ToString() + " not permitted");
                    CloseClientSocketConnection(e.AcceptSocket);
                    iniciarAceptaciones(e);
                    return;
                }
            }

            // Check if this IP has MaxSimultaneousConnections connections
            /* if (MaxSimultaneousConnections != 0)
            {
                List<Socket> lst2delete = new List<Socket>();
                lock (clientList)
                {
                    if (clientList.ContainsKey(ip))
                    {
                        l = clientList[ip];
                        if (l.Count >= MaxSimultaneousConnections)
                        {
                            foreach (var item in l)
                                lst2delete.Add(item.Socket);
                        }
                    }
                }
                if (lst2delete.Count > 0)
                {
                    // Close this connection
                    this.CloseClientSocketConnection(e.AcceptSocket);
                    // and close the others MaxSimultaneousConnections connections
                    foreach (Socket sk in lst2delete)
                    {
                        try
                        {
                            sk.Shutdown(SocketShutdown.Send);
                        }
                        catch (Exception) { }
                        sk.Close();
                    }

                    string banmsg = "More than " + MaxSimultaneousConnections.ToString() + " sim. conn. ";
                    AddToBannedList(ip, banmsg, 0);   // Permanent ban

                    // Accept the next connection request.
                    semaphoreConcurrentAccepts.Release();
                    this.StartAccept(e);
                    return;
                }
            } */

            
            // si el cliente pasó todas las validaciones entonces se le asigna ya un socket de trabajo del pool con todas sus propiedes
            infoSocketTrabajoCliente = administradorDeEstadosDeSockets.obtenerUnElemento();
            infoSocketTrabajoCliente.SetParentSocketServer(this);
            infoSocketTrabajoCliente.socketDeTrabajo = e.AcceptSocket;
            infoSocketTrabajoCliente.ipCliente = ip.ToString();
            infoSocketTrabajoCliente.puertoCliente = puerto;

            // Se llama al método de OnAceptacion ya del socket de trabajo para indicar la continuación del flujo
            // con estas acciones puedo controlar las acciones en cada fase del proceso de recepción y envío de ser necesario
            estadoDelSocketDeTrabajo.OnAceptacion(infoSocketTrabajoCliente);

            
            ip = IPAddress.Parse(infoSocketTrabajoCliente.ipCliente);
            // se ingresa el cliente a la lista de clientes
            // Monitor proporciona un mecanismo que sincroniza el acceso a datos entre hilos
            bool gotLock = Monitor.TryEnter(listaClientes, 5000);
            if (gotLock)
            {
                try
                {
                    if (!listaClientes.ContainsKey(infoSocketTrabajoCliente.user_Guid))
                        listaClientes.Add(infoSocketTrabajoCliente.user_Guid, infoSocketTrabajoCliente);
                }
                finally
                {
                    Monitor.Exit(listaClientes);
                }
            }
            else
            {
                // si no puedo ingresarlo en la lista de clientes debo rechazarlo
                CloseClientSocket(infoSocketTrabajoCliente);
                
                InvokeAppendLog("Timeout de 5 seg para obtener bloqueo en procesarSolicitudDeAceptacionDeConexion, listaClientes, add");
                // coloco nuevamente el socket en proceso de aceptación
                this.iniciarAceptaciones(e);
                return;
            }

            // se comprueba que exista un mensaje de bienvenida, esto unicamente con el fin de indicarle al cliente que se conectó
            if (estadoDelSocketDeTrabajo.mensajeBienvenida(infoSocketTrabajoCliente) != "")
            {
                // se envía el mensaje de bienvenida por medio del socket de trabajo
                // TODO: debo hacer dinámico el indicador de fin de mensaje, ahora está "\r\n"
                string mensajeBienvenida = estadoDelSocketDeTrabajo.mensajeBienvenida(infoSocketTrabajoCliente) + "\r\n";
                int resultNumberOfBytes = Encoding.ASCII.GetBytes(mensajeBienvenida, 0, mensajeBienvenida.Length, infoSocketTrabajoCliente.saeaDeRecepcion.Buffer, infoSocketTrabajoCliente.saeaDeRecepcion.Offset);
                // ya cuento con el mensaje, se solicita un pedazo del buffer de trabajo para su recepción TODO: verificar si el de recepción es el correcto
                infoSocketTrabajoCliente.saeaDeRecepcion.SetBuffer(infoSocketTrabajoCliente.saeaDeRecepcion.Offset, resultNumberOfBytes);

                infoSocketTrabajoCliente.ultimoMensajeAlCliente = estadoDelSocketDeTrabajo.mensajeBienvenida(infoSocketTrabajoCliente);
                // para dejarlo como proceso, se maneja el evento manual pero forzado
                bool willRaiseEvent = true;
                try
                {
                    // se envía el mensaje al cliente, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    willRaiseEvent = infoSocketTrabajoCliente.socketDeTrabajo.SendAsync(infoSocketTrabajoCliente.saeaDeRecepcion);                    
                    if (!willRaiseEvent)
                        // se llama a la función que completa el flujo de envío cuando la respuesta es false,
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        OnIOCompleted(infoSocketTrabajoCliente.socketDeTrabajo, infoSocketTrabajoCliente.saeaDeRecepcion);                    
                }
                catch (Exception ex)
                {
                    InvokeAppendLog(ex.Message+ ", se perdió la conexión al enviar el mensaje de bienvenida, procesarSolicitudDeAceptacionDeConexion");                    
                    CloseClientSocket(infoSocketTrabajoCliente);
                }
            }
            else
            {
                // se inicia la recepción de datos del cliente
                if (infoSocketTrabajoCliente.socketDeTrabajo.Connected)
                {
                    // solo por si se requiere dejar en modo de espera al servidor, se manejan estados de operación
                    if (infoSocketTrabajoCliente.NextOperation == NextOperationModes.Normal)
                    {
                        // para dejarlo como proceso, se maneja el evento manual pero forzado
                        bool willRaiseEvent = true;
                        try
                        {
                            // se solicita un pedazo de buffer para la recepción del mensaje
                            infoSocketTrabajoCliente.saeaDeRecepcion.SetBuffer(infoSocketTrabajoCliente.saeaDeRecepcion.Offset, tamanoBufferRecepcion);
                            // se procede a la recepción asincrona del mensaje,el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                            willRaiseEvent = infoSocketTrabajoCliente.socketDeTrabajo.ReceiveAsync(infoSocketTrabajoCliente.saeaDeRecepcion);
                            if (!willRaiseEvent)
                                // se llama a la función que completa el flujo de envío, 
                                // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                                // en su evento callback
                                OnIOCompleted(infoSocketTrabajoCliente.socketDeTrabajo, infoSocketTrabajoCliente.saeaDeRecepcion);
                        }
                        catch (Exception ex)
                        {
                            InvokeAppendLog(ex.Message + "ProcesarSolicituDeAceptacionDeConexion, recibiendo mensaje del cliente");                            
                            CloseClientSocket(infoSocketTrabajoCliente);
                        }
                    }
                    else
                    {
                        // TODO: modo ocioso, no se envía ni se recibe info, esto es para ahorro de recursos pero falta definir
                    }
                }
            }

            // se indica que puede aceptar más solicitudes
            this.iniciarAceptaciones(e);
        }

        /// <summary>
        /// Operación de callBack que se llama cuando se envía o se recibe de un socket de trabajo para completar la operación
        /// </summary>
        /// <param name="sender">Objeto principal para devolver la llamada</param>
        /// <param name="e">SocketAsyncEventArg asociado a la operación de envío o recepción</param>
        /// </summary>
        private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
        {            
            // obtengo el estado del socket
            T infoYSocketDeTrabajo = e.UserToken as T;
            // se comprueba que el estado haya sido obtenido correctamente
            if (infoYSocketDeTrabajo == null)
            {
                InvokeAppendLog("infoYSocketDeTrabajo recibido es inválido para la operacion");                
                return;
            }

            // se determina que operación se está llevando a cabo para indicar que manejador de eventos se ejecuta
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    // se comprueba que exista información
                    if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                    {
                        // se procesa la solicitud
                        this.procesarRecepcion(infoYSocketDeTrabajo);
                    }
                    else
                    {
                        // se marca para la bitácora el error dentro del estado
                        infoYSocketDeTrabajo.ultimoErrorConexionCliente = e.SocketError.ToString();
                        // se cierra todo el socket de trabajo
                        CloseClientSocket(infoYSocketDeTrabajo);
                    }
                    break;
                case SocketAsyncOperation.Send:
                    // si se está enviando, entonces el argumento es un SAEA de envío, de lo contrario es de recepción ciclico
                    if (e == infoYSocketDeTrabajo.saeaDeEnvio)
                    {
                        // si es de envío se coloca el evento manual indicando que se estará enviando algo al cliente
                        infoYSocketDeTrabajo.waitSend.Set();
                        // se comprueba que no hay errores con el socket
                        if (e.SocketError == SocketError.Success)
                        {
                            // se procesa el envío
                            this.procesarEnvio(infoYSocketDeTrabajo);
                        }
                        else
                        {
                            InvokeAppendLog("Error en el envío, onIOCompleted" + e.SocketError.ToString());                            
                            CloseClientSocket(infoYSocketDeTrabajo);
                        }
                    }
                    else if (e == infoYSocketDeTrabajo.saeaDeRecepcion)
                    {
                        // se comprueba que no hay errores con el socket
                        if (e.SocketError == SocketError.Success)
                        {
                            // si el SAEA es de recepción en un flujo de envío, quiere decir que hay comunicación ciclica
                            this.procesarEnvioCiclico(infoYSocketDeTrabajo);
                        }
                        else
                        {
                            infoYSocketDeTrabajo.ultimoErrorConexionCliente = e.SocketError.ToString();
                            CloseClientSocket(infoYSocketDeTrabajo);
                        }
                    }
                    break;
                default:
                    InvokeAppendLog("La ultima operación no se detecto como de recepcion o envío, OnIOCompleted, " + e.LastOperation.ToString());                    
                    CloseClientSocket(infoYSocketDeTrabajo);
                    break;
            }
        }

        /// <summary>
        /// Este método se invoca cuando la operación de recepción asincrona se completa y si el cliente
        /// cierra la conexión el socket también se cierra y se libera
        /// </summary>
        /// <param name="infoYSocketDeTrabajoCliente">Objeto que tiene la información y socket de trabajo del cliente</param>
        private void procesarRecepcion(T infoYSocketDeTrabajoCliente)
        {
            // se obtiene el SAEA de recepción
            SocketAsyncEventArgs saeaRecepcion = infoYSocketDeTrabajoCliente.saeaDeRecepcion;
            // se obtienen los bytes que han sido recibidos
            Int32 bytesTransferred = saeaRecepcion.BytesTransferred;

            // se obtiene el mensaje y se decodifica
            String mensajeRecibido = Encoding.ASCII.GetString(saeaRecepcion.Buffer, saeaRecepcion.Offset, bytesTransferred);
                        
            // incrementa el contador de bytes totales recibidos
            // debido a que la variable está compartida entre varios procesos, se utiliza interlocked que ayuda a que no se revuelvan
            Interlocked.Add(ref this.totalBytesLeidos, bytesTransferred);
                        
            // el mensaje recibido llevará un proceso, que no debe ser llevado por el core, se coloca en la función virtual
            // para que se consuma en otra capa, se procese y se entregue una respuesta
            try
            {
                infoYSocketDeTrabajoCliente.procesamientoTramaGeneral(mensajeRecibido);
            }
            catch (Exception ex)
            {
                this.CloseClientSocket(infoYSocketDeTrabajoCliente);
                InvokeAppendLog(ex.Message + ", error llenado el buffer interno");                
                return;
            }
                        
            // si hay un error de parseo de la trama se desconecta al cliente inmediatamente
            if (infoYSocketDeTrabajoCliente.errorParseando)
            {
                CloseClientSocket(infoYSocketDeTrabajoCliente);
                return;
            }
            else
            {
                // Si ya se cuenta con una respuesta(s) para el cliente
                if (infoYSocketDeTrabajoCliente.secuenciaDeRespuestasAlCliente != "")
                {
                    // se obtiene el mensaje de respuesta que se enviará cliente
                    string mensajeRespuesta = infoYSocketDeTrabajoCliente.secuenciaDeRespuestasAlCliente;
                    // se obtiene la cantidad de bytes de la trama completa
                    int numeroDeBytes = Encoding.ASCII.GetBytes(mensajeRespuesta, 0, mensajeRespuesta.Length, infoYSocketDeTrabajoCliente.saeaDeRecepcion.Buffer, infoYSocketDeTrabajoCliente.saeaDeRecepcion.Offset);
                    // si el número de bytes es mayor al buffer que se tiene destinado a la recepción, no se puede proceder, no es válido el mensaje
                    if (numeroDeBytes > tamanoBufferRecepcion)
                    {
                        InvokeAppendLog("La respuesta es más grande que el buffer");                        
                        CloseClientSocket(infoYSocketDeTrabajoCliente);
                        return;
                    }
                    // Se solicita el espacio de buffer para los bytes que se van a enviar
                    infoYSocketDeTrabajoCliente.saeaDeRecepcion.SetBuffer(infoYSocketDeTrabajoCliente.saeaDeRecepcion.Offset, numeroDeBytes);

                    try
                    {
                        // se envía asincronamente por medio del socket copia de recepción que es
                        // con el que se está trabajando en esta operación, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                        bool willRaiseEvent = infoYSocketDeTrabajoCliente.socketDeTrabajo.SendAsync(saeaRecepcion);
                        if (!willRaiseEvent)
                            // Si se tiene una respuesta False de que el proceso está pendiente, se completa el flujo,
                            // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                            // en su evento callback
                            OnIOCompleted(infoYSocketDeTrabajoCliente.socketDeTrabajo, saeaRecepcion);
                    }
                    catch (Exception)
                    {
                        CloseClientSocket(infoYSocketDeTrabajoCliente);
                        return;
                    }
                }
                else  // El proceso de recepción no entrega una respuesta al cliente, se continúa la recepción de más tramas
                {
                    if (infoYSocketDeTrabajoCliente.socketDeTrabajo.Connected)
                    {
                        if (infoYSocketDeTrabajoCliente.NextOperation == NextOperationModes.Normal)
                        {
                            try
                            {
                                // se solicita el espacio de buffer para la recepción del mensaje
                                saeaRecepcion.SetBuffer(saeaRecepcion.Offset, tamanoBufferRecepcion);
                                // se solicita un proceso de recepción asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                                bool willRaiseEvent = infoYSocketDeTrabajoCliente.socketDeTrabajo.ReceiveAsync(saeaRecepcion);
                                if (!willRaiseEvent)
                                    // si el evento indica que el proceso asincrono está pendiente, se completa el flujo,
                                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                                    // en su evento callback
                                    OnIOCompleted(infoYSocketDeTrabajoCliente.socketDeTrabajo, saeaRecepcion);
                            }
                            catch (Exception ex)
                            {
                                InvokeAppendLog(ex.Message + ", procesarRecepcion, Desconectando cliente " + infoYSocketDeTrabajoCliente.user_Guid);                                
                                CloseClientSocket(infoYSocketDeTrabajoCliente);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Función callback que se utiliza cuando en un proceso ciclico de envio y recepción
        /// </summary>
        /// <param name="infoYSocketDeTrabajoCliente">Objeto con la información y socket de trabajo de cliente</param>
        private void procesarEnvioCiclico(T infoYSocketDeTrabajoCliente)
        {
            infoYSocketDeTrabajoCliente.fechaHoraUltimoMensajeAlCliente = DateTime.Now;
                        
            // Una vez terminado el envio, se continua escuchando por el Socket de trabajo
            try
            {
                // se asigna nuevo buffer para continuar la siguiente recepción
                infoYSocketDeTrabajoCliente.saeaDeRecepcion.SetBuffer(infoYSocketDeTrabajoCliente.saeaDeRecepcion.Offset, tamanoBufferRecepcion);
                // se inicia el proceso de recepción asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                bool willRaiseEvent = infoYSocketDeTrabajoCliente.socketDeTrabajo.ReceiveAsync(infoYSocketDeTrabajoCliente.saeaDeRecepcion);
                if (!willRaiseEvent)
                    // si el evento indica que el proceso está pendiente, se completa el flujo,
                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                    // en su evento callback
                    OnIOCompleted(infoYSocketDeTrabajoCliente.socketDeTrabajo, infoYSocketDeTrabajoCliente.saeaDeRecepcion);
            }
            catch (Exception)
            {
                CloseClientSocket(infoYSocketDeTrabajoCliente);
            }
        }

        /// <summary>
        /// Este metodo se invoca cuando una operación de envío asincrono se completa        
        /// </summary>
        /// <param name="infoYSocketDeTrabajo">Objeto con la información y el socket de trabajo sobre el envío</param>
        private void procesarEnvio(T infoYSocketDeTrabajo)
        {
            try
            {                
                // Se actualiza la fecha y hora del ultimo envío para controlar por si requerimos bitácora
                infoYSocketDeTrabajo.fechaHoraUltimoMensajeAlCliente = DateTime.Now;

                // se envía un elemento al cliente, revisando que está conectado
                if (infoYSocketDeTrabajo.socketDeTrabajo.Connected)
                {
                    // se genera un bloqueo con el objeto que destinamos para ello
                    lock (infoYSocketDeTrabajo.sincronizarEnvio)
                    {
                        // se coloca la bandera de que no se está enviando algo, esto lo cambiaré cuando procese el envío asincrono
                        infoYSocketDeTrabajo.isSending = false;
                    }
                    // se coloca el objeto de trabajo en la cola de envío para procesarlo en su momento
                    colaDeProcesamiento(infoYSocketDeTrabajo);
                }
            }
            catch (Exception ex)
            {
                InvokeAppendLog(ex.Message + "error fuera de excepcion, puede suceder por muchas cosas, se tratará de continuar operando");                
            }
        }

        /// <summary>
        /// Método que procesa tantos mensajes como sea posible en la cola de envío
        /// </summary>
        /// <param name="infoYSocketDeTrabajo">Objeto con la información y el socket de trabajo sobre el envío</param>
        private void colaDeProcesamiento(T infoYSocketDeTrabajo)
        {
            // genero dos matrices, una para ingresar el mensaje
            byte[] bytes;
            // y otra como buffer para su envío y así reutilizar de la pila de buffers
            byte[] bufferLocal = new byte[tamanoBufferRecepcion];

            int contadorBytes = 0;
            int cantMsgsSent = 0;


            if (!infoYSocketDeTrabajo.socketDeTrabajo.Connected)
            {
                InvokeAppendLog("Socket desconectado, colaDeProcesamiento");                
                CloseClientSocket(infoYSocketDeTrabajo);
                return;
            }

            // se sincroniza el envío con el objeto que se destinó para ello en cada objeto de estado
            lock (infoYSocketDeTrabajo.sincronizarEnvio)
            {
                // si ya se está enviando algo no se enviar al más en este momento
                if (infoYSocketDeTrabajo.isSending) return;

                string mensajeAEnviar = "";
                // también hago un bloqueo sobre la cola de envío mientras realizo una extracción y envío para que no exista choque de operaciones
                lock (infoYSocketDeTrabajo.colaEnvio)
                {
                    // si hay en cola mensajes a enviar, se procede
                    if (infoYSocketDeTrabajo.colaEnvio.Count > 0)
                    {
                        do
                        {
                            // se obtiene el primer elemento de la cola
                            mensajeAEnviar = infoYSocketDeTrabajo.colaEnvio.First();
                            // se revisa que el mensaje quepa en el buffer
                            if (mensajeAEnviar.Length > tamanoBufferRecepcion)
                                throw new Exception("Message size is greater than buffer size");

                            // si existen varios mensajes, el acumulado se tiene que estar validando si cabe en el buffer
                            if (contadorBytes + mensajeAEnviar.Length < tamanoBufferRecepcion)
                            {
                                // obtiene el objeto al principio de la cola y luego lo elimina
                                infoYSocketDeTrabajo.colaEnvio.Dequeue();
                                // se codifica
                                bytes = Encoding.ASCII.GetBytes(mensajeAEnviar);
                                try
                                {
                                    // se realiza la copia al buffer que será el de trabajo en el envío
                                    Buffer.BlockCopy(bytes, 0, bufferLocal, contadorBytes, bytes.Length);
                                }
                                catch (Exception ex)
                                {
                                    InvokeAppendLog("1st Block Copy error: " + ex.Message);
                                }

                                // se incrementa el contador de bytes
                                contadorBytes += bytes.Length;
                                // se incrementa el contador de mensajes a enviar
                                cantMsgsSent++;
                            }
                            else
                            {
                                break;
                            }
                        } while (infoYSocketDeTrabajo.colaEnvio.Count > 0);
                    }
                }

                // siempre y cuando exista por lo menos un mensaje a enviar se procede con este bloque
                if (cantMsgsSent > 0)
                {
                    try
                    {
                        // se copia al buffer de envío el mensaje codificado
                        Buffer.BlockCopy(bufferLocal, 0, infoYSocketDeTrabajo.saeaDeEnvio.Buffer, infoYSocketDeTrabajo.saeaDeEnvio.Offset, contadorBytes);
                    }
                    catch (Exception ex)
                    {
                        InvokeAppendLog(ex.Message + " segundo bloque de copiado, colaDeProcesamiento");                        
                    }

                    // se establece que parte del buffer se utilizará en este envío
                    infoYSocketDeTrabajo.saeaDeEnvio.SetBuffer(infoYSocketDeTrabajo.saeaDeEnvio.Offset, contadorBytes);
                    // se indica que se está enviando
                    infoYSocketDeTrabajo.isSending = true;
                    // se indica que el evento manual se reinicia a no señalado para bloquear el subproceso
                    infoYSocketDeTrabajo.waitSend.Reset();
                    try
                    {
                        // Se realiza el proceso de envío asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                        bool willRaiseEvent = infoYSocketDeTrabajo.socketDeTrabajo.SendAsync(infoYSocketDeTrabajo.saeaDeEnvio);
                        if (!willRaiseEvent)
                            // si la operación asincrona está pendiente, se completa el flujo,
                            // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                            // en su evento callback
                            OnIOCompleted(infoYSocketDeTrabajo.socketDeTrabajo, infoYSocketDeTrabajo.saeaDeEnvio);
                    }
                    catch (Exception ex)
                    {
                        InvokeAppendLog(ex.Message + "Desconectado al cliente " + infoYSocketDeTrabajo.user_Guid + ", colaDeProcesamiento");
                        //InvokeAppendLog("Disconnecting client (ProcessQueue) " + infoYSocketDeTrabajo.user_Guid + "\r\nConn: " + infoYSocketDeTrabajo.fechaHoraConexionCliente + ", Offset: " +
                        //    infoYSocketDeTrabajo.saeaDeEnvio.Offset.ToString() + " Sent: [" + infoYSocketDeTrabajo.ultimoMensajeAlCliente + "], Recv: [" + infoYSocketDeTrabajo.ultimoMensajeRecibidoCliente + "]\r\nError: " + ex.Message);
                        // se marca el evento manual en señalado para que otros proceso puedan continuar
                        infoYSocketDeTrabajo.waitSend.Set();
                        // se cierra la conexión
                        CloseClientSocket(infoYSocketDeTrabajo);
                    }
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Auxiliar logging function
        /// </summary>
        /// <param name="msg">Log message</param>
        private void InvokeAppendLog(string msg)
        {
            //Log.WriteLog(ELogLevel.DEBUG, "[" + this.nombreServidor + "] " + msg);
            cLogErrores.sNombreLog = "Graf_Serv";
            cLogErrores.sNombreOrigen = cLogErrores.sNombreLog;
            cLogErrores.Crear_Log();

            cLogErrores.Escribir_Log_Error(msg);

        }

        #endregion

    }


}
