using CapaNegocio;
using ServidorCore;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Userver.Clases;

namespace Userver
{
    public partial class Userver : MetroFramework.Forms.MetroForm
    {
        ServidorTransaccional<EstadoDelCliente, EstadoDelServidor, EstadoDelProveedor> servidor;

        bool enEjecucion = false;
        int maxNumDeClientesSimultaneos = 1000;

        public Userver()
        {
            InitializeComponent();
        }

        private void Userver_Load(object sender, EventArgs e)
        {
            metroLabel_Saturacion.Visible = false;
        }

        private void metroButton_Iniciar_Click(object sender, EventArgs e)
        {
            if (enEjecucion == false)
            {

                Task<bool> CargarConfiguracionTask = Task.Run(() => OperacionesFront.CargarConfiguracion());

                if (CargarConfiguracionTask.Result == false)
                {
                    MessageBox.Show("Error al cargar la configuración del sistema, revise el visor de sucesos Application o bien el log del sistema", "ERROR AL INICIAR", MessageBoxButtons.OK);
                    Environment.Exit(666);
                }

                metroListView_Eventos.Items.Add("Se ha cargado la configuración correctamente");
                metroListView_Eventos.Items.Add("Puerto de escucha: " + Utileria.puertoLocal.ToString());
                metroListView_Eventos.Items.Add("Base de datos local en la ip:" + Utileria.cadenaConexionTrx.Substring((Utileria.cadenaConexionTrx.IndexOf("Source=") + 7), 13));
                metroListView_Eventos.Items.Add("Base de datos BO en la ip:" + Utileria.cadenaConexionBO.Substring((Utileria.cadenaConexionTrx.IndexOf("Source=") + 7), 13));

                servidor = new ServidorTransaccional<EstadoDelCliente, EstadoDelServidor, EstadoDelProveedor>(maxNumDeClientesSimultaneos, 1024, 100);
                servidor.ConfigInicioServidor();
                servidor.IniciarServidor(
                    Utileria.puertoLocal,
                    Utileria.ipProveedor,
                    Utileria.puertoProveedor
                    );
                metroButton_Iniciar.Text = "Detener";

                metroListView_Eventos.Items.Add("Todas las instancias comprobadas");

                timer_Refresh.Interval = 1000;
                timer_Refresh.Start();

                metroListView_Eventos.Items.Add("****SERVIDOR EN EJECUCIÓN****");
                enEjecucion = true;
            }
            else
            {
                DialogResult cerrar = MessageBox.Show("¿Cuidado, estás seguro de detener el servidor?", "ALERTA", MessageBoxButtons.OKCancel);

                if (cerrar == DialogResult.OK)
                {
                    servidor.DetenerServidor();
                    metroListView_Eventos.Items.Clear();
                    metroListView_Eventos.Items.Add("****Servidor detenido****");
                    metroButton_Iniciar.Text = "Iniciar";
                    enEjecucion = false;
                }
            }
        }

        private void timer_Refresh_Tick(object sender, EventArgs e)
        {
            metroLabel_ClientesConectados.Text = "Clientes conectados: " + servidor.numeroclientesConectados;
            metroLabel_TotalBytesLeidos.Text = "Total De Gb Leidos: " + (servidor.totalDeBytesTransferidos / 1024);
            int saturacion = (maxNumDeClientesSimultaneos * 90) / 100;
            if (servidor.numeroclientesConectados >= saturacion)
            {
                metroLabel_Saturacion.Text="SERVIDOR AL 90% DE SATURACIÓN!!!";
            }
            else
            {
                metroLabel_Saturacion.Visible = true;
                metroLabel_Saturacion.Text = "Servidor en optimas condiciones";
            }
        }

        private void Userver_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult cerrar = MessageBox.Show("¿Cuidado, estás seguro de cerrar el servidor?", "ALERTA", MessageBoxButtons.OKCancel);

            if (cerrar == DialogResult.OK && enEjecucion)
            {
                servidor.DetenerServidor();
            }
        }

        private void Userver_Resize(object sender, EventArgs e)
        {
            if (WindowState==FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Normal;
            }
            else
            {
                Size = new System.Drawing.Size(431, 506);
            }
        }
    }
}
