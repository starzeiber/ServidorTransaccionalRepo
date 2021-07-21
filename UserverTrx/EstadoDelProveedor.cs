using CapaNegocio;
using CapaNegocio.Clases;
using ServidorCore;
using System;
using System.Threading.Tasks;

namespace Userver
{
    public class EstadoDelProveedor : EstadoDelProveedorBase
    {
        RespuestaGenerica respuestaGenerica = new RespuestaGenerica();
        public override void IngresarDatos(int cabeceraMensaje, object objPeticion)
        {
            try
            {
                switch (cabeceraMensaje)
                {
                    case (int)Operaciones.CabecerasTrama.compraTaePx:
                        CompraPxTae compraPxTae = objPeticion as CompraPxTae;
                        Task<RespuestaGenerica> procesarMensajeriaTask = Task.Run(() => Operaciones.CompraTpv(compraPxTae));
                        procesarMensajeriaTask.Wait();
                        codigoRespuesta = procesarMensajeriaTask.Result.codigoRespuesta;
                        this.objPeticion = procesarMensajeriaTask.Result.objPeticionProveedor;
                        this.objRespuesta = procesarMensajeriaTask.Result.objRespuestaProveedor;
                        break;
                    case (int)Operaciones.CabecerasTrama.compraDatosPx:
                        break;
                    case (int)Operaciones.CabecerasTrama.consultaTaePx:
                        break;
                    case (int)Operaciones.CabecerasTrama.consultaDatosPx:
                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public override void ProcesarTramaDelProveeedor(string mensaje)
        {

        }

        public override void ObtenerTramaPeticion()
        {
            switch (respuestaGenerica.categoriaProducto)
            {
                case Operaciones.categoriaProducto.TAE:
                    CompraTpvTae compraTpvTae = objPeticion as CompraTpvTae;
                    tramaSolicitud = compraTpvTae.Obtener();
                    CheaderTPV.CheaderTPV cheaderTPV = new CheaderTPV.CheaderTPV();
                    tramaSolicitud = cheaderTPV.CreaHeader(tramaSolicitud.Length) + tramaSolicitud;
                    break;
                case Operaciones.categoriaProducto.Datos:
                    break;
                default:
                    break;
            }
        }

        public override void ObtenerTramaRespuesta(int codigoRespuesta, int codigoAutorizacion)
        {
            switch (respuestaGenerica.categoriaProducto)
            {
                case Operaciones.categoriaProducto.TAE:
                    this.codigoRespuesta = codigoRespuesta;
                    this.codigoAutorizacion = codigoAutorizacion;
                    CompraTpvTae compraTpvTae = objPeticion as CompraTpvTae;
                    RespuestaCompraTpvTAE respuestaCompraTpvTAE = objRespuesta as RespuestaCompraTpvTAE;
                    respuestaCompraTpvTAE.Ingresar(compraTpvTae);
                    respuestaCompraTpvTAE.codigoRespuesta = codigoRespuesta;
                    respuestaCompraTpvTAE.autorizacion = codigoAutorizacion;
                    tramaRespuesta = respuestaCompraTpvTAE.Obtener();
                    break;
                case Operaciones.categoriaProducto.Datos:
                    break;
                default:
                    break;
            }
        }
    }
}
