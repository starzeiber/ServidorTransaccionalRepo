using Userver.Clases;
using ServidorCore;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Forms;
using CapaNegocio;

namespace Userver
{
    public partial class Userver : Form
    {
        public Userver()
        {
            InitializeComponent();
        }

        private void button_Iniciar_Click(object sender, EventArgs e)
        {
            ServidorTransaccional<EstadoDelCliente, EstadoDelServidor, EstadoDelProveedor> servidor =
                new ServidorTransaccional<EstadoDelCliente, EstadoDelServidor, EstadoDelProveedor>(1000, 1024, 100);
            servidor.ConfigInicioServidor();
            servidor.IniciarServidor(
                UtileriaVariablesGlobales.puertoLocal,
                UtileriaVariablesGlobales.ipProveedor,
                UtileriaVariablesGlobales.puertoProveedor
                );
            button_Iniciar.Enabled = false;
        }

        private void Userver_Load(object sender, EventArgs e)
        {
            Task<bool> CargarConfiguracionTask = Task.Run(() => OperacionesFront.CargarConfiguracion());
            if (CargarConfiguracionTask.Result == false)
            {
                MessageBox.Show("Error al cargar la configuración del sistema, revise el visor de sucesos Application o bien el log del sistema", "ERROR AL INICIAR", MessageBoxButtons.OK);
                Environment.Exit(666);
            }
            
        }
    }
}
