using System;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio.Clases
{
    /// <summary>
    /// Clase que contiene todas las propiedades de una compra TPV
    /// </summary>
    public class CompraTpvTae : CompraTpvBase
    {
        /// <summary>
        /// 
        /// </summary>
        public string sku { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int idProveedor { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int idMaster { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public decimal saldoActual { get; set; }

        /// <summary>
        /// Fecha y hora completa del sistema al momento de la instancia
        /// </summary>
        public DateTime fechaHoraCompleta { get; set; }


        /// <summary>
        /// Función que obtiene las propiedades a partir de la instancia CompraPxTae
        /// </summary>
        /// <param name="compraPxTae">Instancia CompraPxTae</param>
        /// <returns></returns>
        public bool Ingresar(CompraPxTae compraPxTae)
        {
            try
            {
                pCode = 650000;
                monto = compraPxTae.productoInfo.monto;
                fechaHora = compraPxTae.fecha.Substring(2) + compraPxTae.hora;
                fechaHoraCompleta = DateTime.Now;
                systemTrace = compraPxTae.numeroTransaccion;
                //TODO es para pruebas la siguiente línea
                //systemTrace = Utileria.ObtenerNumeroResultadoAleatorio(6);
                horaTerminal = compraPxTae.hora;
                fechaTerminal = compraPxTae.fecha.Substring(2);
                fechaContableTerminal = fechaTerminal;
                fechaCapturaTerminal = fechaTerminal;
                issuer = compraPxTae.proveedorInfo.issuer.Length + compraPxTae.proveedorInfo.issuer;
                referencia = Utileria.ObtenerNumeroResultadoAleatorio(6);
                TerminalId = "STTN" +
                    Utileria.formatoValor(compraPxTae.idGrupo.ToString(), Utileria.TipoFormato.N, 3) +
                    Utileria.formatoValor(compraPxTae.idCadena.ToString(), Utileria.TipoFormato.N, 5) +
                    Utileria.formatoValor(compraPxTae.idTienda.ToString(), Utileria.TipoFormato.N, 4);
                merchantData = "TARJETASN      " +
                    Utileria.formatoValor(compraPxTae.idGrupo.ToString(), Utileria.TipoFormato.N, 5) +
                    Utileria.formatoValor(compraPxTae.idCadena.ToString(), Utileria.TipoFormato.N, 5) +
                    Utileria.formatoValor(compraPxTae.idTienda.ToString(), Utileria.TipoFormato.N, 5) +
                    Utileria.formatoValor(compraPxTae.idPos.ToString(), Utileria.TipoFormato.N, 5) +
                    "DF MX";
                telefono = compraPxTae.telefono;

                sku = compraPxTae.productoInfo.sku;
                idProveedor = compraPxTae.proveedorInfo.idProveedor;
                idMaster = compraPxTae.proveedorInfo.idMaster;
                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerRutaDeLlamada(ex.Message), Utileria.TiposLog.error));
                return false;
            }

        }

        /// <summary>
        /// Ingresa la información a la clase
        /// </summary>
        /// <param name="consultaPxTae"></param>
        /// <returns></returns>
        public bool Ingresar(ConsultaPxTae consultaPxTae)
        {
            try
            {
                pCode = 650000;
                monto = consultaPxTae.productoInfo.monto;
                fechaHora = consultaPxTae.fecha.Substring(2) + consultaPxTae.hora;
                systemTrace = consultaPxTae.numeroTransaccion;
                horaTerminal = consultaPxTae.hora;
                fechaTerminal = consultaPxTae.fecha.Substring(2);
                fechaContableTerminal = fechaTerminal;
                fechaCapturaTerminal = fechaTerminal;
                issuer = consultaPxTae.proveedorInfo.issuer.Length + consultaPxTae.proveedorInfo.issuer;
                referencia = Utileria.ObtenerNumeroResultadoAleatorio(6);
                TerminalId = "STTN" +
                    Utileria.formatoValor(consultaPxTae.idGrupo.ToString(), Utileria.TipoFormato.N, 3) +
                    Utileria.formatoValor(consultaPxTae.idCadena.ToString(), Utileria.TipoFormato.N, 5) +
                    Utileria.formatoValor(consultaPxTae.idTienda.ToString(), Utileria.TipoFormato.N, 4);
                merchantData = "TARJETASN      " +
                    Utileria.formatoValor(consultaPxTae.idGrupo.ToString(), Utileria.TipoFormato.N, 5) +
                    Utileria.formatoValor(consultaPxTae.idCadena.ToString(), Utileria.TipoFormato.N, 5) +
                    Utileria.formatoValor(consultaPxTae.idTienda.ToString(), Utileria.TipoFormato.N, 5) +
                    Utileria.formatoValor(consultaPxTae.idPos.ToString(), Utileria.TipoFormato.N, 5) +
                    "DF MX";
                telefono = consultaPxTae.telefono;

                sku = consultaPxTae.productoInfo.sku;
                idProveedor = consultaPxTae.proveedorInfo.idProveedor;
                idMaster = consultaPxTae.proveedorInfo.idMaster;
                return true;
            }
            catch (Exception ex)
            {
                Task.Run(() => Utileria.Log(Utileria.ObtenerRutaDeLlamada(ex.Message), Utileria.TiposLog.error));
                return false;
            }

        }

        /// <summary>
        /// Obtiene la trama en procotolo TPV
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
                respuesta.Append(Utileria.formatoValor(referencia.ToString(), Utileria.TipoFormato.N, 12));
                respuesta.Append(Utileria.formatoValor(TerminalId, Utileria.TipoFormato.ANS, 16));
                respuesta.Append(Utileria.formatoValor(merchantData, Utileria.TipoFormato.ANS, 40));
                respuesta.Append(Utileria.formatoValor(codigoMoneda.ToString(), Utileria.TipoFormato.N, 3));
                respuesta.Append(Utileria.formatoValor(datosAdicionales, Utileria.TipoFormato.N, 15));
                respuesta.Append("015" + Utileria.formatoValor(telefono, Utileria.TipoFormato.N, 15));

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
