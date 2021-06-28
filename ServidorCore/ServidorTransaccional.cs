using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ServidorCore
{
    /// <summary>
    /// Clase principal sobre el core del servidor transaccional, contiene todas las propiedades 
    /// del servidor y los métodos de envío y recepción asincronos
    /// </summary>
    /// <typeparam name="T">Instancia sobre la clase que contiene la información de un cliente conectado y su
    /// socket de trabajo una vez asignado desde el pool</typeparam>
    /// <typeparam name="S">Instancia sobre la clase que contiene el estado de flujo de un socket de trabajo</typeparam>
    public class ServidorTransaccional<T, S, X>
        where T : EstadoDelClienteBase, new()
        where S : EstadoDelServidorBase, new()
        where X : EstadoDelProveedorBase, new()
    {
        #region Propiedades públicas

        /// <summary>        
        /// nombre del servidor para identificarlo en una lista
        /// </summary>
        public string nombreServidor { get; set; }

        /// <summary>
        /// Descripción del servidor
        /// </summary>
        public string descripcionServidor { get; set; }

        /// <summary>
        /// Puerto del servidor
        /// </summary>
        public Int32 puertoServidor { get; set; }

        /// <summary>
        /// Obtiene una lista de clientes ordenados por un GUID
        /// </summary>
        public Dictionary<Guid, T> listaClientes;

        /// <summary>
        /// Obtiene o ingresa una ip a bloquear
        /// </summary>
        public Dictionary<IPAddress, ClienteBloqueo> listaClientesBloqueados { get; set; }

        /// <summary>
        /// Ingresa u obtiene a la lista de ip permitidas, como simulando un firewall
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
        /// Obtiene o ingresa el estado del socket del servidor
        /// </summary>
        public S estadoDelServidorBase { get; set; }

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
        private AdminEstadosDeCliente<T> adminEstadosCliente;

        private AdminEstadosDeProveedor<X> adminEstadosDeProveedor;

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
        private AdminBuffer administradorBuffer;

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
        /// Tamaño del buffer
        /// </summary>
        private int tamanoBuffer;

        /// <summary>
        /// Retraso en el envío, es para uso en Debug
        /// </summary>
        internal static int maxRetrasoParaEnvio = 0;

        #endregion

        /// <summary>
        /// Crea una instancia del administrador de sockets, posterior se tiene que llamar al método
        /// inicializar para iniciar el proceso de asignacion de socket        
        /// </summary>
        /// <param name="numeroConexionesSimultaneas">Maximo número de conexiones simultaneas a manejar en el servidor</param>
        /// <param name="tamanoBuffer">Tamaño del buffer para la operación global</param>
        /// <param name="backLog">Parámetro TCP/IP backlog</param>
        public ServidorTransaccional(Int32 numeroConexionesSimultaneas, Int32 tamanoBuffer, int backLog)
        {
            //Se inicializan los parámetros
            totalBytesLeidos = 0;
            this.numeroConexionesSimultaneas = numeroConexionesSimultaneas;
            //Se coloca ilimitado para fines no restrictivos
            numeroMaximoConexionesPorIpCliente = 0;
            this.backLog = backLog;
            listaClientes = new Dictionary<Guid, T>();
            listaClientesBloqueados = new Dictionary<IPAddress, ClienteBloqueo>();
            listaClientesPermitidos = new List<Regex>();
            listaClientesPendientesDesconexion = new List<T>();
            this.tamanoBuffer = tamanoBuffer;

            try
            {
                estadoDelServidorBase = new S();
            }
            catch (Exception ex)
            {
                //puede haber un error al crear una nueva instancia estadoDelServidorBase por cuestiones de sistema operativo
                InvokeAppendLog(ex.Message);
            }

            estadoDelServidorBase.procesoPrincipal = this;
            enEjecucion = false;

            //Asignación de un buffer tomando en cuenta por lo menos los 2 sockets por defecto para lectura y escritura iniciales
            // es decir, el tamaño del buffer por operación por el número de conexiónes por el número de sockets iniciales no dará
            // el valor de buffer enorme en bytes, por ejemplo: tamanoBuffer= 1024 * 1000 * 2 =2048000 bytes
            this.administradorBuffer = new AdminBuffer(tamanoBuffer * numeroConexionesSimultaneas * opsToPreAlloc, tamanoBuffer);

            //se inicializan los estados de socket necesarios para el número simultaneo de conexiones, para tenerlos listos a usarse como una pila            
            adminEstadosCliente = new AdminEstadosDeCliente<T>(numeroConexionesSimultaneas);

            //se inicializan los estados de socket necesarios para el número simultaneo de conexiones hacia el proveedor, para tenerlos listos a usarse como una pila            
            adminEstadosDeProveedor = new AdminEstadosDeProveedor<X>(numeroConexionesSimultaneas);

            //Se inicializa el número inicial y maximo de conexiones simultaneas soportadas, será el semáforo quien indique que hay saturación.
            this.semaforoClientesAceptados = new Semaphore(numeroConexionesSimultaneas, numeroConexionesSimultaneas);
        }

        /// <summary>
        /// Inicializa el servidor con una pre asignación de buffers reusables y estados de sockets
        /// </summary>
        public void ConfigInicioServidor()
        {
            //objetos para operaciones asincronas en los sockets
            SocketAsyncEventArgs saeaDeRecepcion, saeaDeEnvio;

            //Asigna un buffer suficientemente grande para todas las operaciones y poder reutilizarlo por secciones
            this.administradorBuffer.inicializarBuffer();

            //pre asignar un conjunto de estados de socket para usarlos inmediatamente en cada una
            // de la conexiones simultaneas que se pueden esperar
            for (Int32 i = 0; i < this.numeroConexionesSimultaneas; i++)
            {
                T estadoDelCliente = new T();
                //Se inicializa en cada asignación
                estadoDelCliente.InicializarEstadoDelClienteBase();


                saeaDeRecepcion = new SocketAsyncEventArgs();
                //El manejador de eventos para cada lectura de una peticion del cliente
                saeaDeRecepcion.Completed += new EventHandler<SocketAsyncEventArgs>(RecepcionEnvioEntranteCallBack);
                //SocketAsyncEventArgs necesita un objeto con la información de cada cliente para su administración
                saeaDeRecepcion.UserToken = estadoDelCliente;
                //Se establece el buffer que se utilizará en la operación de lectura del cliente en el eventArgDeRecepcion
                administradorBuffer.asignarBuffer(saeaDeRecepcion);
                //Se establece el socket asincrono de EventArg a utilizar en la lectura del cliente
                estadoDelCliente.saeaDeRecepcion = saeaDeRecepcion;


                saeaDeEnvio = new SocketAsyncEventArgs();
                //El manejador de eventos para cada envío a un cliente
                saeaDeEnvio.Completed += new EventHandler<SocketAsyncEventArgs>(RecepcionEnvioEntranteCallBack);
                //SocketAsyncEventArgs necesita un objeto con la información de cada cliente para su administración
                saeaDeEnvio.UserToken = estadoDelCliente;
                //Se establece el buffer que se utilizará en la operación de envío al cliente
                administradorBuffer.asignarBuffer(saeaDeEnvio);
                //Se establece el socket asincrono de EventArg a utilizar en el envío al cliente
                estadoDelCliente.saeaDeEnvio = saeaDeEnvio;

                //Ya con los parametros establecidos para cada operacion, se ingresa en la pila
                //de estados de socket y desde ahi administar su uso en cada petición
                adminEstadosCliente.ingresarUnElemento(estadoDelCliente);



                //Ahora genero la pila de estados para el servidor
                X estadoDelProveedor = new X();
                estadoDelProveedor.InicializarEstadoDelProveedorBase();

                SocketAsyncEventArgs saeaDeEnvioRecepcion;
                saeaDeEnvioRecepcion = new SocketAsyncEventArgs();
                //El manejador de eventos para cada lectura de una peticion del proveedor
                saeaDeEnvioRecepcion.Completed += new EventHandler<SocketAsyncEventArgs>(RecepcionEnvioSalienteCallBack);
                //SocketAsyncEventArgs necesita un objeto con la información de cada proveedor para su administración
                saeaDeEnvioRecepcion.UserToken = estadoDelProveedor;
                //Se establece el buffer que se utilizará en la operación de lectura del proveedor en el eventArgDeEnvioRecepcion
                administradorBuffer.asignarBuffer(saeaDeEnvioRecepcion);
                //Se establece el socket asincrono de EventArg a utilizar en las operaciones con el proveedor
                estadoDelProveedor.saeaDeEnvioRecepcion = saeaDeEnvioRecepcion;
            }
        }

        /// <summary>
        /// Se inicia el servidor de manera que esté escuchando solicitudes de conexión entrantes.
        /// </summary>
        /// <param name="puerto">Puerto de escucha del servidor</param>
        public void IniciarServidor(Int32 puerto)
        {
            //Se inicializa la bandera de que no hay ningún cliente pendiente por desconectar
            desconectando = false;
            //De acuerdo a las buenas practicas de manejo de operaciones asincronas, se debe ANUNCIAR el inicio
            //de un trabajo asincrono para ir controlando su avance por eventos
            estadoDelServidorBase.OnInicio();
            listaClientes = new Dictionary<Guid, T>();
            listaClientesBloqueados = new Dictionary<IPAddress, ClienteBloqueo>();

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
            this.IniciarAceptaciones(null);
            enEjecucion = true;

            // se marca en pantalla que se inicia el servidor            
            //InvokeAppendLog("Se inicia el servidor");
        }



        #region ProcesoDePeticionesCliente

        /// <summary>
        /// Se inicia la operación de aceptar solicitudes por parte de un cliente
        /// </summary>
        /// <param name="acceptEventArg">Objeto que se utilizará en cada aceptación de una solicitud</param>
        private void IniciarAceptaciones(SocketAsyncEventArgs acceptEventArg)
        {
            // de ser null quiere decir que no hay objeto instanciado y debe crearse desde cero
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AceptarConexionCallBack);
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
                bool seHizoAsync = socketDeEscucha.AcceptAsync(acceptEventArg);
                if (!seHizoAsync)
                    // se manda llamar a la función que procesa la solicitud, 
                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                    // en su evento callback
                    AceptarConexionCallBack(socketDeEscucha, acceptEventArg);
            }
            catch (Exception ex)
            {
                InvokeAppendLog(ex.Message + "en iniciarAceptaciones");
                // se hace un último intento para volver a iniciar el servidor por si el error fue una excepción no controlada
                IniciarAceptaciones(acceptEventArg);
            }
        }

        /// <summary>
        /// Procesa la solicitud por medio de socket principal de escucha
        /// </summary>
        /// <param name="sender">Objeto que se tomará como quien dispara el evento principal</param>
        /// <param name="e">SocketAsyncEventArg asociado al proceso asincrono.</param>
        private void AceptarConexionCallBack(object sender, SocketAsyncEventArgs e)
        {
            // se valida que existan errores registrados
            if (e.SocketError != SocketError.Success)
            {
                // se comprueba que no hay un proceso de desconexión en curso
                if (desconectando)
                {
                    InvokeAppendLog("Socket de escucha desconectado, procesarSolicitudDeAceptacionDeConexion");
                    return;
                }

                // se libera el evento manualmente
                semaforoClientesAceptados.Release();

                // aquí el truco en volver a iniciar el proceso de aceptación de solicitudes con el mismo
                // objeto de socket que tiene el cliente si hubo un error para re utilizarlo
                IniciarAceptaciones(e);
                return;
            }

            // como no hay errores declaro un objeto de trabajo al cliente
            T estadoDelCliente = null;
            IPAddress ip = (e.AcceptSocket.RemoteEndPoint as IPEndPoint).Address;
            Int32 puerto = (e.AcceptSocket.RemoteEndPoint as IPEndPoint).Port;

            // Se valida que la ip no esté bloqueada
            if (ClienteEstaBloqueado(ip))
            {
                CerrarConexionForzadaCliente(e.AcceptSocket);
                IniciarAceptaciones(e);
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
                    CerrarConexionForzadaCliente(e.AcceptSocket);
                    IniciarAceptaciones(e);
                    return;
                }
            }

            // Check if this IP has MaxSimultaneousConnections connections
            //if (MaxSimultaneousConnections != 0)
            //{
            //    List<Socket> lst2delete = new List<Socket>();
            //    lock (clientList)
            //    {
            //        if (clientList.ContainsKey(ip))
            //        {
            //            l = clientList[ip];
            //            if (l.Count >= MaxSimultaneousConnections)
            //            {
            //                foreach (var item in l)
            //                    lst2delete.Add(item.Socket);
            //            }
            //        }
            //    }
            //    if (lst2delete.Count > 0)
            //    {
            //        // Close this connection
            //        this.CloseClientSocketConnection(e.AcceptSocket);
            //        // and close the others MaxSimultaneousConnections connections
            //        foreach (Socket sk in lst2delete)
            //        {
            //            try
            //            {
            //                sk.Shutdown(SocketShutdown.Send);
            //            }
            //            catch (Exception) { }
            //            sk.Close();
            //        }

            //        string banmsg = "More than " + MaxSimultaneousConnections.ToString() + " sim. conn. ";
            //        AddToBannedList(ip, banmsg, 0);   // Permanent ban

            //        // Accept the next connection request.
            //        semaphoreConcurrentAccepts.Release();
            //        this.StartAccept(e);
            //        return;
            //    }
            //}


            // si el cliente pasó todas las validaciones entonces se le asigna ya un estado de trabajo del pool de estados de cliente listos con todas sus propiedes
            estadoDelCliente = adminEstadosCliente.obtenerUnElemento();
            estadoDelCliente.IngresarReferenciaSocketPrincipal(this);
            // Del SAEV de aceptación de conexión, se recupera el socket para asignarlo al estado del cliente obtenido del pool de estados
            estadoDelCliente.socketDeTrabajo = e.AcceptSocket;
            //  de la misma forma se ingresa la ip y puerto del cliente que se aceptó
            estadoDelCliente.ipCliente = ip.ToString();
            estadoDelCliente.puertoCliente = puerto;

            // Se llama al método de OnAceptacion ya del socket de trabajo para indicar la continuación del flujo
            // con estas acciones puedo controlar las acciones en cada fase del proceso de recepción y envío de ser necesario
            estadoDelServidorBase.OnAceptacion(estadoDelCliente);

            //TODO creo que está de más
            ip = IPAddress.Parse(estadoDelCliente.ipCliente);
            // se ingresa el cliente a la lista de clientes
            // Monitor proporciona un mecanismo que sincroniza el acceso a datos entre hilos
            bool seSincronzo = Monitor.TryEnter(listaClientes, 5000);
            if (seSincronzo)
            {
                try
                {
                    if (!listaClientes.ContainsKey(estadoDelCliente.idUnicoCliente))
                        listaClientes.Add(estadoDelCliente.idUnicoCliente, estadoDelCliente);
                }
                finally
                {
                    Monitor.Exit(listaClientes);
                }
            }
            else
            {
                // si no puedo ingresarlo en la lista de clientes debo rechazarlo
                CerrarSocketCliente(estadoDelCliente);

                InvokeAppendLog("Timeout de 5 seg para obtener bloqueo en procesarSolicitudDeAceptacionDeConexion, listaClientes, add");
                // coloco nuevamente el socket en proceso de aceptación
                this.IniciarAceptaciones(e);
                return;
            }

            // se comprueba que exista un mensaje de bienvenida, esto unicamente con el fin de indicarle al cliente que se conectó
            if (estadoDelServidorBase.mensajeBienvenida(estadoDelCliente) != "")
            {
                // se envía el mensaje de bienvenida por medio del socket de trabajo
                // TODO: debo hacer dinámico el indicador de fin de mensaje, ahora está "\r\n"
                string mensajeBienvenida = estadoDelServidorBase.mensajeBienvenida(estadoDelCliente) + "\r\n";
                int numeroBytes = Encoding.ASCII.GetBytes(mensajeBienvenida, 0, mensajeBienvenida.Length, estadoDelCliente.saeaDeRecepcion.Buffer, estadoDelCliente.saeaDeRecepcion.Offset);
                // ya cuento con el mensaje, se solicita un pedazo del buffer de trabajo para su recepción
                // TODO: verificar si el de recepción es el correcto
                estadoDelCliente.saeaDeRecepcion.SetBuffer(estadoDelCliente.saeaDeRecepcion.Offset, numeroBytes);

                estadoDelCliente.ultimoMensajeAlCliente = estadoDelServidorBase.mensajeBienvenida(estadoDelCliente);
                // para dejarlo como proceso, se maneja el evento manual pero forzado
                bool seHizoAsync = true;
                try
                {
                    // se envía el mensaje al cliente, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                    seHizoAsync = estadoDelCliente.socketDeTrabajo.SendAsync(estadoDelCliente.saeaDeRecepcion);
                    if (!seHizoAsync)
                        // se llama a la función que completa el flujo de envío cuando la respuesta es false,
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        RecepcionEnvioEntranteCallBack(estadoDelCliente.socketDeTrabajo, estadoDelCliente.saeaDeRecepcion);
                }
                catch (Exception ex)
                {
                    InvokeAppendLog(ex.Message + ", se perdió la conexión al enviar el mensaje de bienvenida, procesarSolicitudDeAceptacionDeConexion");
                    CerrarSocketCliente(estadoDelCliente);
                }
            }
            else
            {
                // se inicia la recepción de datos del cliente si es que sigue conectado dicho cliente  
                if (estadoDelCliente.socketDeTrabajo.Connected)
                {
                    // solo por si se requiere dejar en modo de espera al servidor, se manejan estados de operación
                    if (estadoDelCliente.NextOperation == NextOperationModes.Normal)
                    {
                        // para dejarlo como proceso, se maneja el evento manual pero forzado
                        bool seHizoAsync = true;
                        try
                        {
                            // se solicita un pedazo de buffer para la recepción del mensaje                            
                            estadoDelCliente.saeaDeRecepcion.SetBuffer(estadoDelCliente.saeaDeRecepcion.Offset, tamanoBuffer);
                            // se procede a la recepción asincrona del mensaje,el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                            // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                            seHizoAsync = estadoDelCliente.socketDeTrabajo.ReceiveAsync(estadoDelCliente.saeaDeRecepcion);
                            if (!seHizoAsync)
                                // se llama a la función que completa el flujo de envío, 
                                // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                                // en su evento callback
                                RecepcionEnvioEntranteCallBack(estadoDelCliente.socketDeTrabajo, estadoDelCliente.saeaDeRecepcion);
                        }
                        catch (Exception ex)
                        {
                            InvokeAppendLog(ex.Message + "ProcesarSolicituDeAceptacionDeConexion, recibiendo mensaje del cliente");
                            CerrarSocketCliente(estadoDelCliente);
                        }
                    }
                    else
                    {
                        // TODO: modo ocioso, no se envía ni se recibe info, esto es para ahorro de recursos pero falta definir
                    }
                }
            }

            // se indica que puede aceptar más solicitudes
            this.IniciarAceptaciones(e);
        }

        /// <summary>
        /// Operación de callBack que se llama cuando se envía o se recibe de un socket de asincrono para completar la operación
        /// </summary>
        /// <param name="sender">Objeto principal para devolver la llamada</param>
        /// <param name="e">SocketAsyncEventArg asociado a la operación de envío o recepción</param>
        /// </summary>
        private void RecepcionEnvioEntranteCallBack(object sender, SocketAsyncEventArgs e)
        {
            // obtengo el estado del socket
            T estadoDelCliente = e.UserToken as T;
            // se comprueba que el estado haya sido obtenido correctamente
            if (estadoDelCliente == null)
            {
                InvokeAppendLog("estadoDelCliente recibido es inválido para la operacion");
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
                        this.ProcesarRecepcion(estadoDelCliente);
                    }
                    else
                    {
                        // se marca para la bitácora el error dentro del estado
                        estadoDelCliente.ultimoErrorConexionCliente = e.SocketError.ToString();
                        // se cierra todo el socket de trabajo
                        CerrarSocketCliente(estadoDelCliente);
                    }
                    break;
                case SocketAsyncOperation.Send:
                    // si se está enviando, entonces el argumento es un SAEA de envío, de lo contrario es de recepción ciclico
                    if (e == estadoDelCliente.saeaDeEnvio)
                    {
                        // si es de envío se coloca el evento manual indicando que se estará enviando algo al cliente
                        estadoDelCliente.esperandoEnvio.Set();
                        // se comprueba que no hay errores con el socket
                        if (e.SocketError == SocketError.Success)
                        {
                            // se procesa el envío
                            this.ProcesarEnvio(estadoDelCliente);
                        }
                        else
                        {
                            InvokeAppendLog("Error en el envío, onIOCompleted" + e.SocketError.ToString());
                            CerrarSocketCliente(estadoDelCliente);
                        }
                    }
                    else if (e == estadoDelCliente.saeaDeRecepcion)
                    {
                        // se comprueba que no hay errores con el socket
                        if (e.SocketError == SocketError.Success)
                        {
                            // si el SAEA es de recepción en un flujo de envío, quiere decir que hay comunicación ciclica
                            this.ProcesarRecepcionEnvioCiclicoCliente(estadoDelCliente);
                        }
                        else
                        {
                            estadoDelCliente.ultimoErrorConexionCliente = e.SocketError.ToString();
                            CerrarSocketCliente(estadoDelCliente);
                        }
                    }
                    break;
                default:
                    InvokeAppendLog("La ultima operación no se detecto como de recepcion o envío, OnIOCompleted, " + e.LastOperation.ToString());
                    CerrarSocketCliente(estadoDelCliente);
                    break;
            }
        }

        /// <summary>
        /// Este método se invoca cuando la operación de recepción asincrona se completa y si el cliente
        /// cierra la conexión el socket también se cierra y se libera
        /// </summary>
        /// <param name="estadoDelCliente">Objeto que tiene la información y socket de trabajo del cliente</param>
        private void ProcesarRecepcion(T estadoDelCliente)
        {
            // se obtiene el SAEA de recepción
            SocketAsyncEventArgs saeaRecepcion = estadoDelCliente.saeaDeRecepcion;
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
                // aquí se debe realizar lo necesario con la trama entrante para preparar la trama al proveedor
                estadoDelCliente.ProcesamientoTramaEntrante(mensajeRecibido);

                if (estadoDelCliente.tramaProveedor!=string.Empty)
                {                
                //Se prepara el estado del proveedor que servirá como operador de envío y recepción de trama
                SocketAsyncEventArgs saeaProveedor = new SocketAsyncEventArgs();
                saeaProveedor.Completed += new EventHandler<SocketAsyncEventArgs>(ConexionProveedorCallBack);


                IPAddress iPAddress = IPAddress.Parse("192.168.23.4");
                IPEndPoint endPointProcesa = new IPEndPoint(iPAddress, int.Parse("8002"));

                // se genera un socket que será usado en el envío y recepción
                Socket socketDeTrabajo = new Socket(endPointProcesa.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                bool seHizoSync = true;                                
                seHizoSync = socketDeTrabajo.ConnectAsync(saeaProveedor);
                if (!seHizoSync)
                    // se llama a la función que completa el flujo de envío, 
                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                    // en su evento callback                    
                    ConexionProveedorCallBack(socketDeTrabajo, saeaProveedor);

                    //TODO para este punto ya se procesó la trama del cliente y se envió a procesa. Falta guardar en base de datos
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                this.CerrarSocketCliente(estadoDelCliente);
                InvokeAppendLog(ex.Message + ", error llenado el buffer interno");
                return;
            }

            //IMPORTANTE: LO QUE SE REALIZÓ EN estadoDelCliente.ProcesamientoTramaEntrante SE DEJÓ EN LA VARIABLE secuenciaDeRespuestasAlCliente LLENA
            //SI CONTIENE ALGO, SERÁ LA RESPUESTA AL CLIENTE. ENTONCES ESA VARIABLE DEBE SER LO ÚLTIMO QUE SE HACE DESPUÉS DE ENVIARLA Y TENER RESPUESTA DE PROCESA
            // si hay un error de parseo de la trama se desconecta al cliente inmediatamente
            if (estadoDelCliente.errorParseando)
            {
                CerrarSocketCliente(estadoDelCliente);
                return;
            }
            else
            {
                // Si ya se cuenta con una respuesta(s) para el cliente
                if (estadoDelCliente.secuenciaDeRespuestasAlCliente != "")
                {
                    // se obtiene el mensaje de respuesta que se enviará cliente
                    string mensajeRespuesta = estadoDelCliente.secuenciaDeRespuestasAlCliente;
                    // se obtiene la cantidad de bytes de la trama completa
                    int numeroDeBytes = Encoding.ASCII.GetBytes(mensajeRespuesta, 0, mensajeRespuesta.Length, estadoDelCliente.saeaDeRecepcion.Buffer, estadoDelCliente.saeaDeRecepcion.Offset);
                    // si el número de bytes es mayor al buffer que se tiene destinado a la recepción, no se puede proceder, no es válido el mensaje
                    if (numeroDeBytes > tamanoBuffer)
                    {
                        InvokeAppendLog("La respuesta es más grande que el buffer");
                        CerrarSocketCliente(estadoDelCliente);
                        return;
                    }
                    // Se solicita el espacio de buffer para los bytes que se van a enviar                    
                    estadoDelCliente.saeaDeRecepcion.SetBuffer(estadoDelCliente.saeaDeRecepcion.Offset, numeroDeBytes);

                    try
                    {
                        // se envía asincronamente por medio del socket copia de recepción que es
                        // con el que se está trabajando en esta operación, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                        // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                        bool seHizoAsync = estadoDelCliente.socketDeTrabajo.SendAsync(saeaRecepcion);
                        if (!seHizoAsync)
                            // Si se tiene una respuesta False de que el proceso está pendiente, se completa el flujo,
                            // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                            // en su evento callback
                            RecepcionEnvioEntranteCallBack(estadoDelCliente.socketDeTrabajo, saeaRecepcion);
                    }
                    catch (Exception)
                    {
                        CerrarSocketCliente(estadoDelCliente);
                        return;
                    }
                }
                else  // Si el proceso no tuvo una respuesta se procede con el siguiente bloque de transmisión en la recepción
                {
                    if (estadoDelCliente.socketDeTrabajo.Connected)
                    {
                        if (estadoDelCliente.NextOperation == NextOperationModes.Normal)
                        {
                            try
                            {
                                // se solicita el espacio de buffer para la recepción del mensaje
                                saeaRecepcion.SetBuffer(saeaRecepcion.Offset, tamanoBuffer);
                                // se solicita un proceso de recepción asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                                // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                                bool seHizoAsync = estadoDelCliente.socketDeTrabajo.ReceiveAsync(saeaRecepcion);
                                if (!seHizoAsync)
                                    // si el evento indica que el proceso asincrono está pendiente, se completa el flujo,
                                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                                    // en su evento callback
                                    RecepcionEnvioEntranteCallBack(estadoDelCliente.socketDeTrabajo, saeaRecepcion);
                            }
                            catch (Exception ex)
                            {
                                InvokeAppendLog(ex.Message + ", procesarRecepcion, Desconectando cliente " + estadoDelCliente.idUnicoCliente);
                                CerrarSocketCliente(estadoDelCliente);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Función callback que se utiliza cuando en un proceso ciclico de envio y recepción
        /// </summary>
        /// <param name="estadoDelCliente">Objeto con la información y socket de trabajo de cliente</param>
        private void ProcesarRecepcionEnvioCiclicoCliente(T estadoDelCliente)
        {
            estadoDelCliente.fechaHoraUltimoMensajeAlCliente = DateTime.Now;

            // Una vez terminado el envio, se continua escuchando por el Socket de trabajo
            try
            {
                // se asigna el buffer para continuar el envío
                estadoDelCliente.saeaDeRecepcion.SetBuffer(estadoDelCliente.saeaDeRecepcion.Offset, tamanoBuffer);
                // se inicia el proceso de recepción asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                bool seHizoAsync = estadoDelCliente.socketDeTrabajo.ReceiveAsync(estadoDelCliente.saeaDeRecepcion);
                if (!seHizoAsync)
                    // si el evento indica que el proceso está pendiente, se completa el flujo,
                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                    // en su evento callback
                    RecepcionEnvioEntranteCallBack(estadoDelCliente.socketDeTrabajo, estadoDelCliente.saeaDeRecepcion);
            }
            catch (Exception)
            {
                CerrarSocketCliente(estadoDelCliente);
            }
        }

        /// <summary>
        /// Este metodo se invoca cuando una operación de envío asincrono se completa        
        /// </summary>
        /// <param name="estadoDelCliente">Objeto con la información y el socket de trabajo sobre el envío</param>
        private void ProcesarEnvio(T estadoDelCliente)
        {
            try
            {
                // Se actualiza la fecha y hora del ultimo envío para controlar por si requerimos bitácora
                estadoDelCliente.fechaHoraUltimoMensajeAlCliente= DateTime.Now;

                // se envía un elemento al cliente, revisando que está conectado
                if (estadoDelCliente.socketDeTrabajo.Connected)
                {
                    // se genera un bloqueo con el objeto que destinamos para ello
                    lock (estadoDelCliente.sincronizarEnvio)
                    {
                        // se coloca la bandera de que no se está enviando algo, esto lo cambiaré cuando procese el envío asincrono
                        estadoDelCliente.seEstaEnviandoAlgo = false;
                    }
                    // se coloca el objeto de trabajo en la cola de envío para procesarlo en su momento
                    ColaDeEnvios(estadoDelCliente);
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
        /// <param name="estadoDelCLiente">Objeto con la información y el socket de trabajo sobre el envío</param>
        private void ColaDeEnvios(T estadoDelCLiente)
        {
            // genero dos matrices, una para ingresar el mensaje
            byte[] bytes;
            // y otra como buffer para su envío y así reutilizar de la pila de buffers
            byte[] bufferLocal = new byte[tamanoBuffer];

            int contadorBytes = 0;
            int numMensajesEnvio = 0;


            if (!estadoDelCLiente.socketDeTrabajo.Connected)
            {
                InvokeAppendLog("Socket desconectado, colaDeProcesamiento");
                CerrarSocketCliente(estadoDelCLiente);
                return;
            }

            // se sincroniza el envío con el objeto que se destinó para ello en cada objeto de estado
            lock (estadoDelCLiente.sincronizarEnvio)
            {
                // si ya se está enviando algo no se enviar al menos en este momento
                if (estadoDelCLiente.seEstaEnviandoAlgo) return;

                string mensajeAEnviar = "";
                // también hago un bloqueo sobre la cola de envío mientras realizo una extracción y envío para que no exista choque de operaciones
                lock (estadoDelCLiente.colaEnvio)
                {
                    // si hay en cola mensajes a enviar, se procede
                    if (estadoDelCLiente.colaEnvio.Count > 0)
                    {
                        do
                        {
                            // se obtiene el primer elemento de la cola
                            mensajeAEnviar = estadoDelCLiente.colaEnvio.First();
                            // se revisa que el mensaje quepa en el buffer
                            if (mensajeAEnviar.Length > tamanoBuffer)
                                throw new Exception("Mensaje demasiado grande para el buffer");

                            // si existen varios mensajes, el acumulado se tiene que estar validando si cabe en el buffer
                            if (contadorBytes + mensajeAEnviar.Length < tamanoBuffer)
                            {
                                // obtiene el objeto al principio de la cola y luego lo elimina
                                estadoDelCLiente.colaEnvio.Dequeue();
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
                                numMensajesEnvio++;
                            }
                            else
                            {
                                break;
                            }
                        } while (estadoDelCLiente.colaEnvio.Count > 0);
                    }
                }

                // siempre y cuando exista por lo menos un mensaje a enviar se procede con este bloque
                if (numMensajesEnvio > 0)
                {
                    try
                    {
                        // se copia al buffer de envío el mensaje codificado
                        Buffer.BlockCopy(bufferLocal, 0, estadoDelCLiente.saeaDeEnvio.Buffer, estadoDelCLiente.saeaDeEnvio.Offset, contadorBytes);
                    }
                    catch (Exception ex)
                    {
                        InvokeAppendLog(ex.Message + " segundo bloque de copiado, colaDeProcesamiento");
                    }

                    // se establece que parte del buffer se utilizará en este envío
                    estadoDelCLiente.saeaDeEnvio.SetBuffer(estadoDelCLiente.saeaDeEnvio.Offset, contadorBytes);
                    // se indica que se está enviando
                    estadoDelCLiente.seEstaEnviandoAlgo = true;
                    // se indica que el evento manual se reinicia a no señalado para bloquear el subproceso
                    estadoDelCLiente.esperandoEnvio.Reset();
                    try
                    {
                        // Se realiza el proceso de envío asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                        // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                        bool seHizoAsync = estadoDelCLiente.socketDeTrabajo.SendAsync(estadoDelCLiente.saeaDeEnvio);
                        if (!seHizoAsync)
                            // si la operación asincrona está pendiente, se completa el flujo,
                            // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                            // en su evento callback
                            RecepcionEnvioEntranteCallBack(estadoDelCLiente.socketDeTrabajo, estadoDelCLiente.saeaDeEnvio);
                    }
                    catch (Exception ex)
                    {
                        InvokeAppendLog(ex.Message + "Desconectado al cliente " + estadoDelCLiente.idUnicoCliente + ", colaDeProcesamiento");
                        //InvokeAppendLog("Disconnecting client (ProcessQueue) " + infoYSocketDeTrabajo.user_Guid + "\r\nConn: " + infoYSocketDeTrabajo.fechaHoraConexionCliente + ", Offset: " +
                        //    infoYSocketDeTrabajo.saeaDeEnvio.Offset.ToString() + " Sent: [" + infoYSocketDeTrabajo.ultimoMensajeAlCliente + "], Recv: [" + infoYSocketDeTrabajo.ultimoMensajeRecibidoCliente + "]\r\nError: " + ex.Message);
                        // se marca el evento manual en señalado para que otros proceso puedan continuar
                        estadoDelCLiente.esperandoEnvio.Set();
                        // se cierra la conexión
                        CerrarSocketCliente(estadoDelCLiente);
                    }
                }
                else
                {
                    return;
                }
            }
        }

        #endregion

        #region ProcesoDePeticionesProveedor

        private void ConexionProveedorCallBack(object sender, SocketAsyncEventArgs e)
        {
            // se valida que existan errores registrados
            if (e.SocketError != SocketError.Success)
            {                
                return;
            }

            // obtengo un estadoDelProveedor del pool que ya se tiene
            X estadoDelProvedor = adminEstadosDeProveedor.obtenerUnElemento();
            estadoDelProvedor.IngresarReferenciaSocketPrincipal(this);   
            // se le indica al estado del proveedor el socket de trabajo
            estadoDelProvedor.socketDeTrabajo = e.AcceptSocket;

            if (estadoDelProvedor.socketDeTrabajo.Connected)
            {
                // solo por si se requiere dejar en modo de espera al servidor, se manejan estados de operación
                if (estadoDelProvedor.NextOperation == NextOperationModes.Normal)
                {
                    // para dejarlo como proceso, se maneja el evento manual pero forzado
                    bool seHizoAsync = true;
                    try
                    {
                        // Se prepara el buffer del SAEA con el tamaño predefinido                         
                        estadoDelProvedor.saeaDeEnvioRecepcion.SetBuffer(estadoDelProvedor.saeaDeEnvioRecepcion.Offset, tamanoBuffer);
                        // se procede a la recepción asincrona del mensaje,el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                        // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                        seHizoAsync = estadoDelProvedor.socketDeTrabajo.SendAsync(estadoDelProvedor.saeaDeEnvioRecepcion);
                        if (!seHizoAsync)
                            // se llama a la función que completa el flujo de envío, 
                            // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                            // en su evento callback
                            RecepcionEnvioSalienteCallBack(estadoDelProvedor.socketDeTrabajo, estadoDelProvedor.saeaDeEnvioRecepcion);
                    }
                    catch (Exception ex)
                    {
                        InvokeAppendLog(ex.Message + "ProcesarSolicitudDeConexionProveedor, iniciando conexión");
                        CerrarConexionForzadaProveedor(estadoDelProvedor.socketDeTrabajo);
                    }
                }
                else
                {
                    // TODO: modo ocioso, no se envía ni se recibe info, esto es para ahorro de recursos pero falta definir
                }
            }
        }


        private void RecepcionEnvioSalienteCallBack(object sender, SocketAsyncEventArgs e)
        {
            // obtengo el estado del proveedor
            X estadoDelProveedor = e.UserToken as X;
            // se comprueba que el estado haya sido obtenido correctamente
            if (estadoDelProveedor == null)
            {
                InvokeAppendLog("estadoDelProveedor recibido es inválido para la operacion");
                return;
            }

            // se determina que operación se está llevando a cabo para indicar que manejador de eventos se ejecuta
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Send:                    
                    // se comprueba que no hay errores con el socket
                    if (e.SocketError == SocketError.Success)
                    {
                        // se procesa el envío
                        this.ProcesarEnvio(estadoDelProveedor);
                    }
                    else
                    {
                        InvokeAppendLog("Error en el envío, onIOCompleted" + e.SocketError.ToString());
                        CerrarConexionForzadaProveedor(estadoDelProveedor.socketDeTrabajo);
                    }
                    break;
                case SocketAsyncOperation.Receive:
                    // se comprueba que exista información
                    if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                    {
                        // se procesa la solicitud
                        this.ProcesarRecepcion(estadoDelProveedor);
                    }
                    else
                    {
                        // se marca para la bitácora el error dentro del estado
                        estadoDelProveedor.ultimoErrorConexion = e.SocketError.ToString();
                        // se cierra todo el socket de trabajo
                        CerrarConexionForzadaProveedor(estadoDelProveedor.socketDeTrabajo);
                    }
                    break;
                default:
                    InvokeAppendLog("La ultima operación no se detecto como de recepcion o envío, OnIOCompleted, " + e.LastOperation.ToString());
                    CerrarConexionForzadaProveedor(estadoDelProveedor.socketDeTrabajo);
                    break;
            }
        }


        /// <summary>
        /// Este metodo se invoca cuando una operación de envío asincrono se completa        
        /// </summary>
        /// <param name="estadoDelProveedor">Objeto con la información y el socket de trabajo sobre el envío</param>
        private void ProcesarEnvio(X estadoDelProveedor)
        {
            try
            {
                // Se actualiza la fecha y hora del ultimo envío para controlar por si requerimos bitácora
                estadoDelProveedor.fechaHoraUltimoMensajeAlProveedor = DateTime.Now;

                // se envía un elemento al cliente, revisando que está conectado
                if (estadoDelProveedor.socketDeTrabajo.Connected)
                {
                    // se genera un bloqueo con el objeto que destinamos para ello
                    lock (estadoDelProveedor.sincronizarEnvio)
                    {
                        // se coloca la bandera de que no se está enviando algo, esto lo cambiaré cuando procese el envío asincrono
                        estadoDelProveedor.seEstaEnviandoAlgo = false;
                    }
                    // se coloca el objeto de trabajo en la cola de envío para procesarlo en su momento
                    ColaDeEnvios(estadoDelProveedor);
                }
            }
            catch (Exception ex)
            {
                InvokeAppendLog(ex.Message + "error fuera de excepcion, puede suceder por muchas cosas, se tratará de continuar operando");
            }
        }

        private void ColaDeEnvios(X estadoDelProveedor)
        {
            // genero dos matrices, una para ingresar el mensaje
            byte[] bytes;
            // y otra como buffer para su envío y así reutilizar de la pila de buffers
            byte[] bufferLocal = new byte[tamanoBuffer];

            int contadorBytes = 0;
            int numMensajesEnvio = 0;


            if (!estadoDelProveedor.socketDeTrabajo.Connected)
            {
                InvokeAppendLog("Socket desconectado, colaDeProcesamiento");
                CerrarConexionForzadaProveedor(estadoDelProveedor.socketDeTrabajo);
                return;
            }

            // se sincroniza el envío con el objeto que se destinó para ello en cada objeto de estado
            lock (estadoDelProveedor.sincronizarEnvio)
            {
                // si ya se está enviando algo no se puede enviar al menos en este momento
                if (estadoDelProveedor.seEstaEnviandoAlgo) return;

                string mensajeAEnviar = "";
                // también hago un bloqueo sobre la cola de envío mientras realizo una extracción y envío para que no exista choque de operaciones
                lock (estadoDelProveedor.colaEnvio)
                {
                    // si hay en cola mensajes a enviar, se procede
                    if (estadoDelProveedor.colaEnvio.Count > 0)
                    {
                        do
                        {
                            // se obtiene el primer elemento de la cola
                            mensajeAEnviar = estadoDelProveedor.colaEnvio.First();
                            // se revisa que el mensaje quepa en el buffer
                            if (mensajeAEnviar.Length > tamanoBuffer)
                                throw new Exception("Mensaje demasiado grande para el buffer");

                            // si existen varios mensajes, el acumulado se tiene que estar validando si cabe en el buffer
                            if (contadorBytes + mensajeAEnviar.Length < tamanoBuffer)
                            {
                                // obtiene el objeto al principio de la cola y luego lo elimina
                                estadoDelProveedor.colaEnvio.Dequeue();
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
                                numMensajesEnvio++;
                            }
                            else
                            {
                                break;
                            }
                        } while (estadoDelProveedor.colaEnvio.Count > 0);
                    }
                }

                // siempre y cuando exista por lo menos un mensaje a enviar se procede con este bloque
                if (numMensajesEnvio > 0)
                {
                    try
                    {
                        // se copia al buffer de envío el mensaje codificado
                        Buffer.BlockCopy(bufferLocal, 0, estadoDelProveedor.saeaDeEnvioRecepcion.Buffer, estadoDelProveedor.saeaDeEnvioRecepcion.Offset, contadorBytes);
                    }
                    catch (Exception ex)
                    {
                        InvokeAppendLog(ex.Message + " segundo bloque de copiado, colaDeProcesamiento");
                    }

                    // se establece que parte del buffer se utilizará en este envío
                    estadoDelProveedor.saeaDeEnvioRecepcion.SetBuffer(estadoDelProveedor.saeaDeEnvioRecepcion.Offset, contadorBytes);
                    // se indica que se está enviando
                    estadoDelProveedor.seEstaEnviandoAlgo = true;
                    
                    try
                    {
                        // Se realiza el proceso de envío asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                        // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                        bool seHizoAsync = estadoDelProveedor.socketDeTrabajo.SendAsync(estadoDelProveedor.saeaDeEnvioRecepcion);
                        if (!seHizoAsync)
                            // si la operación asincrona está pendiente, se completa el flujo,
                            // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                            // en su evento callback
                            RecepcionEnvioEntranteCallBack(estadoDelProveedor.socketDeTrabajo, estadoDelProveedor.saeaDeEnvioRecepcion);
                    }
                    catch (Exception ex)
                    {
                        InvokeAppendLog(ex.Message + "Desconectado al cliente " + estadoDelProveedor.idUnicoCliente + ", colaDeProcesamiento");
                        //InvokeAppendLog("Disconnecting client (ProcessQueue) " + infoYSocketDeTrabajo.user_Guid + "\r\nConn: " + infoYSocketDeTrabajo.fechaHoraConexionCliente + ", Offset: " +
                        //    infoYSocketDeTrabajo.saeaDeEnvio.Offset.ToString() + " Sent: [" + infoYSocketDeTrabajo.ultimoMensajeAlCliente + "], Recv: [" + infoYSocketDeTrabajo.ultimoMensajeRecibidoCliente + "]\r\nError: " + ex.Message);
                        // se marca el evento manual en señalado para que otros proceso puedan continuar
                        estadoDelProveedor.esperandoEnvio.Set();
                        // se cierra la conexión
                        CerrarConexionForzadaProveedor(estadoDelProveedor.socketDeTrabajo);
                    }
                }
                else
                {
                    return;
                }
            }
        }

        private void ProcesarRecepcion(X estadoDelProveedor)
        {
            // se obtiene el SAEA de recepción
            SocketAsyncEventArgs saeaRecepcion = estadoDelProveedor.saeaDeEnvioRecepcion;
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
                //TODO aquí se debe realizar todo con la trama
                estadoDelProveedor.ProcesamientoTramaSaliente(mensajeRecibido);

            }
            catch (Exception ex)
            {
                this.CerrarConexionForzadaProveedor(estadoDelProveedor.socketDeTrabajo);
                InvokeAppendLog(ex.Message + ", error llenado el buffer interno");
                return;
            }

            //IMPORTANTE: LO QUE SE REALIZÓ EN estadoDelCliente.ProcesamientoTramaEntrante SE DEJÓ EN LA VARIABLE secuenciaDeRespuestasAlCliente LLENA
            //SI CONTIENE ALGO, SERÁ LA RESPUESTA AL CLIENTE. ENTONCES ESA VARIABLE DEBE SER LO ÚLTIMO QUE SE HACE DESPUÉS DE ENVIARLA Y TENER RESPUESTA DE PROCESA
            // si hay un error de parseo de la trama se desconecta al cliente inmediatamente
            if (estadoDelProveedor.errorParseando)
            {
                CerrarConexionForzadaProveedor(estadoDelProveedor.socketDeTrabajo);
                return;
            }
            else
            {
                // Si ya se cuenta con una respuesta(s) para el cliente
                if (estadoDelProveedor.secuenciaDeRespuestasDelProveedor != "")
                {
                    // se obtiene el mensaje de respuesta que se enviará cliente
                    string mensajeRespuesta = estadoDelProveedor.secuenciaDeRespuestasDelProveedor;
                    // se obtiene la cantidad de bytes de la trama completa
                    int numeroDeBytes = Encoding.ASCII.GetBytes(mensajeRespuesta, 0, mensajeRespuesta.Length, estadoDelProveedor.saeaDeEnvioRecepcion.Buffer, estadoDelProveedor.saeaDeEnvioRecepcion.Offset);
                    // si el número de bytes es mayor al buffer que se tiene destinado a la recepción, no se puede proceder, no es válido el mensaje
                    if (numeroDeBytes > tamanoBuffer)
                    {
                        InvokeAppendLog("La respuesta es más grande que el buffer");
                        CerrarSocketCliente(estadoDelProveedor);
                        return;
                    }
                    // Se solicita el espacio de buffer para los bytes que se van a enviar                    
                    estadoDelProveedor.saeaDeRecepcion.SetBuffer(estadoDelProveedor.saeaDeRecepcion.Offset, numeroDeBytes);

                    try
                    {
                        // se envía asincronamente por medio del socket copia de recepción que es
                        // con el que se está trabajando en esta operación, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                        // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                        bool seHizoAsync = estadoDelProveedor.socketDeTrabajo.SendAsync(saeaRecepcion);
                        if (!seHizoAsync)
                            // Si se tiene una respuesta False de que el proceso está pendiente, se completa el flujo,
                            // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                            // en su evento callback
                            RecepcionEnvioEntranteCallBack(estadoDelProveedor.socketDeTrabajo, saeaRecepcion);
                    }
                    catch (Exception)
                    {
                        CerrarSocketCliente(estadoDelProveedor);
                        return;
                    }
                }
                else  // Si el proceso aún no tiene una respuesta se procede con el siguiente bloque de transmisión en la recepción
                {
                    if (estadoDelProveedor.socketDeTrabajo.Connected)
                    {
                        if (estadoDelProveedor.NextOperation == NextOperationModes.Normal)
                        {
                            try
                            {
                                // se solicita el espacio de buffer para la recepción del mensaje
                                saeaRecepcion.SetBuffer(saeaRecepcion.Offset, tamanoBuffer);
                                // se solicita un proceso de recepción asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                                // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                                bool seHizoAsync = estadoDelProveedor.socketDeTrabajo.ReceiveAsync(saeaRecepcion);
                                if (!seHizoAsync)
                                    // si el evento indica que el proceso asincrono está pendiente, se completa el flujo,
                                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                                    // en su evento callback
                                    RecepcionEnvioEntranteCallBack(estadoDelProveedor.socketDeTrabajo, saeaRecepcion);
                            }
                            catch (Exception ex)
                            {
                                InvokeAppendLog(ex.Message + ", procesarRecepcion, Desconectando cliente " + estadoDelProveedor.idUnicoCliente);
                                CerrarSocketCliente(estadoDelProveedor);
                            }
                        }
                    }
                }
            }
        }

        private void ProcesarRecepcionEnvioCiclicoProveedor(X estadoDelProveedor)
        {
            estadoDelProveedor.fechaHoraUltimoMensajeAlProveedor = DateTime.Now;

            CerrarConexionForzadaProveedor(estadoDelProveedor.socketDeTrabajo);
        }

        #endregion


        #region FuncionesDeAyuda

        /// <summary>
        /// Manda el mensaje al cliente sobre el socket de trabajo
        /// </summary>
        /// <param name="mensaje">Mensaje a enviar por el socket</param>
        /// <param name="estadoDelCliente">la información del cliente y su socket de trabajo</param>
        public void EnvioInfo(string mensaje, T estadoDelCliente)
        {
            int longitudColaEnvio;
            if (mensaje == "")
            {
                InvokeAppendLog("Mensaje vacío, no se puede enviar, envioInfo");
                return;
            }
            // si está desconectado se marca como error
            if (!estadoDelCliente.socketDeTrabajo.Connected)
            {
                InvokeAppendLog("Socket desconectado, envioInfo");
                // se cierra el socket para reutilizarlo
                CerrarSocketCliente(estadoDelCliente);
                return;
            }

            // se utiliza un bloqueo sobre la cola de envío al cliente y no
            // choquen procesos de ingreso y obtención
            lock (estadoDelCliente.colaEnvio)
            {
                // se ingresa el mensaje que se enviará al cliente al final de la cola
                estadoDelCliente.colaEnvio.Enqueue(mensaje);
                // se obtiene la longitud de cola de envío
                longitudColaEnvio = estadoDelCliente.colaEnvio.Count();
            }

            // TODO: Este límite hasta ahora funciona, en un cambio se podría mejorar para hacerlo dinámico
            // la cola de mensajes no puede ser mayor a 500, es demasiado pero será nuestro límite
            if (longitudColaEnvio > 499)
            {
                InvokeAppendLog("Cola de mensaje superó el límite de 500 elementos, envioInfo");
                // TODO: revisar porque no se puede obtener el GUID del cliente para el log anterior
                //InvokeAppendLog(socketDeTrabajoInfoCliente.user_Guid + " send queue " + longitudColaEnvio + " exceeded limit");
                // es mejor cerrar el socket porque existe un sobre encolamiento de transacciones
                CerrarSocketCliente(estadoDelCliente);
                return;
            }

            // se llama la función que procesará la cola de envío
            ColaDeEnvios(estadoDelCliente);
        }

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
        public void AgregarListaBloqueados(IPAddress ip, string razon, int segundosBloqueo)
        {
            // se hace un bloqueo sobre la lista para que no exista choque de procesos
            // en el ingreso y obtención
            lock (listaClientesBloqueados)
            {
                if (!listaClientesBloqueados.ContainsKey(ip))
                {
                    listaClientesBloqueados.Add(ip, new ClienteBloqueo(ip, razon, segundosBloqueo, true));
                }
            }
        }

        /// <summary>
        /// Remueve una ip como bloqueada
        /// </summary>
        /// <param name="ip">Ip a remover de la lista</param>
        public void RemoverListaBloqueados(IPAddress ip)
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
        public bool ClienteEstaBloqueado(IPAddress ip)
        {
            return listaClientesBloqueados.ContainsKey(ip);
        }

        /// <summary>
        /// Se detiene el servidor
        /// </summary>
        public void DetenerServidor()
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
                CerrarSocketCliente(socketDeTrabajoPorCliente);
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
        public void CerrarSocketCliente(T estadoDelCliente)
        {
            // Se comprueba que la información del socket de trabajo sea null, ya que podría ser invocado como resultado 
            // de una operación de E / S sin valores
            if (estadoDelCliente == null) return;

            // proporciona un mecanismo de sincronización de acceso a datos donde un hilo solo puede tener acceso a un
            // bloque de código a la vez, en este caso en ingresar al listado de clientes, de lo contrario habría 
            // cross threading y provocaría error
            bool gotLock = Monitor.TryEnter(listaClientes, 5000);
            if (gotLock)
            {
                try
                {
                    // se busca en la lista el cliente y se remueve porque se va a desconectar
                    if (listaClientes.ContainsKey(estadoDelCliente.idUnicoCliente))
                        listaClientes.Remove(estadoDelCliente.idUnicoCliente);
                    else
                        return;     // quiere decir que ya está desconectado
                }
                catch (Exception ex)
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
            lock (estadoDelCliente.colaEnvio)
            {
                estadoDelCliente.colaEnvio.Clear();
            }

            // se obtiene el socket específico del cliente en cuestión
            Socket socketDeTrabajoACerrar = estadoDelCliente.socketDeTrabajo;

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

            string tmpIP = estadoDelCliente.ipCliente;
            Guid tmpGuid = estadoDelCliente.idUnicoCliente;

            // se llama a la secuencia de cerrando para tener un flujo de eventos
            estadoDelServidorBase.OnClienteCerrado(estadoDelCliente);

            // se espera a que un cliente acaba de recibir la última información y se cierra
            int tiempoInicial, tiempoFinal;
            tiempoInicial = Environment.TickCount;
            bool receivedSignal = estadoDelCliente.esperandoEnvio.WaitOne(10000, false);
            tiempoFinal = Environment.TickCount;
            // la señal debe ser false si excedió el TO
            if (!receivedSignal)
            {
                // se mide el tiempo de espera para el envío de la información y si supera el establecido es un error
                if (tiempoFinal - tiempoInicial > maxRetrasoParaEnvio)
                {
                    maxRetrasoParaEnvio = tiempoFinal - tiempoInicial;
                    InvokeAppendLog("maximo tiempo de espera para el envío de información: " + maxRetrasoParaEnvio + " ms, CloseClientSocket");
                }
                InvokeAppendLog("No se puede esperar para terminar el envío de información al cliente, CloseClientSocket");
                // como no puedo esperar tanto al envío se deja pendiente, esto para no perder ninguna transacción
                listaClientesPendientesDesconexion.Add(estadoDelCliente);
                return;
            }

            // se libera la instancia de socket de trabajo para reutilizarlo
            adminEstadosCliente.ingresarUnElemento(estadoDelCliente);
            // se marca el semáforo de que puede aceptar otro cliente
            this.semaforoClientesAceptados.Release();
        }
                

        /// <summary>
        /// Cierra el socket asociado al cliente pero este método no retira de la lista de clientes conectados al cliente actual
        /// </summary>
        /// <param name="socketCliente">The socket to close</param>
        private void CerrarConexionForzadaCliente(Socket socketCliente)
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

        private void CerrarConexionForzadaProveedor(Socket socketProveedor)
        {
            try
            {
                socketProveedor.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                InvokeAppendLog(ex.Message + ", CloseClientSocketConnection, shutdown");
            }
            socketProveedor.Close();
        }

        /// <summary>
        /// Auxiliar logging function
        /// </summary>
        /// <param name="msg">Log message</param>
        private void InvokeAppendLog(string msg)
        {
            //Log.WriteLog(ELogLevel.DEBUG, "[" + this.nombreServidor + "] " + msg);
            //cLogErrores.sNombreLog = "Graf_Serv";
            //cLogErrores.sNombreOrigen = cLogErrores.sNombreLog;
            //cLogErrores.Crear_Log();

            //cLogErrores.Escribir_Log_Error(msg);

        }

        #endregion


    }
}
