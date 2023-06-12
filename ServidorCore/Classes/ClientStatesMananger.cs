using System;
using System.Collections.Generic;

namespace ServerCore
{
    /// <summary>
    /// Clase que controla el almacenado y asignación de estados de un socket, que sirven en 
    /// las operaciones de entrada y salida de dicho socket asincronamente
    /// </summary>
    /// <typeparam name="T">Instancia de la clase estadoDelClienteBase</typeparam>
    internal class ClientStatesMananger<T>
        where T : ClientStateBase, new()
    {
        /// <summary>
        /// El conjunto de estados se almacena como una pila
        /// </summary>
        private readonly Stack<T> clientStatesStack;

        /// <summary>
        /// Constructor que inicializa el objeto pilaEstadosSocket con una dimensión máxima
        /// </summary>
        /// <param name="clientStatesStackCapacity">Máximo número de objetos que la pila de estados podrá almacenar</param>
        internal ClientStatesMananger(int clientStatesStackCapacity)
        {
            clientStatesStack = new Stack<T>(clientStatesStackCapacity);
        }

        /// <summary>
        /// Variable que contiene el número de elementos en la pila 
        /// </summary>
        internal int ClientStatesStackCount
        {
            get { return this.clientStatesStack.Count; }
        }

        /// <summary>
        /// Obtiene un estadoDelClienteBase de la pila de estados del cliente
        /// </summary>
        /// <returns>Objeto de la pila que es también removido mientras se usa</returns>
        internal T GetStackItem()
        {
            // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso
            lock (this.clientStatesStack)
            {
                // obtengo un estado de la pila
                T estadoDelClienteBase = clientStatesStack.Pop();
                //  con el estado obtenido, se inicializa sin una nueva instancia ya que la pila ya estaba creada
                estadoDelClienteBase.Initialize();
                return estadoDelClienteBase;
            }
        }

        /// <summary>
        /// Ingresa un estadoDelClienteBase a la pila de estados del cliente
        /// </summary>
        /// <param name="clientStateBase">Objeto de EstadoDelClienteBase a ingresar</param>
        internal void SetStackItem(T clientStateBase)
        {
            if (clientStateBase == null)
            {
                throw new ArgumentNullException("El objeto no puede ser nulo");
            }
            // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso
            lock (this.clientStatesStack)
            {
                if (!clientStatesStack.Contains(clientStateBase))
                    this.clientStatesStack.Push(clientStateBase);
            }
        }
    }
}
