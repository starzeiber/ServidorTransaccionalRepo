using System;
using System.Collections.Generic;
using System.Threading;

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
            get
            {
                Monitor.Enter(clientStateStack);
                try
                {
                    return clientStateStack.Count;
                }
                finally
                {
                    Monitor.Exit(clientStateStack);
                }
            }
        }

        /// <summary>
        /// Retrieves and initializes the current client state from the state stack.
        /// </summary>
        /// <remarks>This method attempts to acquire a lock on the client state stack to ensure
        /// thread-safe access.  If the lock is successfully acquired, it retrieves the top state from the stack,
        /// initializes it,  and returns it. If the lock cannot be acquired, an <see cref="InvalidOperationException"/>
        /// is thrown.  In the event of an error, the method logs the exception and returns <see
        /// langword="null"/>.</remarks>
        /// <returns>The initialized client state object of type <typeparamref name="T"/> if successful; otherwise, <see
        /// langword="null"/> if an error occurs.</returns>
        internal T GetClientState()
        {
            bool isLock = false;
            try
            {
                // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso
                isLock = Monitor.TryEnter(clientStateStack, Utilities.milisecondsTimeOutLock);
                if (isLock)
                {
                    // obtengo un estado de la pila
                    T estadoDelClienteBase = clientStateStack.Pop();
                    //  con el estado obtenido, se inicializa sin una nueva instancia ya que la pila ya estaba creada
                    estadoDelClienteBase.InitializeClientStateBase();
                    return estadoDelClienteBase;
                }
                else
                {
                    throw new InvalidOperationException("No se pudo obtener el lock para acceder a la pila de estados del cliente.");
                }
            }
            catch (Exception ex)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("Error en ");
                sb.Append(nameof(GetClientState));
                sb.Append(": ");
                sb.Append(ex.Message);
                Utilities.Log(sb.ToString(), Utilities.LogType.Error);
                return null;
            }
            finally
            {
                if (isLock)
                {
                    Monitor.Exit(clientStateStack);
                }
            }

        }

        /// <summary>
        /// Ingresa un estadoDelClienteBase a la pila de estados del cliente
        /// </summary>
        /// <param name="estadoDelClienteBase">Objeto de EstadoDelClienteBase a ingresar</param>
        internal void AddClientState(T estadoDelClienteBase)
        {
            bool isLock = false;
            try
            {
                if (estadoDelClienteBase == null)
                {
                    Utilities.Log($"El objeto {nameof(estadoDelClienteBase)} no puede ser nulo", Utilities.LogType.Warning);
                }
                // como la pila de estados se utiliza en todo el proyecto comunmente, se debe sincronizar su acceso
                isLock = Monitor.TryEnter(clientStateStack, Utilities.milisecondsTimeOutLock);
                if (isLock)
                {
                    if (!clientStateStack.Contains(estadoDelClienteBase))
                        this.clientStateStack.Push(estadoDelClienteBase);
                }
            }
            catch (Exception ex)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("Error en ");
                sb.Append(nameof(AddClientState));
                sb.Append(": ");
                sb.Append(ex.Message);
                sb.Append(". ClienteId: ");
                sb.Append(estadoDelClienteBase?.UniqueClientId.ToString() ?? "N/A");
                Utilities.Log(sb.ToString(), Utilities.LogType.Error);
            }
            finally
            {
                if (isLock)
                {
                    Monitor.Exit(clientStateStack);
                }
            }
        }
    }
}
