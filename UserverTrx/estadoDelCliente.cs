using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServidorCore;

namespace CapaPresentacion
{
    /// <summary>
    /// Clase que recibe el mensaje del cliente, realizará el envío de ese mensaje a la capa de negocio
    /// para su tratamiento y devolverá la respuesta por el socket al cliente
    /// </summary>
    class estadoDelCliente : EstadoDelClienteBase
    {
        public override void procesamientoTramaEntrante(string mensajeCliente)
        {
            ultimoMensajeRecibidoCliente += mensajeCliente;
            int posSeparador = ultimoMensajeRecibidoCliente.IndexOf(".");
            string palabra = "";
            if (posSeparador != -1)
            {
                palabra = ultimoMensajeRecibidoCliente.Substring(0, posSeparador);
                ultimoMensajeRecibidoCliente = ultimoMensajeRecibidoCliente.Substring(posSeparador, ultimoMensajeRecibidoCliente.Length - posSeparador - 1);
                secuenciaDeRespuestasAlCliente = palabra.ToUpper();
            }
        }
    }
}
