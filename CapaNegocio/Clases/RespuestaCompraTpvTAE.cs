using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio.Clases
{
    public class RespuestaCompraTpvTAE: RespuestaCompraTpvBase
    {
        public bool Ingresar(CompraPxTae compraPxTae)
        {
            try
            {
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string Obtener()
        {
            string trama = "";
            try
            {
                return trama;
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
