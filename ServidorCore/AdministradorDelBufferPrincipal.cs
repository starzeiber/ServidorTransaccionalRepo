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
    /// necesario en cada envío y recepción de información para después, volver a agregarlo.
    /// Así siempre se tiene un buffer justo a cada operación y reusable
    /// </summary>
    class AdministradorDelBufferPrincipal
    {
        /// <summary>        
        /// Matriz de bytes utilizada como buffer en la operación
        /// </summary>
        private Byte[] buffer;

        /// <summary>
        /// Tamaño del arreglo de bytes usado como buffer en cada operación
        /// </summary>
        private Int32 tamanoBuffer;

        /// <summary>
        /// indice en el arreglo de byte (buffer).
        /// </summary>
        private Int32 indiceBuffer;

        /// <summary>
        /// Pila de indices para el administrador de buffer
        /// </summary>
        private Stack<Int32> indiceDisponible;

        /// <summary>
        /// Número de total de bytes controlados por la pila de buffer
        /// </summary>
        private Int32 numeroBytesAdministrados;

        /// <summary>
        /// Constructor que inicializa los valores del administrador de buffer
        /// </summary>
        /// <param name="totalBytesAdministrar">Número total de bytes que tendrá la pila del buffer</param>
        /// <param name="tamanoBuffer">Tamaño del buffer para la operación</param>
        internal AdministradorDelBufferPrincipal(Int32 totalBytesAdministrar, Int32 tamanoBuffer)
        {
            this.numeroBytesAdministrados = totalBytesAdministrar;
            this.indiceBuffer = 0;
            this.tamanoBuffer = tamanoBuffer;
            this.indiceDisponible = new Stack<Int32>();
        }

        /// <summary>
        /// Remueve el buffer de un objeto SocketAsyncEventArg. Al liberarlo
        /// lo regresa a la pila de bufferes disponibles para volver a usarlo
        /// </summary>
        /// <param name="args">SocketAsyncEventArgs donde está el buffer que se quiere remover</param>
        internal void LiberarBuffer(SocketAsyncEventArgs args)
        {
            this.indiceDisponible.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }

        /// <summary>
        ///  Asigna el espacio de buffer usado por la pila de buffer
        /// </summary>
        internal void InicializarBuffer()
        {            
            // Se crea un enorme buffer y se divide después para cada objeto SocketAsyncEventArg
            this.buffer = new Byte[this.numeroBytesAdministrados];
        }

        /// <summary>
        /// Asigna un buffer desde la pila de bufferes para el objeto SocketAsyncEventArgs específico
        /// </summary>
        /// <param name="args">SocketAsyncEventArgs donde el buffer se asignará</param>
        /// <returns>True si el buffer fue correctamente asignado</returns>
        internal Boolean AsignarBuffer(SocketAsyncEventArgs args)
        {
            // si el indice de la pila es mayor a cero quiere decir que tenemos disponible espacio en
            // la pila para asignar buffer al objeto
            if (this.indiceDisponible.Count > 0)
            {
                // se asigna un espacio para ser el buffer de trabajo, indicando el tamaño
                // para la operación y su desplazamiento será el número del elemento de 
                // la pila de indices, al mismo tiempo se le quita un elemento a dicha pila
                args.SetBuffer(this.buffer, this.indiceDisponible.Pop(), this.tamanoBuffer);
            }
            else // si es la primera vez que se instancia
            {
                // se comprueba que si le restamos el número de bytes a utilizar del número
                // de bytes disponibles, si es menor al indice actual entonces no alcanza
                if ((this.numeroBytesAdministrados - this.tamanoBuffer) < this.indiceBuffer)
                {
                    return false;
                }
                args.SetBuffer(this.buffer, this.indiceBuffer, this.tamanoBuffer);
                // aquí está la clave, con este offset, me posiciono dentro del buffer enorme
                this.indiceBuffer += this.tamanoBuffer;
            }

            return true;
        }
    }
}
