using CapaNegocio;
using ServidorCore;

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
            respuestaGenerica = Operaciones.ProcesarMensajeria(mensajeCliente);

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
                    if (codigoRespuesta == 0 && codigoAutorizacion > 0)
                    {
                        respuestaCompraPxTae.monto = (objPeticion as CompraPxTae).productoInfo.monto;
                        respuestaCompraPxTae.nombreProveedor = (objPeticion as CompraPxTae).productoInfo.nombreProveedor;
                        respuestaCompraPxTae.mensajeTicket1 = (objPeticion as CompraPxTae).productoInfo.mensajeTicket1;
                        respuestaCompraPxTae.mensajeTicket2 = (objPeticion as CompraPxTae).productoInfo.mensajeTicket2;
                    }
                    respuestaCompraPxTae.codigoRespuesta = codigoRespuesta;
                    respuestaCompraPxTae.autorizacion = codigoAutorizacion;
                    tramaRespuesta = respuestaCompraPxTae.ObtenerTrama();
                    break;
                case Operaciones.CabecerasTrama.consultaTaePx:
                    this.codigoRespuesta = codigoRespuesta;
                    this.codigoAutorizacion = codigoAutorizacion;
                    RespuestaConsultaPxTae respuestaConsultaPxTae = objRespuesta as RespuestaConsultaPxTae;
                    if (codigoRespuesta == 0 && codigoAutorizacion > 0)
                    {
                        respuestaConsultaPxTae.monto = (objPeticion as ConsultaPxTae).productoInfo.monto;
                        respuestaConsultaPxTae.nombreProveedor = (objPeticion as ConsultaPxTae).productoInfo.nombreProveedor;
                        respuestaConsultaPxTae.mensajeTicket1 = (objPeticion as ConsultaPxTae).productoInfo.mensajeTicket1;
                        respuestaConsultaPxTae.mensajeTicket2 = (objPeticion as ConsultaPxTae).productoInfo.mensajeTicket2;
                    }
                    respuestaConsultaPxTae.codigoRespuesta = codigoRespuesta;
                    respuestaConsultaPxTae.autorizacion = codigoAutorizacion;
                    tramaRespuesta = respuestaConsultaPxTae.ObtenerTrama();
                    break;
                case Operaciones.CabecerasTrama.compraDatosPx:
                    this.codigoRespuesta = codigoRespuesta;
                    this.codigoAutorizacion = codigoAutorizacion;
                    RespuestaCompraPxDatos respuestaCompraPxDatos = objRespuesta as RespuestaCompraPxDatos;
                    if (codigoRespuesta == 0 && codigoAutorizacion > 0)
                    {
                        respuestaCompraPxDatos.monto = (objPeticion as CompraPxDatos).productoInfo.monto;
                        respuestaCompraPxDatos.nombreProveedor = (objPeticion as CompraPxDatos).productoInfo.nombreProveedor;
                        respuestaCompraPxDatos.mensajeTicket1 = (objPeticion as CompraPxDatos).productoInfo.mensajeTicket1;
                        respuestaCompraPxDatos.mensajeTicket2 = (objPeticion as CompraPxDatos).productoInfo.mensajeTicket2;
                    }
                    respuestaCompraPxDatos.codigoRespuesta = codigoRespuesta;
                    respuestaCompraPxDatos.autorizacion = codigoAutorizacion;
                    tramaRespuesta = respuestaCompraPxDatos.ObtenerTrama();
                    break;
                case Operaciones.CabecerasTrama.consultaDatosPx:
                    this.codigoRespuesta = codigoRespuesta;
                    this.codigoAutorizacion = codigoAutorizacion;
                    RespuestaConsultaPxDatos respuestaConsultaPxDatos = objRespuesta as RespuestaConsultaPxDatos;
                    if (codigoRespuesta == 0 && codigoAutorizacion > 0)
                    {
                        respuestaConsultaPxDatos.monto = (objPeticion as ConsultaPxDatos).productoInfo.monto;
                        respuestaConsultaPxDatos.nombreProveedor = (objPeticion as ConsultaPxDatos).productoInfo.nombreProveedor;
                        respuestaConsultaPxDatos.mensajeTicket1 = (objPeticion as ConsultaPxDatos).productoInfo.mensajeTicket1;
                        respuestaConsultaPxDatos.mensajeTicket2 = (objPeticion as ConsultaPxDatos).productoInfo.mensajeTicket2;
                    }
                    respuestaConsultaPxDatos.codigoRespuesta = codigoRespuesta;
                    respuestaConsultaPxDatos.autorizacion = codigoAutorizacion;
                    tramaRespuesta = respuestaConsultaPxDatos.ObtenerTrama();
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
