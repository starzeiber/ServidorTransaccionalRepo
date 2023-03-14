namespace ServerCore
{
    public interface IEstadoDelProveedorBase
    {
        /// <summary>
        /// Función que guardará la operación con el proveedor
        /// </summary>
        void GuardarTransaccion();
        /// <summary>
        /// Función que se utiliza para marcar un timeout de forma segura
        /// </summary>
        void IndicarVencimientoPorTimeOut();
        /// <summary>
        /// Ingresa de forma segura el valor de la instancia de socket principal para un retorno de flujo
        /// </summary>
        /// <param name="obj"></param>
        void IngresarObjetoPeticionCliente(object obj);
        /// <summary>
        /// Función virtual para poder sobre escribirla, sirve para limpiar e inicializar 
        /// todas las variables del info y socket de trabajo
        /// </summary>
        void InicializarEstadoDelProveedorBase();
        /// <summary>
        /// Función que obtiene la trama de petición al proveedor
        /// </summary>
        void ObtenerTramaPeticion();
        /// <summary>
        /// Función que obtiene la trama de respuesta de una proveedor
        /// </summary>
        void ObtenerTramaRespuesta();
        /// <summary>
        /// Función virtual para poder sobre escribirla, en esta se controla
        /// toda la operación sobre el mensaje del cliente así como su mensaje de respuesta
        /// </summary>
        void ProcesarTramaDelProveeedor(string trama);
        /// <summary>
        /// Función que se utiliza para desmarcar la bandera de timeout de forma segura
        /// </summary>
        void ReinicioBanderaTimeOut();
    }
}