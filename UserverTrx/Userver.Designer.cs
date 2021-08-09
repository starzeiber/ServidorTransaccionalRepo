namespace Userver
{
    partial class Userver
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.metroButton_Iniciar = new MetroFramework.Controls.MetroButton();
            this.metroLabel_ClientesConectados = new MetroFramework.Controls.MetroLabel();
            this.timer_Refresh = new System.Windows.Forms.Timer(this.components);
            this.metroLabel_TotalBytesLeidos = new MetroFramework.Controls.MetroLabel();
            this.metroListView_Eventos = new MetroFramework.Controls.MetroListView();
            this.metroLabel_Saturacion = new MetroFramework.Controls.MetroLabel();
            this.SuspendLayout();
            // 
            // metroButton_Iniciar
            // 
            this.metroButton_Iniciar.FontSize = MetroFramework.MetroButtonSize.Tall;
            this.metroButton_Iniciar.Location = new System.Drawing.Point(154, 76);
            this.metroButton_Iniciar.Name = "metroButton_Iniciar";
            this.metroButton_Iniciar.Size = new System.Drawing.Size(123, 43);
            this.metroButton_Iniciar.TabIndex = 1;
            this.metroButton_Iniciar.Text = "Iniciar";
            this.metroButton_Iniciar.UseSelectable = true;
            this.metroButton_Iniciar.Click += new System.EventHandler(this.metroButton_Iniciar_Click);
            // 
            // metroLabel_ClientesConectados
            // 
            this.metroLabel_ClientesConectados.AutoSize = true;
            this.metroLabel_ClientesConectados.Location = new System.Drawing.Point(49, 137);
            this.metroLabel_ClientesConectados.Name = "metroLabel_ClientesConectados";
            this.metroLabel_ClientesConectados.Size = new System.Drawing.Size(124, 19);
            this.metroLabel_ClientesConectados.TabIndex = 2;
            this.metroLabel_ClientesConectados.Text = "Clientes conectados";
            // 
            // timer_Refresh
            // 
            this.timer_Refresh.Tick += new System.EventHandler(this.timer_Refresh_Tick);
            // 
            // metroLabel_TotalBytesLeidos
            // 
            this.metroLabel_TotalBytesLeidos.AutoSize = true;
            this.metroLabel_TotalBytesLeidos.Location = new System.Drawing.Point(199, 137);
            this.metroLabel_TotalBytesLeidos.Name = "metroLabel_TotalBytesLeidos";
            this.metroLabel_TotalBytesLeidos.Size = new System.Drawing.Size(130, 19);
            this.metroLabel_TotalBytesLeidos.TabIndex = 3;
            this.metroLabel_TotalBytesLeidos.Text = "Total de Bytes Leidos";
            // 
            // metroListView_Eventos
            // 
            this.metroListView_Eventos.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.metroListView_Eventos.FullRowSelect = true;
            this.metroListView_Eventos.Location = new System.Drawing.Point(49, 183);
            this.metroListView_Eventos.Name = "metroListView_Eventos";
            this.metroListView_Eventos.OwnerDraw = true;
            this.metroListView_Eventos.Size = new System.Drawing.Size(332, 181);
            this.metroListView_Eventos.TabIndex = 4;
            this.metroListView_Eventos.UseCompatibleStateImageBehavior = false;
            this.metroListView_Eventos.UseSelectable = true;
            this.metroListView_Eventos.View = System.Windows.Forms.View.List;
            // 
            // metroLabel_Saturacion
            // 
            this.metroLabel_Saturacion.AutoSize = true;
            this.metroLabel_Saturacion.FontSize = MetroFramework.MetroLabelSize.Tall;
            this.metroLabel_Saturacion.ForeColor = System.Drawing.Color.Yellow;
            this.metroLabel_Saturacion.Location = new System.Drawing.Point(82, 376);
            this.metroLabel_Saturacion.Name = "metroLabel_Saturacion";
            this.metroLabel_Saturacion.Size = new System.Drawing.Size(259, 25);
            this.metroLabel_Saturacion.TabIndex = 5;
            this.metroLabel_Saturacion.Text = "Servidor en optimas condiciones";
            // 
            // Userver
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(431, 415);
            this.Controls.Add(this.metroLabel_Saturacion);
            this.Controls.Add(this.metroListView_Eventos);
            this.Controls.Add(this.metroLabel_TotalBytesLeidos);
            this.Controls.Add(this.metroLabel_ClientesConectados);
            this.Controls.Add(this.metroButton_Iniciar);
            this.Name = "Userver";
            this.Text = "U Server";
            this.TextAlign = MetroFramework.Forms.MetroFormTextAlign.Center;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Userver_FormClosing);
            this.Load += new System.EventHandler(this.Userver_Load);
            this.Resize += new System.EventHandler(this.Userver_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private MetroFramework.Controls.MetroButton metroButton_Iniciar;
        private MetroFramework.Controls.MetroLabel metroLabel_ClientesConectados;
        private System.Windows.Forms.Timer timer_Refresh;
        private MetroFramework.Controls.MetroLabel metroLabel_TotalBytesLeidos;
        private MetroFramework.Controls.MetroListView metroListView_Eventos;
        private MetroFramework.Controls.MetroLabel metroLabel_Saturacion;
    }
}

