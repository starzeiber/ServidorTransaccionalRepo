using CapaNegocio.Clases;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    /// <summary>
    /// clase que contiene todas las propiedades de una solicitud TAE al px
    /// </summary>
    public class CompraPxDatos : CompraPxBase
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
        public decimal monto { get; set; }
        /// <summary>
        /// información del paquete
        /// </summary>
        public String datosAdicionales { get; set; }
        /// <summary>
        /// Extensión del protocolo para información varia
        /// </summary>
        public String extension { get; set; }


        private const int LONGITUD_GRUPO = 4;
        private const int LONGITUD_CADENA = 4;
        private const int LONGITUD_TIENDA = 4;
        private const int LONGITUD_POS = 4;
        private const int LONGITUD_FECHA = 6;
        private const int LONGITUD_HORA = 6;
        private const int LONGITUD_REGION = 2;
        private const int LONGITUD_SKU = 20;
        private const int LONGITUD_CUENTA = 10;
        private const int LONGITUD_NUM_TRANS = 5;
        private const int LONGITUD_MONTO = 9;
        private const int LONGITUD_FOLIO = 20;
        private const int LONGITUD_DATOS_ADICIONALES = 20;
        private const int LONGITUD_EXTENSION = 80;

        public ProductoInfo productoInfo;

        public ProveedorInfo proveedorInfo;

        /// <summary>
        /// 
        /// </summary>
        public CompraPxDatos()
        {
            cuenta = "";
            folio = " ";
            datosAdicionales = " ";
            extension = " ";
        }

        /// <summary>
        /// Divide la trama en sus propiedades
        /// </summary>
        /// <param name="trama">trama completa de solicitud de datos con la mensajería PX</param>
        /// <returns></returns>
        public bool Ingresar(string trama)
        {
            int posicionParseo = 0;
            encabezado = int.Parse(Utileria.ENCABEZADO_SOLICITUD_DATOS_PX);
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
                cuenta = trama.Substring(posicionParseo, LONGITUD_CUENTA);
                posicionParseo += LONGITUD_CUENTA;
                numeroTransaccion = int.Parse(trama.Substring(posicionParseo, LONGITUD_NUM_TRANS));
                posicionParseo += LONGITUD_NUM_TRANS;
                monto= decimal.Parse(trama.Substring(posicionParseo, LONGITUD_MONTO));
                posicionParseo += LONGITUD_MONTO;
                folio= trama.Substring(posicionParseo, LONGITUD_FOLIO);
                telefono = double.Parse(folio).ToString();
                posicionParseo += LONGITUD_FOLIO;
                datosAdicionales= trama.Substring(posicionParseo, LONGITUD_DATOS_ADICIONALES);
                posicionParseo += LONGITUD_DATOS_ADICIONALES;
                extension= trama.Substring(posicionParseo, LONGITUD_EXTENSION);

                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerNombreFuncion(ex.Message + ". Trama:" + trama), Utileria.TiposLog.error));
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
                respuesta.Append(Validaciones.formatoValor(cuenta, TipoFormato.N, 10));
                respuesta.Append(Validaciones.formatoValor(numeroTransaccion.ToString(), TipoFormato.N, 5));
                respuesta.Append(Validaciones.formatoValor(monto.ToString().Split('.')[0] + monto.ToString().Split('.')[1], TipoFormato.N, 9));
                respuesta.Append(Validaciones.formatoValor(folio, TipoFormato.N, 20));
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
