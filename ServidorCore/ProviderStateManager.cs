using System;
using System.Collections.Generic;

namespace ServerCore
{
    /// <summary>
    /// Clase que controla el almacenado y asignación de estados de un socket, que sirven en 
    /// las operaciones de entrada y salida de dicho socket asincronamente
    /// </summary>
    /// <typeparam name="X">Instancia de la clase estadoDelClienteBase</typeparam>
    class ProviderStateManager<X>
        where X : ProviderStateBase
    {
        /// <summary>
        /// El conjunto de estados se almacena como una pila
        /// </summary>
        private readonly Stack<X> providerStateStack;

        /// <summary>
        /// Constructor que inicializa el objeto pilaEstadosSocket con una dimensión máxima
        /// </summary>
        /// <param name="sizeProviderStateStack">Máximo número de objetos que la pila de estados podrá almacenar</param>
        internal ProviderStateManager(Int32 sizeProviderStateStack)
        {
            providerStateStack = new Stack<X>(sizeProviderStateStack);
        }

        /// <summary>
        /// Variable que contiene el número de elementos en la pila 
        /// </summary>
        internal Int32 ProviderStateCounter
        {
            get { return this.providerStateStack.Count; }
        }

        /// <summary>
        /// Obtiene un estadoDelClienteBase de la pila de estados del cliente
        /// </summary>
        /// <returns>Objeto de la pila que es también removido mientras se usa</returns>
        internal X GetProviderState()
        {
            // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso
            lock (this.providerStateStack)
            {
                // obtengo un estado de la pila
                X estadoDelProveedorBase = providerStateStack.Pop();
                //  con el estado obtenido, se inicializa sin una nueva instancia ya que la pila ya estaba creada
                estadoDelProveedorBase.InitializeProviderStateBase();
                return estadoDelProveedorBase;
            }
        }

        /// <summary>
        /// Ingresa un estadoDelClienteBase a la pila de estados del cliente
        /// </summary>
        /// <param name="estadoDelProveedorBase">Objeto de EstadoDelClienteBase a ingresar</param>
        internal void AddProviderState(X estadoDelProveedorBase)
        {
            if (estadoDelProveedorBase == null)
            {
                throw new ArgumentNullException("El objeto no puede ser nulo");
            }
            // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso
            lock (this.providerStateStack)
            {
                if (!providerStateStack.Contains(estadoDelProveedorBase))
                    this.providerStateStack.Push(estadoDelProveedorBase);
            }
        }
    }
}
