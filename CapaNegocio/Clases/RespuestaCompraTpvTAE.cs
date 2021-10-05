using System;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio.Clases
{
    /// <summary>
    /// Contiene todas las propiedades de una respuesta en protocolo TPV
    /// </summary>
    public class RespuestaCompraTpvTAE : RespuestaCompraTpvBase
    {

        /// <summary>
        /// Ingresa la información en las propiedades de la clase a partir de la compra
        /// </summary>
        /// <param name="compraTpvTae">Instancia de CompraTpvTae</param>
        /// <returns></returns>
        public bool Ingresar(CompraTpvTae compraTpvTae)
        {
            try
            {
                pCode = compraTpvTae.pCode;
                monto = compraTpvTae.monto;
                systemTrace = compraTpvTae.systemTrace;
                issuer = compraTpvTae.issuer;
                referencia = compraTpvTae.referencia;
                TerminalId = compraTpvTae.TerminalId;
                merchantData = compraTpvTae.merchantData;
                telefono = compraTpvTae.telefono;
                fechaHora = compraTpvTae.fechaHora;
                horaTerminal = compraTpvTae.horaTerminal;
                fechaTerminal = compraTpvTae.fechaTerminal;
                fechaCapturaTerminal = compraTpvTae.fechaCapturaTerminal;
                fechaContableTerminal = compraTpvTae.fechaContableTerminal;
                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerRutaDeLlamada(ex.Message), Utileria.TiposLog.error));
                return false;
            }
        }

        /// <summary>
        /// Ingresa la información en las propiedades de la clase a partir de la trama de respuesta
        /// </summary>
        /// <param name="trama"></param>
        /// <returns></returns>
        public bool Ingresar(string trama)
        {
            try
            {
                autorizacion = int.Parse(trama.Substring(91, 6));
                codigoRespuesta = int.Parse(trama.Substring(97, 2));
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
        public string Obtener()
        {
            StringBuilder respuesta = new StringBuilder();
            try
            {
                respuesta.Append(encabezado.ToString());
                respuesta.Append(Utileria.formatoValor(pCode.ToString(), Utileria.TipoFormato.N, 6));
                int dosDecimales = (int)(((decimal)monto % 1) * 100);
                respuesta.Append(Utileria.formatoValor(monto.ToString().Split('.')[0] + dosDecimales.ToString("00"), Utileria.TipoFormato.N, 12));
                respuesta.Append(Utileria.formatoValor(fechaHora.ToString(), Utileria.TipoFormato.N, 10));
                respuesta.Append(Utileria.formatoValor(systemTrace.ToString(), Utileria.TipoFormato.N, 6));
                respuesta.Append(Utileria.formatoValor(horaTerminal, Utileria.TipoFormato.N, 6));
                respuesta.Append(Utileria.formatoValor(fechaTerminal, Utileria.TipoFormato.N, 4));
                respuesta.Append(Utileria.formatoValor(fechaContableTerminal.ToString(), Utileria.TipoFormato.N, 4));
                respuesta.Append(Utileria.formatoValor(fechaCapturaTerminal.ToString(), Utileria.TipoFormato.N, 4));
                respuesta.Append(Utileria.formatoValor(adquiriente, Utileria.TipoFormato.N, 12));
                respuesta.Append(Utileria.formatoValor(issuer, Utileria.TipoFormato.N, 11));
                respuesta.Append(Utileria.formatoValor(autorizacion.ToString(), Utileria.TipoFormato.N, 6));
                respuesta.Append(Utileria.formatoValor(codigoRespuesta.ToString(), Utileria.TipoFormato.N, 2));
                respuesta.Append(Utileria.formatoValor(referencia.ToString(), Utileria.TipoFormato.N, 12));
                respuesta.Append(Utileria.formatoValor(TerminalId, Utileria.TipoFormato.ANS, 16));
                respuesta.Append(Utileria.formatoValor(merchantData, Utileria.TipoFormato.ANS, 40));
                respuesta.Append(Utileria.formatoValor(codigoMoneda.ToString(), Utileria.TipoFormato.N, 3));
                respuesta.Append(Utileria.formatoValor(datosAdicionales, Utileria.TipoFormato.N, 15));
                respuesta.Append(Utileria.formatoValor(telefono, Utileria.TipoFormato.N, 18));

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
