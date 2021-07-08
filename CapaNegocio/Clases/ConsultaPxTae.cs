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
        private const int LONGITUD_GRUPO = 4;
        private const int LONGITUD_CADENA = 4;
        private const int LONGITUD_TIENDA = 4;
        private const int LONGITUD_POS = 4;
        private const int LONGITUD_FECHA = 6;
        private const int LONGITUD_HORA = 6;
        private const int LONGITUD_REGION = 2;
        private const int LONGITUD_SKU = 20;
        private const int LONGITUD_TELEFONO = 10;
        private const int LONGITUD_NUM_TRANS = 5;


        public bool DividirTrama(string trama)
        {
            int posicionParseo = 0;
            encabezado = int.Parse(UtileriaVariablesGlobales.ENCABEZADO_CONSULTA_TAE_PX);
            posicionParseo += 2;

            try
            {
                idGrupo = int.Parse(trama.Substring(posicionParseo, LONGITUD_GRUPO));
                posicionParseo += LONGITUD_GRUPO;
                idCadena = int.Parse(trama.Substring(posicionParseo, LONGITUD_CADENA));
                posicionParseo += LONGITUD_CADENA;
                idTienda = int.Parse(trama.Substring(posicionParseo, LONGITUD_TIENDA));
                posicionParseo += LONGITUD_TIENDA;
                idPos = int.Parse(trama.Substring(posicionParseo, LONGITUD_POS));
                posicionParseo += LONGITUD_POS;
                fecha = trama.Substring(posicionParseo, LONGITUD_FECHA);
                posicionParseo += LONGITUD_FECHA;
                hora = trama.Substring(posicionParseo, LONGITUD_HORA);
                posicionParseo += LONGITUD_HORA;
                region = int.Parse(trama.Substring(posicionParseo, LONGITUD_REGION));
                posicionParseo += LONGITUD_REGION;
                sku = trama.Substring(posicionParseo, LONGITUD_SKU);
                posicionParseo += LONGITUD_SKU;
                telefono = trama.Substring(posicionParseo, LONGITUD_TELEFONO);
                posicionParseo += LONGITUD_TELEFONO;
                numeroTransaccion = int.Parse(trama.Substring(posicionParseo, LONGITUD_NUM_TRANS));
                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => UtileriaVariablesGlobales.Log(UtileriaVariablesGlobales.ObtenerNombreFuncion(ex.Message + ". Trama:" + trama),UtileriaVariablesGlobales.TiposLog.error));
                return false;
            }
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
                Task.Run(() => UtileriaVariablesGlobales.Log(UtileriaVariablesGlobales.ObtenerNombreFuncion(ex.Message), UtileriaVariablesGlobales.TiposLog.error));
                //Utileria.log.EscribirLogError("ConsultaPxTae.ObtenerTrama. " + ex.Message);
                return String.Empty;
            }
        }
    }
}
