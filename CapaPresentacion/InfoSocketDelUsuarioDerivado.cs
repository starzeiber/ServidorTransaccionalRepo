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
    /// para su tratamiento y devolverá la respuesta por el socket al cliente. Su herencia debe ser de la clase base para unicamente
    /// sobre escribir el método a usar
    /// </summary>
    class InfoSocketDelUsuarioDerivado : InfoSocketDelUsuarioBase
    {     
        
        public override void ProcesamientoTramaGeneral(string mensajeCliente)
        {
            mensajeRecibidoAux = mensajeCliente;
            Operaciones operaciones = new Operaciones(mensajeRecibidoAux);

            if (operaciones.ParsearTrama() != true)
            {
                secuenciaDeRespuestasAlCliente = "Falla";
                errorParseando = true;
                return;
            }
            operaciones.EnviarTransaccionOtroServer();

            errorParseando = true;
        }
    }
}
