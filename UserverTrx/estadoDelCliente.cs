using CapaNegocio;
using ServidorCore;
using System;

namespace CapaPresentacion
{
    /// <summary>
    /// Clase que recibe el mensaje del cliente, realizará el envío de ese mensaje a la capa de negocio
    /// para su tratamiento y devolverá la respuesta por el socket al cliente
    /// </summary>
    class EstadoDelCliente : EstadoDelClienteBase
    {
        
        public override void ProcesamientoTramaEntrante(string mensajeCliente)
        {
            ultimoMensajeRecibidoCliente += mensajeCliente;
            //TODO colocar el fin de texto
            int posSeparadorTramas = ultimoMensajeRecibidoCliente.IndexOf(".");
            
            string tramaProveedor = "";
            if (posSeparadorTramas != -1)
            {
                //mensajeRespuesta = ultimoMensajeRecibidoCliente.Substring(0, posSeparadorTramas);
                ultimoMensajeRecibidoCliente = ultimoMensajeRecibidoCliente.Substring(posSeparadorTramas, ultimoMensajeRecibidoCliente.Length - posSeparadorTramas - 1);

                tramaProveedor = Operaciones.PrepararMensajeriaProveedor(ultimoMensajeRecibidoCliente);

                base.tramaEnvioProveedor = tramaProveedor;

                secuenciaDeRespuestasAlCliente = tramaProveedor.ToUpper();

            }
        }

    }
}
