using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServidorCore
{
    public class EstadoDelProveedorBase
    {
        EstadoDelClienteBase EstadoDelClienteBase { get; set; }

        /// <summary>
        /// Identificador único para un proveedor
        /// </summary>
        public Guid idUnicoProveedor { get; set; }

        /// <summary>
        /// Referencia al servidor de socket principal
        /// </summary>
        public object referenciaSocketPrincipal;

        /// <summary>
        /// SocketAsyncEventArgs que se utilizará en la recepción
        /// </summary>
        internal SocketAsyncEventArgs saeaDeEnvioRecepcion;

        /// <summary>        
        /// Variable que indica el estado del parseo del mensaje
        /// </summary>
        /// todo: ver si esta variable si es para esto
        public int estadoDeParseo;

        /// <summary>        
        /// Secuencia de respuestas (Respuesta1\r\Respuesta2\r\n...RespuestaN\r\n)
        /// </summary>
        public string secuenciaDeRespuestasDelProveedor;

        /// <summary>
        /// Variable que marca un error en el parseo del mensaje de una solicitud, True = error
        /// </summary>
        public bool errorParseando;

        /// <summary>
        /// Variable que almacena el ultimo error detectado en el cliente, lo utilizo como bitácora
        /// </summary>
        public string ultimoErrorDeParseo;

        /// <summary>
        /// como unicamente debo y puedo mandar un paquete a la vez, si existen muchos se debe tener una cola de envío
        /// </summary>
        public Queue<string> colaEnvio;

        /// <summary>
        /// Objeto para sincronizar envíos
        /// </summary>
        public object sincronizarEnvio = new object();

        /// <summary>
        /// bandera para indicar que se está realizando un envío
        /// </summary>
        public bool seEstaEnviandoAlgo;

        /// <summary>        
        /// evento para sincronización de procesos, con este manejador de evento controlo
        /// el flujo cuando el fin de un envío ocurre
        /// </summary>
        internal EventWaitHandle esperandoEnvio;

        /// <summary>
        /// Ip del proveedor
        /// </summary>
        public string ipProveedor { get; set; }

        /// <summary>
        /// Puerto del proveedor
        /// </summary>
        public Int32 puertoProveedor { get; set; }

        /// <summary>
        /// Fecha y hora de conexión al proveedor
        /// </summary>
        public DateTime fechaHoraConexionProveedor { get; set; }

        /// <summary>
        /// ultimo error en la conexión al proveedor, se utiliza como bitácora
        /// </summary>
        public string ultimoErrorConexion;

        /// <summary>
        /// Ultimo mensaje enviado al cliente
        /// </summary>
        public string ultimoMensajeAlProveedor;

        /// <summary>
        /// Fecha y hora del ultimo mensaje al Proveedor
        /// </summary>
        public DateTime fechaHoraUltimoMensajeAlProveedor { get; set; }

        /// <summary>
        /// Ultimo mensaje recibido del Proveedor
        /// </summary>
        public string ultimoMensajeRecibido;

        /// <summary>
        /// Fecha y hora del ultimo mensaje recibido del Proveedor
        /// </summary>
        public DateTime fechaHoraUltimoMensajeRecibidoDelProveedor { get; set; }

        /// <summary>
        /// Socket asignado de trabajo sobre la conexión del cliente
        /// </summary>
        public Socket socketDeTrabajo { get; set; }

        ///// <summary>
        ///// Experimental para ciertos protocolos, aún se prueba su función
        ///// </summary>
        //public NextOperationModes NextOperation { get; set; }

        /// <summary>
        /// Variable que se utiliza temporalmente para almacenar en un log
        /// </summary>
        public string LogTemporal;

        public EstadoDelClienteBase estadoDelClienteOrigen { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public EstadoDelProveedorBase()
        {
            // waitSend = new AutoResetEvent(true);
            esperandoEnvio = new ManualResetEvent(true);
            // se separa del constructor debido a  que  la inicialización de puede usar nuevamente sin hacer una nueva instancia
            InicializarEstadoDelProveedorBase();
        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, sirve para limpiar e inicializar 
        /// todas las variables del info y socket de trabajo
        /// </summary>
        public virtual void InicializarEstadoDelProveedorBase()
        {
            referenciaSocketPrincipal = null;
            estadoDeParseo = 0;
            //entradaAcumulativa = "";
            //mensajeRecibidoAux = "";
            secuenciaDeRespuestasDelProveedor = "";
            errorParseando = false;
            ultimoErrorDeParseo = "";
            ultimoErrorConexion = "";
            ultimoMensajeAlProveedor = "";
            fechaHoraUltimoMensajeAlProveedor = DateTime.MaxValue;
            ultimoMensajeRecibido = "";
            fechaHoraUltimoMensajeRecibidoDelProveedor = DateTime.MaxValue;
            puertoProveedor = 0;
            colaEnvio = new Queue<string>();
            seEstaEnviandoAlgo = false;
            esperandoEnvio.Set();            
            idUnicoProveedor= Guid.NewGuid();
            ipProveedor = "";
            fechaHoraConexionProveedor = DateTime.Now;
            socketDeTrabajo = null;            
        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, en esta se controla
        /// toda la operación sobre el mensaje del cliente así como su mensaje de respuesta
        /// </summary>
        /// <param name="mensajeCliente">Mensaje que se recibe de un cliente</param>
        public virtual void ProcesarTramaDelProveeedor(string mensaje)
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
    }
}
