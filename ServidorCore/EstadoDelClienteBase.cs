using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServidorCore
{
    // Enumerado experimental para ciertos protocolos, aún se prueba su función
    public enum NextOperationModes { Normal, WaitForData, Idle };

    /// <summary>
    /// Clase contiene toda la información relevante de un cliente así como un socket
    /// que será el de trabajo para el envío y recepción de mensajes
    /// </summary>
    public class EstadoDelClienteBase
    {
        /// <summary>
        /// Identificador único para un cliente
        /// </summary>
        public Guid idUnicoCliente { get; set; }

        /// <summary>
        /// Referencia al servidor de socket principal
        /// </summary>
        public object referenciaSocketPrincipal;

        /// <summary>
        /// SocketAsyncEventArgs que se utilizará en la recepción
        /// </summary>
        internal SocketAsyncEventArgs saeaDeRecepcion;

        /// <summary>
        /// SocketAsyncEventArgs que se utilizará en el envío
        /// </summary>
        internal SocketAsyncEventArgs saeaDeEnvio;

        /// <summary>        
        /// Variable que indica el estado del parseo del mensaje
        /// </summary>
        /// todo: ver si esta variable si es para esto
        public int estadoDeParseo;

        /// <summary>
        /// Cummulative received command char by char
        /// Todo: ver para qué se utiliza
        /// </summary>
        protected string parseCurrentInputCmd;

        /// <summary>        
        /// Variable de apoyo para trabajar el mensaje que se recibe y no el original
        /// </summary>
        protected string mensajeRecibidoAux;

        /// <summary>        
        /// Secuencia de respuestas (Respuesta1\r\Respuesta2\r\n...RespuestaN\r\n)
        /// </summary>
        public string secuenciaDeRespuestasAlCliente;

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
        /// Ip del cliente
        /// </summary>
        public string ipCliente { get; set; }

        /// <summary>
        /// Puerto del cliente
        /// </summary>
        public Int32 puertoCliente { get; set; }

        /// <summary>
        /// Fecha y hora de conexión del cliente
        /// </summary>
        public DateTime fechaHoraConexionCliente { get; set; }

        /// <summary>
        /// ultimo error en la conexión del cliente, se utiliza como bitácora
        /// </summary>
        public string ultimoErrorConexionCliente;

        /// <summary>
        /// Ultimo mensaje enviado al cliente
        /// </summary>
        public string ultimoMensajeAlCliente;

        /// <summary>
        /// Fecha y hora del ultimo mensaje al cliente
        /// </summary>
        public DateTime fechaHoraUltimoMensajeAlCliente { get; set; }

        /// <summary>
        /// Ultimo mensaje recibido del cliente
        /// </summary>
        public string ultimoMensajeRecibidoCliente;

        /// <summary>
        /// Fecha y hora del ultimo mensaje recibido del cliente
        /// </summary>
        public DateTime fechaHoraUltimoMensajeRecibidoCliente { get; set; }

        /// <summary>
        /// Socket asignado de trabajo sobre la conexión del cliente
        /// </summary>
        public Socket socketDeTrabajo { get; set; }

        /// <summary>
        /// Experimental para ciertos protocolos, aún se prueba su función
        /// </summary>
        public NextOperationModes NextOperation { get; set; }

        /// <summary>
        /// Variable que se utiliza temporalmente para almacenar en un log
        /// </summary>
        public string LogTemporal;

        /// <summary>
        /// Constructor
        /// </summary>
        public EstadoDelClienteBase()
        {
            // waitSend = new AutoResetEvent(true);
            esperandoEnvio = new ManualResetEvent(true);
            // se separa del constructor debido a  que  la inicialización de puede usar nuevamente sin hacer una nueva instancia
             inicializarEstadoDelClienteBase();            
        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, en esta se controla
        /// toda la operación sobre el mensaje del cliente así como su mensaje de respuesta
        /// </summary>
        /// <param name="mensajeCliente">Mensaje que se recibe de un cliente</param>
        public virtual void procesamientoTramaEntrante(string mensajeCliente)
        {
        }

        /// <summary>
        /// Función virtual para sobre escribirla, con ella de ingresa a un cliente
        /// en la lista de ip bloqueadas por alguna anomalía con un tiempo especifico
        /// </summary>
        /// <param name="cliente">Objeto sobre la clase clientesBloqueados</param>
        public virtual void agregarClienteListaNegados(ClienteBloqueo cliente)
        {
        }

        /// <summary>
        /// Funcion en la que se va a indicar cuál fue el socket principal sobre el cual
        /// se inició toda la operación
        /// </summary>
        /// <param name="socketPrincipal"> proceso donde se encuentra el socket principal del cuál se desprende el socket de trabajo por cliente</param>
        public void SetParentSocketServer(object socketPrincipal)
        {
            this.referenciaSocketPrincipal = socketPrincipal;
        }

        /// <summary>
        /// Función virtual para poder sobre escribirla, sirve para limpiar e inicializar 
        /// todas las variables del info y socket de trabajo
        /// </summary>
        public virtual void inicializarEstadoDelClienteBase()
        {
            referenciaSocketPrincipal = null;
            estadoDeParseo = 0;
            parseCurrentInputCmd = "";
            mensajeRecibidoAux = "";
            secuenciaDeRespuestasAlCliente = "";
            errorParseando = false;
            ultimoErrorDeParseo = "";
            ultimoErrorConexionCliente = "";
            ultimoMensajeAlCliente = "";
            fechaHoraUltimoMensajeAlCliente = DateTime.MaxValue;
            ultimoMensajeRecibidoCliente = "";
            fechaHoraUltimoMensajeRecibidoCliente = DateTime.MaxValue;
            puertoCliente = 0;
            colaEnvio = new Queue<string>();
            seEstaEnviandoAlgo = false;
            esperandoEnvio.Set();
            idUnicoCliente = Guid.NewGuid();
            ipCliente = "";
            fechaHoraConexionCliente = DateTime.Now;
            socketDeTrabajo = null;
            NextOperation = NextOperationModes.Normal;
            LogTemporal = "";
        }
    }
}
