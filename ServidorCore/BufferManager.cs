using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace ServerCore
{
    /// <summary>
    /// Clase que administra un buffer enorme para seccionarlo y utilizar solamente lo
    /// necesario en cada envío y recepción de información para después, volver a agregar la sección utilizada.
    /// De esta manera, siempre se tiene un buffer justo a cada operación y reusable
    /// </summary>
    class BufferManager
    {
        /// <summary>        
        /// Matriz de bytes utilizada como buffer en la operación
        /// </summary>
        private Byte[] fullBuffer;

        /// <summary>
        /// Tamaño del arreglo de bytes usado como buffer en cada operación
        /// </summary>
        private readonly Int32 sizeBufferPerRequest;

        /// <summary>
        /// indice en el arreglo de byte (buffer).
        /// </summary>
        private Int32 bufferIndex;

        /// <summary>
        /// Pila de indices para el administrador de buffer
        /// </summary>
        private readonly Stack<Int32> bufferStackOffsetsIndex;

        private readonly int maxNumberStackBuffers;

        /// <summary>
        /// Número de total de bytes controlados por la pila de buffer
        /// </summary>
        private readonly Int32 managedByteCounter;

        /// <summary>
        /// Gets the number of available buffers in the internal buffer stack.
        /// </summary>
        internal int AvailableBuffersCounter
        {
            get { return this.bufferStackOffsetsIndex.Count; }
        }

        /// <summary>
        /// Constructor que inicializa los valores del administrador de buffer
        /// </summary>
        /// <param name="managedByteCounter">Número total de bytes que tendrá la pila del buffer</param>
        /// <param name="bufferSize">Tamaño del buffer para la operación</param>
        internal BufferManager(Int32 managedByteCounter, Int32 bufferSize)
        {
            this.managedByteCounter = managedByteCounter;
            this.bufferIndex = 0;
            this.sizeBufferPerRequest = bufferSize;
            this.bufferStackOffsetsIndex = new Stack<Int32>();
            maxNumberStackBuffers = this.managedByteCounter / sizeBufferPerRequest;
        }

        /// <summary>
        /// Remueve el buffer de un objeto SocketAsyncEventArg. Al liberarlo
        /// lo regresar a la pila de bufferes disponibles para volver a usarlo
        /// </summary>
        /// <param name="args">SocketAsyncEventArgs en donde está el buffer que se quiere remover</param>
        internal void FreeBuffer(SocketAsyncEventArgs args, string uniqueId)
        {
            try
            {
                // Validar que el offset no supere el tamaño del buffer principal
                if (args.Offset < 0 || args.Offset >= this.managedByteCounter)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.Append("Offset fuera de rango en AdminBuffer.LiberarBuffer: ");
                    sb.Append(args.Offset);
                    sb.Append(" cliente: ");
                    sb.Append(uniqueId);
                    Utilities.Log(sb.ToString(), Utilities.LogType.Warning);
                    return;
                }

                //Se inserta al principio de la pila un índice que muestra el desplazamiento en el buffer que utilizó SocketAsyncEventArgs
                //para que sea reutilizado, de esta forma secciones iguales se toman y se regresan
                this.bufferStackOffsetsIndex.Push(args.Offset);
                args.SetBuffer(null, 0, 0);
            }
            catch (Exception ex)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("Error en AdminBuffer.LiberarBuffer");
                sb.Append(ex.Message);
                sb.Append(" cliente: ");
                sb.Append(uniqueId);
                Utilities.Log(sb.ToString(), Utilities.LogType.Warning);
            }
        }

        /// <summary>
        ///  Asigna el espacio de buffer usado por la pila de buffer
        /// </summary>
        internal void InitializeFullBuffer()
        {
            // Se crea un enorme buffer y se divide después para cada objeto SocketAsyncEventArg
            this.fullBuffer = new Byte[this.managedByteCounter];
            InitializeStackBuffer();
        }

        /// <summary>
        /// Asigna un buffer desde la pila de bufferes para el objeto SocketAsyncEventArgs específico
        /// </summary>
        /// <param name="socketAsyncEventArgs">SocketAsyncEventArgs donde el buffer se asignará</param>
        /// <returns>True si el buffer fue correctamente asignado</returns>
        internal Boolean SetBuffer(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            // si el indice de la pila es mayor a cero quiere decir que tenemos disponible un espacio seccionado en
            // el buffer grande para asignarlo de buffer al objeto
            if (this.bufferStackOffsetsIndex.Count > 0)
            {
                // se asigna un espacio para ser el buffer de trabajo, indicando el tamaño
                // para la operación y su desplazamiento será el número del elemento de 
                // la pila de indices, al mismo tiempo se le quita un elemento a dicha pila
                socketAsyncEventArgs.SetBuffer(this.fullBuffer, this.bufferStackOffsetsIndex.Pop(), this.sizeBufferPerRequest);
            }
            else // si es la primera vez que se utiliza este socketAsyncEventArgs
            {
                // se comprueba que si le restamos el número de bytes a utilizar del número
                // de bytes disponibles, si es menor al indice actual entonces no alcanza
                if ((this.managedByteCounter - this.sizeBufferPerRequest) < this.bufferIndex)
                {
                    return false;
                }
                socketAsyncEventArgs.SetBuffer(this.fullBuffer, this.bufferIndex, this.sizeBufferPerRequest);
                // aquí está la clave, con este offset, me posiciono dentro del buffer enorme para saber en que sección me encuentro después de haber asignado un pedazo
                this.bufferIndex += this.sizeBufferPerRequest;
            }

            return true;
        }

        /// <summary>
        /// Limpia el contenido del buffer completo, estableciendo todos los bytes en cero.
        /// </summary>
        internal void ClearFullBuffer()
        {
            if (this.fullBuffer != null)
            {
                Array.Clear(this.fullBuffer, 0, this.fullBuffer.Length);
            }
        }

        /// <summary>
        /// Limpia la pila de índices de desplazamiento del buffer, eliminando todos los elementos.
        /// </summary>
        internal void ClearStackBuffer()
        {
            this.bufferStackOffsetsIndex.Clear();
        }

        /// <summary>
        /// Initializes the stack of buffer indices used for managing offsets in the buffer pool.
        /// </summary>
        /// <remarks>This method clears the existing stack and repopulates it with indices calculated
        /// based on          the buffer size per request. The indices represent the starting positions of buffers
        /// within          the managed byte pool.</remarks>
        internal void InitializeStackBuffer()
        {
            this.bufferStackOffsetsIndex.Clear();
            for (int i = 0; i < this.managedByteCounter; i += this.sizeBufferPerRequest)
            {
                this.bufferStackOffsetsIndex.Push(i);
            }
        }
    }
}
