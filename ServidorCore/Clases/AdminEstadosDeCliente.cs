using System;
using System.Collections.Generic;

namespace ServerCore
{
    /// <summary>
    /// Clase que controla el almacenado y asignación de estados de un socket, que sirven en 
    /// las operaciones de entrada y salida de dicho socket asincronamente
    /// </summary>
    /// <typeparam name="T">Instancia de la clase estadoDelClienteBase</typeparam>
    internal class AdminEstadosDeCliente<T>
        where T : EstadoDelClienteBase, new()
    {
        /// <summary>
        /// El conjunto de estados se almacena como una pila
        /// </summary>
        private readonly Stack<T> _pilaEstadosDeCliente;

        /// <summary>
        /// Constructor que inicializa el objeto pilaEstadosSocket con una dimensión máxima
        /// </summary>
        /// <param name="capacidadPilaEstadosSocket">Máximo número de objetos que la pila de estados podrá almacenar</param>
        internal AdminEstadosDeCliente(int capacidadPilaEstadosSocket)
        {
            _pilaEstadosDeCliente = new Stack<T>(capacidadPilaEstadosSocket);
        }

        /// <summary>
        /// Variable que contiene el número de elementos en la pila 
        /// </summary>
        internal int ContadorElementos
        {
            get { return this._pilaEstadosDeCliente.Count; }
        }

        /// <summary>
        /// Obtiene un estadoDelClienteBase de la pila de estados del cliente
        /// </summary>
        /// <returns>Objeto de la pila que es también removido mientras se usa</returns>
        internal T ObtenerUnElemento()
        {
            // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso
            lock (this._pilaEstadosDeCliente)
            {
                // obtengo un estado de la pila
                T estadoDelClienteBase = _pilaEstadosDeCliente.Pop();
                //  con el estado obtenido, se inicializa sin una nueva instancia ya que la pila ya estaba creada
                estadoDelClienteBase.InicializarEstadoDelClienteBase();
                return estadoDelClienteBase;
            }
        }

        /// <summary>
        /// Ingresa un estadoDelClienteBase a la pila de estados del cliente
        /// </summary>
        /// <param name="estadoDelClienteBase">Objeto de EstadoDelClienteBase a ingresar</param>
        internal void IngresarUnElemento(T estadoDelClienteBase)
        {
            if (estadoDelClienteBase == null)
            {
                throw new ArgumentNullException("El objeto no puede ser nulo");
            }
            // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso
            lock (this._pilaEstadosDeCliente)
            {
                if (!_pilaEstadosDeCliente.Contains(estadoDelClienteBase))
                    this._pilaEstadosDeCliente.Push(estadoDelClienteBase);
            }
        }
    }
}
