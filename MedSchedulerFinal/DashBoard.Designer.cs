using System;

namespace MedSchedulerFinal
{
    partial class DashBoard
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.NavBar = new System.Windows.Forms.Panel();
            this.labelAlgorithm = new System.Windows.Forms.Label();
            this.labelDoctors = new System.Windows.Forms.Label();
            this.Surgeries = new System.Windows.Forms.Label();
            this.labelDashBoard = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.TopBar = new System.Windows.Forms.Panel();
            this.panelContentContainer = new System.Windows.Forms.Panel();
            this.NavBar.SuspendLayout();
            this.TopBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(44, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 13);
            this.label1.TabIndex = 0;
            // 
            // NavBar
            // 
            this.NavBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.NavBar.Controls.Add(this.labelAlgorithm);
            this.NavBar.Controls.Add(this.labelDoctors);
            this.NavBar.Controls.Add(this.Surgeries);
            this.NavBar.Controls.Add(this.labelDashBoard);
            this.NavBar.Location = new System.Drawing.Point(0, 0);
            this.NavBar.Name = "NavBar";
            this.NavBar.Size = new System.Drawing.Size(162, 700);
            this.NavBar.TabIndex = 1;
            // 
            // labelAlgorithm
            // 
            this.labelAlgorithm.AutoSize = true;
            this.labelAlgorithm.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAlgorithm.ForeColor = System.Drawing.SystemColors.Window;
            this.labelAlgorithm.Location = new System.Drawing.Point(11, 273);
            this.labelAlgorithm.Name = "labelAlgorithm";
            this.labelAlgorithm.Size = new System.Drawing.Size(102, 25);
            this.labelAlgorithm.TabIndex = 3;
            this.labelAlgorithm.Text = "Algorithm";
            this.labelAlgorithm.Click += new System.EventHandler(this.labelAlgorithm_Click);
            // 
            // labelDoctors
            // 
            this.labelDoctors.AutoSize = true;
            this.labelDoctors.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDoctors.ForeColor = System.Drawing.SystemColors.Window;
            this.labelDoctors.Location = new System.Drawing.Point(11, 180);
            this.labelDoctors.Name = "labelDoctors";
            this.labelDoctors.Size = new System.Drawing.Size(82, 25);
            this.labelDoctors.TabIndex = 2;
            this.labelDoctors.Text = "Doctors";
            this.labelDoctors.Click += new System.EventHandler(this.labelDoctors_Click);
            // 
            // Surgeries
            // 
            this.Surgeries.AutoSize = true;
            this.Surgeries.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Surgeries.ForeColor = System.Drawing.SystemColors.Window;
            this.Surgeries.Location = new System.Drawing.Point(11, 226);
            this.Surgeries.Name = "Surgeries";
            this.Surgeries.Size = new System.Drawing.Size(96, 25);
            this.Surgeries.TabIndex = 1;
            this.Surgeries.Text = "Surgeries";
            this.Surgeries.Click += new System.EventHandler(this.Surgeries_Click);
            // 
            // labelDashBoard
            // 
            this.labelDashBoard.AutoSize = true;
            this.labelDashBoard.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDashBoard.ForeColor = System.Drawing.SystemColors.Window;
            this.labelDashBoard.Location = new System.Drawing.Point(11, 142);
            this.labelDashBoard.Name = "labelDashBoard";
            this.labelDashBoard.Size = new System.Drawing.Size(109, 25);
            this.labelDashBoard.TabIndex = 0;
            this.labelDashBoard.Text = "DashBoard";
            this.labelDashBoard.Click += new System.EventHandler(this.labelDashBoard_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.SystemColors.Window;
            this.label6.Location = new System.Drawing.Point(3, 2);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(235, 45);
            this.label6.TabIndex = 0;
            this.label6.Text = "MedScheduler";
            // 
            // TopBar
            // 
            this.TopBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(44)))), ((int)(((byte)(62)))), ((int)(((byte)(80)))));
            this.TopBar.Controls.Add(this.label6);
            this.TopBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.TopBar.Location = new System.Drawing.Point(0, 0);
            this.TopBar.Name = "TopBar";
            this.TopBar.Size = new System.Drawing.Size(1000, 62);
            this.TopBar.TabIndex = 3;
            this.TopBar.Paint += new System.Windows.Forms.PaintEventHandler(this.TopBar_Paint);
            // 
            // panelContentContainer
            // 
            this.panelContentContainer.BackColor = System.Drawing.SystemColors.Window;
            this.panelContentContainer.Location = new System.Drawing.Point(162, 52);
            this.panelContentContainer.Name = "panelContentContainer";
            this.panelContentContainer.Size = new System.Drawing.Size(838, 648);
            this.panelContentContainer.TabIndex = 4;
            this.panelContentContainer.Paint += new System.Windows.Forms.PaintEventHandler(this.panelContentContainer_Paint);
            // 
            // DashBoard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.ClientSize = new System.Drawing.Size(1000, 700);
            this.Controls.Add(this.TopBar);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.NavBar);
            this.Controls.Add(this.panelContentContainer);
            this.Name = "DashBoard";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.DashBoard_Load);
            this.NavBar.ResumeLayout(false);
            this.NavBar.PerformLayout();
            this.TopBar.ResumeLayout(false);
            this.TopBar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }



        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel NavBar;
        private System.Windows.Forms.Label labelDashBoard;
        private System.Windows.Forms.Label labelAlgorithm;
        private System.Windows.Forms.Label labelDoctors;
        private System.Windows.Forms.Label Surgeries;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Panel TopBar;
        private System.Windows.Forms.Panel panelContentContainer;
    }
}

