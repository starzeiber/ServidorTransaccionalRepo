﻿using System.Collections.Generic;
using System.Net.Sockets;

namespace ServerCore
{
    public interface IServidorTransaccional<T, X>
        where T : EstadoDelClienteBase, new()
        where X : EstadoDelProveedorBase, new()
    {
        /// <summary>
        /// Obtiene o ingresa a la lista de clientes pendientes de desconexión, esta lista es para la verificación de que todos los cliente
        /// se desconectan adecuadamente, su uso es más para debug
        /// </summary>
        List<T> ClientesPendientesDesconexion { get; set; }
        /// <summary>
        /// IP de escucha de la aplicación  para recibir mensajes
        /// </summary>
        string IpDeEscuchaServidor { get; set; }
        /// <summary>
        /// Ip a la cual se apuntarán todas las transacciones del proveedor
        /// </summary>
        string IpProveedor { get; set; }
        /// <summary>
        /// Obtiene el número de clientes conectados actualmente al servidor
        /// </summary>
        int NumeroclientesConectados { get; }
        /// <summary>
        /// Numero que muestra cuantas conexiones puede aún soportar el servidor del lado del cliente
        /// </summary>
        int NumeroDeRecursosDisponiblesCliente { get; }
        /// <summary>
        /// Numero que muestra cuantas conexiones puede aún soportar el servidor del lado del proveedor
        /// </summary>
        int NumeroDeRecursosDisponiblesProveedor { get; }
        /// <summary>        
        /// Obtiene o ingresa el número máximo de conexiones simultaneas de una misma IP del cliente (0=ilimitadas)
        /// </summary>
        int NumeroMaximoConexionesPorIpCliente { get; set; }
        /// <summary>
        /// Obtiene o ingresa a la lista de proveedores pendientes de desconexión, esta lista es para la verificación de que todos los proveedores
        /// se desconectan adecuadamente, su uso es más para debug pero queda para mejorar
        /// </summary>
        List<X> ProveedoresPendientesDesconexion { get; set; }
        /// <summary>
        /// Lista de puertos en la IP configurada al proveedor para el envío de la mensajería
        /// </summary>
        List<int> PuertosProveedor { get; set; }
        /// <summary>
        /// Obtiene o ingresa el valor de que si el servidor está o no ejecutandose
        /// </summary>
        bool ServidorEnEjecucion { get; set; }
        /// <summary>
        /// Bytes que se han transmitido desde el inicio de la aplicación
        /// </summary>
        int TotalDeBytesTransferidos { get; }

        /// <summary>
        /// Cierra el socket asociado a un cliente y retira al cliente de la lista de clientes conectados
        /// </summary>
        /// <param name="estadoDelCliente">Instancia del cliente a cerrar</param>
        void CerrarSocketCliente(T estadoDelCliente);
        /// <summary>
        /// Cierra el socket asociado a un proveedor y retira al proveedor de la lista de conectados
        /// </summary>
        void CerrarSocketProveedor(X estadoDelProveedor);
        /// <summary>
        /// Inicializa el servidor con una pre asignación de buffers reusables y estados de sockets
        /// </summary>
        void PreConfiguracionInicial(int timeOutCliente);
        /// <summary>
        /// Se detiene el servidor
        /// </summary>
        void DetenerServidor();
        /// <summary>
        /// Envía un mensaje sincronamente (Discontinuado porque ya se puede hacer asincrono)
        /// </summary>
        /// <param name="mensaje">mensaje a enviar</param>
        /// <param name="e">A client's SocketAsyncEventArgs</param>
        void EnvioInfoSincro(string mensaje, SocketAsyncEventArgs e);
        /// <summary>
        /// Se inicia el servidor de manera que esté escuchando solicitudes de conexión entrantes.
        /// </summary>
        /// <param name="puertoLocalEscucha">Puerto de escucha del servidor</param>
        /// <param name="ipProveedor">Ip del servidor del proveedor</param>
        /// <param name="listaPuertosProveedor">Puertos del proveedor</param>
        /// <param name="modoTest">Modo pruebas</param>
        /// <param name="modoRouter">Indicador de que el servidor tendrá la función de enviar mensajes a otro proveedor</param>
        void Iniciar(int puertoLocalEscucha, bool modoTest = false, bool modoRouter = true, string ipProveedor = "127.0.0.0", List<int> listaPuertosProveedor = null);
    }
}