using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServidorCore;
using CapaNegocio;

namespace CapaPresentacion
{
    /// <summary>
    /// Clase que recibe el mensaje del cliente, realizará el envío de ese mensaje a la capa de negocio
    /// para su tratamiento y devolverá la respuesta por el socket al cliente
    /// </summary>
    class infoYSocketDeTrabajoCliente : infoYSocketDeTrabajo
    {
        public override void procesamientoTramaGeneral(string mensajeCliente)
        {
            base.procesamientoTramaGeneral(mensajeCliente);
            mensajeRecibidoAux = mensajeCliente;

            operacionesGenerales operaciones = new operacionesGenerales();

            if (operaciones.procesamientoTrama(mensajeRecibidoAux) != true)
            {
                errorParseando = true;
                cLogErrores.Escribir_Log_Error("Error en el procesamiento de la trama, procesamientoTramaGeneral, mensajeRecibidoAux: " + mensajeRecibidoAux);
            }

            secuenciaDeRespuestasAlCliente = operaciones.palabra.ToUpper();
        }
    }
}
