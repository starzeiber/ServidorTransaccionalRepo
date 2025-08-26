using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerCore
{
    /// <summary>
    /// Clase que contiene las propiedades de un proveedor en el flujo del servidor
    /// </summary>
    public class EstadoDelProveedorBase : IDisposable
    {

        /// <summary>
        /// Referencia al servidor de socket principal
        /// </summary>
        public object referenciaSocketPrincipal;

        /// <summary>
        /// SocketAsyncEventArgs que se utilizará en la recepción
        /// </summary>
        internal SocketAsyncEventArgs saeaDeEnvioRecepcion;

        /// <summary>
        /// Ip del proveedor
        /// </summary>
        public string ipProveedor { get; set; } = "127.0.0.0";

        /// <summary>
        /// Puerto del proveedor
        /// </summary>
        public Int32 puertoProveedor { get; set; } = 0;

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
        /// Estado del cliente desde donde proviene la petición para un retorno
        /// </summary>
        public EstadoDelClienteBase estadoDelClienteOrigen { get; set; }

        /// <summary>
        /// Trama de petición a un proveedor
        /// </summary>
        public string tramaSolicitud;

        /// <summary>
        /// Trama de respuesta de un proveedor
        /// </summary>
        public string tramaRespuesta;

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de petición de un proveedor
        /// </summary>
        public object objSolicitud;

        /// <summary>
        /// Objeto genérico donde se almacena la clase donde se encuentran los valores de respuesta de un proveedor
        /// </summary>
        public object objRespuesta;


        /// <summary>
        /// Represents a timer used to trigger events or actions at specified intervals.
        /// </summary>
        /// <remarks>This timer can be used to schedule recurring tasks or delayed actions. Ensure proper
        /// disposal of the timer to release resources when it is no longer needed.</remarks>
        public Timer providerTimer;

        /// <summary>
        /// Bandera para indicar que hubo un vencimiento de TimeOut  y poder controlar la respuesta
        /// </summary>
        internal bool seVencioElTimeOut { get; set; } = false;


        private readonly object objetoDeBloqueo = new object();


        internal IPEndPoint endPoint;


        private bool disposed = false;



        /// <summary>
        /// Constructor
        /// </summary>
        public EstadoDelProveedorBase()
        {
            // se separa del constructor debido a  que  la inicialización de puede usar nuevamente sin hacer una nueva instancia
            InicializarEstadoDelProveedorBase();
        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, sirve para limpiar e inicializar 
        /// todas las variables del info y socket de trabajo
        /// </summary>
        public virtual void InicializarEstadoDelProveedorBase()
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

            // Liberar y limpiar el timer si existe
            if (providerTimer != null)
            {
                try { providerTimer.Dispose(); } catch { }
                providerTimer = null;
            }


            referenciaSocketPrincipal = null;
            codigoRespuesta = 0;
            codigoAutorizacion = 0;
            tramaSolicitud = "";
            tramaRespuesta = "";
            estadoDelClienteOrigen = null;
            objSolicitud = null;
            objRespuesta = null;
            endPoint = null;

        }

        /// <summary>
        /// Ingresa de forma segura el valor de la instancia de socket principal para un retorno de flujo
        /// </summary>
        /// <param name="obj"></param>
        public virtual void IngresarObjetoPeticionCliente(object obj)
        {

        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, en esta se controla
        /// toda la operación sobre el mensaje del cliente así como su mensaje de respuesta
        /// </summary>
        public virtual void ProcesarTramaDelProveeedor(string trama)
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
        /// Función que obtiene la trama de petición al proveedor
        /// </summary>
        public virtual void ObtenerTramaPeticion()
        {

        }

        /// <summary>
        /// Función que obtiene la trama de respuesta de una proveedor
        /// </summary>
        public virtual void ObtenerTramaRespuesta()
        {

        }

        /// <summary>
        /// Función que guardará la operación con el proveedor
        /// </summary>
        public virtual void GuardarTransaccion()
        {

        }

        /// <summary>
        /// Marks the operation as timed out if it has not already been marked.
        /// </summary>
        /// <remarks>This method is thread-safe and ensures that the timeout state is set only
        /// once.</remarks>
        public void IndicarVencimientoPorTimeOut()
        {
            lock (objetoDeBloqueo)
                if (!seVencioElTimeOut) seVencioElTimeOut = true;

        }

        /// <summary>
        /// Resets the timeout flag to indicate that the timeout condition is no longer met.
        /// </summary>
        /// <remarks>This method is thread-safe and ensures that the timeout flag is reset only when it
        /// has been set.  It should be called to clear the timeout state after handling a timeout condition.</remarks>
        public void ReinicioBanderaTimeOut()
        {
            lock (objetoDeBloqueo)
                if (seVencioElTimeOut) seVencioElTimeOut = false;
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
                    if (saeaDeEnvioRecepcion != null)
                    {
                        saeaDeEnvioRecepcion.Dispose();
                        saeaDeEnvioRecepcion = null;
                    }
                    if (socketDeTrabajo != null)
                    {
                        try { socketDeTrabajo.Shutdown(SocketShutdown.Both); } catch { }
                        socketDeTrabajo.Close();
                        socketDeTrabajo.Dispose();
                        socketDeTrabajo = null;
                    }
                }
                // Liberar recursos no administrados aquí si los hubiera
                disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the instance of the <see cref="EstadoDelProveedorBase"/> class.
        /// </summary>
        /// <remarks>This destructor ensures that unmanaged resources are released by calling the <see
        /// cref="Dispose(bool)"/> method.</remarks>
        ~EstadoDelProveedorBase()
        {
            Dispose(false);
        }

    }
}
