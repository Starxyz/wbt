namespace WindowsFormsApp1
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.btn_print = new System.Windows.Forms.Button();
            this.btn_select_file = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.lblTcpStatus = new System.Windows.Forms.Label();
            this.btnTcpControl = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_print
            // 
            this.btn_print.Location = new System.Drawing.Point(12, 12);
            this.btn_print.Name = "btn_print";
            this.btn_print.Size = new System.Drawing.Size(75, 23);
            this.btn_print.TabIndex = 0;
            this.btn_print.Text = "打印";
            this.btn_print.UseVisualStyleBackColor = true;
            this.btn_print.Click += new System.EventHandler(this.btn_print_Click);
            // 
            // btn_select_file
            // 
            this.btn_select_file.Location = new System.Drawing.Point(93, 12);
            this.btn_select_file.Name = "btn_select_file";
            this.btn_select_file.Size = new System.Drawing.Size(75, 23);
            this.btn_select_file.TabIndex = 1;
            this.btn_select_file.Text = "选择文件";
            this.btn_select_file.UseVisualStyleBackColor = true;
            this.btn_select_file.Click += new System.EventHandler(this.btn_select_file_Click);
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Location = new System.Drawing.Point(12, 70);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(260, 179);
            this.txtLog.TabIndex = 2;
            // 
            // lblTcpStatus
            // 
            this.lblTcpStatus.AutoSize = true;
            this.lblTcpStatus.Location = new System.Drawing.Point(174, 17);
            this.lblTcpStatus.Name = "lblTcpStatus";
            this.lblTcpStatus.Size = new System.Drawing.Size(65, 12);
            this.lblTcpStatus.TabIndex = 3;
            this.lblTcpStatus.Text = "TCP: 未启动";
            // 
            // btnTcpControl
            // 
            this.btnTcpControl.Location = new System.Drawing.Point(12, 41);
            this.btnTcpControl.Name = "btnTcpControl";
            this.btnTcpControl.Size = new System.Drawing.Size(75, 23);
            this.btnTcpControl.TabIndex = 4;
            this.btnTcpControl.Text = "启动TCP";
            this.btnTcpControl.UseVisualStyleBackColor = true;
            this.btnTcpControl.Click += new System.EventHandler(this.btnTcpControl_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.btnTcpControl);
            this.Controls.Add(this.lblTcpStatus);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btn_select_file);
            this.Controls.Add(this.btn_print);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_print;
        private System.Windows.Forms.Button btn_select_file;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label lblTcpStatus;
        private System.Windows.Forms.Button btnTcpControl;
    }
}
