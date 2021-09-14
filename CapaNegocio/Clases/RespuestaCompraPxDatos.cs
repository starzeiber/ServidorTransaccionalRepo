﻿using System;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    /// <summary>
    /// Clase que contiene todas las propiedades
    /// </summary>
    public class RespuestaCompraPxDatos : RespuestaCompraPxBase
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
        /// 
        /// </summary>
        public RespuestaCompraPxDatos()
        {
            encabezado = int.Parse(Utileria.ENCABEZADO_RESPUESTA_DATOS_PX);
            PIN = "";
            fechaExpiracion = "";
            nombreProveedor = "";
            mensajeTicket1 = "";
            mensajeTicket2 = "";

        }

        /// <summary>
        /// Divide en sus propiedades a partir de la instancia CompraPxDatos
        /// </summary>
        /// <param name="compraPxDatos">Instancia de CompraPxDatos</param>
        /// <returns></returns>
        public bool Ingresar(CompraPxDatos compraPxDatos)
        {
            try
            {
                idGrupo = compraPxDatos.idGrupo;
                idCadena = compraPxDatos.idCadena;
                idTienda = compraPxDatos.idTienda;
                idPos = compraPxDatos.idPos;
                fecha = compraPxDatos.fecha;
                hora = compraPxDatos.hora;
                region = compraPxDatos.region;
                sku = compraPxDatos.sku;
                cuenta = compraPxDatos.cuenta;
                numeroTransaccion = compraPxDatos.numeroTransaccion;
                monto = compraPxDatos.monto;
                folio = compraPxDatos.folio;
                datosAdicionales = compraPxDatos.datosAdicionales;
                extension = compraPxDatos.extension;
                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion("Error en el parseo de la trama: " + ex.Message), Utileria.TiposLog.error));

                return false;
            }
        }

        /// <summary>
        /// Actualiza cierta parte de información de una respuesta a una compra de datos en protocolo PX
        /// </summary>
        /// <param name="compraPxDatos">Instancia CompraPxDatos</param>
        public void Actualizar(CompraPxDatos compraPxDatos)
        {
            monto = compraPxDatos.monto;
            folio = compraPxDatos.folio;
            datosAdicionales = compraPxDatos.datosAdicionales;
            extension = compraPxDatos.extension;
        }

        /// <summary>
        /// Divide la trama en los respectivos campos de la clase
        /// </summary>
        /// <param name="tramaRecibida">trama en formato PX encabezado 25</param>
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
        /// Obtiene la trama en protocolo px
        /// </summary>
        /// <returns></returns>
        public string ObtenerTrama()
        {
            StringBuilder respuesta = new StringBuilder();
            try
            {
                respuesta.Append(encabezado.ToString());
                respuesta.Append(Validaciones.formatoValor(idCadena.ToString(), TipoFormato.N, 4));
                respuesta.Append(Validaciones.formatoValor(idTienda.ToString(), TipoFormato.N, 4));
                respuesta.Append(Validaciones.formatoValor(idPos.ToString(), TipoFormato.N, 4));
                respuesta.Append(Validaciones.formatoValor(fecha, TipoFormato.N, 6));
                respuesta.Append(Validaciones.formatoValor(hora, TipoFormato.N, 6));
                respuesta.Append(Validaciones.formatoValor(region.ToString(), TipoFormato.N, 2));
                respuesta.Append(Validaciones.formatoValor(sku, TipoFormato.ANS, 20));
                respuesta.Append(Validaciones.formatoValor(cuenta, TipoFormato.N, 10));
                respuesta.Append(Validaciones.formatoValor(numeroTransaccion.ToString(), TipoFormato.N, 5));
                respuesta.Append(Validaciones.formatoValor(autorizacion.ToString(), TipoFormato.N, 9));
                respuesta.Append(Validaciones.formatoValor(PIN.ToString(), TipoFormato.ANS, 20));
                respuesta.Append(Validaciones.formatoValor(fechaExpiracion.ToString(), TipoFormato.N, 6));
                respuesta.Append(Validaciones.formatoValor(monto.ToString(), TipoFormato.N, 9));
                respuesta.Append(Validaciones.formatoValor(folio, TipoFormato.N, 20));
                respuesta.Append(Validaciones.formatoValor(nombreProveedor, TipoFormato.ANS, 14));
                respuesta.Append(Validaciones.formatoValor(mensajeTicket1, TipoFormato.ANS, 80));
                respuesta.Append(Validaciones.formatoValor(mensajeTicket2, TipoFormato.ANS, 60));
                respuesta.Append(Validaciones.formatoValor(codigoRespuesta.ToString(), TipoFormato.N, 2));
                respuesta.Append(Validaciones.formatoValor(datosAdicionales, TipoFormato.ANS, 20));
                respuesta.Append(Validaciones.formatoValor(extension, TipoFormato.ANS, 80));
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
