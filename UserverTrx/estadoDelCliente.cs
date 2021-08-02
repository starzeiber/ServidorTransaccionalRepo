using CapaNegocio;
using ServidorCore;
using System;
using System.Threading.Tasks;

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
        RespuestaGenerica respuestaGenerica;

        /// <summary>
        /// Función que realiza todo el procesamiento de particionar la trama y evaluarla
        /// </summary>
        /// <param name="mensajeCliente">Mensaje enviado por el cliente</param>
        public override void ProcesarTrama(string mensajeCliente)
        {   

            // se envía la mensajería a la capa de negocio para su evaluación
            respuestaGenerica = Operaciones.ProcesarMensajeria(ultimoMensajeRecibidoCliente, idUnicoCliente);

            // el proceso de evaluación de la mensajería entrega un codigo de respuesta
            codigoRespuesta = respuestaGenerica.codigoRespuesta;
            // también la cabecera que identifica que tipo de mensajería fue
            cabeceraMensaje = (int)respuestaGenerica.cabecerasTrama;
            // y los objetos genéricos de petición y respuesta pre seteados
            objPeticion = respuestaGenerica.objPeticionCliente;
            objRespuesta = respuestaGenerica.objRespuestaCliente;
            //TODO para pruebas
            //codigoRespuesta = 02;

        }

        /// <summary>
        /// Función que obtiene la trama de respuesta a un mensaje del cliente
        /// </summary>
        /// <param name="codigoRespuesta"></param>
        /// <param name="codigoAutorizacion"></param>
        public override void ObtenerTrama(int codigoRespuesta, int codigoAutorizacion)
        {
            switch (respuestaGenerica.cabecerasTrama)
            {
                case Operaciones.CabecerasTrama.compraTaePx:
                    this.codigoRespuesta = codigoRespuesta;
                    this.codigoAutorizacion = codigoAutorizacion;
                    RespuestaCompraPxTae respuestaCompraPxTae = objRespuesta as RespuestaCompraPxTae;
                    respuestaCompraPxTae.codigoRespuesta = codigoRespuesta;
                    respuestaCompraPxTae.autorizacion = codigoAutorizacion;
                    tramaRespuesta = respuestaCompraPxTae.ObtenerTrama();
                    break;
                case Operaciones.CabecerasTrama.consultaTaePx:
                    this.codigoRespuesta = codigoRespuesta;
                    this.codigoAutorizacion = codigoAutorizacion;
                    tramaRespuesta = (objRespuesta as RespuestaConsultaPxTae).ObtenerTrama();
                    break;
                case Operaciones.CabecerasTrama.compraDatosPx:
                    this.codigoRespuesta = codigoRespuesta;
                    this.codigoAutorizacion = codigoAutorizacion;
                    tramaRespuesta = (objRespuesta as RespuestaCompraPxDatos).ObtenerTrama();
                    break;
                case Operaciones.CabecerasTrama.consultaDatosPx:
                    this.codigoRespuesta = codigoRespuesta;
                    this.codigoAutorizacion = codigoAutorizacion;
                    tramaRespuesta = (objRespuesta as RespuestaConsultaPxDatos).ObtenerTrama();
                    break;
                case Operaciones.CabecerasTrama.compraTpv:
                    this.codigoRespuesta = codigoRespuesta;
                    this.codigoAutorizacion = codigoAutorizacion;
                    //TODO obtener la trama TPV
                    break;
                case Operaciones.CabecerasTrama.consultaTpv:
                    this.codigoRespuesta = codigoRespuesta;
                    this.codigoAutorizacion = codigoAutorizacion;
                    //TODO obtener la trama TPV
                    break;
                default:
                    break;
            }
        }
    }
}
