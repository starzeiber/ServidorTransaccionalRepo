using CapaNegocio;
using CapaNegocio.Clases;
using ServidorCore;
using System;

namespace Userver
{
    public class EstadoDelProveedor : EstadoDelProveedorBase
    {
        RespuestaProcesosProveedor respuestaProcesosProveedor;

        public override void IngresarObjetoPeticionCliente(object obj)
        {
            respuestaProcesosProveedor = Operaciones.ProcesarMensajeriaProveedor(obj);

            codigoRespuesta = respuestaProcesosProveedor.codigoRespuesta;
            objPeticion = respuestaProcesosProveedor.objPeticionProveedor;
            objRespuesta = respuestaProcesosProveedor.objRespuestaProveedor;
        }

        public override void ObtenerTramaPeticion()
        {
            switch (respuestaProcesosProveedor.categoriaProducto)
            {
                case Operaciones.CategoriaProducto.TAE:
                    CompraTpvTae compraTpvTae = objPeticion as CompraTpvTae;
                    tramaSolicitud = compraTpvTae.Obtener();
                    CheaderTPV.CheaderTPV cheaderTPV = new CheaderTPV.CheaderTPV();
                    tramaSolicitud = cheaderTPV.CreaHeader(tramaSolicitud.Length) + tramaSolicitud;
                    break;
                case Operaciones.CategoriaProducto.Datos:
                    break;
                default:
                    break;
            }
        }

        public override void ProcesarTramaDelProveeedor(string trama)
        {
            Operaciones.ProcesarTramaProveedor(trama, ref respuestaProcesosProveedor);

            // el proceso de evaluación de la mensajería entrega un codigo de respuesta
            codigoRespuesta = respuestaProcesosProveedor.codigoRespuesta;
            // y los objetos genéricos de petición y respuesta pre seteados
            objPeticion = respuestaProcesosProveedor.objPeticionProveedor;
            objRespuesta = respuestaProcesosProveedor.objRespuestaProveedor;

        }

        public override void ObtenerTramaRespuesta()
        {
            switch (respuestaProcesosProveedor.categoriaProducto)
            {
                case Operaciones.CategoriaProducto.TAE:
                    CompraTpvTae compraTpvTae = objPeticion as CompraTpvTae;
                    RespuestaCompraTpvTAE respuestaCompraTpvTAE = objRespuesta as RespuestaCompraTpvTAE;
                    respuestaCompraTpvTAE.Ingresar(compraTpvTae);
                    tramaRespuesta = respuestaCompraTpvTAE.Obtener();
                    codigoAutorizacion = respuestaCompraTpvTAE.autorizacion;
                    break;
                case Operaciones.CategoriaProducto.Datos:
                    break;
                default:
                    break;
            }
        }

        public override void GuardarTransaccion()
        {
            try
            {
                Type tipo = objRespuesta.GetType();
                if (tipo == typeof(RespuestaCompraTpvTAE))
                {
                    CompraTpvTae compraTpvTae = objPeticion as CompraTpvTae;
                    RespuestaCompraTpvTAE respuestaCompraTpvTAE = objRespuesta as RespuestaCompraTpvTAE;
                    respuestaCompraTpvTAE.codigoRespuesta = codigoRespuesta;
                    respuestaCompraTpvTAE.autorizacion = codigoAutorizacion;
                    Operaciones.GuardarTrx(compraTpvTae, respuestaCompraTpvTAE);
                }
                else if (tipo == typeof(RespuestaCompraTpvTAE))
                {
                    CompraTpvDatos compraTpvDatos = objPeticion as CompraTpvDatos;
                    RespuestaCompraTpvDatos respuestaCompraTpvDatos = objRespuesta as RespuestaCompraTpvDatos;
                    respuestaCompraTpvDatos.codigoRespuesta = codigoRespuesta;
                    respuestaCompraTpvDatos.autorizacion = codigoAutorizacion;
                    
                }
            }
            catch (Exception)
            {
            }

        }
    }
}
