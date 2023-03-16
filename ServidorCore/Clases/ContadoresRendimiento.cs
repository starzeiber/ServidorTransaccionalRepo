using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore.Clases
{
    internal class ContadoresRendimiento
    {
        const String CONEXIONES_ENTRANTES_CORE = "conexionesEntrantesUserver";
        const string PETICIONES_ENTRANTES_CLIENTES_CORE = "PeticionesEntrantesClientesUserver";
        const string PETICIONES_RESPONDIDAS_CLIENTES_CORE = "PeticionesRespondidasClientesUserver";
        const string PETICIONES_SALIENTES_PROVEEDOR_CORE = "PeticionesSalientesProveedorUserver";
        const string PETICIONES_RESPONDIDAS_PROVEEDOR_CORE = "PeticionesRespondidasProveedorUserver";

        const string CATEGORIA_DE_CONTADORES = "Core";

        internal void CrearContadores()
        {
            try
            {
                if (PerformanceCounterCategory.Exists(CATEGORIA_DE_CONTADORES))
                {
                    PerformanceCounterCategory.Delete(CATEGORIA_DE_CONTADORES);
                }

                CounterCreationDataCollection counterDataCollection = new CounterCreationDataCollection
                {
                    InstanciarPerformance(CONEXIONES_ENTRANTES_CORE),
                    InstanciarPerformance(PETICIONES_ENTRANTES_CLIENTES_CORE),
                    InstanciarPerformance(PETICIONES_RESPONDIDAS_CLIENTES_CORE),
                    InstanciarPerformance(PETICIONES_SALIENTES_PROVEEDOR_CORE),
                    InstanciarPerformance(PETICIONES_RESPONDIDAS_PROVEEDOR_CORE)
                };

                PerformanceCounterCategory.Create(CATEGORIA_DE_CONTADORES, "Contadores para estadisticas del core transaccional", PerformanceCounterCategoryType.SingleInstance, counterDataCollection);
                
            }
            catch (Exception)
            {
                throw;
            }
        }

        private CounterCreationData InstanciarPerformance(string nombre)
        {
            CounterCreationData counterCreationData = new CounterCreationData();
            try
            {
                counterCreationData.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                counterCreationData.CounterName = nombre;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
            return counterCreationData;
        }
    }
}
