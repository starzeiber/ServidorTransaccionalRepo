using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static ServerCore.Utileria;

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
    public class ServidorTransaccional<T, S, X>
        where T : EstadoDelClienteBase, new()
        where S : EstadoDelServidorBase, new()
        where X : EstadoDelProveedorBase, new()
    {
        /// <summary>
        /// Instancia del performance counter de peticiones entrantes
        /// </summary>
        public PerformanceCounter peformanceConexionesEntrantes;

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
        public int numeroMaximoConexionesPorIpCliente { get; set; }

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
        /// Obtiene o ingresa a la lista de proveedores pendientes de desconexión, esta lista es para la verificación de que todos los proveedores
        /// se desconectan adecuadamente, su uso es más para debug pero queda para mejorar
        /// </summary>
        public List<X> listaProveedoresPendientesDesconexion { get; set; }

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

        /// <summary>
        /// Ip a la cual se apuntarán todas las transacciones del proveedor
        /// </summary>
        public string ipProveedor { get; set; }

        /// <summary>
        /// Puerto del proveedor para la conexión
        /// </summary>
        public int puertoProveedor { get; set; }

        /// <summary>
        /// Bytes que se han transmitido desde el inicio de la aplicación
        /// </summary>
        public int totalDeBytesTransferidos
        {
            get
            {
                return totalBytesLeidos;
            }
        }

        /// <summary>
        /// IP de escucha
        /// </summary>
        public string ipLocal;

        private const string PROGRAM = "UServer";

        private readonly DateTime localValidity;

        private string processorId = "";
        private string product = "";
        private string manufacturer = "";

        private string licence="";

        private const string NOTLICENCE = "No cuenta con licencia válida";

        #endregion

        #region Propiedades privadas

        /// <summary>
        /// Número de conexiones simultaneas que podrá manejar el servidor por defecto
        /// </summary>
        private readonly int numeroConexionesSimultaneas;

        /// <summary>
        /// Número sockest para lectura y escritura sin asignación de espacio del buffer para aceptar peticiones como default
        /// esto para tener siempre por lo menos sockects disponibles al inicio del servidor
        /// </summary>
        private const int operacionesPreDisponibles = 3;

        /// <summary>
        /// instancia al administrador de estados de socket de trabajo
        /// </summary>
        private readonly AdminEstadosDeCliente<T> adminEstadosCliente;

        /// <summary>
        /// Instancia del administrador de estados del proveedor
        /// </summary>
        private readonly AdminEstadosDeProveedor<X> adminEstadosDeProveedor;

        /// <summary>
        /// semáforo sobre las peticiones de clientes para controlar el número total que podrá soportar el servidor
        /// </summary>
        private readonly SemaphoreSlim semaforoParaAceptarClientes;

        /// <summary>
        /// semáforo sobre las peticiones a proveedores para controlar el número total que podrá soportar el servidor
        /// </summary>
        private readonly SemaphoreSlim semaforoParaAceptarProveedores;

        /// <summary>
        /// Número total de bytes recibido en el servidor, para uso estadístico
        /// </summary>
        private int totalBytesLeidos;

        /// <summary>
        /// Representa un conjunto enorme de buffer reutilizables entre todos los sockects de trabajo
        /// </summary>
        private readonly AdminBuffer administradorBuffer;

        /// <summary>
        /// Socket de escucha para las conexiones de clientes
        /// </summary>
        private Socket socketDeEscucha;

        /// <summary>
        /// Bandera para identificar que la conexión está bien establecida
        /// </summary>
        private bool desconectado = false;

        /// <summary>
        /// Parámetros que  indica el máximo de pedidos que pueden encolarse simultáneamente en caso que el servidor 
        /// esté ocupado atendiendo una nueva conexión.
        /// </summary>
        private readonly int backLog;

        /// <summary>
        /// Tamaño del buffer por petición
        /// </summary>
        private readonly int tamanoBufferPorPeticion;

        /// <summary>
        /// Retraso en el envío, es para uso en Debug
        /// </summary>
        internal static int maxRetrasoParaEnvio = 0;

        private enum Licence
        {
            Program = 0,
            Validity = 2,
            ProcessorId = 4,
            Product = 6,
            Manufacturer = 8
        }

        #endregion

        /// <summary>
        /// Crea una instancia del administrador de sockets, posterior se tiene que llamar al método
        /// ConfigInicioServidor para iniciar el proceso de asignacion de recursos        
        /// </summary>
        /// <param name="numeroConexSimultaneas">Maximo número de conexiones simultaneas a manejar en el servidor</param>
        /// <param name="tamanoBuffer">Tamaño del buffer por conexión, un parámetro standart es 1024</param>
        /// <param name="backlog">Parámetro TCP/IP backlog, el recomendable es 100</param>
        public ServidorTransaccional(Int32 numeroConexSimultaneas, Int32 tamanoBuffer = 1024, int backlog = 100)
        {
            totalBytesLeidos = 0;
            this.numeroConexionesSimultaneas = numeroConexSimultaneas;
            //Se coloca ilimitado para fines no restrictivos
            numeroMaximoConexionesPorIpCliente = 0;
            this.backLog = backlog;
            listaClientes = new Dictionary<Guid, T>();
            //listaClientesBloqueados = new Dictionary<IPAddress, ClienteBloqueo>();
            //listaClientesPermitidos = new List<Regex>();
            listaClientesPendientesDesconexion = new List<T>();
            this.tamanoBufferPorPeticion = tamanoBuffer;

            localValidity = DateTime.Now;

            try
            {
                estadoDelServidorBase = new S();
            }
            catch (Exception ex)
            {
                EscribirLog(ex.Message + "ServidorTransaccional", tipoLog.ERROR);
            }

            // establezco el proceso principal para referencia futura
            estadoDelServidorBase.procesoPrincipal = this;
            // indico que aún no está en funcionamiento, faltan parámetros
            enEjecucion = false;

            //Asignación de un buffer tomando en cuenta por lo menos los 3 sockets por defecto para lectura y escritura iniciales
            // es decir, el tamaño del buffer por operación por el número de conexiónes por el número de sockets iniciales no dará
            // el valor de buffer enorme en bytes, por ejemplo: tamanoBuffer= 1024 * 1000 * 3 =2048000 bytes
            this.administradorBuffer = new AdminBuffer(tamanoBuffer * numeroConexionesSimultaneas * operacionesPreDisponibles, tamanoBufferPorPeticion);

            //se inicializan los estados de socket necesarios para el número simultaneo de conexiones, para tenerlos listos a usarse como una pila            
            adminEstadosCliente = new AdminEstadosDeCliente<T>(numeroConexionesSimultaneas);

            //se inicializan los estados de socket necesarios para el número simultaneo de conexiones hacia el proveedor, para tenerlos listos a usarse como una pila            
            adminEstadosDeProveedor = new AdminEstadosDeProveedor<X>(numeroConexionesSimultaneas);

            //Se inicializa el número inicial y maximo de conexiones simultaneas soportadas, será el semáforo quien indique que hay saturación.            
            this.semaforoParaAceptarClientes = new SemaphoreSlim(numeroConexionesSimultaneas, numeroConexionesSimultaneas);
            this.semaforoParaAceptarProveedores = new SemaphoreSlim(numeroConexionesSimultaneas, numeroConexionesSimultaneas);
        }

        /// <summary>
        /// Inicializa el servidor con una pre asignación de buffers reusables y estados de sockets
        /// </summary>
        public void ConfigInicioServidor()
        {
            try
            {
                peformanceConexionesEntrantes = new PerformanceCounter("TN", "conexionesEntrantesUserver", false);
                peformanceConexionesEntrantes.IncrementBy(1);
            }
            catch (Exception ex)
            {
                EscribirLog(ex.Message + "ConfigInicioServidor", tipoLog.ERROR);
                throw;
            }

            if (!ValidateLicence())
            {
                EscribirLog(NOTLICENCE, tipoLog.ERROR);
                Environment.Exit(666);
            }

            //objetos para operaciones asincronas en los sockets de los clientes
            SocketAsyncEventArgs saeaDeEnvioRecepcionCliente;
            //SocketAsyncEventArgs saeaDeEnvioForzadoAlCliente;

            //Se prepara un buffer suficientemente grande para todas las operaciones y poder reutilizarlo por secciones
            administradorBuffer.inicializarBuffer();

            //pre asignar un conjunto de estados de socket para usarlos inmediatamente en cada una
            // de la conexiones simultaneas que se pueden esperar
            for (Int32 i = 0; i < this.numeroConexionesSimultaneas; i++)
            {
                T estadoDelCliente = new T();
                estadoDelCliente.InicializarEstadoDelClienteBase();

                saeaDeEnvioRecepcionCliente = new SocketAsyncEventArgs();
                //El manejador de eventos para cada lectura de una peticion del cliente
                saeaDeEnvioRecepcionCliente.Completed += new EventHandler<SocketAsyncEventArgs>(RecepcionEnvioEntranteCallBack);
                //SocketAsyncEventArgs necesita un objeto con la información de cada cliente para su administración
                saeaDeEnvioRecepcionCliente.UserToken = estadoDelCliente;
                //Se establece el buffer que se utilizará en la operación de lectura del cliente en el eventArgDeRecepcion
                administradorBuffer.asignarBuffer(saeaDeEnvioRecepcionCliente);
                //Se establece el socket asincrono de EventArg a utilizar en la lectura del cliente
                estadoDelCliente.saeaDeEnvioRecepcion = saeaDeEnvioRecepcionCliente;


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
                adminEstadosCliente.ingresarUnElemento(estadoDelCliente);


                //Ahora genero la pila de estados para el proveedor
                X estadoDelProveedor = new X();
                estadoDelProveedor.InicializarEstadoDelProveedorBase();

                SocketAsyncEventArgs saeaDeEnvioRecepcionAlProveedor;
                saeaDeEnvioRecepcionAlProveedor = new SocketAsyncEventArgs();
                //El manejador de eventos para cada lectura de una peticion del proveedor
                saeaDeEnvioRecepcionAlProveedor.Completed += new EventHandler<SocketAsyncEventArgs>(RecepcionEnvioSalienteCallBack);
                //SocketAsyncEventArgs necesita un objeto con la información de cada proveedor para su administración
                saeaDeEnvioRecepcionAlProveedor.UserToken = estadoDelProveedor;
                //Se establece el buffer que se utilizará en la operación de lectura del proveedor en el eventArgDeEnvioRecepcion
                administradorBuffer.asignarBuffer(saeaDeEnvioRecepcionAlProveedor);
                //Se establece el socket asincrono de EventArg a utilizar en las operaciones con el proveedor
                estadoDelProveedor.saeaDeEnvioRecepcion = saeaDeEnvioRecepcionAlProveedor;

                //Ya con los parametros establecidos para cada operacion, se ingresa en la pila
                //de estados del proveedor y desde ahi administar su uso en cada petición
                adminEstadosDeProveedor.ingresarUnElemento(estadoDelProveedor);
            }
        }

        /// <summary>
        /// Se inicia el servidor de manera que esté escuchando solicitudes de conexión entrantes.
        /// </summary>
        /// <param name="puertoLocal">Puerto de escucha del servidor</param>
        /// <param name="ipProveedor">Ip del servidor del proveedor</param>
        /// <param name="puertoProveedor">Puerto del proveedor</param>
        public void IniciarServidor(Int32 puertoLocal, string ipProveedor, int puertoProveedor)
        {
            //Se inicializa la bandera de que no hay ningún cliente pendiente por desconectar
            desconectado = false;

            //De acuerdo a las buenas practicas de manejo de operaciones asincronas, se debe ANUNCIAR el inicio
            //de un trabajo asincrono para ir controlando su avance por eventos si fuera necesario
            estadoDelServidorBase.OnInicio();

            this.ipProveedor = ipProveedor;
            this.puertoProveedor = puertoProveedor;

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, puertoLocal);

            // se crea el socket que se utilizará de escucha para las conexiones entrantes
            socketDeEscucha = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // se asocia con el puerto de escucha el socket de escucha
            this.socketDeEscucha.Bind(localEndPoint);

            ipLocal = socketDeEscucha.LocalEndPoint.ToString().Split(':')[0];

            // se inicia la escucha de conexiones con un backlog de 100 conexiones
            this.socketDeEscucha.Listen(backLog);

            // Se indica al sistema que se empiezan a aceptar conexiones, se envía una referencia a null para que se indique que es la primera vez
            this.IniciarAceptaciones(null);
            enEjecucion = true;
        }


        #region ProcesoDePeticionesCliente

        /// <summary>
        /// Se inicia la operación de aceptar solicitudes por parte de un cliente
        /// </summary>
        /// <param name="saeaAceptarConexion">Objeto que se utilizará en cada aceptación de una solicitud</param>
        private void IniciarAceptaciones(SocketAsyncEventArgs saeaAceptarConexion)
        {
            // de ser null quiere decir que no hay objeto instanciado y debe crearse desde cero. Es por el primer proceso de escucha
            if (saeaAceptarConexion == null)
            {
                saeaAceptarConexion = new SocketAsyncEventArgs();
                saeaAceptarConexion.Completed += new EventHandler<SocketAsyncEventArgs>(AceptarConexionCallBack);
            }
            else
            {
                // si ya existe instancia, se limpia el socket para trabajo. Esto se utiliza cuando se vuelve a colocar en escucha
                saeaAceptarConexion.AcceptSocket = null;
            }

            // se comprueba el semáforo que nos indica que se tiene recursos para aceptar la conexión
            semaforoParaAceptarClientes.Wait();
            try
            {
                peformanceConexionesEntrantes.IncrementBy(1);
                // se comienza asincronamente el proceso de aceptación y mediante un evento manual 
                // se verifica que haya sido exitoso. Cuando el proceso asincrono es exitoso devuelve false
                bool seHizoAsync = socketDeEscucha.AcceptAsync(saeaAceptarConexion);
                if (!seHizoAsync)
                    // se manda llamar a la función que procesa la solicitud, 
                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                    // en su evento callback
                    AceptarConexionCallBack(socketDeEscucha, saeaAceptarConexion);
            }
            catch (Exception ex)
            {
                EscribirLog(ex.Message + ". IniciarAceptaciones", tipoLog.ERROR);
                // se hace un último intento para volver a iniciar el servidor por si el error fue una excepción al empezar la aceptación
                IniciarAceptaciones(saeaAceptarConexion);
            }
        }

        /// <summary>
        /// Procesa la solicitud por medio de socket principal de escucha
        /// </summary>
        /// <param name="sender">Objeto que se tomará como quien dispara el evento principal</param>
        /// <param name="saea">SocketAsyncEventArg asociado al proceso asincrono.</param>
        private void AceptarConexionCallBack(object sender, SocketAsyncEventArgs saea)
        {
            // se valida que existan errores registrados
            if (saea.SocketError != SocketError.Success)
            {
                // se comprueba que no hay un proceso de cierre del programa o desconexión en curso para no dejar sockets en segundo plano
                if (desconectado)
                {
                    EscribirLog("Socket de escucha desconectado porque el programa principal se está cerrando, AceptarConexionCallBack", tipoLog.ERROR);
                    // se le indica al semáforo que puede permitir la siguiente conexion....al final se cerrará pero no se bloqueará el proceso
                    semaforoParaAceptarClientes.Release();
                    return;
                }
                // se le indica al semáforo que puede asignar otra conexion
                semaforoParaAceptarClientes.Release();
                // aquí el truco en volver a iniciar el proceso de aceptación de solicitudes con el mismo
                // saea que tiene el cliente si hubo un error para re utilizarlo
                IniciarAceptaciones(saea);
                return;
            }

            // si el cliente pasó todas las Utileria entonces se le asigna ya un estado de trabajo del pool de estados de cliente listo con todas sus propiedes
            T estadoDelCliente = adminEstadosCliente.obtenerUnElemento();

            // debo colocar la referencia del proceso principal donde genero el estado del cliente para tenerlo como referencia de retorno
            estadoDelCliente.IngresarReferenciaSocketPrincipal(this);
            // Del SAEA de aceptación de conexión, se recupera el socket para asignarlo al estado del cliente obtenido del pool de estados
            estadoDelCliente.socketDeTrabajo = saea.AcceptSocket;
            //  de la misma forma se ingresa la ip y puerto del cliente que se aceptó
            estadoDelCliente.ipCliente = (saea.AcceptSocket.RemoteEndPoint as IPEndPoint).Address.ToString();
            estadoDelCliente.puertoCliente = (saea.AcceptSocket.RemoteEndPoint as IPEndPoint).Port;

            // con estas instrucciones puedo controlar las acciones en cada fase del proceso de recepción y envío de ser necesario
            estadoDelServidorBase.OnAceptacion(estadoDelCliente);

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
                // si no puedo ingresarlo en la lista de clientes debo rechazarlo porque no tendría control para manipularlo en un futuro
                CerrarSocketCliente(estadoDelCliente);

                EscribirLog("Timeout de 5 seg para obtener bloqueo en AceptarConexionCallBack, listaClientes", tipoLog.ALERTA);
                // coloco nuevamente el socket en proceso de aceptación con el mismo saea para un reintento de conexión
                this.IniciarAceptaciones(saea);
                return;
            }

            // se inicia la recepción de datos del cliente si es que sigue conectado dicho cliente  
            if (estadoDelCliente.socketDeTrabajo.Connected)
            {
                try
                {
                    // se ingresa la configuración del buffer para la recepción del mensaje                    
                    estadoDelCliente.saeaDeEnvioRecepcion.SetBuffer(estadoDelCliente.saeaDeEnvioRecepcion.Offset, tamanoBufferPorPeticion);
                    // se procede a la recepción asincrona del mensaje,el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                    bool seHizoAsync = estadoDelCliente.socketDeTrabajo.ReceiveAsync(estadoDelCliente.saeaDeEnvioRecepcion);
                    if (!seHizoAsync)
                        // se llama a la función que completa el flujo de envío, 
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        RecepcionEnvioEntranteCallBack(estadoDelCliente.socketDeTrabajo, estadoDelCliente.saeaDeEnvioRecepcion);
                }
                catch (Exception ex)
                {
                    EscribirLog(ex.Message + "AceptarConexionCallBack, recibiendo mensaje del cliente", tipoLog.ERROR);
                    CerrarSocketCliente(estadoDelCliente);
                }
            }
            else
            {
                EscribirLog("AceptarConexiónCallBack.estadoDelCliente.socketDeTrabajo no conectado", tipoLog.ALERTA);
            }

            // se indica que puede aceptar más solicitudes con el mismo saea que es el principal
            this.IniciarAceptaciones(saea);
        }

        /// <summary>
        /// Operación de callBack que se llama cuando se envía o se recibe de un socket de asincrono para completar la operación
        /// </summary>
        /// <param name="sender">Objeto principal para devolver la llamada</param>
        /// <param name="e">SocketAsyncEventArg asociado a la operación de envío o recepción</param>        
        private void RecepcionEnvioEntranteCallBack(object sender, SocketAsyncEventArgs e)
        {
            // obtengo el estado del socket
            // se comprueba que el estado haya sido obtenido correctamente
            if (!(e.UserToken is T estadoDelCliente))
            {
                EscribirLog("estadoDelCliente recibido es inválido para la operacion", tipoLog.ERROR);
                return;
            }

            // se determina que operación se está llevando a cabo para indicar que manejador de eventos se ejecuta
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    // se comprueba que exista información y que el socket no refleje errores
                    if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                    {
                        // se procesa la solicitud
                        ProcesarRecepcion(estadoDelCliente);
                    }
                    else
                    {
                        // si no hay datos por X razón, se cierra el cliente porque puede perdurar indefinidamente la conexión
                        CerrarSocketCliente(estadoDelCliente);
                    }
                    break;
                case SocketAsyncOperation.Send:
                    //indico que estaré enviando algo al cliente para que otro proceso con la misma conexión no quiera enviar algo al mismo tiempo
                    estadoDelCliente.esperandoEnvio.Set();
                    // se comprueba que no hay errores con el socket
                    if (e.SocketError == SocketError.Success)
                    {
                        //Intento colocar el socket de nuevo en escucha por si el cliente envía otra trama con la misma conexión
                        ProcesarRecepcionEnvioCiclicoCliente(estadoDelCliente);
                    }
                    else
                    {
                        // si no hay datos por X razón, se cierra el cliente porque puede perdurar indefinidamente la conexión
                        CerrarSocketCliente(estadoDelCliente);
                    }
                    break;
                default:
                    // se da por errores de TCP/IP en alguna intermitencia
                    EscribirLog("La ultima operación no se detecto como de recepcion o envío, RecepcionEnvioEntranteCallBack, " + e.LastOperation.ToString(), tipoLog.ERROR);
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
            //Para ir midiendo el TO por cada recepción
            bool bloqueo = Monitor.TryEnter(estadoDelCliente.fechaInicioTrx, 5000);
            if (bloqueo)
            {
                try
                {
                    estadoDelCliente.fechaInicioTrx = DateTime.Now;
                }
                catch (Exception ex)
                {
                    EscribirLog(ex.Message + ", ProcesarRecepcion, bloqueo para ingresar la fecha de inicio", tipoLog.ERROR);
                }
            }
            else
            {
                EscribirLog("Error al intentar hacer un interbloqueo, ProcesarRecepcion, bloqueo para ingresar la fecha de inicio", tipoLog.ERROR);
            }

            // se obtiene el SAEA de trabajo
            SocketAsyncEventArgs saeaDeEnvioRecepcion = estadoDelCliente.saeaDeEnvioRecepcion;
            // se obtienen los bytes que han sido recibidos
            Int32 bytesTransferred = saeaDeEnvioRecepcion.BytesTransferred;

            // se obtiene el mensaje y se decodifica para entenderlo
            String mensajeRecibido = Encoding.ASCII.GetString(saeaDeEnvioRecepcion.Buffer, saeaDeEnvioRecepcion.Offset, bytesTransferred);

            EscribirLog("Mensaje recibido: " + mensajeRecibido + " del cliente:" + estadoDelCliente.idUnicoCliente, tipoLog.INFORMACION);

            // incrementa el contador de bytes totales recibidos para tener estadísticas nada más
            // debido a que la variable está compartida entre varios procesos, se utiliza interlocked que ayuda a que no se revuelvan
            if (totalBytesLeidos == LIMITE_BYTES_CONTADOR)
            {
                Interlocked.Exchange(ref this.totalBytesLeidos, 0);
            }
            else
            {
                Interlocked.Add(ref this.totalBytesLeidos, bytesTransferred);
            }

            // el mensaje recibido llevará un proceso, que no debe ser llevado por el core, se coloca en la función virtual
            // para que se consuma en otra capa, se procese y se entregue una respuesta
            try
            {
                // bloqueo los procesos sobre este mismo cliente hasta no terminar con esta petición para no tener revolturas de mensajes
                estadoDelCliente.esperandoEnvio.Reset();
                estadoDelCliente.esConsulta = false;
                // aquí se debe realizar lo necesario con la trama entrante para preparar la trama al proveedor en la varia tramaEnvioProveedor
                estadoDelCliente.ProcesarTrama(mensajeRecibido);

                if (SeVencioTO(estadoDelCliente))
                {
                    EscribirLog("Se venció el TimeOut para el cliente " + estadoDelCliente.idUnicoCliente.ToString() + ". Después de procesar la trama", tipoLog.ALERTA);
                    estadoDelCliente.codigoRespuesta = (int)CodigosRespuesta.TimeOutInterno;
                    estadoDelCliente.codigoAutorizacion = 0;
                    ResponderAlCliente(estadoDelCliente);
                    return;
                }
                // cuando haya terminado la clase estadoDelCliente de procesar la trama, se debe evaluar su éxito para enviar la solicitud al proveedor
                if (estadoDelCliente.codigoRespuesta == (int)CodigosRespuesta.TransaccionExitosa)
                {
                    if (estadoDelCliente.esConsulta)
                    {
                        ResponderAlCliente(estadoDelCliente);
                        return;
                    }

                    // me espero a ver si tengo disponibilidad de SAEA para un proveedor
                    semaforoParaAceptarProveedores.Wait();

                    //Se prepara el estado del proveedor que servirá como operador de envío y recepción de trama
                    SocketAsyncEventArgs saeaProveedor = new SocketAsyncEventArgs();
                    saeaProveedor.Completed += new EventHandler<SocketAsyncEventArgs>(ConexionProveedorCallBack);

                    X estadoDelProveedor = adminEstadosDeProveedor.obtenerUnElemento();
                    // ingreso la información de peticion para llenar las clases al proveedor
                    estadoDelProveedor.IngresarObjetoPeticionCliente(estadoDelCliente.objPeticion);
                    estadoDelProveedor.estadoDelClienteOrigen = estadoDelCliente;
                    // Para medir el inicio del proceso y tener control de time out
                    estadoDelProveedor.fechaInicioTrx = DateTime.Now;


                    saeaProveedor.UserToken = estadoDelProveedor;

                    //TODO que la ip y puerto del proveedor sean dinámicas
                    IPAddress iPAddress = IPAddress.Parse(ipProveedor);
                    IPEndPoint endPointProveedor = new IPEndPoint(iPAddress, puertoProveedor);
                    saeaProveedor.RemoteEndPoint = endPointProveedor;

                    // se genera un socket que será usado en el envío y recepción
                    Socket socketDeTrabajoProveedor = new Socket(endPointProveedor.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    if (SeVencioTO(estadoDelCliente))
                    {
                        EscribirLog("Se venció el TimeOut para el cliente. Preparando la conexión al proveedor", tipoLog.ALERTA);
                        estadoDelCliente.codigoRespuesta = (int)CodigosRespuesta.TimeOutInterno;
                        estadoDelCliente.codigoAutorizacion = 0;
                        ResponderAlCliente(estadoDelCliente);
                        return;
                    }

                    // como todo en asyncrono, no hay forma de verificar con un Time Out si la conexión es efectiva, por lo tanto
                    // utilizo una conexión con AsyncResult y le doy un tiempo de 1 segundo para conectarse, de lo contrario, considero error

                    //IAsyncResult conectar = socketDeTrabajoProveedor.BeginConnect(endPointProveedor, null, null);                    
                    //bool success = conectar.AsyncWaitHandle.WaitOne(5000, true);
                    try
                    {
                        socketDeTrabajoProveedor.Connect(endPointProveedor);
                        //se logró conectar el socket
                        if (socketDeTrabajoProveedor.Connected)
                        {
                            // se finaliza sin acción la conexión asincrona
                            //socketDeTrabajoProveedor.EndConnect(conectar);
                            //socketDeTrabajoProveedor.Shutdown(SocketShutdown.Both);

                            socketDeTrabajoProveedor = new Socket(endPointProveedor.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                            saeaProveedor.AcceptSocket = socketDeTrabajoProveedor;
                            //Inicio el proceso de conexión                    
                            bool seHizoSync = socketDeTrabajoProveedor.ConnectAsync(saeaProveedor);
                            if (!seHizoSync)
                                // se llama a la función que completa el flujo de envío, 
                                // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                                // en su evento callback                    
                                ConexionProveedorCallBack(socketDeTrabajoProveedor, saeaProveedor);

                            //semaforoParaAceptarProveedores.Release();
                            //adminEstadosDeProveedor.ingresarUnElemento(estadoDelProveedor);
                        }
                    }
                    catch (Exception)
                    {
                        socketDeTrabajoProveedor.Close();
                        estadoDelProveedor.codigoRespuesta = (int)CodigosRespuesta.ErrorConexionServer;
                        estadoDelProveedor.codigoAutorizacion = 0;
                        ResponderAlCliente(estadoDelProveedor);
                        // se libera el semaforo por si otra petición está solicitando acceso
                        semaforoParaAceptarProveedores.Release();
                        // el SAEA del proveedor se ingresa nuevamente al pool para ser re utilizado
                        adminEstadosDeProveedor.ingresarUnElemento(estadoDelProveedor);
                    }


                    //else // si no se logró conectar el socket en 1 segundo, se responde con error
                    //{

                    //}
                }
                // si el código de respuesta es 30(error en el formato) o 50 (Error en algún paso de evaluar la mensajería),
                // se debe responder al cliente, de lo contrario si es un codigo de los anteriores, no se puede responder porque no se tienen confianza en los datos
                else if (estadoDelCliente.codigoRespuesta != (int)CodigosRespuesta.ErrorFormato && estadoDelCliente.codigoRespuesta != (int)CodigosRespuesta.ErrorProceso)
                {
                    EscribirLog("Error en la identificación de la trama", tipoLog.ALERTA);
                    ResponderAlCliente(estadoDelCliente);
                }
            }
            catch (Exception ex)
            {
                CerrarSocketCliente(estadoDelCliente);
                EscribirLog(ex.Message + ", ProcesarRecepcion", tipoLog.ERROR);
            }
        }

        /// <summary>
        /// Función que entrega una respuesta al cliente por medio del socket de conexión
        /// </summary>
        /// <param name="estadoDelCliente">Estado del cliente con los valores de retorno</param>
        private void ResponderAlCliente(T estadoDelCliente)
        {
            if (estadoDelCliente == null)
            {
                EscribirLog("estadoDelCliente null", tipoLog.ERROR);
                return;
            }

            // trato de obtener la trama que se le responderá al cliente
            estadoDelCliente.ObtenerTramaRespuesta();

            // Si ya se cuenta con una respuesta(s) para el cliente
            if (estadoDelCliente.tramaRespuesta != "")
            {
                // se obtiene el mensaje de respuesta que se enviará cliente
                string mensajeRespuesta = estadoDelCliente.tramaRespuesta;
                EscribirLog("Mensaje de respuesta: " + mensajeRespuesta + " al cliente " + estadoDelCliente.idUnicoCliente, tipoLog.INFORMACION);
                // se obtiene la cantidad de bytes de la trama completa
                int numeroDeBytes = Encoding.ASCII.GetBytes(mensajeRespuesta, 0, mensajeRespuesta.Length, estadoDelCliente.saeaDeEnvioRecepcion.Buffer, estadoDelCliente.saeaDeEnvioRecepcion.Offset);
                // si el número de bytes es mayor al buffer que se tiene destinado a la recepción, no se puede proceder, no es válido el mensaje
                if (numeroDeBytes > tamanoBufferPorPeticion)
                {
                    EscribirLog("La respuesta es más grande que el buffer", tipoLog.ALERTA);
                    CerrarSocketCliente(estadoDelCliente);
                    return;
                }
                try
                {
                    // Se solicita el espacio de buffer para los bytes que se van a enviar                    
                    estadoDelCliente.saeaDeEnvioRecepcion.SetBuffer(estadoDelCliente.saeaDeEnvioRecepcion.Offset, numeroDeBytes);
                }
                catch (Exception ex)
                {
                    EscribirLog("Error asignando buffer para la respuesta al cliente: " + ex.Message, tipoLog.ERROR);
                    return;
                }

                try
                {

                    // se envía asincronamente por medio del socket copia de recepción que es
                    // con el que se está trabajando en esta operación, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                    bool seHizoAsync = estadoDelCliente.socketDeTrabajo.SendAsync(estadoDelCliente.saeaDeEnvioRecepcion);
                    if (!seHizoAsync)
                        // Si se tiene una respuesta False de que el proceso está pendiente, se completa el flujo,
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        RecepcionEnvioEntranteCallBack(estadoDelCliente.socketDeTrabajo, estadoDelCliente.saeaDeEnvioRecepcion);
                }
                catch (Exception)
                {
                    CerrarSocketCliente(estadoDelCliente);
                    return;
                }
            }
            else  // Si el proceso no tuvo una respuesta o se descartó por error, se procede a volver a escuchar para recibir la siguiente trama del mismo cliente
            {
                if (estadoDelCliente.socketDeTrabajo.Connected)
                {
                    try
                    {
                        // se solicita el espacio de buffer para la recepción del mensaje
                        estadoDelCliente.saeaDeEnvioRecepcion.SetBuffer(estadoDelCliente.saeaDeEnvioRecepcion.Offset, tamanoBufferPorPeticion);
                        // se solicita un proceso de recepción asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                        // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                        bool seHizoAsync = estadoDelCliente.socketDeTrabajo.ReceiveAsync(estadoDelCliente.saeaDeEnvioRecepcion);
                        if (!seHizoAsync)
                            // si el evento indica que el proceso asincrono está pendiente, se completa el flujo,
                            // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                            // en su evento callback
                            RecepcionEnvioEntranteCallBack(estadoDelCliente.socketDeTrabajo, estadoDelCliente.saeaDeEnvioRecepcion);
                    }
                    catch (Exception ex)
                    {
                        EscribirLog(ex.Message + ", procesarRecepcion, Desconectando cliente " + estadoDelCliente.idUnicoCliente, tipoLog.ALERTA);
                        CerrarSocketCliente(estadoDelCliente);
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
            //estadoDelCliente.fechaHoraUltimoMensajeAlCliente = DateTime.Now;

            // Una vez terminado el envio, se continua escuchando por el Socket de trabajo
            try
            {
                // se asigna el buffer para continuar el envío
                estadoDelCliente.saeaDeEnvioRecepcion.SetBuffer(estadoDelCliente.saeaDeEnvioRecepcion.Offset, tamanoBufferPorPeticion);
                // se inicia el proceso de recepción asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                bool seHizoAsync = estadoDelCliente.socketDeTrabajo.ReceiveAsync(estadoDelCliente.saeaDeEnvioRecepcion);
                if (!seHizoAsync)
                    // si el evento indica que el proceso está pendiente, se completa el flujo,
                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                    // en su evento callback
                    RecepcionEnvioEntranteCallBack(estadoDelCliente.socketDeTrabajo, estadoDelCliente.saeaDeEnvioRecepcion);
            }
            catch (Exception ex)
            {
                EscribirLog(ex.Message, tipoLog.ERROR);
                CerrarSocketCliente(estadoDelCliente);
            }
        }

        /// <summary>
        /// Cierra el socket asociado a un cliente y retira al cliente de la lista de clientes conectados
        /// </summary>
        /// <param name="estadoDelCliente">Instancia del cliente a cerrar</param>
        public void CerrarSocketCliente(T estadoDelCliente)
        {
            // Se comprueba que la información del socket de trabajo sea null, ya que podría ser invocado como resultado 
            // de una operación de E / S sin valores
            if (estadoDelCliente == null)
            {
                EscribirLog("estadoDelCliente null", tipoLog.ERROR);
                return;
            }

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
                    EscribirLog(ex.Message + " en CerrarSocketCliente, listaClientes", tipoLog.ERROR);
                }
                finally
                {
                    Monitor.Exit(listaClientes);
                }
            }
            else
            {
                EscribirLog("Error obteniendo el bloqueo, CerrarSocketCliente, listaClientes", tipoLog.ALERTA);
            }

            // se limpia la cola de envío de datos para cada socket, para así forzar la detención de respuestas
            //lock (estadoDelCliente.colaEnvio)
            //{
            //    estadoDelCliente.colaEnvio.Clear();
            //}

            // se obtiene el socket específico del cliente en cuestión
            Socket socketDeTrabajoACerrar = estadoDelCliente.socketDeTrabajo;

            // se inhabilita y se cierra dicho socket
            try
            {
                socketDeTrabajoACerrar.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                EscribirLog(ex.Message + " en CerrarSocketCliente, shutdown el socket de trabajo", tipoLog.ERROR);
            }
            socketDeTrabajoACerrar.Close();

            // se llama a la secuencia de cerrando para tener un flujo de eventos
            estadoDelServidorBase.OnClienteCerrado(estadoDelCliente);

            // se libera la instancia de socket de trabajo para reutilizarlo
            adminEstadosCliente.ingresarUnElemento(estadoDelCliente);
            // se marca el semáforo de que puede aceptar otro cliente
            semaforoParaAceptarClientes.Release();
        }

        ///// <summary>
        ///// Cierra el socket asociado al cliente pero este método no retira de la lista de clientes conectados al cliente actual
        ///// </summary>
        ///// <param name="socketCliente">The socket to close</param>
        //private void CerrarConexionForzadaCliente(Socket socketCliente)
        //{
        //    try
        //    {
        //        socketCliente.Shutdown(SocketShutdown.Send);
        //    }
        //    catch (Exception ex)
        //    {
        //        EscribirLog(ex.Message + ", CerrarConexionForzadaCliente, shutdown", tipoLog.ERROR);
        //    }
        //    socketCliente.Close();

        //    this.semaforoParaAceptarClientes.Release();
        //}

        #endregion

        #region ProcesoDePeticionesProveedor

        /// <summary>
        /// Funcion callback para la conexión al proveedor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConexionProveedorCallBack(object sender, SocketAsyncEventArgs e)
        {
            // Si hay errores, debo regresar el estado del proveedor que se está usando a la pila de estados para ser reutilizado
            X estadoDelProveedor = e.UserToken as X;
            // se valida que existan errores registrados
            if (e.SocketError != SocketError.Success && e.SocketError != SocketError.IsConnected)
            {
                if (SeVencioTO((T)estadoDelProveedor.estadoDelClienteOrigen))
                {
                    estadoDelProveedor.codigoRespuesta = 71;
                    estadoDelProveedor.codigoAutorizacion = 0;
                    ResponderAlCliente(estadoDelProveedor);

                }
                else
                {
                    estadoDelProveedor.codigoRespuesta = 70;
                    estadoDelProveedor.codigoAutorizacion = 0;
                    ResponderAlCliente(estadoDelProveedor);

                }

                adminEstadosDeProveedor.ingresarUnElemento(estadoDelProveedor);
                semaforoParaAceptarProveedores.Release();
                return;
            }

            // si todo va bien
            estadoDelProveedor.IngresarReferenciaSocketPrincipal(this);
            // se le indica al estado del proveedor el socket de trabajo
            estadoDelProveedor.socketDeTrabajo = e.AcceptSocket;
            estadoDelProveedor.saeaDeEnvioRecepcion.UserToken = estadoDelProveedor;

            // obtengo las tramas para considerar cualquier evento antes de enviar la petición al proveedor.
            // se puede actualizar más adelante
            estadoDelProveedor.ObtenerTramaPeticion();
            estadoDelProveedor.codigoRespuesta = 0;
            estadoDelProveedor.codigoAutorizacion = 0;
            estadoDelProveedor.ObtenerTramaRespuesta();

            if (SeVencioTO((T)estadoDelProveedor.estadoDelClienteOrigen))
            {
                estadoDelProveedor.codigoRespuesta = 71;
                estadoDelProveedor.codigoAutorizacion = 0;
                ResponderAlCliente(estadoDelProveedor);
                CerrarSocketProveedor(estadoDelProveedor);
                return;
            }
            else if (SeVencioTO(estadoDelProveedor))
            {
                estadoDelProveedor.codigoRespuesta = 8;
                estadoDelProveedor.codigoAutorizacion = 0;
                ResponderAlCliente(estadoDelProveedor);
                CerrarSocketProveedor(estadoDelProveedor);
                return;
            }

            if (estadoDelProveedor.socketDeTrabajo.Connected)
            {
                try
                {
                    string mensajeAlProveedor = estadoDelProveedor.tramaSolicitud;
                    //EscribirLog(mensajeAlProveedor.Substring(2), tipoLog.INFORMACION);

                    // se obtiene la cantidad de bytes de la trama completa
                    int numeroDeBytes = Encoding.Default.GetBytes(mensajeAlProveedor, 0, mensajeAlProveedor.Length, estadoDelProveedor.saeaDeEnvioRecepcion.Buffer, estadoDelProveedor.saeaDeEnvioRecepcion.Offset);
                    // si el número de bytes es mayor al buffer que se tiene destinado a la recepción, no se puede proceder, no es válido el mensaje
                    if (numeroDeBytes > tamanoBufferPorPeticion)
                    {
                        EscribirLog("El mensaje al proveedor es más grande que el buffer", tipoLog.ALERTA);
                        CerrarSocketProveedor(estadoDelProveedor);
                        return;
                    }

                    // Se prepara el buffer del SAEA con el tamaño predefinido                         
                    estadoDelProveedor.saeaDeEnvioRecepcion.SetBuffer(estadoDelProveedor.saeaDeEnvioRecepcion.Offset, numeroDeBytes);

                    // se procede a la recepción asincrona del mensaje,el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                    bool seHizoAsync = estadoDelProveedor.socketDeTrabajo.SendAsync(estadoDelProveedor.saeaDeEnvioRecepcion);
                    if (!seHizoAsync)
                        // se llama a la función que completa el flujo de envío, 
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        RecepcionEnvioSalienteCallBack(estadoDelProveedor.socketDeTrabajo, estadoDelProveedor.saeaDeEnvioRecepcion);
                }
                catch (Exception ex)
                {
                    EscribirLog(ex.Message + "ConexionProveedorCallBack, iniciando conexión", tipoLog.ERROR);
                    estadoDelProveedor.codigoRespuesta = 50;
                    estadoDelProveedor.codigoAutorizacion = 0;
                    ResponderAlCliente(estadoDelProveedor);
                    CerrarSocketProveedor(estadoDelProveedor);
                }
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
        /// <param name="e"></param>
        private void RecepcionEnvioSalienteCallBack(object sender, SocketAsyncEventArgs e)
        {
            // obtengo el estado del proveedor
            // se comprueba que el estado haya sido obtenido correctamente
            if (!(e.UserToken is X estadoDelProveedor))
            {
                EscribirLog("estadoDelProveedor recibido es inválido para la operacion", tipoLog.ERROR);
                return;
            }

            // se determina que operación se está llevando a cabo para indicar que manejador de eventos se ejecuta
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Send:
                    EscribirLog("Mensaje enviado al proveedor: " + estadoDelProveedor.tramaSolicitud.Substring(2) + " del cliente: " + estadoDelProveedor.estadoDelClienteOrigen.idUnicoCliente, tipoLog.INFORMACION);
                    // se comprueba que no hay errores con el socket
                    if (e.SocketError == SocketError.Success)
                    {
                        // se procesa el envío
                        this.ProcesarRecepcionEnvioCiclicoProveedor(estadoDelProveedor);
                    }
                    else
                    {
                        EscribirLog("Error en el envío, RecepcionEnvioSalienteCallBack" + e.SocketError.ToString(), tipoLog.ERROR);
                        estadoDelProveedor.codigoRespuesta = 5;
                        estadoDelProveedor.codigoAutorizacion = 0;
                        ResponderAlCliente(estadoDelProveedor);
                        CerrarSocketProveedor(estadoDelProveedor);
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
                        estadoDelProveedor.codigoRespuesta = 5;
                        estadoDelProveedor.codigoAutorizacion = 0;
                        ResponderAlCliente(estadoDelProveedor);
                        //CerrarSocketProveedor(estadoDelProveedor);
                    }
                    break;
                default:
                    EscribirLog("La ultima operación no se detecto como de recepcion o envío, RecepcionEnvioSalienteCallBack, " + e.LastOperation.ToString(), tipoLog.ALERTA);
                    estadoDelProveedor.codigoRespuesta = 5;
                    estadoDelProveedor.codigoAutorizacion = 0;
                    ResponderAlCliente(estadoDelProveedor);
                    CerrarSocketProveedor(estadoDelProveedor);
                    break;
            }
        }

        /// <summary>
        /// Función para procesar el envío al proveedor y dejar de nuevo en escucha al socket
        /// </summary>
        /// <param name="estadoDelProveedor">Estado del proveedor con la información de conexión</param>
        private void ProcesarRecepcionEnvioCiclicoProveedor(X estadoDelProveedor)
        {
            //estadoDelProveedor.fechaHoraUltimoMensajeAlProveedor = DateTime.Now;

            if (SeVencioTO((T)estadoDelProveedor.estadoDelClienteOrigen))
            {
                estadoDelProveedor.codigoRespuesta = 71;
                estadoDelProveedor.codigoAutorizacion = 0;
                ResponderAlCliente(estadoDelProveedor);
                CerrarSocketProveedor(estadoDelProveedor);
                return;
            }
            else if (SeVencioTO(estadoDelProveedor))
            {
                estadoDelProveedor.codigoRespuesta = 8;
                estadoDelProveedor.codigoAutorizacion = 0;
                ResponderAlCliente(estadoDelProveedor);
                CerrarSocketProveedor(estadoDelProveedor);
                return;
            }
            else
            {
                // Una vez terminado el envio, se continua escuchando por el Socket de trabajo
                try
                {
                    // se asigna el buffer para continuar el envío
                    estadoDelProveedor.saeaDeEnvioRecepcion.SetBuffer(estadoDelProveedor.saeaDeEnvioRecepcion.Offset, tamanoBufferPorPeticion);
                    // se inicia el proceso de recepción asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                    bool seHizoAsync = estadoDelProveedor.socketDeTrabajo.ReceiveAsync(estadoDelProveedor.saeaDeEnvioRecepcion);
                    if (!seHizoAsync)
                        // si el evento indica que el proceso está pendiente, se completa el flujo,
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        RecepcionEnvioSalienteCallBack(estadoDelProveedor.socketDeTrabajo, estadoDelProveedor.saeaDeEnvioRecepcion);
                }
                catch (Exception)
                {
                    estadoDelProveedor.codigoRespuesta = 5;
                    estadoDelProveedor.codigoAutorizacion = 0;
                    ResponderAlCliente(estadoDelProveedor);
                    CerrarSocketProveedor(estadoDelProveedor);
                }
            }
        }

        /// <summary>
        /// Función que realiza la recepción del mensaje y lo procesa
        /// </summary>
        /// <param name="estadoDelProveedor">Estado del proveedor con la información de conexión</param>
        private void ProcesarRecepcion(X estadoDelProveedor)
        {
            // se obtiene el SAEA de recepción
            SocketAsyncEventArgs saeaRecepcion = estadoDelProveedor.saeaDeEnvioRecepcion;
            // se obtienen los bytes que han sido recibidos
            Int32 bytesTransferred = saeaRecepcion.BytesTransferred;

            // se obtiene el mensaje y se decodifica
            String mensajeRecibido = Encoding.ASCII.GetString(saeaRecepcion.Buffer, saeaRecepcion.Offset, bytesTransferred);
            EscribirLog("Mensaje recibido del proveedor: " + estadoDelProveedor.tramaSolicitud.Substring(2) + " para el cliente: " + estadoDelProveedor.estadoDelClienteOrigen.idUnicoCliente, tipoLog.INFORMACION);

            // incrementa el contador de bytes totales recibidos
            // debido a que la variable está compartida entre varios procesos, se utiliza interlocked que ayuda a que no se revuelvan
            //Interlocked.Add(ref this.totalBytesLeidos, bytesTransferred);

            // el mensaje recibido llevará un proceso, que no debe ser llevado por el core, se coloca en la función virtual
            // para que se consuma en otra capa, se procese y se entregue una respuesta
            try
            {
                estadoDelProveedor.ProcesarTramaDelProveeedor(mensajeRecibido);
                estadoDelProveedor.ObtenerTramaRespuesta();
            }
            catch (Exception ex)
            {
                EscribirLog(ex.Message + ", procesando trama del proveedor, ProcesarRecepcion", tipoLog.ERROR);
                return;
            }

            if (SeVencioTO((T)estadoDelProveedor.estadoDelClienteOrigen))
            {
                estadoDelProveedor.codigoRespuesta = 71;
                estadoDelProveedor.codigoAutorizacion = 0;
                ResponderAlCliente(estadoDelProveedor);
                CerrarSocketProveedor(estadoDelProveedor);
            }
            else if (SeVencioTO(estadoDelProveedor))
            {
                estadoDelProveedor.codigoRespuesta = 8;
                estadoDelProveedor.codigoAutorizacion = 0;
                ResponderAlCliente(estadoDelProveedor);
                CerrarSocketProveedor(estadoDelProveedor);
            }
            else
            {
                ResponderAlCliente(estadoDelProveedor);
                CerrarSocketProveedor(estadoDelProveedor);
            }
        }

        /// <summary>
        /// Cierra el socket asociado a un proveedor y retira al proveedor de la lista de conectados
        /// </summary>
        public void CerrarSocketProveedor(X estadoDelProveedor)
        {
            // Se comprueba que la información del socket de trabajo sea null, ya que podría ser invocado como resultado 
            // de una operación de E / S sin valores
            if (estadoDelProveedor == null) return;

            // se obtiene el socket específico del cliente en cuestión
            Socket socketDeTrabajoACerrar = estadoDelProveedor.socketDeTrabajo;

            // se inhabilita y se cierra dicho socket
            try
            {
                socketDeTrabajoACerrar.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                EscribirLog(ex.Message + " en CerrarSocketProveedor, shutdown el socket de trabajo", tipoLog.ERROR);
            }
            socketDeTrabajoACerrar.Close();

            // se espera a que un cliente acaba de recibir la última información y se cierra
            int tiempoInicial, tiempoFinal;
            tiempoInicial = Environment.TickCount;
            //bool receivedSignal = estadoDelProveedor.esperandoEnvio.WaitOne(10000, false);
            bool receivedSignal = true;
            tiempoFinal = Environment.TickCount;
            // la señal debe ser false si excedió el TO
            if (!receivedSignal)
            {
                // se mide el tiempo de espera para el envío de la información y si supera el establecido es un error
                if (tiempoFinal - tiempoInicial > maxRetrasoParaEnvio)
                {
                    maxRetrasoParaEnvio = tiempoFinal - tiempoInicial;
                    EscribirLog("maximo tiempo de espera para el envío de información: " + maxRetrasoParaEnvio + " ms, CerrarSocketCliente", tipoLog.ALERTA);
                }
                EscribirLog("No se puede esperar para terminar el envío de información al cliente, CerrarSocketCliente", tipoLog.ALERTA);
                // como no puedo esperar tanto al envío se deja pendiente, esto para no perder ninguna transacción
                listaProveedoresPendientesDesconexion.Add(estadoDelProveedor);
                return;
            }

            // se libera la instancia de socket de trabajo para reutilizarlo
            adminEstadosDeProveedor.ingresarUnElemento(estadoDelProveedor);
            // se marca el semáforo de que puede aceptar otro cliente
            this.semaforoParaAceptarProveedores.Release();
        }
        /// <summary>
        /// Función que realiza el envío de la trama de respuesta al cliente desde los estado del proveedor
        /// </summary>
        /// <param name="estadoDelProveedor">EstadoDelProveedor</param>
        private void ResponderAlCliente(X estadoDelProveedor)
        {
            T estadoDelCliente = (T)estadoDelProveedor.estadoDelClienteOrigen;
            if (estadoDelCliente == null)
            {
                return;
            }
            estadoDelCliente.codigoAutorizacion = estadoDelProveedor.codigoAutorizacion;
            estadoDelCliente.codigoRespuesta = estadoDelProveedor.codigoRespuesta;

            // trato de obtener la trama que se le responderá al cliente
            estadoDelCliente.ObtenerTramaRespuesta();

            // Si ya se cuenta con una respuesta(s) para el cliente
            if (estadoDelCliente.tramaRespuesta != "")
            {
                // se guarda la transacción sin importar si pudiera existir un error porque al cliente siempre se le debe responder
                estadoDelProveedor.GuardarTransaccion();

                // se obtiene el mensaje de respuesta que se enviará cliente
                string mensajeRespuesta = estadoDelCliente.tramaRespuesta;
                EscribirLog("Mensaje de respuesta: " + mensajeRespuesta + " al cliente " + estadoDelCliente.idUnicoCliente, tipoLog.INFORMACION);

                // se obtiene la cantidad de bytes de la trama completa
                int numeroDeBytes = Encoding.ASCII.GetBytes(mensajeRespuesta, 0, mensajeRespuesta.Length, estadoDelCliente.saeaDeEnvioRecepcion.Buffer, estadoDelCliente.saeaDeEnvioRecepcion.Offset);
                // si el número de bytes es mayor al buffer que se tiene destinado a la recepción, no se puede proceder, no es válido el mensaje
                if (numeroDeBytes > tamanoBufferPorPeticion)
                {
                    EscribirLog("La respuesta es más grande que el buffer", tipoLog.ALERTA);
                    CerrarSocketCliente(estadoDelCliente);
                    return;
                }
                try
                {
                    // Se solicita el espacio de buffer para los bytes que se van a enviar                    
                    estadoDelCliente.saeaDeEnvioRecepcion.SetBuffer(estadoDelCliente.saeaDeEnvioRecepcion.Offset, numeroDeBytes);
                }
                catch (Exception ex)
                {
                    EscribirLog(ex.Message, tipoLog.ERROR);
                    return;
                }

                try
                {

                    // se envía asincronamente por medio del socket copia de recepción que es
                    // con el que se está trabajando en esta operación, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                    bool seHizoAsync = estadoDelCliente.socketDeTrabajo.SendAsync(estadoDelCliente.saeaDeEnvioRecepcion);
                    if (!seHizoAsync)
                        // Si se tiene una respuesta False de que el proceso está pendiente, se completa el flujo,
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        RecepcionEnvioEntranteCallBack(estadoDelCliente.socketDeTrabajo, estadoDelCliente.saeaDeEnvioRecepcion);
                }
                catch (Exception)
                {
                    CerrarSocketCliente(estadoDelCliente);
                    return;
                }
            }
            else  // Si el proceso no tuvo una respuesta o se descartó por error, se procede a volver a escuchar para recibir la siguiente trama del mismo cliente
            {
                if (estadoDelCliente.socketDeTrabajo.Connected)
                {
                    try
                    {
                        // se solicita el espacio de buffer para la recepción del mensaje
                        estadoDelCliente.saeaDeEnvioRecepcion.SetBuffer(estadoDelCliente.saeaDeEnvioRecepcion.Offset, tamanoBufferPorPeticion);
                        // se solicita un proceso de recepción asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                        // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                        bool seHizoAsync = estadoDelCliente.socketDeTrabajo.ReceiveAsync(estadoDelCliente.saeaDeEnvioRecepcion);
                        if (!seHizoAsync)
                            // si el evento indica que el proceso asincrono está pendiente, se completa el flujo,
                            // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                            // en su evento callback
                            RecepcionEnvioEntranteCallBack(estadoDelCliente.socketDeTrabajo, estadoDelCliente.saeaDeEnvioRecepcion);
                    }
                    catch (Exception ex)
                    {
                        EscribirLog(ex.Message + ", procesarRecepcion, Desconectando cliente " + estadoDelCliente.idUnicoCliente, tipoLog.ALERTA);
                        CerrarSocketCliente(estadoDelCliente);
                    }
                }
            }

        }




        #endregion


        #region FuncionesDeAyuda       

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
        public void DetenerServidor()
        {
            // se indica que se está ejecutando el proceso de desconexión de los clientes
            desconectado = true;
            List<T> listaDeClientesEliminar = new List<T>();

            // Primero se detiene y se cierra el socket de escucha
            try
            {
                this.socketDeEscucha.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                EscribirLog(ex.Message + " en detenerServidor", tipoLog.ERROR);
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
            desconectado = false;

            // Se marca en log que se detiene el servidor            
        }

        //private void CerrarConexionForzadaProveedor(Socket socketProveedor)
        //{
        //    try
        //    {
        //        socketProveedor.Shutdown(SocketShutdown.Send);
        //    }
        //    catch (Exception ex)
        //    {
        //        EscribirLog(ex.Message + ", CerrarConexionForzadaProveedor, shutdown", tipoLog.ERROR);
        //    }
        //    socketProveedor.Close();
        //}

        /// <summary>
        /// Funcion que el guardado de logs en el event log de windows
        /// </summary>
        /// <param name="mensaje"></param>
        /// <param name="tipoLog"></param>
        private void EscribirLog(string mensaje, tipoLog tipoLog)
        {
            switch (tipoLog)
            {
                case tipoLog.INFORMACION:
                    Trace.TraceInformation(mensaje);
                    break;
                case tipoLog.ALERTA:
                    Trace.TraceWarning(mensaje);
                    break;
                case tipoLog.ERROR:
                    Trace.TraceError(mensaje);
                    break;
                default:
                    Trace.WriteLine(mensaje);
                    break;
            }
        }

        /// <summary>
        /// Verificación del tiempo de la transacción sobre el proceso del clente
        /// </summary>
        /// <param name="estadoDelCliente">instancia del estado del cliente</param>
        /// <returns></returns>
        private bool SeVencioTO(T estadoDelCliente)
        {
            TimeSpan timeSpan = DateTime.Now - estadoDelCliente.fechaInicioTrx;
            return timeSpan.Seconds > estadoDelCliente.timeOut;
        }

        /// <summary>
        /// Verificación del tiempo de la transacción sobre el proceso del proveedor
        /// </summary>
        /// <param name="estadoDelProveedor">instancia del estado del proveedor</param>
        /// <returns></returns>
        private bool SeVencioTO(X estadoDelProveedor)
        {
            TimeSpan timeSpan = DateTime.Now - estadoDelProveedor.fechaInicioTrx;
            return timeSpan.Seconds > estadoDelProveedor.timeOut;
        }

        private bool ValidateLicence()
        {
            try
            {
                Encrypter.Encrypter encrypter = new Encrypter.Encrypter("AdmindeServicios");

                if (!GetLicence())
                    return false;

                if (!GetInfoPc())
                    return false;

                return string.Compare(PROGRAM, encrypter.DesEncrypterText(licence.Split('|')[(int)Licence.Program])) == 0
                        && (DateTime.Compare(localValidity, DateTime.Parse(encrypter.DesEncrypterText(licence.Split('|')[(int)Licence.Validity]))) <= 0
                        && (string.Compare(processorId, encrypter.DesEncrypterText(licence.Split('|')[(int)Licence.ProcessorId])) == 0)
                        && (string.Compare(product, encrypter.DesEncrypterText(licence.Split('|')[(int)Licence.Product])) == 0)
                        && (string.Compare(manufacturer, encrypter.DesEncrypterText(licence.Split('|')[(int)Licence.Manufacturer])) == 0));
            }
            catch (Exception ex)
            {
                EscribirLog(ex.Message, tipoLog.ERROR);
                return false;
            }
        }


        private bool GetLicence()
        {
            FileStream fileStream;
            try
            {
                using (fileStream = File.OpenRead(Environment.CurrentDirectory + "\\Licence.txt"))
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
                EscribirLog(ex.Message, tipoLog.ERROR);
                return false;
            }
        }

        private bool GetInfoPc()
        {
            try
            {
                processorId=RunQuery("Processor", "ProcessorId").ToUpper();

                product= RunQuery("BaseBoard", "Product").ToUpper();

                manufacturer = RunQuery("BaseBoard", "Manufacturer").ToUpper();

                return true;
            }
            catch (Exception ex)
            {
                EscribirLog(ex.Message, tipoLog.ERROR);
                return false;
            }
        }


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


        #endregion


    }
}
