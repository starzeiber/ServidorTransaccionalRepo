using CapaNegocio;
using ServidorCore;
using System;

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

            // se envía la mensajería a la capa de negocio para su evaluación
            respuestaProcesosCliente = Operaciones.ProcesarMensajeriaCliente(mensajeCliente);

            // el proceso de evaluación de la mensajería entrega un codigo de respuesta
            codigoRespuesta = respuestaProcesosCliente.codigoRespuesta;
            // también la cabecera que identifica que tipo de mensajería fue
            //cabeceraMensaje = (int)respuestaProcesosCliente.cabeceraTrama;
            // y los objetos genéricos de petición y respuesta pre seteados
            objPeticion = respuestaProcesosCliente.objPeticionCliente;
            objRespuesta = respuestaProcesosCliente.objRespuestaCliente;
        }

        /// <summary>
        /// Función que obtiene la trama de respuesta a un mensaje del cliente
        /// </summary>
        /// <param name="codigoRespuesta"></param>
        /// <param name="codigoAutorizacion"></param>
        public override void ObtenerTramaRespuesta()
        {
            Type tipo = objRespuesta.GetType();

            if (tipo == typeof(RespuestaCompraPxTae))
            {
                RespuestaCompraPxTae respuestaCompraPxTae = objRespuesta as RespuestaCompraPxTae;
                if (codigoRespuesta != 0)
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
                if (codigoRespuesta != 0)
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
                if (codigoRespuesta != 0)
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
                if (codigoRespuesta != 0)
                {
                    respuestaConsultaPxDatos.nombreProveedor = "";
                    respuestaConsultaPxDatos.mensajeTicket1 = "";
                    respuestaConsultaPxDatos.mensajeTicket2 = "";
                }
                respuestaConsultaPxDatos.codigoRespuesta = codigoRespuesta;
                respuestaConsultaPxDatos.autorizacion = codigoAutorizacion;
                tramaRespuesta = respuestaConsultaPxDatos.ObtenerTrama();
            }


            //switch (respuestaProcesosCliente.cabeceraTrama)
            //{
            //    case Operaciones.CabecerasTrama.compraTaePx:
            //        RespuestaCompraPxTae respuestaCompraPxTae = objRespuesta as RespuestaCompraPxTae;
            //        if (codigoRespuesta != 0)
            //        {
            //            respuestaCompraPxTae.nombreProveedor = "";
            //            respuestaCompraPxTae.mensajeTicket1 = "";
            //            respuestaCompraPxTae.mensajeTicket2 = "";
            //        }
            //        respuestaCompraPxTae.codigoRespuesta = codigoRespuesta;
            //        respuestaCompraPxTae.autorizacion = codigoAutorizacion;
            //        tramaRespuesta = respuestaCompraPxTae.ObtenerTrama();
            //        break;
            //    case Operaciones.CabecerasTrama.consultaTaePx:
            //        RespuestaConsultaPxTae respuestaConsultaPxTae = objRespuesta as RespuestaConsultaPxTae;
            //        if (codigoRespuesta != 0)
            //        {
            //            respuestaConsultaPxTae.nombreProveedor = "";
            //            respuestaConsultaPxTae.mensajeTicket1 = "";
            //            respuestaConsultaPxTae.mensajeTicket2 = "";
            //        }
            //        respuestaConsultaPxTae.codigoRespuesta = codigoRespuesta;
            //        respuestaConsultaPxTae.autorizacion = codigoAutorizacion;
            //        tramaRespuesta = respuestaConsultaPxTae.ObtenerTrama();
            //        break;
            //    case Operaciones.CabecerasTrama.compraDatosPx:
            //        RespuestaCompraPxDatos respuestaCompraPxDatos = objRespuesta as RespuestaCompraPxDatos;
            //        if (codigoRespuesta != 0)
            //        {
            //            respuestaCompraPxDatos.nombreProveedor = "";
            //            respuestaCompraPxDatos.mensajeTicket1 = "";
            //            respuestaCompraPxDatos.mensajeTicket2 = "";
            //        }
            //        respuestaCompraPxDatos.codigoRespuesta = codigoRespuesta;
            //        respuestaCompraPxDatos.autorizacion = codigoAutorizacion;
            //        tramaRespuesta = respuestaCompraPxDatos.ObtenerTrama();
            //        break;
            //    case Operaciones.CabecerasTrama.consultaDatosPx:
            //        RespuestaConsultaPxDatos respuestaConsultaPxDatos = objRespuesta as RespuestaConsultaPxDatos;
            //        if (codigoRespuesta != 0)
            //        {
            //            respuestaConsultaPxDatos.nombreProveedor = "";
            //            respuestaConsultaPxDatos.mensajeTicket1 = "";
            //            respuestaConsultaPxDatos.mensajeTicket2 = "";
            //        }
            //        respuestaConsultaPxDatos.codigoRespuesta = codigoRespuesta;
            //        respuestaConsultaPxDatos.autorizacion = codigoAutorizacion;
            //        tramaRespuesta = respuestaConsultaPxDatos.ObtenerTrama();
            //        break;
            //    case Operaciones.CabecerasTrama.compraTpv:
            //        //TODO obtener la trama TPV
            //        break;
            //    case Operaciones.CabecerasTrama.consultaTpv:
            //        //TODO obtener la trama TPV
            //        break;
            //    default:
            //        break;
            //}
        }

    }
}
