using System;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio.Clases
{
    /// <summary>
    /// Clase que contiene todas las propiedades de una respuesta para la compra de datos en protocolo tpv
    /// </summary>
    public class RespuestaCompraTpvDatos : RespuestaCompraTpvBase
    {
        /// <summary>
        /// 
        /// </summary>
        public string idPaquete { get; set; }

        /// <summary>
        /// A partir de la trama realiza la obtención de la información
        /// </summary>
        /// <param name="trama">Cadena a parsear</param>
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
        /// Ingresa la información de la compra en las propiedades de la respuesta
        /// </summary>
        /// <param name="compraTpvDatos"></param>
        /// <returns></returns>
        public bool Ingresar(CompraTpvDatos compraTpvDatos)
        {
            try
            {
                pCode = compraTpvDatos.pCode;
                monto = compraTpvDatos.monto;
                systemTrace = compraTpvDatos.systemTrace;
                issuer = compraTpvDatos.issuer;
                referencia = compraTpvDatos.referencia;
                TerminalId = compraTpvDatos.TerminalId;
                merchantData = compraTpvDatos.merchantData;
                telefono = compraTpvDatos.telefono;
                fechaHora = compraTpvDatos.fechaHora;
                horaTerminal = compraTpvDatos.horaTerminal;
                fechaTerminal = compraTpvDatos.fechaTerminal;
                fechaCapturaTerminal = compraTpvDatos.fechaCapturaTerminal;
                fechaContableTerminal = compraTpvDatos.fechaContableTerminal;
                idPaquete = compraTpvDatos.idPaquete;
                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerRutaDeLlamada(ex.Message), Utileria.TiposLog.error));
                return false;
            }
        }

        /// <summary>
        /// Obtiene una trama a partir de las propiedades de la clase
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
                respuesta.Append(Utileria.formatoValor(idPaquete, Utileria.TipoFormato.ANS, 10));

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
