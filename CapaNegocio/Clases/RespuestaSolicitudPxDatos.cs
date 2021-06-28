using System;
using System.Threading.Tasks;

namespace CapaNegocio
{
    /// <summary>
    /// Clase que contiene todas las propiedades
    /// </summary>
    public class RespuestaSolicitudPxDatos : RespuestaPxBase
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
        public Double monto { get; set; }
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


        public RespuestaSolicitudPxDatos(SolicitudPxDatos solicitudPxDatos)
        {
            encabezado = int.Parse(UtileriaVariablesGlobales.ENCABEZADO_RESPUESTA_TAE_PX);
            idCadena = solicitudPxDatos.idCadena;
            idTienda = solicitudPxDatos.idTienda;
            idPos = solicitudPxDatos.idPos;
            fecha = solicitudPxDatos.fecha;
            hora = solicitudPxDatos.hora;
            region = solicitudPxDatos.region;
            sku = solicitudPxDatos.sku;
            cuenta = solicitudPxDatos.cuenta;
            numeroTransaccion = solicitudPxDatos.numeroTransaccion;
            monto = solicitudPxDatos.monto;
            folio = solicitudPxDatos.folio;
            datosAdicionales = solicitudPxDatos.datosAdicionales;
            extension = solicitudPxDatos.extension;
            PIN = "";
            fechaExpiracion = "";
            nombreProveedor = "";
            mensajeTicket1 = "";
            mensajeTicket2 = "";

        }

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
                Task.Run(() => UtileriaVariablesGlobales.log.EscribirLogError("RespuestaSolicitudPxDatos.ObtenerParametrosTrama: Error en el parseo de la trama: " + ex.Message));
                //Utileria.log.EscribirLogError("RespuestaSolicitudPxDatos.ObtenerParametrosTrama: Error en el parseo de la trama: " + ex.Message);
                return false;
            }
        }
    }
}
