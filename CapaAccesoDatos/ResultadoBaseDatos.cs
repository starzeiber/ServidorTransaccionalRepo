using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaDatos
{
    public  class ResultadoBaseDatos
    {
        /// <summary>
        /// Propiedad que indentifica que hay un error
        /// </summary>
        public bool Error { get; set; }

        /// <summary>
        /// propiedad que contiene una excepción controlada del error
        /// </summary>
        public Exception Excepcion { get; set; }

        /// <summary>
        /// propiedad que contiene los registros de resultado de la acción en base de datos
        /// </summary>
        public DataSet Datos { get; set; }
    }
}
