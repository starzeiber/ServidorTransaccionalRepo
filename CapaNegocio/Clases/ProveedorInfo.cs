using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio.Clases
{
    public class ProveedorInfo
    {
        public int idProveedor { get; set; }
        public string nombreProveedor { get; set; }
        public string marca { get; set; }
        public string issuer { get; set; }
        public int idMaster { get; set; }

        public ProveedorInfo()
        {
            nombreProveedor = "";
            marca = "";
            issuer = "6800000001";
        }
    }
}
