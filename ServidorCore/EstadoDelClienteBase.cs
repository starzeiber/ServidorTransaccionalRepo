using System;
using System.Net.Sockets;
using System.Threading;

namespace UServerCore
{

    /// <summary>
    /// Clase contiene toda la información relevante de un cliente así como un socket
    /// que será el de trabajo para el envío y recepción de mensajes
    /// </summary>
    public class EstadoDelClienteBase
    {
        /// <summary>
        /// Identificador único para un cliente
        /// </summary>
        public Guid IdUnicoCliente { get; set; }

        /// <summary>
        /// Referencia al servidor de socket principal
        /// </summary>
        public object referenciaSocketPrincipal;

        /// <summary>
        /// SocketAsyncEventArgs que se utilizará en la recepción
        /// </summary>
        internal SocketAsyncEventArgs saeaDeEnvioRecepcion;

        /// <summary>        
        /// trama de respuesta al cliente
        /// </summary>
        public string tramaRespuesta;

        /// <summary>        
        /// evento para sincronización de procesos, con este manejador de evento controlo
        /// el flujo cuando el fin de un envío ocurre
        /// </summary>
        internal EventWaitHandle esperandoEnvio;

        /// <summary>
        /// Ip del cliente
        /// </summary>
        public string IpCliente { get; set; } = "127.0.0.0";

        /// <summary>
        /// Puerto del cliente
        /// </summary>
        public Int32 PuertoCliente { get; set; } = 0;

        /// <summary>
        /// Socket asignado de trabajo sobre la conexión del cliente
        /// </summary>
        public Socket socketDeTrabajo { get; set; }

        /// <summary>
        /// Codigo de respuesta sobre el proceso del cliente
        /// </summary>
        public int codigoRespuesta;

        /// <summary>
        /// Codigo de autorización sobre el proceso del cliente
        /// </summary>
        public int codigoAutorizacion;

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de petición de un cliente
        /// </summary>
        public object objSolicitud;

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de respuesta de un cliente
        /// </summary>
        public object objRespuesta;

        public object objSolicitudProveedor;
        public object objRespuestaProveedor;

        /// <summary>
        /// Fecha marcada como inicio de operaciones con el cliente
        /// </summary>
        public DateTime fechaInicioTrx { get; set; } = DateTime.Now;

        /// <summary>
        /// Tiempo de espera general del lado del cliente
        /// </summary>
        public int timeOut;

        /// <summary>
        /// Bandera para identificar si el proceso solo es de consulta sobre una transacción
        /// </summary>
        public bool esConsulta;

        ///// <summary>
        ///// Bandera  para indicar que el proceso de responder se ha concluido correctamente
        ///// </summary>
        //public bool seHaRespondido { get; set; } = false;

        public bool seEstaRespondiendo;

        public int idTrxBD;

        private readonly object objetoDeBloqueo = new object();


        /// <summary>
        /// Constructor
        /// </summary>
        public EstadoDelClienteBase()
        {
            esperandoEnvio = new ManualResetEvent(true);
            // se separa del constructor debido a  que  la inicialización de puede usar nuevamente sin hacer una nueva instancia
            InicializarEstadoDelClienteBase();
        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, sirve para limpiar e inicializar 
        /// todas las variables del info y socket de trabajo
        /// </summary>
        public virtual void InicializarEstadoDelClienteBase()
        {
            IdUnicoCliente = Guid.NewGuid();
            referenciaSocketPrincipal = null;
            tramaRespuesta = "";
            esperandoEnvio.Set();
            socketDeTrabajo = null;
            codigoRespuesta = 0;
            codigoAutorizacion = 0;
            objSolicitud = null;
            objRespuesta = null;
            timeOut = Configuracion.timeOutCliente;
            esConsulta = false;
            //este no porque hay una función con lock para hacerlo seEstaRespondiendo = false;
        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, en esta se controla
        /// toda la operación sobre el mensaje del cliente así como su mensaje de respuesta
        /// </summary>
        /// <param name="mensajeCliente">Mensaje que se recibe de un cliente</param>
        public virtual void ProcesarTrama(string mensajeCliente)
        {
        }

        /// <summary>
        /// Funcion en la que se va a indicar cuál fue el socket principal sobre el cual
        /// se inició toda la operación
        /// </summary>
        /// <param name="socketPrincipal"> proceso donde se encuentra el socket principal del cuál se desprende el socket de trabajo por cliente</param>
        public void IngresarReferenciaSocketPrincipal(object socketPrincipal)
        {
            this.referenciaSocketPrincipal = socketPrincipal;
        }

        /// <summary>
        /// Función para obtener la trama de respuesta al cliente dependiendo de su mensajería entrante
        /// </summary>
        public virtual void ObtenerTramaRespuesta()
        {

        }

        /// <summary>
        /// Función que guardará el resultado de la transacción
        /// </summary>
        public virtual void ActualizarTransaccion()
        {

        }

        public void SeEstaProcesandoRespuesta()
        {
            lock (objetoDeBloqueo)
                if (!seEstaRespondiendo) seEstaRespondiendo = true;
        }

        public void SeFinalizaProcesoRespuesta()
        {
            lock (objetoDeBloqueo)
                if (seEstaRespondiendo) seEstaRespondiendo = false;
        }
    }
}
