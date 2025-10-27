using System;
using System.Collections.Generic;
using System.Threading;

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
        private Stack<X> providerStateStack;

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
            get
            {
                bool isLock = Monitor.TryEnter(providerStateStack);
                try
                {
                    if (isLock)
                        return providerStateStack.Count;
                    else
                        throw new InvalidOperationException("No se pudo obtener el lock para acceder al contador de la pila de estados del proveedor.");
                }
                catch (Exception ex)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.Append("Error en ");
                    sb.Append(nameof(ProviderStateCounter));
                    sb.Append(": ");
                    sb.Append(ex.Message);
                    Utilities.Log(sb.ToString(), Utilities.LogType.Error);
                    return -1;
                }
                finally
                {
                    if (isLock)
                    {
                        Monitor.Exit(providerStateStack);
                    }
                }
            }
        }

        /// <summary>
        /// Obtiene un estadoDelClienteBase de la pila de estados del cliente
        /// </summary>
        /// <returns>Objeto de la pila que es también removido mientras se usa</returns>
        internal X GetProviderState()
        {
            // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso

            bool isLock = false;
            try
            {
                isLock = Monitor.TryEnter(this.providerStateStack, Utilities.milisecondsTimeOutLock);
                if (isLock)
                {
                    // obtengo un estado de la pila
                    X estadoDelProveedorBase = providerStateStack.Pop();
                    //  con el estado obtenido, se inicializa sin una nueva instancia ya que la pila ya estaba creada
                    estadoDelProveedorBase.InitializeProviderStateBase();
                    return estadoDelProveedorBase;
                }
                else
                {
                    throw new InvalidOperationException("No se pudo obtener el lock para acceder a la pila de estados del proveedor.");
                }
            }
            catch (Exception ex)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("Error en ");
                sb.Append(nameof(GetProviderState));
                sb.Append(": ");
                sb.Append(ex.Message);
                Utilities.Log(sb.ToString(), Utilities.LogType.Error);
                return null;
            }
            finally
            {
                if (isLock)
                {
                    Monitor.Exit(this.providerStateStack);
                }
            }
        }

        /// <summary>
        /// Ingresa un estadoDelClienteBase a la pila de estados del cliente
        /// </summary>
        /// <param name="estadoDelProveedorBase">Objeto de EstadoDelClienteBase a ingresar</param>
        internal void AddProviderState(X estadoDelProveedorBase)
        {
            bool isLock = false;
            try
            {
                if (estadoDelProveedorBase == null)
                {
                    Utilities.Log($"El objeto {nameof(estadoDelProveedorBase)} no puede ser nulo", Utilities.LogType.Warning);
                }
                // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso
                isLock = Monitor.TryEnter(providerStateStack, Utilities.milisecondsTimeOutLock);
                if (isLock)
                {
                    if (!providerStateStack.Contains(estadoDelProveedorBase))
                        this.providerStateStack.Push(estadoDelProveedorBase);
                }
            }
            catch (Exception ex)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("Error en ");
                sb.Append(nameof(AddProviderState));
                sb.Append(": ");
                sb.Append(ex.Message);
                sb.Append(". ClienteId: ");
                sb.Append(estadoDelProveedorBase?.clientStateSource?.UniqueClientId.ToString() ?? "N/A");
                Utilities.Log(sb.ToString(), Utilities.LogType.Error);
            }
            finally
            {
                if (isLock)
                {
                    Monitor.Exit(providerStateStack);
                }
            }
        }
    }
}
