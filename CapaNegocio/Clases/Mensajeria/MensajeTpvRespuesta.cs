using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    public class MensajeTpvRespuesta: MensajeTpvSolicitud
    {
        public string codigoAutorizacion { get; set; }
        public string codigoRespuesta { get; set; }

        private int posicionCodigoAutorizacion { get; set; }
        private int posicionCodigoRespuesta { get; set; }

        private int longitudCodigoAutorizacion { get; set; }
        private int longitudCodigoRespuesta { get; set; }

        public MensajeTpvRespuesta(MensajeTpvSolicitud mensajeTpv)
        {
            pCode = mensajeTpv.pCode;
            monto = mensajeTpv.monto;
            fechaHoraMMDDhhmmss = mensajeTpv.fechaHoraMMDDhhmmss;
            systemTrace = mensajeTpv.systemTrace;
            horaTerminalhhmmss = mensajeTpv.horaTerminalhhmmss;
            fechaTerminalMMDD = mensajeTpv.fechaTerminalMMDD;
            fechaContable = mensajeTpv.fechaContable;
            fechaCaptura = mensajeTpv.fechaCaptura;
            adquiriente = mensajeTpv.adquiriente;
            issuer = mensajeTpv.issuer;
            referencia = mensajeTpv.referencia;
            terminaId = mensajeTpv.terminaId;
            merchantData = mensajeTpv.merchantData;
            codigoMoneda = mensajeTpv.codigoMoneda;
            datosAdicionales = mensajeTpv.datosAdicionales;
            telefono = mensajeTpv.telefono;

            codigoAutorizacion = string.Empty;
            codigoRespuesta = string.Empty;            

            posicionCodigoAutorizacion = 91;
            longitudCodigoAutorizacion = 6;
            posicionCodigoRespuesta = posicionCodigoAutorizacion + longitudCodigoAutorizacion;
            longitudCodigoRespuesta = 2;
        }       

        public Boolean Parsear(String trama)
        {
            try
            {
                codigoAutorizacion = trama.Substring(posicionCodigoAutorizacion, longitudCodigoAutorizacion);
                codigoRespuesta = trama.Substring(posicionCodigoRespuesta, longitudCodigoRespuesta);
                return true;
            }
            catch (Exception)
            {
                //TODO: log
                return false;
            }
        }

        public new String ObtenerTrama()
        {
            StringBuilder creaTrama = new StringBuilder();
            try
            {
                creaTrama.Append(Validaciones.DarFormato(encabezado, Validaciones.opcionesFormato.cerosDerecha, 3));
                creaTrama.Append(Validaciones.DarFormato(pCode, Validaciones.opcionesFormato.cerosDerecha, 6));
                creaTrama.Append(Validaciones.DarFormato(monto, Validaciones.opcionesFormato.cerosIzquierda, 12));
                creaTrama.Append(Validaciones.DarFormato(fechaHoraMMDDhhmmss, Validaciones.opcionesFormato.cerosIzquierda, 10));
                creaTrama.Append(Validaciones.DarFormato(systemTrace, Validaciones.opcionesFormato.cerosIzquierda, 6));
                creaTrama.Append(Validaciones.DarFormato(horaTerminalhhmmss, Validaciones.opcionesFormato.cerosIzquierda, 6));
                creaTrama.Append(Validaciones.DarFormato(fechaTerminalMMDD, Validaciones.opcionesFormato.cerosIzquierda, 4));
                creaTrama.Append(Validaciones.DarFormato(fechaContable, Validaciones.opcionesFormato.cerosIzquierda, 4));
                creaTrama.Append(Validaciones.DarFormato(fechaCaptura, Validaciones.opcionesFormato.cerosIzquierda, 4));
                creaTrama.Append(Validaciones.DarFormato(adquiriente, Validaciones.opcionesFormato.cerosIzquierda, 12));
                creaTrama.Append(Validaciones.DarFormato(issuer, Validaciones.opcionesFormato.cerosIzquierda, 12));
                creaTrama.Append(Validaciones.DarFormato(referencia, Validaciones.opcionesFormato.cerosIzquierda, 12));

                creaTrama.Append(Validaciones.DarFormato(codigoAutorizacion, Validaciones.opcionesFormato.cerosIzquierda, 6));
                creaTrama.Append(Validaciones.DarFormato(codigoRespuesta, Validaciones.opcionesFormato.cerosIzquierda, 2));

                creaTrama.Append(Validaciones.DarFormato(terminaId, Validaciones.opcionesFormato.espaciosDerecha, 16));
                creaTrama.Append(Validaciones.DarFormato(merchantData, Validaciones.opcionesFormato.espaciosDerecha, 40));
                creaTrama.Append(Validaciones.DarFormato(codigoMoneda, Validaciones.opcionesFormato.cerosIzquierda, 3));
                creaTrama.Append(Validaciones.DarFormato(datosAdicionales, Validaciones.opcionesFormato.cerosIzquierda, 15));
                creaTrama.Append(Validaciones.DarFormato(telefono, Validaciones.opcionesFormato.cerosIzquierda, 18));
                return creaTrama.ToString();
            }
            catch (Exception)
            {
                //TODO: log
                return creaTrama.ToString();
            }
        }
    }
}
