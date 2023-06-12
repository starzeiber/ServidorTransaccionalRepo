using System;
using System.Collections.Generic;

namespace ServerCore
{
    /// <summary>
    /// Clase que controla el almacenado y asignación de estados de un socket, que sirven en 
    /// las operaciones de entrada y salida de dicho socket asincronamente
    /// </summary>
    /// <typeparam name="X">Instancia de la clase estadoDelClienteBase</typeparam>
    internal class ProviderStateManager<X>
        where X : ProviderStateBase, new()
    {
        /// <summary>
        /// El conjunto de estados se almacena como una pila
        /// </summary>
        private readonly Stack<X> providerStatesStack;

        /// <summary>
        /// Constructor que inicializa el objeto pilaEstadosSocket con una dimensión máxima
        /// </summary>
        /// <param name="providerStatesStackCapacity">Máximo número de objetos que la pila de estados podrá almacenar</param>
        internal ProviderStateManager(int providerStatesStackCapacity)
        {
            providerStatesStack = new Stack<X>(providerStatesStackCapacity);
        }

        /// <summary>
        /// Variable que contiene el número de elementos en la pila 
        /// </summary>
        internal int StackCount
        {
            get { return this.providerStatesStack.Count; }
        }

        /// <summary>
        /// Obtiene un estadoDelClienteBase de la pila de estados del cliente
        /// </summary>
        /// <returns>Objeto de la pila que es también removido mientras se usa</returns>
        internal X GetStackItem()
        {
            // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso
            lock (this.providerStatesStack)
            {
                // obtengo un estado de la pila
                X estadoDelProveedorBase = providerStatesStack.Pop();
                //  con el estado obtenido, se inicializa sin una nueva instancia ya que la pila ya estaba creada
                estadoDelProveedorBase.Initialize();
                return estadoDelProveedorBase;
            }
        }

        /// <summary>
        /// Ingresa un estadoDelClienteBase a la pila de estados del cliente
        /// </summary>
        /// <param name="providerStateBase">Objeto de EstadoDelClienteBase a ingresar</param>
        internal void SetStackItem(X providerStateBase)
        {
            if (providerStateBase == null)
            {
                throw new ArgumentNullException("El objeto no puede ser nulo");
            }
            // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso
            lock (this.providerStatesStack)
            {
                if (!providerStatesStack.Contains(providerStateBase))
                    this.providerStatesStack.Push(providerStateBase);
            }
        }
    }
}
