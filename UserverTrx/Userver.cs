using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CapaPresentacion.Clases;
using ServidorCore;

namespace CapaPresentacion
{
    public partial class Userver : Form
    {
        public Userver()
        {
            InitializeComponent();
        }

        private void button_Iniciar_Click(object sender, EventArgs e)
        {
            ServidorTransaccional<EstadoDelCliente, EstadoDelServidor,EstadoDelProveedor> servidor =
                new ServidorTransaccional<EstadoDelCliente, EstadoDelServidor, EstadoDelProveedor>(10000, 1024, 10);
            servidor.ConfigInicioServidor();
            servidor.IniciarServidor(int.Parse(ConfigurationManager.ConnectionStrings["puertoLocal"].ToString()));
        }

        private void Userver_Load(object sender, EventArgs e)
        {
            Task<bool> CargarConfiguracionTask = Task.Run(() => OperacionesFront.CargarConfiguracion());
            if (CargarConfiguracionTask.Result == false)
            {
                MessageBox.Show("Error al cargar la configuración del sistema, revise el visor de sucesos Application o bien el log del sistema", "ERROR AL INICIAR", MessageBoxButtons.OK);
            }
        }
    }
}
