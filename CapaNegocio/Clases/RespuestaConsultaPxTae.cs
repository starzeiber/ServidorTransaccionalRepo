using System;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    /// <summary>
    /// Clase que contiene todas las propiedades
    /// </summary>
    public class RespuestaConsultaPxTae : RespuestaCompraPxBase
    {
        /// <summary>
        /// Autorización de la recarga
        /// </summary>
        public int autorizacion { get; set; }
        /// <summary>
        /// Clave PIN o adicional a la compra
        /// </summary>
        public String PIN { get; set; }
        /// <summary>
        /// Fecha de expiración de la recarga YYMMdd
        /// </summary>
        public String fechaExpiracion { get; set; }
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
        /// Constructor del objeto
        /// </summary>
        public RespuestaConsultaPxTae()
        {
            encabezado = int.Parse(Utileria.ENCABEZADO_RESPUESTA_CONSULTA_TAE_PX);
            PIN = "";
            fechaExpiracion = "";
            folio = "";
            nombreProveedor = "";
            mensajeTicket1 = "";
            mensajeTicket2 = "";
        }

        /// <summary>
        /// Ingresa la información de consulta en las propiedades de la clase
        /// </summary>
        /// <param name="consultaPxTae">Instancia de ConsultaPxTae</param>
        /// <returns></returns>
        public bool Ingresar(ConsultaPxTae consultaPxTae)
        {
            try
            {
                idGrupo = consultaPxTae.idGrupo;
                idCadena = consultaPxTae.idCadena;
                idTienda = consultaPxTae.idTienda;
                idPos = consultaPxTae.idPos;
                fecha = consultaPxTae.fecha;
                hora = consultaPxTae.hora;
                region = consultaPxTae.region;
                sku = consultaPxTae.sku;
                telefono = consultaPxTae.telefono;
                numeroTransaccion = consultaPxTae.numeroTransaccion;
                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerRutaDeLlamada(ex.Message), Utileria.TiposLog.error));
                return false;
            }
        }

        /// <summary>
        /// Actualiza la información de algunas propiedades de la clase a partir de la información de la consulta más reciente
        /// </summary>
        /// <param name="consultaPxTae">Instancia de ConsultaPxTae</param>
        public void Actualizar(ConsultaPxTae consultaPxTae)
        {
            monto = consultaPxTae.productoInfo.monto;
            nombreProveedor = consultaPxTae.proveedorInfo.nombreProveedor;
            mensajeTicket1 = consultaPxTae.productoInfo.mensajeTicket1;
            mensajeTicket2 = consultaPxTae.productoInfo.mensajeTicket2;
        }

        /// <summary>
        /// Función que obtiene los parámetros de la trama entrante
        /// </summary>
        /// <param name="tramaRecibida">Trama de respuesta para poder dividirla</param>
        /// <returns></returns>
        public Boolean ObtenerParametrosTrama(String tramaRecibida)
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
                Task.Run(() => Utileria.Log(Utileria.ObtenerRutaDeLlamada(ex.Message), Utileria.TiposLog.error));
                return false;
            }

        }
        /// <summary>
        /// Obtiene la trama de respuesta a partir de las propiedades de la clase
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
                respuesta.Append(Validaciones.formatoValor(monto.ToString().Split('.')[0] + monto.ToString().Split('.')[1], TipoFormato.N, 9));
                respuesta.Append(Validaciones.formatoValor(nombreProveedor.ToString(), TipoFormato.ANS, 14));
                respuesta.Append(Validaciones.formatoValor(mensajeTicket1.ToString(), TipoFormato.ANS, 80));
                respuesta.Append(Validaciones.formatoValor(mensajeTicket2.ToString(), TipoFormato.ANS, 60));
                respuesta.Append(Validaciones.formatoValor(codigoRespuesta.ToString(), TipoFormato.N, 2));

                return respuesta.ToString();
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerRutaDeLlamada(ex.Message), Utileria.TiposLog.error));
                return String.Empty;
            }
        }
    }
}
