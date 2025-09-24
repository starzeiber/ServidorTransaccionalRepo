using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    internal class StatisticsAndCounters
    {
        /// <summary>
        /// Número total de bytes recibido en el servidor, para uso estadístico
        /// </summary>
        private int totalBytesRead;

        /// <summary>
        /// Bytes que se han transmitido desde el inicio de la aplicación
        /// </summary>
        public int totalBytesTransferred
        {
            get
            {
                return totalBytesRead;
            }
        }

        
    }
}
