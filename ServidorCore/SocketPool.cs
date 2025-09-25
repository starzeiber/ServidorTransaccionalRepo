using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    /// <summary>
    /// Manages a pool of reusable <see cref="Socket"/> instances, limiting the number of concurrent sockets to a
    /// specified maximum size.
    /// </summary>
    /// <remarks>The <see cref="SocketPool"/> class is designed to optimize socket reuse and manage resource
    /// consumption by maintaining a pool of connected sockets. It ensures that the number of active sockets does not
    /// exceed the specified maximum size, and provides methods to retrieve and return sockets to the pool. This class
    /// is thread-safe and can be used in concurrent environments. <para> When a socket is retrieved from the pool, its
    /// connectivity is verified. Disconnected sockets are removed from the pool and replaced with new ones as needed.
    /// Sockets returned to the pool are reused if they remain connected. </para> <para> The <see cref="Dispose"/>
    /// method should be called when the pool is no longer needed to release all resources and close any remaining
    /// sockets. </para></remarks>
    public class SocketPool : IDisposable
    {
        private readonly ConcurrentBag<Socket> pool = new ConcurrentBag<Socket>();
        private readonly int maxPoolSize;
        private int currentCount = 0;
        //private readonly SemaphoreSlim semaphore;

        /// <summary>
        /// Gets the number of sockets currently available in the pool.
        /// </summary>
        public int SocketInPoolCounter
        {
            get
            {
                lock (pool)
                {
                    return pool.Count;
                }
            }
            private set { }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="SocketPool"/> class with a specified maximum pool size.
        /// </summary>
        /// <remarks>The <see cref="SocketPool"/> class manages a pool of sockets, limiting the number of
        /// concurrent  sockets to the specified maximum size. This ensures controlled resource usage and prevents 
        /// excessive socket creation.</remarks>
        /// <param name="maxSize">The maximum number of sockets that can be held in the pool. Must be a positive integer.  The default value
        /// is 10.</param>
        public SocketPool(int maxSize = 10)
        {
            maxPoolSize = maxSize;
            //semaphore = new SemaphoreSlim(maxSize, maxSize);
        }

        /// <summary>
        /// Retrieves a socket from the pool or creates a new one if the pool is not at maximum capacity.
        /// </summary>
        /// <remarks>If a socket is retrieved from the pool, it is checked for connectivity before being
        /// returned.  Disconnected sockets are closed and removed from the pool. If a new socket is created, it is 
        /// immediately connected to the specified <paramref name="endPoint"/>.</remarks>
        /// <param name="endPoint">The <see cref="System.Net.IPEndPoint"/> to which the socket will connect if a new socket is created.</param>
        /// <returns>A connected <see cref="System.Net.Sockets.Socket"/> instance. Returns <see langword="null"/> if an error
        /// occurs during socket creation or retrieval.</returns>
        public Socket GetSocket(IPEndPoint endPoint, string uniqueId)
        {
            Socket socket = null;
            try
            {
                if (pool.TryTake(out socket))
                {
                    Interlocked.Decrement(ref currentCount);
                    if (socket == null)
                    {
                        throw new InvalidOperationException("El socket obtenido del pool es nulo.");
                    }
                }
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("Error al obtener un socket del pool: ");
                sb.Append(ex.Message);
                sb.Append(", cliente: ");
                sb.Append(uniqueId);
                Utilities.EscribirLog(sb.ToString(), Utilities.tipoLog.ERROR);
            }

            if (socket != null && IsSocketConnected(socket))
            {
                return socket;
            }
            socket?.Close();

            if (Interlocked.Increment(ref currentCount) <= maxPoolSize)
            {
                try
                {
                    var newSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    newSocket.Connect(endPoint);
                    return newSocket;
                }
                catch (Exception ex)
                {
                    Interlocked.Decrement(ref currentCount);                    
                    var sb = new StringBuilder();
                    sb.Append("Error al obtener un nuevo socket del pool: ");
                    sb.Append(ex.Message);
                    sb.Append(", cliente: ");
                    sb.Append(uniqueId);
                    Utilities.EscribirLog(sb.ToString(), Utilities.tipoLog.ERROR);
                    return null;
                }
            }
            Interlocked.Decrement(ref currentCount);
            return null;
        }

        /// <summary>
        /// Returns a socket to the pool if it is connected; otherwise, closes the socket and updates the pool count.
        /// </summary>
        /// <remarks>If the socket is connected, it is added back to the pool for reuse. If the socket is
        /// not connected, it is closed,  and the total count of active sockets in the pool is decremented. Regardless
        /// of the socket's state, the semaphore  is released to signal that a slot in the pool is available.</remarks>
        /// <param name="socket">The <see cref="Socket"/> instance to return to the pool. Must not be <see langword="null"/>.</param>
        public void ReturnSocket(Socket socket, string clientId)
        {
            try
            {                
                if (IsSocketConnected(socket))
                {
                    pool.Add(socket);
                }
                else
                {
                    socket?.Close();
                    Interlocked.Decrement(ref currentCount);
                }
            }
            catch (Exception ex)
            {
                //var sb = new StringBuilder();
                //sb.Append("Error al devolver un socket al pool:");
                //sb.Append(ex.Message);
                //sb.Append(", cliente: ");
                //sb.Append(clientId);
                //Utilities.EscribirLog(sb.ToString(), Utilities.tipoLog.ALERTA);
                Interlocked.Decrement(ref currentCount);
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="Socket"/> is currently connected.
        /// </summary>
        /// <remarks>This method checks the connectivity of the socket by polling its state. A return
        /// value of  <see langword="false"/> indicates that the socket is either closed or no longer
        /// connected.</remarks>
        /// <param name="socket">The <see cref="Socket"/> instance to check for connectivity.</param>
        /// <returns><see langword="true"/> if the socket is connected; otherwise, <see langword="false"/>.</returns>
        private bool IsSocketConnected(Socket socket)
        {
            var sb = new StringBuilder();
            try
            {
                if (socket == null)
                {
                    return false;
                }
                sb.Append("verificando conectividad del socket.");
                sb.Append(socket.RemoteEndPoint.ToString());
                Utilities.EscribirLog(sb.ToString(), Utilities.tipoLog.INFORMACION);
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException sex)
            {

                sb.Append("SocketException al verificar la conectividad del socket. ");
                sb.Append(sex.Message);
                Utilities.EscribirLog(sb.ToString(), Utilities.tipoLog.ALERTA);
                return false;
            }
        }

        /// <summary>
        /// Releases all resources used by the object and disposes of any pooled sockets.
        /// </summary>
        /// <remarks>This method iterates through the internal socket pool and disposes of each socket. 
        /// After calling this method, the object should no longer be used.</remarks>
        public void Dispose()
        {
            while (pool.TryTake(out Socket socket))
            {
                socket.Dispose();
            }
        }
    }
}