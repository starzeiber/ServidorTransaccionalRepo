﻿using System;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    /// <summary>
    /// clase que contiene todas las propiedades de una solicitud TAE al px
    /// </summary>
    public class SolicitudPxDatos : SolicitudPxBase
    {
        /// <summary>
        /// Campo para información de la cuenta
        /// </summary>
        public String cuenta { get; set; }
        /// <summary>
        /// campo que se utiliza para el ingreso del número y/o clave
        /// </summary>
        public String folio { get; set; }
        /// <summary>
        /// Monto del paquete
        /// </summary>
        public Double monto { get; set; }
        /// <summary>
        /// información del paquete
        /// </summary>
        public String datosAdicionales { get; set; }
        /// <summary>
        /// Extensión del protocolo para información varia
        /// </summary>
        public String extension { get; set; }



        /// <summary>
        /// Constructor para inicializar el objeto
        /// </summary>
        /// <param name="solicitudDatosXml">Objeto con los valores entrantes</param>
        public SolicitudPxDatos(SolicitudDatosXml solicitudDatosXml)
        {
            encabezado = int.Parse(UtileriaVariablesGlobales.ENCABEZADO_SOLICITUD_DATOS_PX);
            idCadena = solicitudDatosXml.idCadena;
            idTienda = solicitudDatosXml.idTienda;
            idPos = solicitudDatosXml.idPos;
            try
            {
                fecha = solicitudDatosXml.fechaHora.Substring(0, 8).Replace("/", "");
            }
            catch (Exception)
            {
                fecha = DateTime.Now.Date.ToString("ddMMyyyy");
            }
            try
            {
                hora = solicitudDatosXml.fechaHora.Substring(11, 8).Replace(":", "");
            }
            catch (Exception)
            {
                hora = DateTime.Now.ToString("hhmmss");
            }

            region = 9;
            sku = solicitudDatosXml.Sku;
            folio = solicitudDatosXml.telefono.ToString();
            numeroTransaccion = solicitudDatosXml.numeroTransaccion;
            monto = double.Parse(UtileriaVariablesGlobales.ObtenerMontoPorSku(sku) + "00");
            cuenta = "";
            datosAdicionales = solicitudDatosXml.idProducto;
            extension = "";

        }

        /// <summary>
        /// Función para  formar la trama de envío 
        /// </summary>
        /// <returns></returns>
        public String ObtenerTrama()
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
                respuesta.Append(Validaciones.formatoValor(monto.ToString(), TipoFormato.N, 9));
                respuesta.Append(Validaciones.formatoValor(folio, TipoFormato.N, 20));
                respuesta.Append(Validaciones.formatoValor(datosAdicionales, TipoFormato.ANS, 20));
                respuesta.Append(Validaciones.formatoValor(extension, TipoFormato.ANS, 80));
                return respuesta.ToString();
            }
            catch (Exception ex)
            {
                Task.Run(() => UtileriaVariablesGlobales.log.EscribirLogError("SolicitudPxDatos.ObtenerTrama. " + ex.Message));
                //Utileria.log.EscribirLogError("SolicitudPxDatos.ObtenerTrama. " + ex.Message);
                return String.Empty;
            }
        }
    }
}
