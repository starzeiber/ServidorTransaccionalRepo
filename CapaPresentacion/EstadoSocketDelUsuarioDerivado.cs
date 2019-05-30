using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServidorCore;

namespace CapaPresentacion
{
    /// <summary>
    /// Clase con herencia para poder realizar acciones durante el flujo de la transacción sobre escribiendo los métodos correspondientes
    /// de la clase base
    /// </summary>
    public class EstadoSocketDelUsuarioDerivado: ServidorCore.EstadoSocketDelUsuarioBase
    {
        /// <summary>
        /// Instancia al formulario principal para poder pintar información
        /// </summary>
        public frmMain formularioPrincipal;
        /// <summary>
        /// para obtener la información de conexión del cliente se tiene que tener una instancia al socket de trabajo del cliente
        /// </summary>
        private InfoSocketDelUsuarioBase informacionCliente;

        public override void OnAceptacion(object args)
        {
            base.OnAceptacion(args);
            //para obtener la información de conexión del cliente se tiene que tener una instancia al socket de trabajo del cliente
            informacionCliente = (InfoSocketDelUsuarioBase)args;
            //Se realiza un cast al formulario donde se necesita la información con la ayuda del objeto genérico en la capa del core
            formularioPrincipal = (frmMain)formulario;
            formularioPrincipal.BeginInvoke(formularioPrincipal.pintarLista, new object[] { "Una conexión de la ip:" + informacionCliente.ipCliente});
        }
    }
}
