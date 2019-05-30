using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    public class MensajePXRespuesta : MensajePxSolicitud
    {
        public string codigoAutorizacion { get; set; }
        public string pin { get; set; }
        public string fechaExpiracionYYMMDD { get; set; }
        public string monto { get; set; }
        public string folio { get; set; }
        public string proveedor { get; set; }
        public string mensajeTicket1 { get; set; }
        public string mensajeTicket2 { get; set; }
        public string codigoRespuesta { get; set; }

        private int posicionCodigoAutorizacion { get; set; }
        private int posicionPin { get; set; }
        private int posicionFechaExpiracion { get; set; }
        private int posicionMonto { get; set; }
        private int posicionFolio { get; set; }
        private int posicionProveedor { get; set; }
        private int posicionMensajeTicket1 { get; set; }
        private int posicionMensajeTicket2 { get; set; }
        private int posicionCodigoRespuesta { get; set; }

        private int longitudCodigoAutorizacion { get; set; }
        private int longitudPin { get; set; }
        private int longitudFechaExpiracion { get; set; }
        private int longitudMonto { get; set; }
        private int longitudFolio { get; set; }
        private int longitudProveedor { get; set; }
        private int longitudMensajeTicket1 { get; set; }
        private int longitudMensajeTicket2 { get; set; }
        private int longitudCodigoRespuesta { get; set; }

        public MensajePXRespuesta(MensajePxSolicitud mensajePxSolicitud)
        {
            encabezado = mensajePxSolicitud.encabezado;
            idCadena = mensajePxSolicitud.idCadena;
            idTienda = mensajePxSolicitud.idTienda;
            idCaja = mensajePxSolicitud.idCaja;
            fechaYYMMDD = mensajePxSolicitud.fechaYYMMDD;
            horaHHMMSS = mensajePxSolicitud.horaHHMMSS;
            region = mensajePxSolicitud.region;
            sku = mensajePxSolicitud.sku;
            telefono = mensajePxSolicitud.telefono;
            transNumber = mensajePxSolicitud.transNumber;

            codigoAutorizacion = string.Empty;
            pin = string.Empty;
            fechaExpiracionYYMMDD = string.Empty;
            monto = string.Empty;
            folio = string.Empty;
            proveedor = string.Empty;
            mensajeTicket1 = string.Empty;
            mensajeTicket2 = string.Empty;
            codigoRespuesta = string.Empty;
        }

        public Boolean ParseaMensaje(MensajeTpvRespuesta mensajeTPVRespuesta)
        {
            //TODO: cambiar de objeto a clase y parsear
            try
            {
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public String ObtenerTrama()
        {
            StringBuilder creaTrama = new StringBuilder();
            try
            {
                creaTrama.Append(encabezado);
                creaTrama.Append(Validaciones.DarFormato(idCadena, Validaciones.opcionesFormato.cerosIzquierda,4));
                creaTrama.Append(Validaciones.DarFormato(idTienda, Validaciones.opcionesFormato.cerosIzquierda, 4));
                creaTrama.Append(Validaciones.DarFormato(idCadena, Validaciones.opcionesFormato.cerosIzquierda, 4));
                creaTrama.Append(fechaExpiracionYYMMDD);
                creaTrama.Append(horaHHMMSS);
                creaTrama.Append(Validaciones.DarFormato(region, Validaciones.opcionesFormato.cerosIzquierda, 1));
                creaTrama.Append(Validaciones.DarFormato(sku, Validaciones.opcionesFormato.espaciosDerecha, 20));
                creaTrama.Append(Validaciones.DarFormato(telefono, Validaciones.opcionesFormato.cerosIzquierda, 10));
                creaTrama.Append(Validaciones.DarFormato(transNumber, Validaciones.opcionesFormato.cerosIzquierda, 5));
                creaTrama.Append(Validaciones.DarFormato(codigoAutorizacion, Validaciones.opcionesFormato.cerosIzquierda, 9));
                creaTrama.Append(Validaciones.DarFormato(pin, Validaciones.opcionesFormato.espaciosDerecha, 20));
                creaTrama.Append(fechaExpiracionYYMMDD);
                creaTrama.Append(Validaciones.DarFormato(monto, Validaciones.opcionesFormato.cerosIzquierda, 9));
                creaTrama.Append(Validaciones.DarFormato(folio, Validaciones.opcionesFormato.espaciosDerecha, 20));
                creaTrama.Append(Validaciones.DarFormato(proveedor, Validaciones.opcionesFormato.espaciosDerecha, 14));
                creaTrama.Append(Validaciones.DarFormato(mensajeTicket1, Validaciones.opcionesFormato.espaciosDerecha, 80));
                creaTrama.Append(Validaciones.DarFormato(mensajeTicket2, Validaciones.opcionesFormato.espaciosDerecha, 60));
                creaTrama.Append(Validaciones.DarFormato(codigoRespuesta, Validaciones.opcionesFormato.cerosIzquierda, 2));

                return creaTrama.ToString();
            }
            catch (Exception)
            {
                //TODO: log de error
                return creaTrama.ToString();
            }            
        }        
    }
}
