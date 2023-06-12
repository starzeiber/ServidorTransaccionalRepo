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
    internal class BufferManager
    {
        /// <summary>        
        /// Matriz de bytes utilizada como buffer en la operación
        /// </summary>
        private byte[] fullBuffer;

        /// <summary>
        /// Tamaño del arreglo de bytes usado como buffer en cada operación
        /// </summary>
        private readonly int bufferSizePerRequest;

        /// <summary>
        /// indice en el arreglo de byte (buffer).
        /// </summary>
        private int bufferIndex;

        /// <summary>
        /// Pila de indices para el administrador de buffer
        /// </summary>
        private readonly Stack<int> buffeIndexOffsetStack;


        /// <summary>
        /// Número de total de bytes controlados por la pila de buffer
        /// </summary>
        private readonly int totalBytesInBufferToManagement;

        /// <summary>
        /// Constructor que inicializa los valores del administrador de buffer
        /// </summary>
        /// <param name="totalBytesInBufferToManagement">Número total de bytes que tendrá la pila del buffer</param>
        /// <param name="bufferSizePerRequest">Tamaño del buffer para la operación</param>
        internal BufferManager(int totalBytesInBufferToManagement, int bufferSizePerRequest)
        {
            this.totalBytesInBufferToManagement = totalBytesInBufferToManagement;
            this.bufferIndex = 0;
            this.bufferSizePerRequest = bufferSizePerRequest;
            this.buffeIndexOffsetStack = new Stack<int>();
        }

        /// <summary>
        /// Remueve el buffer de un objeto SocketAsyncEventArg. Al liberarlo
        /// lo regresar a la pila de bufferes disponibles para volver a usarlo
        /// </summary>
        /// <param name="socketAsyncEventArgs">SocketAsyncEventArgs en donde está el buffer que se quiere remover</param>
        internal void ReleaseBuffer(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            //Se inserta al principio de la pila un índice que muestra el desplazamiento en el buffer que utilizó SocketAsyncEventArgs
            //para que sea reutilizado, de esta forma secciones iguales se toman y se regresan
            this.buffeIndexOffsetStack.Push(socketAsyncEventArgs.Offset);
            socketAsyncEventArgs.SetBuffer(null, 0, 0);
        }

        /// <summary>
        ///  Asigna el espacio de buffer usado por la pila de buffer
        /// </summary>
        internal void InitializeBuffer()
        {
            // Se crea un enorme buffer y se divide después para cada objeto SocketAsyncEventArg
            this.fullBuffer = new Byte[this.totalBytesInBufferToManagement];
        }

        /// <summary>
        /// Asigna un buffer desde la pila de bufferes para el objeto SocketAsyncEventArgs específico
        /// </summary>
        /// <param name="socketAsyncEventArgs">SocketAsyncEventArgs donde el buffer se asignará</param>
        /// <returns>True si el buffer fue correctamente asignado</returns>
        internal bool SetBuffer(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            // si el indice de la pila es mayor a cero quiere decir que tenemos disponible espacio en
            // el buffer grande para asignar una sección de buffer al objeto
            if (this.buffeIndexOffsetStack.Count > 0)
            {
                // se asigna un espacio para ser el buffer de trabajo, indicando el tamaño
                // para la operación y su desplazamiento será el número del elemento de 
                // la pila de indices, al mismo tiempo se le quita un elemento a dicha pila
                socketAsyncEventArgs.SetBuffer(this.fullBuffer, this.buffeIndexOffsetStack.Pop(), this.bufferSizePerRequest);
            }
            else // si es la primera vez que se utiliza este socketAsyncEventArgs
            {
                // se comprueba que si le restamos el número de bytes a utilizar del número
                // de bytes disponibles, si es menor al indice actual entonces no alcanza
                if ((this.totalBytesInBufferToManagement - this.bufferSizePerRequest) < this.bufferIndex)
                {
                    return false;
                }
                socketAsyncEventArgs.SetBuffer(this.fullBuffer, this.bufferIndex, this.bufferSizePerRequest);
                // aquí está la clave, con este offset, me posiciono dentro del buffer enorme para saber en que sección me encuentro después de haber asignado un pedazo
                this.bufferIndex += this.bufferSizePerRequest;
            }

            return true;
        }
    }
}
