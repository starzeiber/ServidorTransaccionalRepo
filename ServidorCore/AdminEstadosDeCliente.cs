using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServidorCore
{
    /// <summary>
    /// Clase que controla el almacenado y asignación de estados de un socket, que sirven en 
    /// las operaciones de entrada y salida de dicho socket asincronamente
    /// </summary>
    /// <typeparam name="T">Instancia de la clase estadoDelClienteBase</typeparam>
    class AdminEstadosDeCliente<T>
        where T : EstadoDelClienteBase, new()
    {
        /// <summary>
        /// El conjunto de estados se almacena como una pila
        /// </summary>
        private Stack<T> pilaEstadosSocket;

        /// <summary>
        /// Constructor que inicializa el objeto pilaEstadosSocket con una dimensión máxima
        /// </summary>
        /// <param name="capacidadPilaEstadosSocket">Máximo número de objetos que la pila de estados podrá almacenar</param>
        internal AdminEstadosDeCliente(Int32 capacidadPilaEstadosSocket)
        {
            pilaEstadosSocket = new Stack<T>(capacidadPilaEstadosSocket);
        }

        /// <summary>
        /// Variable que contiene el número de elementos en la pila 
        /// </summary>
        internal Int32 contadorElementos
        {
            get { return this.pilaEstadosSocket.Count; }
        }

        /// <summary>
        /// Obtiene un estadoDelClienteBase de la pila de estados del cliente
        /// </summary>
        /// <returns>Objeto de la pila que es también removido mientras se usa</returns>
        internal T obtenerUnElemento()
        {
            // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso
            lock (this.pilaEstadosSocket)
            {
                // obtengo un estado de la pila
                T estadoDelClienteBase = pilaEstadosSocket.Pop();
                //  con el estado obtenido, se inicializa sin una nueva instancia ya que la pila ya estaba creada
                estadoDelClienteBase.inicializarEstadoDelClienteBase();
                return estadoDelClienteBase;
            }
        }

        /// <summary>
        /// Ingresa un estadoDelClienteBase a la pila de estados del cliente
        /// </summary>
        /// <param name="estadoDelClienteBase">Objeto de EstadoDelClienteBase a ingresar</param>
        internal void ingresarUnElemento(T estadoDelClienteBase)
        {
            if (estadoDelClienteBase == null)
            {
                throw new ArgumentNullException("El objeto no puede ser nulo");
            }
            // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso
            lock (this.pilaEstadosSocket)
            {
                this.pilaEstadosSocket.Push(estadoDelClienteBase);
            }
        }
    }
}
