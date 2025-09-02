using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace ServerCore
{
    /// <summary>
    /// Pool de sockets TCP para reutilización eficiente de conexiones.
    /// </summary>
    public class SocketPool : IDisposable
    {
        private readonly ConcurrentBag<Socket> pool;
        private readonly IPEndPoint endPoint;
        private readonly int maxPoolSize;
        private int currentCount;

        public SocketPool(IPEndPoint endPoint, int maxPoolSize = 10)
        {
            this.endPoint = endPoint;
            this.maxPoolSize = maxPoolSize;
            pool = new ConcurrentBag<Socket>();
            currentCount = 0;
        }

        /// <summary>
        /// Obtiene un socket del pool o crea uno nuevo si hay capacidad.
        /// </summary>
        public Socket Rent()
        {
            if (pool.TryTake(out Socket socket))
            {
                if (socket.Connected)
                    return socket;
                try { socket.Connect(endPoint); } catch { }
                return socket;
            }
            if (currentCount < maxPoolSize)
            {
                var newSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                newSocket.Connect(endPoint);
                System.Threading.Interlocked.Increment(ref currentCount);
                return newSocket;
            }
            throw new InvalidOperationException("No hay sockets disponibles en el pool.");
        }

        /// <summary>
        /// Devuelve el socket al pool para reutilización.
        /// </summary>
        public void Return(Socket socket)
        {
            if (socket != null && socket.Connected)
                pool.Add(socket);
            else
                socket?.Dispose();
        }

        public void Dispose()
        {
            while (pool.TryTake(out Socket socket))
            {
                socket.Dispose();
            }
        }
    }
}
