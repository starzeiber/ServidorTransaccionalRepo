using CapaNegocio;
using ServidorCore;
using System.Threading.Tasks;

namespace Userver
{
    /// <summary>
    /// Clase que recibe el mensaje del cliente, realizará el envío de ese mensaje a la capa de negocio
    /// para su tratamiento y devolverá la respuesta por el socket al cliente
    /// </summary>
    class EstadoDelCliente : EstadoDelClienteBase
    {
        RespuestaGenerica respuestaGenerica;
        public override void ProcesarTrama(string mensajeCliente)
        {
            if (mensajeCliente.Length ==0)
            {
                codigoRespuesta = 30;
            }
            ultimoMensajeRecibidoCliente = mensajeCliente;
            //TODO colocar el fin de texto de trama TPV
            int posSeparadorTramas = ultimoMensajeRecibidoCliente.IndexOf(".");
            if (posSeparadorTramas != -1)
            {
                ultimoMensajeRecibidoCliente = ultimoMensajeRecibidoCliente.Substring(0, posSeparadorTramas);

                Task<RespuestaGenerica> procesarMensajeriaTask = Task.Run(() => Operaciones.ProcesarMensajeria(ultimoMensajeRecibidoCliente));
                procesarMensajeriaTask.Wait();

                respuestaGenerica = procesarMensajeriaTask.Result;
                codigoRespuesta = procesarMensajeriaTask.Result.codigoRespuesta;
                cabeceraMensaje = (int)procesarMensajeriaTask.Result.cabecerasTrama;
                objPeticion = respuestaGenerica.objPeticionCliente;
                objRespuesta = respuestaGenerica.objRespuestaCliente;
                //TODO para pruebas
                //codigoRespuesta = 02;
            }
            else
            {

                codigoRespuesta = (int)UtileriaVariablesGlobales.codigosRespuesta.ErrorFormato;
            }
        }

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
