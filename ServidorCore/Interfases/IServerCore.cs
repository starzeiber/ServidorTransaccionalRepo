﻿using System.Collections.Generic;
using System.Net.Sockets;

namespace ServerCore
{
    /// <summary>
    /// Clase principal sobre el core del servidor transaccional, contiene todas las propiedades 
    /// del servidor y los métodos de envío y recepción asincronos
    /// </summary>
    /// <typeparam name="T">Instancia sobre la clase que contiene la información de un cliente conectado y su
    /// socket de trabajo una vez asignado desde el pool</typeparam>    
    /// <typeparam name="X">Instancia sobre la clase que contiene la información de un cliente conectado y su
    /// socket de trabajo una vez asignado desde el pool</typeparam>
    public interface IServerCore<T, S, X>
        where T : ClientStateBase, new()
        where S : ServerStateBase, new()
        where X : ProviderStateBase, new()
    {
        /// <summary>
        /// Obtiene o ingresa el estado del socket del servidor
        /// </summary>
        S ServerStateBase { get; set; }

        /// <summary>
        /// Obtiene o ingresa a la lista de clientes pendientes de desconexión, esta lista es para la verificación de que todos los cliente
        /// se desconectan adecuadamente, su uso es más para debug
        /// </summary>
        List<T> PendingClientsToDisconnect { get; set; }
        /// <summary>
        /// IP de escucha de la aplicación  para recibir mensajes
        /// </summary>
        string ServerListeningIp { get; set; }
        /// <summary>
        /// Ip a la cual se apuntarán todas las transacciones del proveedor
        /// </summary>
        string ProviderIp { get; set; }
        /// <summary>
        /// Obtiene el número de clientes conectados actualmente al servidor
        /// </summary>
        int ClientsConnectedCounter { get; }
        /// <summary>
        /// Numero que muestra cuantas conexiones puede aún soportar el servidor del lado del cliente
        /// </summary>
        int ResourcesAvailableCounterPerClient { get; }
        /// <summary>
        /// Numero que muestra cuantas conexiones puede aún soportar el servidor del lado del proveedor
        /// </summary>
        int ResourcesAvailableCounterPerProvider { get; }
        /// <summary>        
        /// Obtiene o ingresa el número máximo de conexiones simultaneas de una misma IP del cliente (0=ilimitadas)
        /// </summary>
        int MaximumConnectionsPerClient { get; set; }
        /// <summary>
        /// Obtiene o ingresa a la lista de proveedores pendientes de desconexión, esta lista es para la verificación de que todos los proveedores
        /// se desconectan adecuadamente, su uso es más para debug pero queda para mejorar
        /// </summary>
        List<X> PendingProvidersToDisconnect { get; set; }
        /// <summary>
        /// Lista de puertos en la IP configurada al proveedor para el envío de la mensajería
        /// </summary>
        List<int> ProvidersPorts { get; set; }
        /// <summary>
        /// Obtiene o ingresa el valor de que si el servidor está o no ejecutandose
        /// </summary>
        bool IsRunning { get; set; }
        /// <summary>
        /// Bytes que se han transmitido desde el inicio de la aplicación
        /// </summary>
        int TotalDeBytesTransferidos { get; }

        /// <summary>
        /// Cierra el socket asociado a un cliente y retira al cliente de la lista de clientes conectados
        /// </summary>
        /// <param name="estadoDelCliente">Instancia del cliente a cerrar</param>
        void CloseSocketClient(T estadoDelCliente);
        /// <summary>
        /// Cierra el socket asociado a un proveedor y retira al proveedor de la lista de conectados
        /// </summary>
        void CloseSocketProvider(X estadoDelProveedor);
        /// <summary>
        /// Inicializa el servidor con una pre asignación de buffers reusables y estados de sockets
        /// </summary>
        void PreInitialize(int timeOutCliente);
        /// <summary>
        /// Se detiene el servidor
        /// </summary>
        void StopServer();
        /// <summary>
        /// Envía un mensaje sincronamente (Discontinuado porque ya se puede hacer asincrono)
        /// </summary>
        /// <param name="mensaje">mensaje a enviar</param>
        /// <param name="e">A client's SocketAsyncEventArgs</param>
        void SendMessageSync(string mensaje, SocketAsyncEventArgs e);
        /// <summary>
        /// Se inicia el servidor de manera que esté escuchando solicitudes de conexión entrantes.
        /// </summary>
        /// <param name="puertoLocalEscucha">Puerto de escucha para la recepeción de mensajes</param>
        /// <param name="listaPuertosProveedor">Puertos del proveedor</param>
        /// <param name="modoTest">Variable que indicará si el server entra en modo test.
        /// El modo Test, responderá a toda petición bien formada, con una código de autorización
        /// y respuesta simulado sin enviar la trama a un proveedor externo</param>
        /// <param name="modoRouter">Activación para que el servidor pueda enviar mensajes a otro proveedor</param>
        /// <param name="ipProveedor">IP del proveedor a donde se enviarán mensajes en caso de que el modoRouter esté encendido</param>
        void Start(int puertoLocalEscucha, bool modoTest = false, bool modoRouter = true, string ipProveedor = "127.0.0.0", List<int> listaPuertosProveedor = null);
    }
}