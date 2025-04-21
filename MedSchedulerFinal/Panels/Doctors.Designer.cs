namespace MedSchedulerFinal.Panels
{
    partial class Doctors
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.TableHeader = new System.Windows.Forms.Panel();
            this.c = new System.Windows.Forms.Label();
            this.DoctorRow = new System.Windows.Forms.Panel();
            this.DeleteButton = new System.Windows.Forms.Button();
            this.EditButton = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.button1 = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.GoLeft = new System.Windows.Forms.Button();
            this.GoRight = new System.Windows.Forms.Button();
            this.panel3 = new System.Windows.Forms.Panel();
            this.RowsPanel = new System.Windows.Forms.Panel();
            this.doctorsFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.TableHeader.SuspendLayout();
            this.DoctorRow.SuspendLayout();
            this.panel3.SuspendLayout();
            this.RowsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(491, 9);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(52, 19);
            this.label4.TabIndex = 3;
            this.label4.Text = "Action";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(351, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(63, 19);
            this.label3.TabIndex = 2;
            this.label3.Text = "Surgery";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(19, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 19);
            this.label1.TabIndex = 0;
            this.label1.Text = "Name";
            // 
            // TableHeader
            // 
            this.TableHeader.BackColor = System.Drawing.SystemColors.ControlLight;
            this.TableHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TableHeader.Controls.Add(this.label4);
            this.TableHeader.Controls.Add(this.label3);
            this.TableHeader.Controls.Add(this.c);
            this.TableHeader.Controls.Add(this.label1);
            this.TableHeader.Location = new System.Drawing.Point(21, 25);
            this.TableHeader.Name = "TableHeader";
            this.TableHeader.Size = new System.Drawing.Size(780, 40);
            this.TableHeader.TabIndex = 13;
            this.TableHeader.Paint += new System.Windows.Forms.PaintEventHandler(this.TableHeader_Paint);
            // 
            // c
            // 
            this.c.AutoSize = true;
            this.c.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.c.Location = new System.Drawing.Point(178, 9);
            this.c.Name = "Spezilization";
            this.c.Size = new System.Drawing.Size(94, 19);
            this.c.TabIndex = 1;
            this.c.Text = "Spezilization";
            this.c.Click += new System.EventHandler(this.c_Click);
            // 
            // DoctorRow
            // 
            this.DoctorRow.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.DoctorRow.Controls.Add(this.DeleteButton);
            this.DoctorRow.Controls.Add(this.EditButton);
            this.DoctorRow.Controls.Add(this.label6);
            this.DoctorRow.Controls.Add(this.label5);
            this.DoctorRow.Controls.Add(this.linkLabel1);
            this.DoctorRow.Location = new System.Drawing.Point(3, 9);
            this.DoctorRow.Name = "DoctorRow";
            this.DoctorRow.Size = new System.Drawing.Size(774, 39);
            this.DoctorRow.TabIndex = 1;
            this.DoctorRow.Paint += new System.Windows.Forms.PaintEventHandler(this.DoctorRow_Paint);
            // 
            // DeleteButton
            // 
            this.DeleteButton.Location = new System.Drawing.Point(722, 10);
            this.DeleteButton.Name = "DeleteButton";
            this.DeleteButton.Size = new System.Drawing.Size(33, 23);
            this.DeleteButton.TabIndex = 4;
            this.DeleteButton.Text = "D";
            this.DeleteButton.UseVisualStyleBackColor = true;
            this.DeleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            // 
            // EditButton
            // 
            this.EditButton.Location = new System.Drawing.Point(683, 10);
            this.EditButton.Name = "EditButton";
            this.EditButton.Size = new System.Drawing.Size(33, 23);
            this.EditButton.TabIndex = 3;
            this.EditButton.Text = "E";
            this.EditButton.UseVisualStyleBackColor = true;
            this.EditButton.Click += new System.EventHandler(this.EditButton_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(359, 12);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(52, 21);
            this.label6.TabIndex = 2;
            this.label6.Text = "Demo";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(175, 8);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(52, 21);
            this.label5.TabIndex = 1;
            this.label5.Text = "Demo";
            // 
            // linkLabel1
            // 
            this.linkLabel1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(16, 13);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(92, 13);
            this.linkLabel1.TabIndex = 0;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "DemoForDesigner";
            this.linkLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.SystemColors.Window;
            this.button1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(571, 15);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(95, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "ADD";
            this.button1.UseVisualStyleBackColor = false;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(3, 15);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(202, 20);
            this.textBox1.TabIndex = 0;
            // 
            // button2
            // 
            this.button2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.Location = new System.Drawing.Point(672, 15);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(95, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "Refresh";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // GoLeft
            // 
            this.GoLeft.Location = new System.Drawing.Point(155, 579);
            this.GoLeft.Name = "GoLeft";
            this.GoLeft.Size = new System.Drawing.Size(32, 25);
            this.GoLeft.TabIndex = 17;
            this.GoLeft.Text = "◀";
            this.GoLeft.UseVisualStyleBackColor = true;
            this.GoLeft.Click += new System.EventHandler(this.GoLeft_Click);
            // 
            // GoRight
            // 
            this.GoRight.Location = new System.Drawing.Point(592, 579);
            this.GoRight.Name = "GoRight";
            this.GoRight.Size = new System.Drawing.Size(32, 25);
            this.GoRight.TabIndex = 16;
            this.GoRight.Text = "▶";
            this.GoRight.UseVisualStyleBackColor = true;
            this.GoRight.Click += new System.EventHandler(this.GoRight_Click);
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.button2);
            this.panel3.Controls.Add(this.button1);
            this.panel3.Controls.Add(this.textBox1);
            this.panel3.Location = new System.Drawing.Point(21, 56);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(780, 45);
            this.panel3.TabIndex = 15;
            // 
            // RowsPanel
            // 
            this.RowsPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.RowsPanel.Controls.Add(this.doctorsFlowPanel);
            this.RowsPanel.Controls.Add(this.DoctorRow);
            this.RowsPanel.Location = new System.Drawing.Point(21, 117);
            this.RowsPanel.Name = "RowsPanel";
            this.RowsPanel.Size = new System.Drawing.Size(780, 443);
            this.RowsPanel.TabIndex = 14;
            this.RowsPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.panel2_Paint);
            // 
            // doctorsFlowPanel
            // 
            this.doctorsFlowPanel.AutoScroll = true;
            this.doctorsFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.doctorsFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.doctorsFlowPanel.Location = new System.Drawing.Point(0, 0);
            this.doctorsFlowPanel.Name = "doctorsFlowPanel";
            this.doctorsFlowPanel.Size = new System.Drawing.Size(778, 441);
            this.doctorsFlowPanel.TabIndex = 2;
            this.doctorsFlowPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.doctorsFlowPanel_Paint);
            // 
            // Doctors
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this.TableHeader);
            this.Controls.Add(this.GoLeft);
            this.Controls.Add(this.GoRight);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.RowsPanel);
            this.Name = "Doctors";
            this.Size = new System.Drawing.Size(838, 648);
            this.Load += new System.EventHandler(this.Doctors_Load_1);
            this.TableHeader.ResumeLayout(false);
            this.TableHeader.PerformLayout();
            this.DoctorRow.ResumeLayout(false);
            this.DoctorRow.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.RowsPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel TableHeader;
        private System.Windows.Forms.Label c;
        private System.Windows.Forms.Panel DoctorRow;
        private System.Windows.Forms.Button DeleteButton;
        private System.Windows.Forms.Button EditButton;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button GoLeft;
        private System.Windows.Forms.Button GoRight;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel RowsPanel;
        private System.Windows.Forms.FlowLayoutPanel doctorsFlowPanel;
    }
}
