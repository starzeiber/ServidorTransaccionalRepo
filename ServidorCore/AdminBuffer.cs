using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServidorCore
{
    /// <summary>
    /// Clase que administra un buffer enorme para seccionarlo y utilizar solamente lo
    /// necesario en cada envío y recepción de información para después, volver a agregar la sección utilizada.
    /// De esta manera, siempre se tiene un buffer justo a cada operación y reusable
    /// </summary>
    class AdminBuffer
    {
        /// <summary>        
        /// Matriz de bytes utilizada como buffer en la operación
        /// </summary>
        private Byte[] bufferCompleto;

        /// <summary>
        /// Tamaño del arreglo de bytes usado como buffer en cada operación
        /// </summary>
        private Int32 tamanoBufferPorSeccion;

        /// <summary>
        /// indice en el arreglo de byte (buffer).
        /// </summary>
        private Int32 indiceBuffer;

        /// <summary>
        /// Pila de indices para el administrador de buffer
        /// </summary>
        private Stack<Int32> pilaDeIndicesDeDesplazamientoBuffer;

        /// <summary>
        /// Número de total de bytes controlados por la pila de buffer
        /// </summary>
        private Int32 numeroBytesAdministrados;

        /// <summary>
        /// Constructor que inicializa los valores del administrador de buffer
        /// </summary>
        /// <param name="totalBytesAdministrar">Número total de bytes que tendrá la pila del buffer</param>
        /// <param name="tamanoBuffer">Tamaño del buffer para la operación</param>
        internal AdminBuffer(Int32 totalBytesAdministrar, Int32 tamanoBuffer)
        {
            this.numeroBytesAdministrados = totalBytesAdministrar;
            this.indiceBuffer = 0;
            this.tamanoBufferPorSeccion = tamanoBuffer;
            this.pilaDeIndicesDeDesplazamientoBuffer = new Stack<Int32>();
        }

        /// <summary>
        /// Remueve el buffer de un objeto SocketAsyncEventArg. Al liberarlo
        /// lo regresar a la pila de bufferes disponibles para volver a usarlo
        /// </summary>
        /// <param name="args">SocketAsyncEventArgs en donde está el buffer que se quiere remover</param>
        internal void LiberarBuffer(SocketAsyncEventArgs args)
        {
            //Se inserta al principio de la pila un índice que muestra el desplazamiento en el buffer que utilizó SocketAsyncEventArgs
            //para que sea reutilizado, de esta forma secciones iguales se toman y se regresan
            this.pilaDeIndicesDeDesplazamientoBuffer.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }

        /// <summary>
        ///  Asigna el espacio de buffer usado por la pila de buffer
        /// </summary>
        internal void inicializarBuffer()
        {            
            // Se crea un enorme buffer y se divide después para cada objeto SocketAsyncEventArg
            this.bufferCompleto = new Byte[this.numeroBytesAdministrados];
        }

        /// <summary>
        /// Asigna un buffer desde la pila de bufferes para el objeto SocketAsyncEventArgs específico
        /// </summary>
        /// <param name="socketAsyncEventArgs">SocketAsyncEventArgs donde el buffer se asignará</param>
        /// <returns>True si el buffer fue correctamente asignado</returns>
        internal Boolean asignarBuffer(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            // si el indice de la pila es mayor a cero quiere decir que tenemos disponible espacio en
            // el buffer grande para asignar una sección de buffer al objeto
            if (this.pilaDeIndicesDeDesplazamientoBuffer.Count > 0)
            {
                // se asigna un espacio para ser el buffer de trabajo, indicando el tamaño
                // para la operación y su desplazamiento será el número del elemento de 
                // la pila de indices, al mismo tiempo se le quita un elemento a dicha pila
                socketAsyncEventArgs.SetBuffer(this.bufferCompleto, this.pilaDeIndicesDeDesplazamientoBuffer.Pop(), this.tamanoBufferPorSeccion);
            }
            else // si es la primera vez que se utiliza este socketAsyncEventArgs
            {
                // se comprueba que si le restamos el número de bytes a utilizar del número
                // de bytes disponibles, si es menor al indice actual entonces no alcanza
                if ((this.numeroBytesAdministrados - this.tamanoBufferPorSeccion) < this.indiceBuffer)
                {
                    return false;
                }
                socketAsyncEventArgs.SetBuffer(this.bufferCompleto, this.indiceBuffer, this.tamanoBufferPorSeccion);
                // aquí está la clave, con este offset, me posiciono dentro del buffer enorme para saber en que sección me encuentro después de haber asignado un pedazo
                this.indiceBuffer += this.tamanoBufferPorSeccion;
            }

            return true;
        }
    }
}
