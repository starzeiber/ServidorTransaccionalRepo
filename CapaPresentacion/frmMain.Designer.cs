namespace CapaPresentacion
{
    partial class frmMain
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
            this.button_Iniciar = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.button_Detener = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button_Iniciar
            // 
            this.button_Iniciar.Location = new System.Drawing.Point(341, 25);
            this.button_Iniciar.Name = "button_Iniciar";
            this.button_Iniciar.Size = new System.Drawing.Size(75, 23);
            this.button_Iniciar.TabIndex = 0;
            this.button_Iniciar.Text = "Iniciar";
            this.button_Iniciar.UseVisualStyleBackColor = true;
            this.button_Iniciar.Click += new System.EventHandler(this.button_Iniciar_Click);
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(12, 12);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(303, 368);
            this.listBox1.TabIndex = 1;
            // 
            // button_Detener
            // 
            this.button_Detener.Location = new System.Drawing.Point(341, 80);
            this.button_Detener.Name = "button_Detener";
            this.button_Detener.Size = new System.Drawing.Size(75, 23);
            this.button_Detener.TabIndex = 2;
            this.button_Detener.Text = "Detener";
            this.button_Detener.UseVisualStyleBackColor = true;
            this.button_Detener.Click += new System.EventHandler(this.button_Detener_Click);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(447, 393);
            this.Controls.Add(this.button_Detener);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.button_Iniciar);
            this.Name = "frmMain";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button_Iniciar;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button button_Detener;
    }
}

