using System;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    /// <summary>
    /// Clase que contiene todas las propiedades
    /// </summary>
    public class RespuestaConsultaPxDatos : RespuestaCompraPxBase
    {
        /// <summary>
        /// Campo para información de la cuenta
        /// </summary>
        public String cuenta { get; set; }
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
        /// información del paquete
        /// </summary>
        public String datosAdicionales { get; set; }
        /// <summary>
        /// Extensión del protocolo para información varia
        /// </summary>
        public String extension { get; set; }

        /// <summary>
        /// Constructor del objeto
        /// </summary>
        public RespuestaConsultaPxDatos()
        {
            encabezado = int.Parse(Utileria.ENCABEZADO_RESPUESTA_CONSULTA_DATOS_PX);
            PIN = "";
            fechaExpiracion = "";
            nombreProveedor = "";
            mensajeTicket1 = "";
            mensajeTicket2 = "";

        }

        /// <summary>
        /// Ingresa la información de la compra en las propiedades de la clase de respuesta
        /// </summary>
        /// <param name="consultaPxDatos"></param>
        /// <returns></returns>
        public bool Ingresar(ConsultaPxDatos consultaPxDatos)
        {
            try
            {
                idGrupo = consultaPxDatos.idGrupo;
                idCadena = consultaPxDatos.idCadena;
                idTienda = consultaPxDatos.idTienda;
                idPos = consultaPxDatos.idPos;
                fecha = consultaPxDatos.fecha;
                hora = consultaPxDatos.hora;
                region = consultaPxDatos.region;
                sku = consultaPxDatos.sku;
                cuenta = consultaPxDatos.cuenta;
                numeroTransaccion = consultaPxDatos.numeroTransaccion;
                monto = consultaPxDatos.monto;
                folio = consultaPxDatos.folio;
                datosAdicionales = consultaPxDatos.datosAdicionales;
                extension = consultaPxDatos.extension;
                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerRutaDeLlamada("Error en el parseo de la trama: " + ex.Message), Utileria.TiposLog.error));

                return false;
            }
        }

        /// <summary>
        /// Actualiza la información de las propiedades de la clase con la información de la consulta más reciente
        /// </summary>
        /// <param name="consultaPxDatos"></param>
        public void Actualizar(ConsultaPxDatos consultaPxDatos)
        {
            monto = consultaPxDatos.productoInfo.monto;
            nombreProveedor = consultaPxDatos.proveedorInfo.nombreProveedor;
            mensajeTicket1 = consultaPxDatos.productoInfo.mensajeTicket1;
            mensajeTicket2 = consultaPxDatos.productoInfo.mensajeTicket2;
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
                Task.Run(() => Utileria.Log(Utileria.ObtenerRutaDeLlamada("Error en el parseo de la trama: " + ex.Message), Utileria.TiposLog.error));

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
                respuesta.Append(Utileria.formatoValor(idCadena.ToString(), Utileria.TipoFormato.N, 4));
                respuesta.Append(Utileria.formatoValor(idTienda.ToString(), Utileria.TipoFormato.N, 4));
                respuesta.Append(Utileria.formatoValor(idPos.ToString(), Utileria.TipoFormato.N, 4));
                respuesta.Append(Utileria.formatoValor(fecha, Utileria.TipoFormato.N, 6));
                respuesta.Append(Utileria.formatoValor(hora, Utileria.TipoFormato.N, 6));
                respuesta.Append(Utileria.formatoValor(region.ToString(), Utileria.TipoFormato.N, 2));
                respuesta.Append(Utileria.formatoValor(sku, Utileria.TipoFormato.ANS, 20));
                respuesta.Append(Utileria.formatoValor(cuenta, Utileria.TipoFormato.N, 10));
                respuesta.Append(Utileria.formatoValor(numeroTransaccion.ToString(), Utileria.TipoFormato.N, 5));
                respuesta.Append(Utileria.formatoValor(autorizacion.ToString(), Utileria.TipoFormato.N, 9));
                respuesta.Append(Utileria.formatoValor(PIN.ToString(), Utileria.TipoFormato.ANS, 20));
                respuesta.Append(Utileria.formatoValor(fechaExpiracion.ToString(), Utileria.TipoFormato.N, 6));
                respuesta.Append(Utileria.formatoValor(monto.ToString().Split('.')[0] + monto.ToString().Split('.')[1], Utileria.TipoFormato.N, 9));
                respuesta.Append(Utileria.formatoValor(folio, Utileria.TipoFormato.N, 20));
                respuesta.Append(Utileria.formatoValor(nombreProveedor, Utileria.TipoFormato.ANS, 14));
                respuesta.Append(Utileria.formatoValor(mensajeTicket1, Utileria.TipoFormato.ANS, 80));
                respuesta.Append(Utileria.formatoValor(mensajeTicket2, Utileria.TipoFormato.ANS, 60));
                respuesta.Append(Utileria.formatoValor(codigoRespuesta.ToString(), Utileria.TipoFormato.N, 2));
                respuesta.Append(Utileria.formatoValor(datosAdicionales, Utileria.TipoFormato.ANS, 20));
                respuesta.Append(Utileria.formatoValor(extension, Utileria.TipoFormato.ANS, 80));
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
