using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace ServidorCore
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
        public Guid idUnicoCliente { get; set; }

        /// <summary>
        /// Referencia al servidor de socket principal
        /// </summary>
        public object referenciaSocketPrincipal;

        /// <summary>
        /// SocketAsyncEventArgs que se utilizará en la recepción
        /// </summary>
        internal SocketAsyncEventArgs saeaDeEnvioRecepcion;

        ///// <summary>
        ///// SocketAsyncEventArgs que se utilizará en el envío
        ///// </summary>
        //internal SocketAsyncEventArgs saeaDeEnvioForzadoAlCliente;

        /// <summary>        
        /// Secuencia de respuestas (Respuesta1\r\Respuesta2\r\n...RespuestaN\r\n)
        /// </summary>
        public string tramaRespuesta;

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

        ///// <summary>
        ///// ultimo error en la conexión del cliente, se utiliza como bitácora
        ///// </summary>
        //public string ultimoErrorConexionCliente;

        /// <summary>
        /// Ultimo mensaje enviado al cliente
        /// </summary>
        public string ultimoMensajeAlCliente;

        /// <summary>
        /// Fecha y hora del ultimo mensaje al cliente
        /// </summary>
        public DateTime fechaHoraUltimoMensajeAlCliente { get; set; }
        
        /// <summary>
        /// Fecha y hora del ultimo mensaje recibido del cliente
        /// </summary>
        public DateTime fechaHoraUltimoMensajeRecibidoCliente { get; set; }

        /// <summary>
        /// Socket asignado de trabajo sobre la conexión del cliente
        /// </summary>
        public Socket socketDeTrabajo { get; set; }

        /// <summary>
        /// Codigo de respuesta sobre el proceso del cliente
        /// </summary>
        public int codigoRespuesta { get; set; }

        /// <summary>
        /// Codigo de autorización sobre el proceso del cliente
        /// </summary>
        public int codigoAutorizacion { get; set; }

        public int cabeceraMensaje { get; set; }

        public object objPeticion { get; set; }

        public object objRespuesta { get; set; }

        public DateTime fechaInicioTrx { get; set; }

        //public bool seVencioElTimeOut { get; set; }

        //public bool seRespondioAlgoAlCliente { get; set; }

        public int segundosDeTO { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public EstadoDelClienteBase()
        {
            // waitSend = new AutoResetEvent(true);
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
            referenciaSocketPrincipal = null;
            tramaRespuesta = "";            
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
            segundosDeTO = 20;
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
        /// Función virtual para sobre escribirla, con ella de ingresa a un cliente
        /// en la lista de ip bloqueadas por alguna anomalía con un tiempo especifico
        /// </summary>
        /// <param name="cliente">Objeto sobre la clase clientesBloqueados</param>
        public virtual void AgregarClienteListaNegados(ClienteBloqueo cliente)
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
        /// <param name="codigoRespuesta">código de respuesta a ingresar en la trama</param>
        /// <param name="codigoAutorizacion">código de autorización a ingresar en la trama</param>
        public virtual void ObtenerTrama(int codigoRespuesta, int codigoAutorizacion)
        {

        }


    }
}
