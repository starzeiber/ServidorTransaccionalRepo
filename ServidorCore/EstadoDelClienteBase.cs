using System;
using System.Net.Sockets;
using System.Threading;

namespace ServerCore
{

    /// <summary>
    /// Clase contiene toda la información relevante de un cliente así como un socket
    /// que será el de trabajo para el envío y recepción de mensajes
    /// </summary>
    public class EstadoDelClienteBase : IDisposable
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

        public string msg210 = "";
        public string msg230 = "";

        private readonly object objetoDeBloqueo = new object();
        private bool disposed = false;


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
            // Liberar y limpiar el socket si existe
            if (socketDeTrabajo != null)
            {
                try { socketDeTrabajo.Shutdown(SocketShutdown.Both); } catch { }
                try { socketDeTrabajo.Close(); } catch { }
                try { socketDeTrabajo.Dispose(); } catch { }
                socketDeTrabajo = null;
            }

            // Limpiar el buffer del SAEA si aplica
            if (saeaDeEnvioRecepcion != null)
            {
                saeaDeEnvioRecepcion.AcceptSocket = null;
                //No puedo liberar el buffer porque lo administra el core
                //saeaDeEnvioRecepcion.UserToken = null;
                // El buffer se libera en el core con AdminBuffer.LiberarBuffer
            }

            // Limpiar otros datos de sesión
            IdUnicoCliente = Guid.NewGuid();
            esperandoEnvio.Set();
            tramaRespuesta = "";
            objSolicitud = null;
            objRespuesta = null;
            objSolicitudProveedor = null;
            objRespuestaProveedor = null;
            codigoRespuesta = 0;
            codigoAutorizacion = 0;
            fechaInicioTrx = DateTime.Now;
            timeOut = Configuracion.timeOutCliente;
            esConsulta = false;
            seEstaRespondiendo = false;
            idTrxBD = 0;
            msg210 = "";
            msg230 = "";
            referenciaSocketPrincipal = null;
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

        /// <summary>
        /// 
        /// </summary>
        public void SeEstaProcesandoRespuesta()
        {
            lock (objetoDeBloqueo)
                if (!seEstaRespondiendo) seEstaRespondiendo = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SeFinalizaProcesoRespuesta()
        {
            lock (objetoDeBloqueo)
                if (seEstaRespondiendo) seEstaRespondiendo = false;
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
                    saeaDeEnvioRecepcion?.Dispose();
                    saeaDeEnvioRecepcion = null;

                    if (socketDeTrabajo != null)
                    {
                        try { socketDeTrabajo.Shutdown(SocketShutdown.Both); } catch { }
                        socketDeTrabajo.Close();
                        socketDeTrabajo.Dispose();
                        socketDeTrabajo = null;
                    }
                }
                disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the instance of the <see cref="EstadoDelClienteBase"/> class.
        /// </summary>
        /// <remarks>This destructor ensures that unmanaged resources are released by calling the <see
        /// cref="Dispose(bool)"/> method.</remarks>
        ~EstadoDelClienteBase()
        {
            Dispose(false);
        }
    }
}
