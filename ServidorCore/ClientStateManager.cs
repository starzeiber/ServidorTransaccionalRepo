using System;
using System.Collections.Generic;

namespace ServerCore
{
    /// <summary>
    /// Clase que controla el almacenado y asignación de estados de un socket, que sirven en 
    /// las operaciones de entrada y salida de dicho socket asincronamente
    /// </summary>
    /// <typeparam name="T">Instancia de la clase estadoDelClienteBase</typeparam>
    class ClientStateManager<T>
        where T : ClientStateBase
    {
        /// <summary>
        /// El conjunto de estados se almacena como una pila
        /// </summary>
        private readonly Stack<T> clientStateStack;

        /// <summary>
        /// Constructor que inicializa el objeto pilaEstadosSocket con una dimensión máxima
        /// </summary>
        /// <param name="sizeClientStateStack">Máximo número de objetos que la pila de estados podrá almacenar</param>
        internal ClientStateManager(Int32 sizeClientStateStack)
        {
            clientStateStack = new Stack<T>(sizeClientStateStack);
        }

        /// <summary>
        /// Variable que contiene el número de elementos en la pila 
        /// </summary>
        internal Int32 ClientStateCounter
        {
            get { return this.clientStateStack.Count; }
        }

        /// <summary>
        /// Obtiene un estadoDelClienteBase de la pila de estados del cliente
        /// </summary>
        /// <returns>Objeto de la pila que es también removido mientras se usa</returns>
        internal T GetClientState()
        {
            // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso
            lock (this.clientStateStack)
            {
                // obtengo un estado de la pila
                T estadoDelClienteBase = clientStateStack.Pop();
                //  con el estado obtenido, se inicializa sin una nueva instancia ya que la pila ya estaba creada
                estadoDelClienteBase.InitializeClientStateBase();
                return estadoDelClienteBase;
            }
        }

        /// <summary>
        /// Ingresa un estadoDelClienteBase a la pila de estados del cliente
        /// </summary>
        /// <param name="estadoDelClienteBase">Objeto de EstadoDelClienteBase a ingresar</param>
        internal void AddClientState(T estadoDelClienteBase)
        {
            if (estadoDelClienteBase == null)
            {
                throw new ArgumentNullException("El objeto no puede ser nulo");
            }
            // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso
            lock (this.clientStateStack)
            {
                if (!clientStateStack.Contains(estadoDelClienteBase))
                    this.clientStateStack.Push(estadoDelClienteBase);
            }
        }
    }
}
