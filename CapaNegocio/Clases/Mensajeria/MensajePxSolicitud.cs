using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    public class MensajePxSolicitud
    {
        public string encabezado { get; set; }
        public string idCadena { get; set; }
        public string idTienda { get; set; }
        public string idCaja { get; set; }
        public string fechaYYMMDD { get; set; }
        public string horaHHMMSS { get; set; }
        public string region { get; set; }
        public string sku { get; set; }
        public string telefono { get; set; }
        public string transNumber { get; set; }

        private int posicionEncabezado { get; set; }
        private int posicionIdCadena { get; set; }
        private int posicionIdTienda { get; set; }
        private int posicionIdCaja { get; set; }
        private int posicionFechaYYMMDD { get; set; }
        private int posicionHoraHHMMSS { get; set; }
        private int posicionRegion { get; set; }
        private int posicionSku { get; set; }
        private int posicionTelefono { get; set; }
        private int posicionTransNumber { get; set; }

        private int longitudEncabezado { get; set; }
        private int longitudIdCadena { get; set; }
        private int longitudIdTienda { get; set; }
        private int longitudIdCaja { get; set; }
        private int longitudFechaYYMMDD { get; set; }
        private int longitudHoraHHMMSS { get; set; }
        private int longitudRegion { get; set; }
        private int longitudSku { get; set; }
        private int longitudTelefono { get; set; }
        private int longitudTransNumber { get; set; }        

        public MensajePxSolicitud()
        {
            encabezado = string.Empty;
            idCadena = string.Empty;
            idTienda = string.Empty;
            idCaja = string.Empty;
            fechaYYMMDD = string.Empty;
            horaHHMMSS = string.Empty;
            region = string.Empty;
            sku = string.Empty;
            telefono = string.Empty;
            transNumber = string.Empty;
            
            longitudEncabezado = 2;
            longitudIdCadena = 4;
            longitudIdTienda = 4;
            longitudIdCaja = 4;
            longitudFechaYYMMDD = 6;
            longitudHoraHHMMSS = 6;
            longitudRegion = 2;
            longitudSku = 20;
            longitudTelefono = 10;
            longitudTransNumber = 5;

            posicionEncabezado = 0;
            posicionIdCadena = posicionEncabezado + longitudEncabezado;
            posicionIdTienda = posicionIdCadena + longitudIdCadena;
            posicionIdCaja = posicionIdTienda + longitudIdTienda;
            posicionFechaYYMMDD = posicionIdCaja+ longitudIdCaja;
            posicionHoraHHMMSS = posicionFechaYYMMDD + longitudFechaYYMMDD;
            posicionRegion = posicionHoraHHMMSS + longitudHoraHHMMSS;
            posicionSku = posicionRegion + longitudRegion;
            posicionTelefono = posicionSku + longitudSku;
            posicionTransNumber = posicionTelefono + longitudTelefono;
        }

        public Boolean Parsear(String trama)
        {
            try
            {
                encabezado = trama.Substring(posicionEncabezado, longitudEncabezado);
                idCadena = trama.Substring(posicionIdCadena, longitudIdCadena);
                idTienda = trama.Substring(posicionIdTienda, longitudIdTienda);
                idCaja = trama.Substring(posicionIdCaja, longitudIdCaja);
                fechaYYMMDD = trama.Substring(posicionFechaYYMMDD, longitudFechaYYMMDD);
                horaHHMMSS = trama.Substring(posicionHoraHHMMSS, longitudHoraHHMMSS);
                region = trama.Substring(posicionRegion, longitudRegion);
                sku = trama.Substring(posicionSku, longitudSku);
                telefono = trama.Substring(posicionTelefono, longitudTelefono);
                transNumber = trama.Substring(posicionTransNumber, longitudTransNumber);
                return true;
            }
            catch (Exception)
            {
                //TODO: poner log
                return false;
            }            
        }
    }
}
