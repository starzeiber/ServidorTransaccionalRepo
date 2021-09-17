using System;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    /// <summary>
    /// Clase que contiene todas las propiedades
    /// </summary>
    public class RespuestaCompraPxTae : RespuestaCompraPxBase
    {
        /// <summary>
        /// Autorización de la recarga
        /// </summary>
        public int autorizacion { get; set; }
        /// <summary>
        /// Clave PIN o adicional a la compra
        /// </summary>
        public string PIN { get; set; }
        /// <summary>
        /// Fecha de expiración de la recarga YYMMdd
        /// </summary>
        public string fechaExpiracion { get; set; }
        /// <summary>
        /// Monto de la recarga efectuada
        /// </summary>
        public decimal monto { get; set; }
        /// <summary>
        /// Folio adicional a la recarga
        /// </summary>
        public String folio { get; set; }
        /// <summary>
        /// nombre del proveedor de la recarga
        /// </summary>
        public String nombreProveedor { get; set; }
        /// <summary>
        /// Mensaje1 imprimible en el ticket
        /// </summary>
        public String mensajeTicket1 { get; set; }
        /// <summary>
        /// Mensaje2 imprimible en el ticket
        /// </summary>
        public String mensajeTicket2 { get; set; }
        /// <summary>
        /// Código de respuesta sobre la transacción
        /// </summary>
        public int codigoRespuesta { get; set; }


        /// <summary>
        /// Constructor de la clase
        /// </summary>
        public RespuestaCompraPxTae()
        {
            encabezado = int.Parse(Utileria.ENCABEZADO_RESPUESTA_TAE_PX);
            PIN = "";
            fechaExpiracion = "";
            folio = "";
            nombreProveedor = "";
            mensajeTicket1 = "";
            mensajeTicket2 = "";
        }

        /// <summary>
        /// Ingresa los datos de la clase de compra a la clase de respuesta
        /// </summary>
        /// <param name="compraPxTae">Instancia de CompraPxTae</param>
        /// <returns></returns>
        public bool Ingresar(CompraPxTae compraPxTae)
        {
            try
            {
                idGrupo = compraPxTae.idGrupo;
                idCadena = compraPxTae.idCadena;
                idTienda = compraPxTae.idTienda;
                idPos = compraPxTae.idPos;
                fecha = compraPxTae.fecha;
                hora = compraPxTae.hora;
                region = compraPxTae.region;
                sku = compraPxTae.sku;
                telefono = compraPxTae.telefono;
                numeroTransaccion = compraPxTae.numeroTransaccion;
                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion("Error en el parseo de la trama: " + ex.Message), Utileria.TiposLog.error));
                return false;
            }

        }

        /// <summary>
        /// Actualiza las propiedades de la clase con la información de compra más reciente
        /// </summary>
        /// <param name="compraPxTae"></param>
        public void Actualizar(CompraPxTae compraPxTae)
        {
            monto = compraPxTae.productoInfo.monto;
            nombreProveedor = compraPxTae.proveedorInfo.nombreProveedor;
            mensajeTicket1 = compraPxTae.productoInfo.mensajeTicket1;
            mensajeTicket2 = compraPxTae.productoInfo.mensajeTicket2;
        }

        /// <summary>
        /// Obtiene todos los parámetros de una trama tae de PX
        /// </summary>
        /// <param name="tramaRecibida">trama recibida desde el px</param>
        /// <returns></returns>
        public Boolean Ingresar(String tramaRecibida)
        {
            try
            {
                autorizacion = int.Parse(tramaRecibida.Substring(63, 9));
                fechaExpiracion = tramaRecibida.Substring(92, 6);
                monto = int.Parse(tramaRecibida.Substring(98, 9));
                nombreProveedor = tramaRecibida.Substring(127, 14);
                mensajeTicket1 = tramaRecibida.Substring(141, 80);
                mensajeTicket2 = tramaRecibida.Substring(221, 60);
                codigoRespuesta = int.Parse(tramaRecibida.Substring(281, 2));
                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion("Error en el parseo de la trama: " + ex.Message), Utileria.TiposLog.error));

                return false;
            }
        }

        /// <summary>
        /// Obtiene la trama a partir de las propiedades de la clase
        /// </summary>
        /// <returns></returns>
        public string ObtenerTrama()
        {
            StringBuilder respuesta = new StringBuilder();
            try
            {
                respuesta.Append(encabezado.ToString());
                respuesta.Append(Validaciones.formatoValor(idGrupo.ToString(), TipoFormato.N, 4));
                respuesta.Append(Validaciones.formatoValor(idCadena.ToString(), TipoFormato.N, 4));
                respuesta.Append(Validaciones.formatoValor(idTienda.ToString(), TipoFormato.N, 4));
                respuesta.Append(Validaciones.formatoValor(idPos.ToString(), TipoFormato.N, 4));
                respuesta.Append(Validaciones.formatoValor(fecha, TipoFormato.N, 6));
                respuesta.Append(Validaciones.formatoValor(hora, TipoFormato.N, 6));
                respuesta.Append(Validaciones.formatoValor(region.ToString(), TipoFormato.N, 2));
                respuesta.Append(Validaciones.formatoValor(sku, TipoFormato.ANS, 20));
                respuesta.Append(Validaciones.formatoValor(telefono, TipoFormato.N, 10));
                respuesta.Append(Validaciones.formatoValor(numeroTransaccion.ToString(), TipoFormato.N, 5));
                respuesta.Append(Validaciones.formatoValor(autorizacion.ToString(), TipoFormato.N, 9));
                respuesta.Append(Validaciones.formatoValor(PIN.ToString(), TipoFormato.ANS, 20));
                respuesta.Append(Validaciones.formatoValor(fechaExpiracion.ToString(), TipoFormato.N, 6));
                if (monto > 0)
                {
                    respuesta.Append(Validaciones.formatoValor(monto.ToString().Split('.')[0] + monto.ToString().Split('.')[1], TipoFormato.N, 9));
                }
                else
                {
                    respuesta.Append(Validaciones.formatoValor(monto.ToString(), TipoFormato.N, 9));
                }
                respuesta.Append(Validaciones.formatoValor(nombreProveedor.ToString(), TipoFormato.ANS, 14));
                respuesta.Append(Validaciones.formatoValor(mensajeTicket1.ToString(), TipoFormato.ANS, 80));
                respuesta.Append(Validaciones.formatoValor(mensajeTicket2.ToString(), TipoFormato.ANS, 60));
                respuesta.Append(Validaciones.formatoValor(codigoRespuesta.ToString(), TipoFormato.N, 2));

                return respuesta.ToString();
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message), Utileria.TiposLog.error));
                return String.Empty;
            }
        }
    }
}
