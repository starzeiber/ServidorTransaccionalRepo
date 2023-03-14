using System.Collections.Generic;
using System.Net.Sockets;

namespace ServerCore
{
    public interface IServidorTransaccional<T, X>
        where T : EstadoDelClienteBase, new()
        where X : EstadoDelProveedorBase, new()
    {
        List<T> ClientesPendientesDesconexion { get; set; }
        string IpDeEscuchaServidor { get; set; }
        string IpProveedor { get; set; }
        int NumeroclientesConectados { get; }
        int NumeroDeRecursosDisponiblesCliente { get; }
        int NumeroDeRecursosDisponiblesProveedor { get; }
        int NumeroMaximoConexionesPorIpCliente { get; set; }
        List<X> ProveedoresPendientesDesconexion { get; set; }
        List<int> PuertosProveedor { get; set; }
        bool ServidorEnEjecucion { get; set; }
        int TotalDeBytesTransferidos { get; }

        void CerrarSocketCliente(T estadoDelCliente);
        void CerrarSocketProveedor(X estadoDelProveedor);
        void ConfigInicioServidor(int timeOutCliente);
        void DetenerServidor();
        void EnvioInfoSincro(string mensaje, SocketAsyncEventArgs e);
        void IniciarServidor(int puertoLocalEscucha, bool modoTest = false, bool modoRouter = true, string ipProveedor = "127.0.0.0", List<int> listaPuertosProveedor = null);
    }
}