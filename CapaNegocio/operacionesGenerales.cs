using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio
{
    public class operacionesGenerales
    {
        private PerformanceCounter performance = new PerformanceCounter("Tenserver", "pcCodigos74TEN", false);
        public string palabra = String.Empty;
        public Boolean procesamientoTrama (String trama)
        {
            //try
            //{
            //    int posSeparador = trama.IndexOf(@"\r\n");

            //    if (posSeparador != -1)
            //    {
            //        palabra = trama.Substring(0, posSeparador);
            //    }
            //    return true;
            //}
            //catch (Exception ex)
            //{
            //    TODO: colocar log
            //    return false;
            //}            
            if (dividirTrama(trama.Substring(2, 183)) == false)
            {
                return false;
            }


            palabra = trama;
            return true;
        }

        private Boolean dividirTrama(String trama)
        {
            try
            {
                //mensaje200 solicitudAbono = new mensaje200();
                //if (solicitudAbono.desgloceTrama(trama)==false)
                //{
                //    return false;
                //}

                //mensaje210 respuestaSolicitudAbono = new mensaje210();
                //if (respuestaSolicitudAbono.obtenerTrama()==String.Empty)
                //{
                //    return false;
                //}

                //System.Threading.Thread.Sleep(2000);
                performance.Increment();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
