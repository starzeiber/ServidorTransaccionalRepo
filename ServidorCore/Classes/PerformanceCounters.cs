using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore.Classes
{
    internal class PerformanceCounters
    {
        const String INPUTS_CONNECTIONS_CORE = "conexionesEntrantesUserver";
        const string CLIENT_REQUESTS_CORE = "PeticionesEntrantesClientesUserver";
        const string CLIENT_RESPONSES_CORE = "PeticionesRespondidasClientesUserver";
        const string PROVIDER_REQUESTS_CORE = "PeticionesSalientesProveedorUserver";
        const string PROVIDER_RESPONSES_CORE = "PeticionesRespondidasProveedorUserver";

        const string CATEGORIA_DE_CONTADORES = "Core";

        internal void BuildCounters()
        {
            try
            {
                if (PerformanceCounterCategory.Exists(CATEGORIA_DE_CONTADORES))
                {
                    PerformanceCounterCategory.Delete(CATEGORIA_DE_CONTADORES);
                }

                CounterCreationDataCollection counterDataCollection = new CounterCreationDataCollection
                {
                    CreateCounters(INPUTS_CONNECTIONS_CORE),
                    CreateCounters(CLIENT_REQUESTS_CORE),
                    CreateCounters(CLIENT_RESPONSES_CORE),
                    CreateCounters(PROVIDER_REQUESTS_CORE),
                    CreateCounters(PROVIDER_RESPONSES_CORE)
                };

                PerformanceCounterCategory.Create(CATEGORIA_DE_CONTADORES, "Contadores para estadisticas del core transaccional", PerformanceCounterCategoryType.SingleInstance, counterDataCollection);
                
            }
            catch (Exception)
            {
                throw;
            }
        }

        private CounterCreationData CreateCounters(string nombre)
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
