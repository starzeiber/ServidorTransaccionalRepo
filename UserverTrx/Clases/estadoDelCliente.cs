using CapaNegocio;
using ServidorCore;
using System;
using static CapaNegocio.Utileria;

namespace Userver
{
    /// <summary>
    /// Clase que recibe el mensaje del cliente, realizará el envío de ese mensaje a la capa de negocio
    /// para su tratamiento y devolverá la respuesta por el socket al cliente
    /// </summary>
    class EstadoDelCliente : EstadoDelClienteBase
    {
        /// <summary>
        /// Instancia que contendrá la respuesta y parametros necesarios sobre la evaluación de la mensajería
        /// </summary>
        private RespuestaProcesosCliente respuestaProcesosCliente;

        /// <summary>
        /// Función que realiza todo el procesamiento de particionar la trama y evaluarla
        /// </summary>
        /// <param name="mensajeCliente">Mensaje enviado por el cliente</param>
        public override void ProcesarTrama(string mensajeCliente)
        {
            Utileria.performancePeticionesEntrantesClientesUserver.IncrementBy(1);
            // se envía la mensajería a la capa de negocio para su evaluación
            respuestaProcesosCliente = Operaciones.ProcesarMensajeriaCliente(mensajeCliente);

            // el proceso de evaluación de la mensajería entrega un codigo de respuesta
            codigoRespuesta = respuestaProcesosCliente.codigoRespuesta;

            if (codigoRespuesta != (int)CodigosRespuesta.ErrorFormato && codigoRespuesta != (int)CodigosRespuesta.ErrorProceso)
            {
                // y los objetos genéricos de petición y respuesta pre seteados
                objPeticion = respuestaProcesosCliente.objPeticionCliente;
                objRespuesta = respuestaProcesosCliente.objRespuestaCliente;

                if (objPeticion.GetType() == typeof(ConsultaPxTae) || objPeticion.GetType() == typeof(ConsultaPxDatos))
                {
                    esConsulta = true;
                    respuestaProcesosCliente = Operaciones.ConsultaTrxBaseTransaccional(objPeticion);
                    codigoRespuesta = respuestaProcesosCliente.codigoRespuesta;
                    if (respuestaProcesosCliente.objetoAux != null)
                        codigoAutorizacion = (int)respuestaProcesosCliente.objetoAux;
                }
            }
        }

        /// <summary>
        /// Función que obtiene la trama de respuesta a un mensaje del cliente
        /// </summary>
        public override void ObtenerTramaRespuesta()
        {
            try
            {
                Type tipo = objRespuesta.GetType();
                
                if (tipo == typeof(RespuestaCompraPxTae))
                {
                    RespuestaCompraPxTae respuestaCompraPxTae = objRespuesta as RespuestaCompraPxTae;
                    if (codigoRespuesta != (int)CodigosRespuesta.TransaccionExitosa)
                    {
                        respuestaCompraPxTae.nombreProveedor = "";
                        respuestaCompraPxTae.mensajeTicket1 = "";
                        respuestaCompraPxTae.mensajeTicket2 = "";
                    }
                    respuestaCompraPxTae.codigoRespuesta = codigoRespuesta;
                    respuestaCompraPxTae.autorizacion = codigoAutorizacion;
                    tramaRespuesta = respuestaCompraPxTae.ObtenerTrama();
                }
                else if (tipo == typeof(RespuestaConsultaPxTae))
                {
                    RespuestaConsultaPxTae respuestaConsultaPxTae = objRespuesta as RespuestaConsultaPxTae;
                    if (codigoRespuesta != (int)CodigosRespuesta.TransaccionExitosa)
                    {
                        respuestaConsultaPxTae.nombreProveedor = "";
                        respuestaConsultaPxTae.mensajeTicket1 = "";
                        respuestaConsultaPxTae.mensajeTicket2 = "";
                    }
                    respuestaConsultaPxTae.codigoRespuesta = codigoRespuesta;
                    respuestaConsultaPxTae.autorizacion = codigoAutorizacion;
                    tramaRespuesta = respuestaConsultaPxTae.ObtenerTrama();
                }
                else if (tipo == typeof(RespuestaCompraPxDatos))
                {
                    RespuestaCompraPxDatos respuestaCompraPxDatos = objRespuesta as RespuestaCompraPxDatos;
                    if (codigoRespuesta != (int)CodigosRespuesta.TransaccionExitosa)
                    {
                        respuestaCompraPxDatos.nombreProveedor = "";
                        respuestaCompraPxDatos.mensajeTicket1 = "";
                        respuestaCompraPxDatos.mensajeTicket2 = "";
                    }
                    respuestaCompraPxDatos.codigoRespuesta = codigoRespuesta;
                    respuestaCompraPxDatos.autorizacion = codigoAutorizacion;
                    tramaRespuesta = respuestaCompraPxDatos.ObtenerTrama();
                }
                else if (tipo == typeof(RespuestaConsultaPxDatos))
                {
                    RespuestaConsultaPxDatos respuestaConsultaPxDatos = objRespuesta as RespuestaConsultaPxDatos;
                    if (codigoRespuesta != (int)CodigosRespuesta.TransaccionExitosa)
                    {
                        respuestaConsultaPxDatos.nombreProveedor = "";
                        respuestaConsultaPxDatos.mensajeTicket1 = "";
                        respuestaConsultaPxDatos.mensajeTicket2 = "";
                    }
                    respuestaConsultaPxDatos.codigoRespuesta = codigoRespuesta;
                    respuestaConsultaPxDatos.autorizacion = codigoAutorizacion;
                    tramaRespuesta = respuestaConsultaPxDatos.ObtenerTrama();
                }
                Utileria.performancePeticionesRespondidasClientesUserver.IncrementBy(1);
            }
            catch (Exception)
            {

            }            
        }

    }
}
