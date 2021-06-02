using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
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
            ServidorTransaccional<estadoDelCliente, estadoDelServidor> servidor = 
                new ServidorTransaccional<estadoDelCliente, estadoDelServidor>(10000, 1024, 10);
            servidor.inicializarServidor();
            servidor.iniciarServidor(5555);
        }
    }
}
