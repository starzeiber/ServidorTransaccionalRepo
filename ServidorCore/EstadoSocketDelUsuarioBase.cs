using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServidorCore
{
    /// <summary>
    /// Clase que contiene las funciones que se utilizan para indicar el flujo de una operación 
    /// al cliente o para seguimiento de la transacción
    /// </summary>
    public class EstadoSocketDelUsuarioBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public EstadoSocketDelUsuarioBase() { }

        // Referencia al proceso principal donde se encuentra el socket principal que disparó el flujo
        public object referenciaProcesoPrincipal { get; set; }

        /// <summary>
        /// Función virtual para sobre escribirla que se utiliza cuando se requiera un mensaje de
        /// bienvenida a una conexión de un cliente
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual string mensajeBienvenida(object args)
        {
            return "";
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza para indicar el principio del flujo
        /// </summary>
        public virtual void OnInicio()
        {
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza para indicar en el flujo que un cliente se cierra
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnClienteCerrado(object args)
        {
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza para indicar en el flujo que hay una conexión
        /// </summary>
        public virtual void OnConexion()
        {
            
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza indicar en el flujo que se acepta una solicitud de mensaje
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnAceptacion(object args)
        {            
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza indicar en el flujo que se recibe un mensaje
        /// </summary>
        public virtual void OnRecibido()
        {
        }

        /// <summary>
        /// función virtual para sobre escribirla que se utiliza indicar en el flujo que se envía un mensaje
        /// </summary>
        public virtual void OnEnviado()
        {
        }

        /// <summary>
        /// Objeto de ayuda en caso de requerirse obtener información del cliente o su flujo en la transacción
        /// y mostrar dicha información en el front del formulario o consola. IMPORTANTE: se requiere un cast del lado
        /// del estadodelsocket donde se sobre escriben los métodos de esta clase al formulario donde se pintará la información
        /// </summary>
        public object formulario { get; set; }
    }
}
