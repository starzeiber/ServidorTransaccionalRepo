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
    /// <typeparam name="T">Instancia de la clase InfoSocketDelUsuarioBase</typeparam>
    class PilaEstadosDeSocketsDelUsuario<T>
        where T : InfoSocketDelUsuarioBase, new()
    {
        /// <summary>
        /// El conjunto de estados se almacena como una pila
        /// </summary>
        private Stack<T> pilaEstadosSocket;

        /// <summary>
        /// Constructor que inicializa el objeto pilaEstadosSocket con una dimensión máxima
        /// </summary>
        /// <param name="capacidadPilaEstadosSocket">Máximo número de objetos que la pila de estados podrá almacenar</param>
        internal PilaEstadosDeSocketsDelUsuario(Int32 capacidadPilaEstadosSocket)
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
        /// Obtiene un objeto de la pila
        /// </summary>
        /// <returns>Objeto de la pila que es también removido mientras se usa</returns>
        internal T ObtenerUnElemento()
        {
            // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso
            lock (this.pilaEstadosSocket)
            {
                T tmp = pilaEstadosSocket.Pop();
                tmp.InicializarInfoSocketDelUsuarioBase();
                return tmp;
            }
        }

        /// <summary>
        /// Ingresa un objeto a la pila de estados 
        /// </summary>
        /// <param name="elemento">Objeto de estados a ingresar</param>
        internal void IngresarUnElemento(T elemento)
        {
            if (elemento == null)
            {
                throw new ArgumentNullException("El elemento a ingresar en la pila de estado de socket está nulo");
            }
            // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso
            lock (this.pilaEstadosSocket)
            {
                this.pilaEstadosSocket.Push(elemento);
            }
        }
    }
}
