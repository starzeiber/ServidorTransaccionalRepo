using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    public class MensajeTpvSolicitud
    {
        public string encabezado { get; set; }
        public string pCode { get; set; }
        public string monto { get; set; }
        public string fechaHoraMMDDhhmmss { get; set; }
        public string systemTrace { get; set; }
        public string horaTerminalhhmmss { get; set; }
        public string fechaTerminalMMDD { get; set; }
        public string fechaContable { get; set; }
        public string fechaCaptura { get; set; }
        public string adquiriente { get; set; }
        public string issuer { get; set; }
        public string referencia { get; set; }
        public string terminaId { get; set; }
        public string merchantData { get; set; }
        public string codigoMoneda { get; set; }
        public string datosAdicionales { get; set; }
        public string telefono { get; set; }

        public MensajeTpvSolicitud()
        {
            encabezado = "200";
            pCode = string.Empty;
            monto = string.Empty;
            fechaHoraMMDDhhmmss = string.Empty;
            systemTrace = string.Empty;
            horaTerminalhhmmss = string.Empty;
            fechaTerminalMMDD = string.Empty;
            fechaContable = string.Empty;
            fechaCaptura = string.Empty;
            adquiriente = string.Empty;
            issuer = string.Empty;
            referencia = string.Empty;
            terminaId = string.Empty;
            merchantData = string.Empty;
            codigoMoneda = string.Empty;
            datosAdicionales = string.Empty;
            telefono = string.Empty;
        }

        public String ObtenerTrama()
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
