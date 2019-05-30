using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CapaNegocio
{
    /// <summary>
    /// Clase que contiene todos los elementos de estado, envío y recepción
    /// </summary>
    public class ObjetoDeEstado
    {
        //@ socket cliente
        public Socket socketDeEstado = null;
        //@ tamaños del buffer a recibir de la trama
        public const int longitudBuffer = 1024;
        //@ arreglo donde llega el stream de la trama
        public byte[] bufferRecepcion = new byte[longitudBuffer];
        //@ constructor de la cadena de respuesta
        public StringBuilder constructorMensajeRespuesta = new StringBuilder();
    }

    /// <summary>
    /// Clase para la conexión, envío y recepción de información asincrono
    /// </summary>
    public class EnviarRecibirSocketProveedor
    {
        //@ eventos manuales para saber cuando termina una operación pero ya no es tan asincrino
        public ManualResetEvent connectDone = new ManualResetEvent(false);
        public ManualResetEvent sendDone = new ManualResetEvent(false);
        //public ManualResetEvent receiveDone = new ManualResetEvent(false);

        //@ donde se almacenará la respuesta   
        public String mensajeRespuesta = String.Empty;
        //@ donde se almacenará el mensaje que se enviará
        public String mensajeEnviar = String.Empty;
        //@ Socket que se utiliza en las operaciones asincronas
        public Socket socketPrincipal = null;
        //@ instancia para el formulario donde se escribirá por medio de un delegado en el listbox
        public frmPX formularioPX;
        public frmTenserver formularioTen;


        /// <summary>
        /// constructor para inicializar el socket cuando se utilice
        /// </summary>
        public EnviarRecibirSocketProveedor()
        {
            socketPrincipal = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Función que realiza la conexión a un host
        /// </summary>
        /// <param name="remoteEP"></param>
        /// <returns></returns>
        public Boolean conectarSocket(IPEndPoint remoteEP)
        {
            try
            {
                //cLogErrores.Escribir_Log_Evento("se intenta la conexión");
                //@ se realiza la conexión de manera asincrona
                socketPrincipal.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), socketPrincipal);
                //@ con el controlador del evento coloco el time out
                if (connectDone.WaitOne(5000) == true)
                {
                    //cLogErrores.Escribir_Log_Evento("Se logra la conexión a la host: " + socketPrincipal.RemoteEndPoint.ToString());
                    return true;
                }
                else
                {
                    //cLogErrores.Escribir_Log_Evento("No se logra la conexión a la host: " + socketPrincipal.RemoteEndPoint.ToString());                    
                    return false;
                }


                //if (mensajeRespuesta.Length>0)
                //{
                //    if (mensajeRespuesta.Substring(0, 2) == "14" || mensajeRespuesta.Substring(0, 2) == "18" || mensajeRespuesta.Substring(0, 2) == "22" || mensajeRespuesta.Substring(0, 2) == "24" || mensajeRespuesta.Substring(0, 2) == "26" || mensajeRespuesta.Substring(0, 2) == "28")
                //    {
                //        //cLogErrores.Escribir_Log_Evento(mensajeRespuesta);
                //        return mensajeRespuesta;
                //    }
                //    else
                //    {
                //        //cLogErrores.Escribir_Log_Evento(mensajeRespuesta.Substring(2));
                //        return mensajeRespuesta.Substring(2);
                //    }
                //}
                //else
                //{
                //    cLogErrores.Escribir_Log_Error("mensaje de respuesta vacío");
                //    return mensajeRespuesta;
                //}               
            }
            catch (Exception e)
            {
                cLogErrores.Escribir_Log_Error("conectarSocket: " + e.Message);
                return false;
            }
            finally
            {
                GC.Collect();
            }
        }

        /// <summary>
        /// Función asincrona para la conexión
        /// </summary>
        /// <param name="ar"></param>
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                //@ se recibe por medio del objeto asincrono de estado el socket
                Socket client = (Socket)ar.AsyncState;

                //@ se completa la conexión
                client.EndConnect(ar);

                //@ señal al evento para continuar
                connectDone.Set();
            }
            catch (Exception e)
            {
                cLogErrores.Escribir_Log_Error("ConnectCallback: " + e.Message);
            }
        }

        /// <summary>
        /// Función que envía el mensaje al host
        /// </summary>
        /// <param name="socketConexion">Socket de la conexión</param>
        /// <param name="mensaje">Mensaje</param>
        public void Send(Socket socketConexion, String mensaje)
        {
            try
            {
                //@ para tenerlo más adelante en el log
                mensajeEnviar = mensaje;
                //@ se convierte para poder enviarlo
                byte[] byteData = Encoding.Default.GetBytes(mensaje);

                //@ se envía asincronamente
                socketConexion.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), socketConexion);
            }
            catch (Exception e)
            {
                cLogErrores.Escribir_Log_Error("Send: " + e.Message);
            }
        }

        /// <summary>
        /// Función asincrona para envío
        /// </summary>
        /// <param name="ar"></param>
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                //@ se recupera el socket por medio del estado asincrono
                Socket client = (Socket)ar.AsyncState;

                //@ se completa el envío
                int bytesSent = client.EndSend(ar);

                // Signal that all bytes have been sent.
                //sendDone.Set();


                if (mensajeEnviar.Length > 100)
                {
                    //@ se invoca el delegado del formulario para grabar el mensaje en el listbox
                    formularioTen.Invoke(formularioTen.delegadoListadoEnvio, new Object[] { mensajeEnviar.Substring(2) });
                }
                else
                {
                    //@ se invoca el delegado del formulario para grabar el mensaje en el listbox
                    formularioPX.Invoke(formularioPX.delegadoListadoEnvio, new Object[] { mensajeEnviar });
                }
            }
            catch (Exception e)
            {
                cLogErrores.Escribir_Log_Error("SendCallback: " + e.Message);
            }
        }

        /// <summary>
        /// Función que recibe un mensaje del host
        /// </summary>
        /// <param name="client"></param>
        public void Receive(Socket client)
        {
            try
            {
                //@ se crea una instacia a la clase de estado
                ObjetoDeEstado state = new ObjetoDeEstado();
                //@ se copia el socket con el que se recibe al objeto de estado
                state.socketDeEstado = client;
                //@ se comienza la recepción asincrona
                client.BeginReceive(state.bufferRecepcion, 0, ObjetoDeEstado.longitudBuffer, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                cLogErrores.Escribir_Log_Error("Receive: " + e.Message);
            }
        }

        /// <summary>
        /// función asincrona para la recepción del mensaje
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                //@ se recibe el objeto completo asincrono
                ObjetoDeEstado state = (ObjetoDeEstado)ar.AsyncState;
                //@ se obtiene el socket
                Socket client = state.socketDeEstado;

                //@ se termina la recepción y se cuentan los bytes
                int bytesRead = client.EndReceive(ar);

                //@ si hay mensaje se procesa de lo contrario se queda vacío
                if (bytesRead > 0)
                {
                    state.constructorMensajeRespuesta.Append(Encoding.Default.GetString(state.bufferRecepcion, 0, bytesRead));
                }
                mensajeRespuesta = state.constructorMensajeRespuesta.ToString();

                if (mensajeRespuesta.Length > 0)
                {
                    if (mensajeRespuesta.Substring(0, 2) == "14" || mensajeRespuesta.Substring(0, 2) == "18" || mensajeRespuesta.Substring(0, 2) == "22" || mensajeRespuesta.Substring(0, 2) == "24" || mensajeRespuesta.Substring(0, 2) == "26" || mensajeRespuesta.Substring(0, 2) == "28")
                    {
                        //@ se invoca el delegado para guardar la trama en el list box
                        formularioPX.Invoke(formularioPX.delegadoListadoRecepcion, new Object[] { mensajeRespuesta });
                    }
                    else
                    {
                        //@ se invoca el delegado para guardar la trama en el list box
                        formularioTen.Invoke(formularioTen.delegadoListadoRecepcion, new Object[] { mensajeRespuesta.Substring(2) });
                    }
                }
                else
                {
                    cLogErrores.Escribir_Log_Error("mensaje de respuesta vacío");
                }



                // Signal that all bytes have been received.
                //receiveDone.Set();                                
            }
            catch (Exception e)
            {
                cLogErrores.Escribir_Log_Error("ReceiveCallback:" + e.Message);
            }
        }
    }
