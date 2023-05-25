﻿using ServerCore.Clases;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static ServerCore.Configuracion;
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
    public class ServidorTransaccional<T, S, X> : IServidorTransaccional<T, S, X> where T : EstadoDelClienteBase, new()
        where S : EstadoDelServidorBase, new()
        where X : EstadoDelProveedorBase, new()
    {
        /// <summary>
        /// Instancia del performance counter de peticiones entrantes
        /// </summary>
        public PerformanceCounter peformanceConexionesEntrantes;

        #region Propiedades públicas

        /// <summary>
        /// Obtiene una lista de clientes ordenados por un GUID
        /// </summary>
        internal Dictionary<Guid, T> _clientesConectadosAlServidor;

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
        internal int _numeroMaximoConexionesSimultaneasPorCliente;

        /// <summary>        
        /// Obtiene o ingresa el número máximo de conexiones simultaneas de una misma IP del cliente (0=ilimitadas)
        /// </summary>
        public int NumeroMaximoConexionesPorIpCliente
        {
            get
            {
                return _numeroMaximoConexionesSimultaneasPorCliente;
            }
            set
            {
                if (!_enEjecucion)
                {
                    _numeroMaximoConexionesSimultaneasPorCliente = NumeroMaximoConexionesPorIpCliente;
                }
            }
        }

        /// <summary>
        /// Obtiene o ingresa el valor de que si el servidor está o no ejecutandose
        /// </summary>
        internal bool _enEjecucion;

        /// <summary>
        /// Obtiene o ingresa el valor de que si el servidor está o no ejecutandose
        /// </summary>
        public bool ServidorEnEjecucion
        {
            get
            {
                return _enEjecucion;
            }
            set
            {
                if (!ServidorEnEjecucion)
                {
                    _enEjecucion = ServidorEnEjecucion;
                }
            }
        }

        /// <summary>
        /// Obtiene o ingresa el estado del socket del servidor
        /// </summary>
        public S EstadoDelServidorBase { get; set; }

        /// <summary>
        /// Obtiene o ingresa a la lista de clientes pendientes de desconexión, esta lista es para la verificación de que todos los cliente
        /// se desconectan adecuadamente, su uso es más para debug
        /// </summary>
        internal List<T> _clientesPendientesDesconexion;

        /// <summary>
        /// Obtiene o ingresa a la lista de clientes pendientes de desconexión, esta lista es para la verificación de que todos los cliente
        /// se desconectan adecuadamente, su uso es más para debug
        /// </summary>
        public List<T> ClientesPendientesDesconexion
        {
            get => _clientesPendientesDesconexion is null ? null : _clientesPendientesDesconexion;
            set
            {
                if (!_enEjecucion)
                    _clientesPendientesDesconexion = value;
            }
        }

        /// <summary>
        /// Obtiene o ingresa a la lista de proveedores pendientes de desconexión, esta lista es para la verificación de que todos los proveedores
        /// se desconectan adecuadamente, su uso es más para debug pero queda para mejorar
        /// </summary>
        internal List<X> _proveedoresPendientesDesconexion;

        /// <summary>
        /// Obtiene o ingresa a la lista de proveedores pendientes de desconexión, esta lista es para la verificación de que todos los proveedores
        /// se desconectan adecuadamente, su uso es más para debug pero queda para mejorar
        /// </summary>
        public List<X> ProveedoresPendientesDesconexion
        {
            get => _proveedoresPendientesDesconexion;
            set
            {
                if (!_enEjecucion)
                {
                    _proveedoresPendientesDesconexion = value;
                }
            }
        }

        /// <summary>
        /// Obtiene el número de clientes conectados actualmente al servidor
        /// </summary>
        public int NumeroclientesConectados
        {
            get
            {
                return _clientesConectadosAlServidor.Values.Count;
            }
        }

        /// <summary>
        /// Ip a la cual se apuntarán todas las transacciones del proveedor
        /// </summary>
        internal string _ipProveedor;

        /// <summary>
        /// Ip a la cual se apuntarán todas las transacciones del proveedor
        /// </summary>
        public string IpProveedor
        {
            get => _ipProveedor;
            set
            {
                if (!_enEjecucion)
                    _ipProveedor = value;
            }
        }

        /// <summary>
        /// Lista de puertos en la IP configurada al proveedor para el envío de la mensajería
        /// </summary>
        internal List<int> _puertosProveedor;

        /// <summary>
        /// Lista de puertos en la IP configurada al proveedor para el envío de la mensajería
        /// </summary>
        public List<int> PuertosProveedor
        {
            get => _puertosProveedor;
            set
            {
                if (!_enEjecucion)
                    _puertosProveedor = value;
            }
        }

        /// <summary>
        /// Bytes que se han transmitido desde el inicio de la aplicación
        /// </summary>
        public int TotalDeBytesTransferidos
        {
            get
            {
                return totalBytesLeidos;
            }
        }

        internal string _ipDeEscuchaServidor;
        /// <summary>
        /// IP de escucha de la aplicación  para recibir mensajes
        /// </summary>
        public string IpDeEscucha
        {
            get => _ipDeEscuchaServidor;
            set
            {
                if (!_enEjecucion)
                    _ipDeEscuchaServidor = value;
            }
        }

        /// <summary>
        /// Retraso en el envío, es para uso en Debug
        /// </summary>
        internal static int _maxRetrasoParaEnvio = 0;

        /// <summary>
        /// Numero que muestra cuantas conexiones puede aún soportar el servidor del lado del cliente
        /// </summary>
        public int NumeroDeRecursosDisponiblesCliente
        {
            get
            {
                return semaforoParaAceptarClientes.CurrentCount;
            }
        }

        /// <summary>
        /// Numero que muestra cuantas conexiones puede aún soportar el servidor del lado del proveedor
        /// </summary>
        public int NumeroDeRecursosDisponiblesProveedor
        {
            get
            {
                return semaforoParaAceptarProveedores.CurrentCount;
            }
        }

        /// <summary>
        /// Auxiliar para el conteo de puertos y su balanceo de salida
        /// </summary>
        internal int _contadorPuertos = 0;

        #endregion

        #region Propiedades privadas

        /// <summary>
        /// Número de conexiones simultaneas que podrá manejar el servidor por defecto
        /// </summary>
        private readonly int numeroConexionesSimultaneasCliente;
        private readonly int numeroConexionesSimultaneasProveedor;

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
        private bool desconectando = false;

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
        /// Mensaje de aviso
        /// </summary>
        private const string NOTLICENCE = "No cuenta con permisos para usar esta aplicación en este equipo";

        /// <summary>
        /// Log del sistema
        /// </summary>
        private static EventLogTraceListener logListener;

        private Seguridad seguridad;
        private ContadoresRendimiento contadoresRendimiento;
        private LogTrace logTrace;

        #endregion

        #region Inicializadores del servidor

        /// <summary>
        /// Crea una instancia del administrador de sockets, posterior se tiene que llamar al método
        /// ConfigInicioServidor para iniciar el proceso de asignacion de recursos        
        /// </summary>
        /// <param name="numeroConexSimultaneas">Maximo número de conexiones simultaneas a manejar en el servidor</param>
        /// <param name="tamanoBuffer">Tamaño del buffer por conexión, un parámetro standart es 1024</param>
        /// <param name="backlog">Parámetro TCP/IP backlog, el recomendable es 100</param>
        public ServidorTransaccional(int numeroConexSimultaneas, int tamanoBuffer = 1024, int backlog = 100)
        {
            totalBytesLeidos = 0;
            this.numeroConexionesSimultaneasCliente = numeroConexSimultaneas;
            numeroConexionesSimultaneasProveedor = numeroConexSimultaneas;
            //Se coloca ilimitado para fines no restrictivos
            //TODO crear la restricción
            NumeroMaximoConexionesPorIpCliente = 0;
            this.backLog = backlog;
            _clientesConectadosAlServidor = new Dictionary<Guid, T>();
            //listaClientesBloqueados = new Dictionary<IPAddress, ClienteBloqueo>();
            //listaClientesPermitidos = new List<Regex>();
            _clientesPendientesDesconexion = new List<T>();

            this.tamanoBufferPorPeticion = tamanoBuffer;


            try
            {
                EstadoDelServidorBase = new S();
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + "ServidorTransaccional", tipoLog.ERROR);
            }

            // establezco el proceso principal para referencia futura
            EstadoDelServidorBase.ProcesoPrincipal = this;
            // indico que aún no está en funcionamiento, faltan parámetros
            _enEjecucion = false;

            //Asignación de un buffer tomando en cuenta por lo menos los 3 sockets por defecto para lectura y escritura iniciales
            // es decir, el tamaño del buffer por operación por el número de conexiónes por el número de sockets iniciales no dará
            // el valor de buffer enorme en bytes, por ejemplo: tamanoBuffer= 1024 * 1000 * 3 =2048000 bytes
            this.administradorBuffer = new AdminBuffer(tamanoBuffer * numeroConexionesSimultaneasCliente * operacionesPreDisponibles, tamanoBufferPorPeticion);

            //se inicializan los estados de socket necesarios para el número simultaneo de conexiones, para tenerlos listos a usarse como una pila            
            adminEstadosCliente = new AdminEstadosDeCliente<T>(numeroConexionesSimultaneasCliente);

            //se inicializan los estados de socket necesarios para el número simultaneo de conexiones hacia el proveedor, para tenerlos listos a usarse como una pila            
            adminEstadosDeProveedor = new AdminEstadosDeProveedor<X>(numeroConexionesSimultaneasProveedor);

            //Se inicializa el número inicial y maximo de conexiones simultaneas soportadas, será el semáforo quien indique que hay saturación.            
            semaforoParaAceptarClientes = new SemaphoreSlim(numeroConexionesSimultaneasCliente, numeroConexionesSimultaneasCliente);
            semaforoParaAceptarProveedores = new SemaphoreSlim(numeroConexionesSimultaneasProveedor, numeroConexionesSimultaneasProveedor);
        }

        /// <summary>
        /// Inicializa el servidor con una pre asignación de buffers reusables y estados de sockets
        /// </summary>
        /// <param name="timeOutCliente">Tiempo en segundos de espera antes de cancelar una respuesta por tiempo excedido en el proceso</param>
        public void PreConfiguracionInicial(int timeOutCliente)
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
                Configuracion.timeOutCliente = timeOutCliente;
                peformanceConexionesEntrantes = new PerformanceCounter("TN", "conexionesEntrantesUserver", false);
                peformanceConexionesEntrantes.IncrementBy(1);
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + ", ConfigInicioServidor", tipoLog.ERROR);
                throw;
            }

            seguridad = new Seguridad(logTrace);
            if (!seguridad.ValidacionPermisos())
            {
                logTrace.EscribirLog(NOTLICENCE, tipoLog.ERROR);
                throw new Exception(NOTLICENCE);
            }

            contadoresRendimiento = new ContadoresRendimiento();
            contadoresRendimiento.CrearContadores();

            //objetos para operaciones asincronas en los sockets de los clientes
            SocketAsyncEventArgs saeaDeEnvioRecepcionCliente;
            //SocketAsyncEventArgs saeaDeEnvioForzadoAlCliente;

            //Se prepara un buffer suficientemente grande para todas las operaciones y poder reutilizarlo por secciones
            administradorBuffer.inicializarBuffer();

            //pre asignar un conjunto de estados de socket para usarlos inmediatamente en cada una
            // de la conexiones simultaneas que se pueden esperar
            for (Int32 i = 0; i < this.numeroConexionesSimultaneasCliente; i++)
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
                adminEstadosCliente.IngresarUnElemento(estadoDelCliente);


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
                adminEstadosDeProveedor.IngresarUnElemento(estadoDelProveedor);
            }
        }

        /// <summary>
        /// Se inicia el servidor de manera que esté escuchando solicitudes de conexión entrantes.
        /// </summary>
        /// <param name="puertoLocalEscucha">Puerto de escucha para la recepeción de mensajes</param>
        /// <param name="listaPuertosProveedor">Puertos del proveedor</param>
        /// <param name="modoTest">Variable que indicará si el server entra en modo test.
        /// El modo Test, responderá a toda petición bien formada, con una código de autorización
        /// y respuesta simulado sin enviar la trama a un proveedor externo</param>
        /// <param name="modoRouter">Activación para que el servidor pueda enviar mensajes a otro proveedor</param>
        /// <param name="ipProveedor">IP del proveedor a donde se enviarán mensajes en caso de que el modoRouter esté encendido</param>
        public void Iniciar(int puertoLocalEscucha, bool modoTest = false, bool modoRouter = true, string ipProveedor = "", List<int> listaPuertosProveedor = null)
        {
            //Se inicializa la bandera de que no hay ningún cliente pendiente por desconectar
            desconectando = false;
            Configuracion.modoTest = modoTest;
            Configuracion.modoRouter = modoRouter;
            //De acuerdo a las buenas practicas de manejo de operaciones asincronas, se debe ANUNCIAR el inicio
            //de un trabajo asincrono para ir controlando su avance por eventos si fuera necesario
            EstadoDelServidorBase.OnInicio();

            if (!modoTest && modoRouter)
            {
                if (!IPAddress.TryParse(ipProveedor, out IPAddress address))
                    throw new Exception("IP del proveedor ingresada, es inválida");
                _ipProveedor = ipProveedor;
                if (listaPuertosProveedor == null)
                    throw new Exception("Puertos del proveedor inválidos");
                _puertosProveedor = listaPuertosProveedor;
                _contadorPuertos = listaPuertosProveedor.Count;
            }

            try
            {
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, puertoLocalEscucha);

                // se crea el socket que se utilizará de escucha para las conexiones entrantes
                socketDeEscucha = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // se asocia con el puerto de escucha el socket de escucha
                this.socketDeEscucha.Bind(localEndPoint);

                _ipDeEscuchaServidor = socketDeEscucha.LocalEndPoint.ToString().Split(':')[0];

                // se inicia la escucha de conexiones con un backlog de 100 conexiones
                this.socketDeEscucha.Listen(backLog);
            }
            catch (Exception ex)
            {
                throw ex;
            }


            // Se indica al sistema que se empiezan a aceptar conexiones, se envía una referencia a null para que se indique que es la primera vez
            this.IniciarAceptaciones(null);
            _enEjecucion = true;
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
                logTrace.EscribirLog(ex.Message + ". IniciarAceptaciones", tipoLog.ERROR);
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
                if (desconectando)
                {
                    logTrace.EscribirLog("Socket de escucha desconectado porque el programa principal se está cerrando, AceptarConexionCallBack", tipoLog.ERROR);
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
            T estadoDelCliente = adminEstadosCliente.ObtenerUnElemento();
            // limpio el estado para reutilizar solo el saea no los parámetros
            estadoDelCliente.InicializarEstadoDelClienteBase();
            //por precaución se coloca que no se está procesando respuesta
            estadoDelCliente.SeFinalizaProcesoRespuesta();
            // debo colocar la referencia del proceso principal donde genero el estado del cliente para tenerlo como referencia de retorno
            estadoDelCliente.IngresarReferenciaSocketPrincipal(this);
            // Del SAEA de aceptación de conexión, se recupera el socket para asignarlo al estado del cliente obtenido del pool de estados
            estadoDelCliente.SocketDeTrabajo = saea.AcceptSocket;
            //  de la misma forma se ingresa la ip y puerto del cliente que se aceptó
            estadoDelCliente.IpCliente = (saea.AcceptSocket.RemoteEndPoint as IPEndPoint).Address.ToString();
            estadoDelCliente.PuertoCliente = (saea.AcceptSocket.RemoteEndPoint as IPEndPoint).Port;

            // con estas instrucciones puedo controlar las acciones en cada fase del proceso de recepción y envío de ser necesario
            EstadoDelServidorBase.OnAceptacion(estadoDelCliente);

            // se ingresa el cliente a la lista de clientes
            // Monitor proporciona un mecanismo que sincroniza el acceso a datos entre hilos
            bool seSincronzo = Monitor.TryEnter(_clientesConectadosAlServidor, 1000);
            if (seSincronzo)
            {
                try
                {
                    if (!_clientesConectadosAlServidor.ContainsKey(estadoDelCliente.IdUnicoCliente))
                    {
                        _clientesConectadosAlServidor.Add(estadoDelCliente.IdUnicoCliente, estadoDelCliente);
                    }
                    else
                    {
                        logTrace.EscribirLog("Cliente ya registrado", tipoLog.ALERTA);
                    }
                }
                finally
                {
                    Monitor.Exit(_clientesConectadosAlServidor);
                }
            }
            else
            {
                logTrace.EscribirLog("Timeout de 5 seg para obtener bloqueo en AceptarConexionCallBack, listaClientes", tipoLog.ALERTA);
                // si no puedo ingresarlo en la lista de clientes debo rechazarlo porque no tendría control para manipularlo en un futuro
                CerrarSocketCliente(estadoDelCliente);
                // coloco nuevamente el socket en proceso de aceptación con el mismo saea para un reintento de conexión
                this.IniciarAceptaciones(saea);
                return;
            }

            // se inicia la recepción de datos del cliente si es que sigue conectado dicho cliente  
            if (estadoDelCliente.SocketDeTrabajo.Connected)
            {
                try
                {
                    // se ingresa la configuración del buffer para la recepción del mensaje                    
                    estadoDelCliente.saeaDeEnvioRecepcion.SetBuffer(estadoDelCliente.saeaDeEnvioRecepcion.Offset, tamanoBufferPorPeticion);
                    // se procede a la recepción asincrona del mensaje,el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                    bool seHizoAsync = estadoDelCliente.SocketDeTrabajo.ReceiveAsync(estadoDelCliente.saeaDeEnvioRecepcion);
                    if (!seHizoAsync)
                        // se llama a la función que completa el flujo de envío, 
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        RecepcionEnvioEntranteCallBack(estadoDelCliente.SocketDeTrabajo, estadoDelCliente.saeaDeEnvioRecepcion);
                }
                catch (Exception ex)
                {
                    logTrace.EscribirLog(ex.Message + ", AceptarConexionCallBack, recibiendo mensaje del cliente " + estadoDelCliente.IdUnicoCliente, tipoLog.ERROR);
                    CerrarSocketCliente(estadoDelCliente);
                }
            }
            else
            {
                logTrace.EscribirLog("AceptarConexiónCallBack.estadoDelCliente.socketDeTrabajo no conectado", tipoLog.ALERTA);
            }

            // se indica que puede aceptar más solicitudes con el mismo saea que es el principal
            this.IniciarAceptaciones(saea);
        }

        /// <summary>
        /// Operación de callBack que se llama cuando se envía o se recibe de un socket de asincrono para completar la operación
        /// </summary>
        /// <param name="sender">Objeto principal para devolver la llamada</param>
        /// <param name="saea">SocketAsyncEventArg asociado a la operación de envío o recepción</param>        
        private void RecepcionEnvioEntranteCallBack(object sender, SocketAsyncEventArgs saea)
        {
            // obtengo el estado del socket
            // se comprueba que el estado haya sido obtenido correctamente
            if (!(saea.UserToken is T estadoDelCliente))
            {
                logTrace.EscribirLog("estadoDelCliente recibido es inválido para la operacion", tipoLog.ERROR);
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
                            ProcesarRecepcion(estadoDelCliente);
                        }
                        else
                        {
                            //logTrace.EscribirLog("No hay datos que recibir", tipoLog.ALERTA);
                            // si no hay datos por X razón, se cierra el cliente porque puede perdurar indefinidamente la conexión
                            CerrarSocketCliente(estadoDelCliente);
                        }
                    }
                    else
                    {
                        logTrace.EscribirLog("Error en el proceso de recepción, socket no conectado correctamente, cliente:" + estadoDelCliente.IdUnicoCliente + ", " + saea.SocketError.ToString(), tipoLog.ALERTA);
                        //se cierra el cliente porque puede perdurar indefinidamente la conexión
                        CerrarSocketCliente(estadoDelCliente);
                    }
                    break;
                case SocketAsyncOperation.Send:
                    //indico que estaré enviando algo al cliente para que otro proceso con la misma conexión no quiera enviar algo al mismo tiempo
                    estadoDelCliente.esperandoEnvio.Set();
                    // se comprueba que no hay errores con el socket
                    if (saea.SocketError == SocketError.Success)
                    {
                        //Intento colocar el socket de nuevo en escucha por si el cliente envía otra trama con la misma conexión
                        ProcesarRecepcionEnvioCiclicoCliente(estadoDelCliente);
                    }
                    else
                    {
                        logTrace.EscribirLog("el socket no esta conectado correctamente para el cliente " + estadoDelCliente.IdUnicoCliente, tipoLog.ALERTA);
                        estadoDelCliente.SeEstaProcesandoRespuesta();
                        // si no hay datos por X razón, se cierra el cliente porque puede perdurar indefinidamente la conexión                        
                        CerrarSocketCliente(estadoDelCliente);

                    }
                    break;
                default:
                    // se da por errores de TCP/IP en alguna intermitencia
                    logTrace.EscribirLog("La ultima operación no se detecto como de recepcion o envío, RecepcionEnvioEntranteCallBack, " + saea.LastOperation.ToString(), tipoLog.ERROR);
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
            //por precaución se coloca que no se está procesando respuesta
            estadoDelCliente.SeFinalizaProcesoRespuesta();

            //Para ir midiendo el TO por cada recepción
            bool bloqueo = Monitor.TryEnter(estadoDelCliente.FechaInicioTrx, 1000);
            if (bloqueo)
            {
                estadoDelCliente.FechaInicioTrx = DateTime.Now;
            }
            else
            {
                logTrace.EscribirLog("Error al intentar hacer un interbloqueo, ProcesarRecepcion, para ingresar la fecha de inicio del cliente " + estadoDelCliente.IdUnicoCliente, tipoLog.ERROR);
            }

            // se obtiene el SAEA de trabajo
            SocketAsyncEventArgs saeaDeEnvioRecepcion = estadoDelCliente.saeaDeEnvioRecepcion;
            // se obtienen los bytes que han sido recibidos
            Int32 bytesTransferred = saeaDeEnvioRecepcion.BytesTransferred;


            // se obtiene el mensaje y se decodifica para entenderlo
            string mensajeRecibido = Encoding.ASCII.GetString(saeaDeEnvioRecepcion.Buffer, saeaDeEnvioRecepcion.Offset, bytesTransferred);

            try
            {
                logTrace.EscribirLog("Mensaje recibido: " + mensajeRecibido.Trim() + " del cliente: " + estadoDelCliente.IdUnicoCliente, tipoLog.INFORMACION);
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + ". Error al identificar si tiene encabezado el mensaje recibido, se intenta escribir pero se descarta " + mensajeRecibido.Trim() + " del cliente: " + estadoDelCliente.IdUnicoCliente, tipoLog.ERROR);
                CerrarSocketCliente(estadoDelCliente);
                return;
            }


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

            // bloqueo los procesos sobre este mismo cliente hasta no terminar con esta petición para no tener revolturas de mensajes
            estadoDelCliente.esperandoEnvio.Reset();

            try
            {
                // aquí se debe realizar lo necesario con la trama entrante para preparar la trama al proveedor en la variable tramaEnvioProveedor
                estadoDelCliente.ProcesarTrama(mensajeRecibido);
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog("Error al procesar la trama al llamar la función estadoDelCliente.ProcesarTrama" + ex.Message + ". Del cliente: " + estadoDelCliente.IdUnicoCliente, tipoLog.ERROR);
            }

            if (SeVencioTO(estadoDelCliente))
            {
                logTrace.EscribirLog("Se venció el TimeOut para el cliente " + estadoDelCliente.IdUnicoCliente.ToString() + ". Después de procesar la trama", tipoLog.ALERTA);
                estadoDelCliente.CodigoRespuesta = (int)CodigosRespuesta.TimeOutInterno;
                estadoDelCliente.CodigoAutorizacion = 0;
                ResponderAlCliente(estadoDelCliente);
                return;
            }

            try
            {
                // cuando haya terminado la clase estadoDelCliente de procesar la trama, se debe evaluar su éxito para enviar la solicitud al proveedor
                if (estadoDelCliente.CodigoRespuesta == (int)CodigosRespuesta.TransaccionExitosa)
                {
                    if (!modoTest && modoRouter)
                    {
                        // me espero a ver si tengo disponibilidad de SAEA para un proveedor
                        semaforoParaAceptarProveedores.Wait();

                        //Se prepara el estado del proveedor que servirá como operador de envío y recepción de trama
                        SocketAsyncEventArgs saeaProveedor = new SocketAsyncEventArgs();
                        saeaProveedor.Completed += new EventHandler<SocketAsyncEventArgs>(ConexionProveedorCallBack);

                        X estadoDelProveedor = adminEstadosDeProveedor.ObtenerUnElemento();
                        // ingreso la información de peticion para llenar las clases al proveedor
                        estadoDelProveedor.IngresarObjetoPeticionCliente(estadoDelCliente.ObjSolicitud);
                        estadoDelProveedor.EstadoDelClienteOrigen = estadoDelCliente;
                        //por seguridad, se coloca la bandera de vencimiento por TimeOut en false
                        estadoDelProveedor.ReinicioBanderaTimeOut();

                        if (estadoDelProveedor.codigoRespuesta != (int)CodigosRespuesta.TransaccionExitosa)
                        {
                            estadoDelProveedor.codigoAutorizacion = 0;
                            estadoDelProveedor.EstadoDelClienteOrigen.CodigoRespuesta = estadoDelProveedor.codigoRespuesta;
                            estadoDelProveedor.EstadoDelClienteOrigen.CodigoAutorizacion = estadoDelProveedor.codigoAutorizacion;
                            ResponderAlCliente((T)estadoDelProveedor.EstadoDelClienteOrigen);
                            // el SAEA del proveedor se ingresa nuevamente al pool para ser re utilizado
                            adminEstadosDeProveedor.IngresarUnElemento(estadoDelProveedor);
                            // se libera el semaforo por si otra petición está solicitando acceso
                            semaforoParaAceptarProveedores.Release();
                            return;
                        }

                        saeaProveedor.UserToken = estadoDelProveedor;
                        IPAddress iPAddress = IPAddress.Parse(_ipProveedor);

                        bool seSincronzo = Monitor.TryEnter(_puertosProveedor, 1000);
                        IPEndPoint endPointProveedor;
                        if (seSincronzo)
                        {
                            try
                            {
                                if (_contadorPuertos == 0)
                                {
                                    endPointProveedor = new IPEndPoint(iPAddress, _puertosProveedor.First());
                                }
                                else
                                {
                                    endPointProveedor = new IPEndPoint(iPAddress, _puertosProveedor[_contadorPuertos - 1]);
                                }
                            }
                            catch
                            {
                                endPointProveedor = new IPEndPoint(iPAddress, _puertosProveedor.First());
                            }
                            finally
                            {
                                Monitor.Exit(_puertosProveedor);
                            }
                        }
                        else
                        {
                            logTrace.EscribirLog("Timeout de 5 seg para obtener un puerto de listaPuertosProveedor", tipoLog.ERROR);
                            endPointProveedor = new IPEndPoint(iPAddress, _puertosProveedor.First());
                        }


                        if (_contadorPuertos == _puertosProveedor.Count)
                        {
                            Interlocked.Exchange(ref _contadorPuertos, 0);
                        }
                        else
                        {
                            Interlocked.Increment(ref _contadorPuertos);
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
                                ConexionProveedorCallBack(socketDeTrabajoProveedor, saeaProveedor);
                        }
                        catch (Exception ex)
                        {
                            logTrace.EscribirLog(ex.Message + ",ProcesarRecepcion, ConnectAsync, " + saeaProveedor.RemoteEndPoint.ToString() + ", cliente " + estadoDelCliente.IdUnicoCliente, tipoLog.ERROR);

                            socketDeTrabajoProveedor.Close();
                            estadoDelProveedor.codigoRespuesta = (int)CodigosRespuesta.ErrorEnRed;
                            estadoDelProveedor.codigoAutorizacion = 0;
                            estadoDelProveedor.EstadoDelClienteOrigen.CodigoRespuesta = estadoDelProveedor.codigoRespuesta;
                            estadoDelProveedor.EstadoDelClienteOrigen.CodigoAutorizacion = estadoDelProveedor.codigoAutorizacion;
                            ResponderAlCliente((T)estadoDelProveedor.EstadoDelClienteOrigen);
                            // se libera el semaforo por si otra petición está solicitando acceso
                            semaforoParaAceptarProveedores.Release();
                            // el SAEA del proveedor se ingresa nuevamente al pool para ser re utilizado
                            adminEstadosDeProveedor.IngresarUnElemento(estadoDelProveedor);
                        }
                    }
                    else
                    {
                        ResponderAlCliente(estadoDelCliente);
                    }
                }
                // si el código de respuesta es 30(error en el formato) o 50 (Error en algún paso de evaluar la mensajería),
                // se debe responder al cliente, de lo contrario si es un codigo de los anteriores, no se puede responder porque no se tienen confianza en los datos
                else if (estadoDelCliente.CodigoRespuesta != (int)CodigosRespuesta.ErrorFormato && estadoDelCliente.CodigoRespuesta != (int)CodigosRespuesta.ErrorProceso)
                {
                    logTrace.EscribirLog("Error en el proceso de validación de la trama del cliente " + estadoDelCliente.IdUnicoCliente, tipoLog.ALERTA);
                    ResponderAlCliente(estadoDelCliente);
                }
                else
                {
                    CerrarSocketCliente(estadoDelCliente);
                }
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + ". " + ex.StackTrace + ". ProcesarRecepcion, cliente " + estadoDelCliente.IdUnicoCliente, tipoLog.ERROR);
                CerrarSocketCliente(estadoDelCliente);
            }
        }

        /// <summary>
        /// Función que entrega una respuesta al cliente por medio del socket de conexión
        /// </summary>
        /// <param name="estadoDelCliente">Estado del cliente con los valores de retorno</param>
        private void ResponderAlCliente(T estadoDelCliente)
        {
            if (estadoDelCliente == null || estadoDelCliente.seEstaRespondiendo)
            {
                return;
            }

            estadoDelCliente.SeEstaProcesandoRespuesta();

            // trato de obtener la trama que se le responderá al cliente
            estadoDelCliente.ObtenerTramaRespuesta();

            // Si ya se cuenta con una respuesta(s) para el cliente
            if (estadoDelCliente.TramaRespuesta != "")
            {
                if (!modoTest)
                    estadoDelCliente.GuardarTransaccion();

                // se obtiene el mensaje de respuesta que se enviará cliente
                string mensajeRespuesta = estadoDelCliente.TramaRespuesta;
                logTrace.EscribirLog("Mensaje de respuesta: " + mensajeRespuesta + " al cliente " + estadoDelCliente.IdUnicoCliente, tipoLog.INFORMACION);

                // se obtiene la cantidad de bytes de la trama completa
                int numeroDeBytes = Encoding.ASCII.GetBytes(mensajeRespuesta, 0, mensajeRespuesta.Length, estadoDelCliente.saeaDeEnvioRecepcion.Buffer, estadoDelCliente.saeaDeEnvioRecepcion.Offset);
                // si el número de bytes es mayor al buffer que se tiene destinado a la recepción, no se puede proceder, no es válido el mensaje
                if (numeroDeBytes > tamanoBufferPorPeticion)
                {
                    logTrace.EscribirLog("La respuesta es más grande que el buffer, cliente " + estadoDelCliente.IdUnicoCliente, tipoLog.ALERTA);
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
                    logTrace.EscribirLog("Error asignando buffer para la respuesta al cliente " + estadoDelCliente.IdUnicoCliente + ". " + ex.Message, tipoLog.ERROR);
                    return;
                }

                try
                {

                    // se envía asincronamente por medio del socket copia de recepción que es
                    // con el que se está trabajando en esta operación, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                    bool seHizoAsync = estadoDelCliente.SocketDeTrabajo.SendAsync(estadoDelCliente.saeaDeEnvioRecepcion);
                    if (!seHizoAsync)
                        // Si se tiene una respuesta False de que el proceso está pendiente, se completa el flujo,
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        RecepcionEnvioEntranteCallBack(estadoDelCliente.SocketDeTrabajo, estadoDelCliente.saeaDeEnvioRecepcion);
                }
                catch (Exception ex)
                {
                    logTrace.EscribirLog(ex.Message + ", ResponderAlCliente, cliente " + estadoDelCliente.IdUnicoCliente, tipoLog.ERROR);
                    CerrarSocketCliente(estadoDelCliente);
                    return;
                }
            }
            else  // Si el proceso no tuvo una respuesta o se descartó por error, se procede a volver a escuchar para recibir la siguiente trama del mismo cliente
            {
                if (estadoDelCliente.SocketDeTrabajo.Connected)
                {
                    try
                    {
                        // se solicita el espacio de buffer para la recepción del mensaje
                        estadoDelCliente.saeaDeEnvioRecepcion.SetBuffer(estadoDelCliente.saeaDeEnvioRecepcion.Offset, tamanoBufferPorPeticion);
                        // se solicita un proceso de recepción asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                        // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                        bool seHizoAsync = estadoDelCliente.SocketDeTrabajo.ReceiveAsync(estadoDelCliente.saeaDeEnvioRecepcion);
                        if (!seHizoAsync)
                            // si el evento indica que el proceso asincrono está pendiente, se completa el flujo,
                            // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                            // en su evento callback
                            RecepcionEnvioEntranteCallBack(estadoDelCliente.SocketDeTrabajo, estadoDelCliente.saeaDeEnvioRecepcion);
                    }
                    catch (Exception ex)
                    {
                        logTrace.EscribirLog(ex.Message + ", procesarRecepcion, Desconectando cliente " + estadoDelCliente.IdUnicoCliente, tipoLog.ALERTA);
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

            // Una vez terminado el envio, se continua escuchando por el Socket de trabajo
            try
            {
                // se asigna el buffer para continuar el envío
                estadoDelCliente.saeaDeEnvioRecepcion.SetBuffer(estadoDelCliente.saeaDeEnvioRecepcion.Offset, tamanoBufferPorPeticion);
                // se inicia el proceso de recepción asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                bool seHizoAsync = estadoDelCliente.SocketDeTrabajo.ReceiveAsync(estadoDelCliente.saeaDeEnvioRecepcion);
                if (!seHizoAsync)
                    // si el evento indica que el proceso está pendiente, se completa el flujo,
                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                    // en su evento callback
                    RecepcionEnvioEntranteCallBack(estadoDelCliente.SocketDeTrabajo, estadoDelCliente.saeaDeEnvioRecepcion);
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + ", ProcesarRecepcionEnvioCiclicoCliente, cliente " + estadoDelCliente.IdUnicoCliente, tipoLog.ERROR);
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
                logTrace.EscribirLog("estadoDelCliente null", tipoLog.ERROR);
                return;
            }

            // proporciona un mecanismo de sincronización de acceso a datos donde un hilo solo puede tener acceso a un
            // bloque de código a la vez, en este caso en ingresar al listado de clientes, de lo contrario habría 
            // cross threading y provocaría error
            bool bloqueo = Monitor.TryEnter(_clientesConectadosAlServidor, 1000);
            if (bloqueo)
            {
                try
                {
                    // se busca en la lista el cliente y se remueve porque se va a desconectar
                    if (_clientesConectadosAlServidor.ContainsKey(estadoDelCliente.IdUnicoCliente))
                    {
                        _clientesConectadosAlServidor.Remove(estadoDelCliente.IdUnicoCliente);
                    }
                    else
                    {
                        logTrace.EscribirLog("el cliente " + estadoDelCliente.IdUnicoCliente.ToString() + " no se encuentra en listaClientes a desconectar, ya ha sido desconectado en otro proceso", tipoLog.ALERTA);
                        return;     // quiere decir que ya está desconectado
                    }
                }
                catch (Exception ex)
                {
                    logTrace.EscribirLog(ex.Message + " en CerrarSocketCliente, listaClientes, cliente " + estadoDelCliente.IdUnicoCliente, tipoLog.ERROR);
                }
                finally
                {
                    Monitor.Exit(_clientesConectadosAlServidor);
                }
            }
            else
            {
                logTrace.EscribirLog("Error obteniendo el bloqueo, CerrarSocketCliente, listaClientes", tipoLog.ERROR);
            }


            // se obtiene el socket específico del cliente en cuestión
            Socket socketDeTrabajoACerrar = estadoDelCliente.SocketDeTrabajo;

            // se inhabilita y se cierra dicho socket
            try
            {
                socketDeTrabajoACerrar.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + " en CerrarSocketCliente, shutdown de envío en el socket de trabajo del cliente " + estadoDelCliente.IdUnicoCliente, tipoLog.ALERTA);
            }

            try
            {
                socketDeTrabajoACerrar.Close();
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + " en CerrarSocketCliente, close en el socket de trabajo del cliente " + estadoDelCliente.IdUnicoCliente, tipoLog.ERROR);
            }

            // se llama a la secuencia de cerrando para tener un flujo de eventos
            EstadoDelServidorBase.OnClienteCerrado(estadoDelCliente);

            // se libera la instancia de socket de trabajo para reutilizarlo
            adminEstadosCliente.IngresarUnElemento(estadoDelCliente);
            // se marca el semáforo de que puede aceptar otro cliente

            if (semaforoParaAceptarClientes.CurrentCount < numeroConexionesSimultaneasCliente)
            {
                //logTrace.EscribirLog("Se libera semaforoParaAceptarClientes " + semaforoParaAceptarClientes.CurrentCount.ToString() + ", para el cliente " + estadoDelCliente.IdUnicoCliente, tipoLog.ALERTA);
                semaforoParaAceptarClientes.Release();
            }
        }

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
            //X estadoDelProveedor = e.UserToken as X;
            if (e == null)
            {
                logTrace.EscribirLog("estadoDelProveedor recibido es nulo", tipoLog.ERROR);
                return;
            }

            if (!(e.UserToken is X estadoDelProveedor))
            {
                logTrace.EscribirLog("estadoDelProveedor recibido es inválido para la operacion", tipoLog.ERROR);
                return;
            }

            // se valida que existan errores registrados
            if (e.SocketError != SocketError.Success && e.SocketError != SocketError.IsConnected)
            {
                logTrace.EscribirLog("Error en la conexión, ConexionProveedorCallBack " + estadoDelProveedor.endPoint + ", cliente " + estadoDelProveedor.EstadoDelClienteOrigen.IdUnicoCliente, tipoLog.ERROR);

                estadoDelProveedor.codigoRespuesta = (int)CodigosRespuesta.ErrorEnRed;
                estadoDelProveedor.codigoAutorizacion = 0;
                estadoDelProveedor.EstadoDelClienteOrigen.CodigoRespuesta = estadoDelProveedor.codigoRespuesta;
                estadoDelProveedor.EstadoDelClienteOrigen.CodigoAutorizacion = estadoDelProveedor.codigoAutorizacion;

                estadoDelProveedor.GuardarTransaccion();

                ResponderAlCliente((T)estadoDelProveedor.EstadoDelClienteOrigen);
                adminEstadosDeProveedor.IngresarUnElemento(estadoDelProveedor);
                semaforoParaAceptarProveedores.Release();
                return;
            }

            // si todo va bien
            estadoDelProveedor.IngresarReferenciaSocketPrincipal(this);
            // se le indica al estado del proveedor el socket de trabajo
            try
            {
                estadoDelProveedor.SocketDeTrabajo = e.AcceptSocket;
                if (estadoDelProveedor.SocketDeTrabajo == null)
                {
                    throw new Exception("estadoDelProveedor.socketDeTrabajo recibido es inválido para la operacion");
                }
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + "  ConexionProveedorCallBack, obteniendo el socket de trabajo, cliente " + estadoDelProveedor.EstadoDelClienteOrigen.IdUnicoCliente, tipoLog.ERROR);
                estadoDelProveedor.codigoRespuesta = (int)CodigosRespuesta.ErrorEnRed;
                estadoDelProveedor.codigoAutorizacion = 0;
                estadoDelProveedor.EstadoDelClienteOrigen.CodigoRespuesta = estadoDelProveedor.codigoRespuesta;
                estadoDelProveedor.EstadoDelClienteOrigen.CodigoAutorizacion = estadoDelProveedor.codigoAutorizacion;

                estadoDelProveedor.GuardarTransaccion();

                ResponderAlCliente((T)estadoDelProveedor.EstadoDelClienteOrigen);
                adminEstadosDeProveedor.IngresarUnElemento(estadoDelProveedor);
                semaforoParaAceptarProveedores.Release();
                return;
            }

            estadoDelProveedor.saeaDeEnvioRecepcion.UserToken = estadoDelProveedor;

            // obtengo las tramas para considerar cualquier evento antes de enviar la petición al proveedor.
            // se puede actualizar más adelante
            // solo por precaución se inicializan los valores
            estadoDelProveedor.codigoRespuesta = 0;
            estadoDelProveedor.codigoAutorizacion = 0;
            estadoDelProveedor.ObtenerTramaPeticion();

            //Si todas la validaciones hasta ahora fueron exitosas
            // Se guarda  la transacción para posterior actualizarla
            estadoDelProveedor.GuardarTransaccion();

            try
            {
                if (estadoDelProveedor.SocketDeTrabajo.Connected)
                {
                    string mensajeAlProveedor = estadoDelProveedor.tramaSolicitud;
                    logTrace.EscribirLog("Mensaje enviado del proveedor: " + estadoDelProveedor.tramaSolicitud + " para el cliente: " + estadoDelProveedor.EstadoDelClienteOrigen.IdUnicoCliente, tipoLog.INFORMACION);

                    // se obtiene la cantidad de bytes de la trama completa
                    int numeroDeBytes = Encoding.Default.GetBytes(mensajeAlProveedor, 0, mensajeAlProveedor.Length, estadoDelProveedor.saeaDeEnvioRecepcion.Buffer, estadoDelProveedor.saeaDeEnvioRecepcion.Offset);
                    // si el número de bytes es mayor al buffer que se tiene destinado a la recepción, no se puede proceder, no es válido el mensaje
                    if (numeroDeBytes > tamanoBufferPorPeticion)
                    {
                        logTrace.EscribirLog("El mensaje al proveedor es más grande que el buffer, cliente: " + estadoDelProveedor.EstadoDelClienteOrigen.IdUnicoCliente, tipoLog.ALERTA);
                        CerrarSocketProveedor(estadoDelProveedor);
                        return;
                    }

                    // Se prepara el buffer del SAEA con el tamaño predefinido                         
                    estadoDelProveedor.saeaDeEnvioRecepcion.SetBuffer(estadoDelProveedor.saeaDeEnvioRecepcion.Offset, numeroDeBytes);

                    estadoDelProveedor.providerTimer = new Timer(new TimerCallback(TickTimer), estadoDelProveedor, 1000, 1000);

                    // se procede a la recepción asincrona del mensaje,el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                    // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                    bool seHizoAsync = estadoDelProveedor.SocketDeTrabajo.SendAsync(estadoDelProveedor.saeaDeEnvioRecepcion);
                    if (!seHizoAsync)
                        // se llama a la función que completa el flujo de envío, 
                        // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                        // en su evento callback
                        RecepcionEnvioSalienteCallBack(estadoDelProveedor.SocketDeTrabajo, estadoDelProveedor.saeaDeEnvioRecepcion);
                }
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + "ConexionProveedorCallBack, iniciando conexión con el proveedor, cliente " + estadoDelProveedor.EstadoDelClienteOrigen.IdUnicoCliente, tipoLog.ERROR);
                estadoDelProveedor.codigoRespuesta = (int)CodigosRespuesta.ErrorProcesoSockets;
                estadoDelProveedor.codigoAutorizacion = 0;
                estadoDelProveedor.EstadoDelClienteOrigen.CodigoRespuesta = estadoDelProveedor.codigoRespuesta;
                estadoDelProveedor.EstadoDelClienteOrigen.CodigoAutorizacion = estadoDelProveedor.codigoAutorizacion;
                ResponderAlCliente((T)estadoDelProveedor.EstadoDelClienteOrigen);
                CerrarSocketProveedor(estadoDelProveedor);
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
                logTrace.EscribirLog("estadoDelProveedor recibido es inválido para la operacion", tipoLog.ERROR);
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
                        ProcesarRecepcionEnvioCiclicoProveedor(estadoDelProveedor);
                    }
                    else
                    {
                        logTrace.EscribirLog("Error en el envío a " + estadoDelProveedor.saeaDeEnvioRecepcion.RemoteEndPoint + ", RecepcionEnvioSalienteCallBack" + e.SocketError.ToString() +
                            ", cliente " + estadoDelProveedor.EstadoDelClienteOrigen.IdUnicoCliente, tipoLog.ERROR);
                        estadoDelProveedor.codigoRespuesta = (int)CodigosRespuesta.SinRespuestaCarrier;
                        estadoDelProveedor.codigoAutorizacion = 0;
                        estadoDelProveedor.EstadoDelClienteOrigen.CodigoRespuesta = estadoDelProveedor.codigoRespuesta;
                        estadoDelProveedor.EstadoDelClienteOrigen.CodigoAutorizacion = estadoDelProveedor.codigoAutorizacion;
                        ResponderAlCliente((T)estadoDelProveedor.EstadoDelClienteOrigen);
                        if (!estadoDelProveedor.SeVencioElTimeOut)
                            CerrarSocketProveedor(estadoDelProveedor);
                    }
                    break;
                case SocketAsyncOperation.Receive:

                    // se comprueba que exista información
                    if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                    {
                        // se procesa la solicitud
                        ProcesarRecepcion(estadoDelProveedor);
                    }
                    else
                    {
                        estadoDelProveedor.codigoRespuesta = (int)CodigosRespuesta.SinRespuestaCarrier;
                        estadoDelProveedor.codigoAutorizacion = 0;
                        estadoDelProveedor.EstadoDelClienteOrigen.CodigoRespuesta = estadoDelProveedor.codigoRespuesta;
                        estadoDelProveedor.EstadoDelClienteOrigen.CodigoAutorizacion = estadoDelProveedor.codigoAutorizacion;
                        ResponderAlCliente((T)estadoDelProveedor.EstadoDelClienteOrigen);
                        if (!estadoDelProveedor.SeVencioElTimeOut)
                            CerrarSocketProveedor(estadoDelProveedor);
                    }
                    break;
                default:
                    logTrace.EscribirLog("La ultima operación no se detecto como de recepcion o envío, RecepcionEnvioSalienteCallBack, " + e.LastOperation.ToString(), tipoLog.ALERTA);
                    estadoDelProveedor.codigoRespuesta = (int)CodigosRespuesta.ErrorEnRed;
                    estadoDelProveedor.codigoAutorizacion = 0;
                    estadoDelProveedor.EstadoDelClienteOrigen.CodigoRespuesta = estadoDelProveedor.codigoRespuesta;
                    estadoDelProveedor.EstadoDelClienteOrigen.CodigoAutorizacion = estadoDelProveedor.codigoAutorizacion;
                    ResponderAlCliente((T)estadoDelProveedor.EstadoDelClienteOrigen);
                    //CerrarSocketProveedor(estadoDelProveedor);
                    break;
            }
        }

        /// <summary>
        /// Función para procesar el envío al proveedor y dejar de nuevo en escucha al socket
        /// </summary>
        /// <param name="estadoDelProveedor">Estado del proveedor con la información de conexión</param>
        private void ProcesarRecepcionEnvioCiclicoProveedor(X estadoDelProveedor)
        {
            if (estadoDelProveedor == null)
            {
                logTrace.EscribirLog("estadoDelProveedor es inválido para la operacion", tipoLog.ERROR);
                return;
            }

            // Una vez terminado el envio, se continua escuchando por el Socket de trabajo
            try
            {
                // se asigna el buffer para continuar el envío
                estadoDelProveedor.saeaDeEnvioRecepcion.SetBuffer(estadoDelProveedor.saeaDeEnvioRecepcion.Offset, tamanoBufferPorPeticion);
                // se inicia el proceso de recepción asincrono, el proceso asincrono responde con true cuando está pendiente; es decir, no se ha completado en su callback
                // si regresa un false su operación asincrona no se realizó por lo tanto forzamos su recepción sincronamente
                bool seHizoAsync = estadoDelProveedor.SocketDeTrabajo.ReceiveAsync(estadoDelProveedor.saeaDeEnvioRecepcion);
                if (!seHizoAsync)
                    // si el evento indica que el proceso está pendiente, se completa el flujo,
                    // de manera forzada ya que se tiene asignado un manejador de eventos a esta función
                    // en su evento callback
                    RecepcionEnvioSalienteCallBack(estadoDelProveedor.SocketDeTrabajo, estadoDelProveedor.saeaDeEnvioRecepcion);
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog("Error al ponerse en espera de respuesta del proveedor: " + ex.Message + ", ProcesarRecepcionEnvioCiclicoProveedor, cliente " + estadoDelProveedor.EstadoDelClienteOrigen.IdUnicoCliente, tipoLog.ERROR);
                estadoDelProveedor.codigoRespuesta = (int)CodigosRespuesta.ErrorProcesoSockets;
                estadoDelProveedor.codigoAutorizacion = 0;
                estadoDelProveedor.EstadoDelClienteOrigen.CodigoRespuesta = estadoDelProveedor.codigoRespuesta;
                estadoDelProveedor.EstadoDelClienteOrigen.CodigoAutorizacion = estadoDelProveedor.codigoAutorizacion;
                ResponderAlCliente((T)estadoDelProveedor.EstadoDelClienteOrigen);
                CerrarSocketProveedor(estadoDelProveedor);
            }

        }

        /// <summary>
        /// Función que realiza la recepción del mensaje y lo procesa
        /// </summary>
        /// <param name="estadoDelProveedor">Estado del proveedor con la información de conexión</param>
        private void ProcesarRecepcion(X estadoDelProveedor)
        {
            if (estadoDelProveedor == null)
            {
                logTrace.EscribirLog("estadoDelProveedor es inválido para la operacion", tipoLog.ERROR);
                return;
            }

            // se obtiene el SAEA de recepción
            SocketAsyncEventArgs saeaRecepcion = estadoDelProveedor.saeaDeEnvioRecepcion;
            // se obtienen los bytes que han sido recibidos
            int bytesTransferred = saeaRecepcion.BytesTransferred;

            // se obtiene el mensaje y se decodifica
            string mensajeRecibido = Encoding.ASCII.GetString(saeaRecepcion.Buffer, saeaRecepcion.Offset, bytesTransferred);

            // el mensaje recibido llevará un proceso, que no debe ser llevado por el core, se coloca en la función virtual
            // para que se consuma en otra capa, se procese y se entregue una respuesta
            try
            {
                logTrace.EscribirLog("Mensaje recibido del proveedor: " + mensajeRecibido.Substring(2) + " para el cliente: " + estadoDelProveedor.EstadoDelClienteOrigen.IdUnicoCliente, tipoLog.INFORMACION);
                estadoDelProveedor.ProcesarTramaDelProveeedor(mensajeRecibido);
                estadoDelProveedor.ObtenerTramaRespuesta();
            }
            catch (Exception ex)
            {
                estadoDelProveedor.codigoRespuesta = (int)CodigosRespuesta.ErrorProceso;
                estadoDelProveedor.codigoAutorizacion = 0;
                logTrace.EscribirLog(ex.Message + ", procesando trama del proveedor, ProcesarRecepcion, cliente " + estadoDelProveedor.EstadoDelClienteOrigen.IdUnicoCliente, tipoLog.ERROR);
                return;
            }

            if (estadoDelProveedor.tramaRespuesta == "")
            {
                estadoDelProveedor.codigoRespuesta = (int)CodigosRespuesta.ErrorProceso;
                estadoDelProveedor.codigoAutorizacion = 0;
            }
            estadoDelProveedor.EstadoDelClienteOrigen.CodigoRespuesta = estadoDelProveedor.codigoRespuesta;
            estadoDelProveedor.EstadoDelClienteOrigen.CodigoAutorizacion = estadoDelProveedor.codigoAutorizacion;
            ResponderAlCliente((T)estadoDelProveedor.EstadoDelClienteOrigen);
            CerrarSocketProveedor(estadoDelProveedor);
        }

        /// <summary>
        /// Cierra el socket asociado a un proveedor y retira al proveedor de la lista de conectados
        /// </summary>
        public void CerrarSocketProveedor(X estadoDelProveedor)
        {
            // Se comprueba que la información del socket de trabajo sea null, ya que podría ser invocado como resultado 
            // de una operación de E / S sin valores
            if (estadoDelProveedor == null) return;

            if (estadoDelProveedor.SocketDeTrabajo == null) return;

            // se obtiene el socket específico del cliente en cuestión
            Socket socketDeTrabajoACerrar = estadoDelProveedor.SocketDeTrabajo;

            if (socketDeTrabajoACerrar.Connected)
            {
                // se inhabilita y se cierra dicho socket
                try
                {
                    socketDeTrabajoACerrar.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex)
                {
                    logTrace.EscribirLog(ex.Message + " en CerrarSocketProveedor, shutdown de envio el socket de trabajo del proveedor " + estadoDelProveedor.EstadoDelClienteOrigen.IdUnicoCliente, tipoLog.ERROR);
                }

                try
                {
                    socketDeTrabajoACerrar.Close();
                }
                catch (Exception ex)
                {
                    logTrace.EscribirLog(ex.Message + " en CerrarSocketProveedor, close el socket de trabajo del proveedor " + estadoDelProveedor.EstadoDelClienteOrigen.IdUnicoCliente, tipoLog.ERROR);
                }
            }

            // se libera la instancia de socket de trabajo para reutilizarlo
            adminEstadosDeProveedor.IngresarUnElemento(estadoDelProveedor);
            // se marca el semáforo de que puede aceptar otro cliente
            if (this.semaforoParaAceptarProveedores.CurrentCount < this.numeroConexionesSimultaneasProveedor)
            {
                //logTrace.EscribirLog("Se libera semaforoParaAceptarProveedores " + semaforoParaAceptarProveedores.CurrentCount.ToString() + ", para el cliente " + estadoDelProveedor.estadoDelClienteOrigen.IdUnicoCliente, tipoLog.ALERTA);
                this.semaforoParaAceptarProveedores.Release();
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

        #region Timeout

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
                    if (estadoDelProveedor.EstadoDelClienteOrigen.seEstaRespondiendo)
                    {
                        estadoDelProveedor.providerTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        estadoDelProveedor.providerTimer.Dispose();
                    }
                    else if (SeVencioTO((T)estadoDelProveedor.EstadoDelClienteOrigen))
                    {
                        TimeSpan timeSpan = DateTime.Now - estadoDelProveedor.EstadoDelClienteOrigen.FechaInicioTrx;
                        logTrace.EscribirLog("Se venció el TimeOut para el proveedor: " +
                                estadoDelProveedor.EstadoDelClienteOrigen.IdUnicoCliente +
                                ", TickTimer, fecha hora inicial " + estadoDelProveedor.EstadoDelClienteOrigen.FechaInicioTrx +
                                ", segundos transcurridos " + timeSpan.Seconds +
                                ", TimeOut configurado " + estadoDelProveedor.EstadoDelClienteOrigen.TimeOut, tipoLog.ALERTA);

                        estadoDelProveedor.providerTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        estadoDelProveedor.providerTimer.Dispose();

                        estadoDelProveedor.codigoRespuesta = (int)CodigosRespuesta.SinRespuestaCarrier;
                        estadoDelProveedor.codigoAutorizacion = 0;
                        estadoDelProveedor.EstadoDelClienteOrigen.CodigoRespuesta = estadoDelProveedor.codigoRespuesta;
                        estadoDelProveedor.EstadoDelClienteOrigen.CodigoAutorizacion = estadoDelProveedor.codigoAutorizacion;
                        estadoDelProveedor.IndicarVencimientoPorTimeOut();
                        CerrarSocketProveedor(estadoDelProveedor);
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
        private bool SeVencioTO(T estadoDelCliente)
        {
            try
            {
                TimeSpan timeSpan = DateTime.Now - estadoDelCliente.FechaInicioTrx;
                return timeSpan.Seconds > estadoDelCliente.TimeOut;
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + ", SeVencioTOCliente, cliente " + estadoDelCliente.IdUnicoCliente, tipoLog.ALERTA);
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
        public void EnvioInfoSincro(string mensaje, SocketAsyncEventArgs e)
        {
            T socketDeTrabajoInfoCliente = e.UserToken as T;
            Byte[] bufferEnvio;
            bufferEnvio = Encoding.ASCII.GetBytes(mensaje);

            if (socketDeTrabajoInfoCliente.SocketDeTrabajo.Connected)
            {
                socketDeTrabajoInfoCliente.SocketDeTrabajo.Send(bufferEnvio);
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
            desconectando = true;
            List<T> listaDeClientesEliminar = new List<T>();

            // Primero se detiene y se cierra el socket de escucha
            try
            {
                this.socketDeEscucha.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + " en detenerServidor.Shutdown", tipoLog.ERROR);
            }

            try
            {
                socketDeEscucha.Close();
            }
            catch (Exception ex)
            {
                logTrace.EscribirLog(ex.Message + " en detenerServidor.Close", tipoLog.ERROR);
            }


            // se recorre la lista de clientes conectados y se adiciona a la lista de clientes para desconectar
            foreach (T socketDeTrabajoPorCliente in _clientesConectadosAlServidor.Values)
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
            _clientesConectadosAlServidor.Clear();

            _enEjecucion = false;
            desconectando = false;
        }

        #endregion


    }
}