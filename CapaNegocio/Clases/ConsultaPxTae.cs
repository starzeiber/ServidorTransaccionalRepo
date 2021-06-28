using System;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    /// <summary>
    /// clase que contiene todas las propiedades de una solicitud TAE al px
    /// </summary>
    public class ConsultaPxTae : SolicitudPxBase
    {
        /// <summary>
        /// Constructor para inicializar el objeto
        /// </summary>
        /// <param name="consultaTaeXml">Objeto con los valores entrantes</param>
        public ConsultaPxTae(ConsultaTaeXml consultaTaeXml)
        {
            encabezado = int.Parse(UtileriaVariablesGlobales.ENCABEZADO_CONSULTA_TAE_PX);
            idCadena = consultaTaeXml.idCadena;
            idTienda = consultaTaeXml.idTienda;
            idPos = consultaTaeXml.idPos;
            try
            {
                fecha = consultaTaeXml.fechaHora.Substring(0, 8).Replace("/", "");
            }
            catch (Exception)
            {
                fecha = DateTime.Now.Date.ToString("ddMMyyyy");
            }
            try
            {
                hora = consultaTaeXml.fechaHora.Substring(11, 8).Replace(":", "");
            }
            catch (Exception)
            {
                hora = DateTime.Now.ToString("hhmmss");
            }
            region = 9;
            sku = consultaTaeXml.Sku;
            telefono = consultaTaeXml.telefono.ToString();
            numeroTransaccion = consultaTaeXml.numeroTransaccion;
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
                respuesta.Append(Validaciones.formatoValor(telefono, TipoFormato.N, 10));
                respuesta.Append(Validaciones.formatoValor(numeroTransaccion.ToString(), TipoFormato.N, 5));
                return respuesta.ToString();
            }
            catch (Exception ex)
            {
                Task.Run(() => UtileriaVariablesGlobales.log.EscribirLogError("ConsultaPxTae.ObtenerTrama. " + ex.Message));
                //Utileria.log.EscribirLogError("ConsultaPxTae.ObtenerTrama. " + ex.Message);
                return String.Empty;
            }
        }
    }
}
