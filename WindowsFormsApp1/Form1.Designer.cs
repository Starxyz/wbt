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
            this.lblServerInfo = new System.Windows.Forms.Label();
            this.lblClientInfo = new System.Windows.Forms.Label();
            this.btnModbusControl = new System.Windows.Forms.Button();
            this.lblWeight = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.btnProductRules = new System.Windows.Forms.Button();
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
            this.txtLog.Location = new System.Drawing.Point(12, 120);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(776, 468);
            this.txtLog.TabIndex = 2;
            //
            // lblTcpStatus
            //
            this.lblTcpStatus.AutoSize = true;
            this.lblTcpStatus.Location = new System.Drawing.Point(174, 17);
            this.lblTcpStatus.Name = "lblTcpStatus";
            this.lblTcpStatus.Size = new System.Drawing.Size(71, 12);
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
            // lblServerInfo
            //
            this.lblServerInfo.AutoSize = true;
            this.lblServerInfo.Location = new System.Drawing.Point(12, 70);
            this.lblServerInfo.Name = "lblServerInfo";
            this.lblServerInfo.Size = new System.Drawing.Size(89, 12);
            this.lblServerInfo.TabIndex = 5;
            this.lblServerInfo.Text = "服务器: 未启动";
            //
            // lblClientInfo
            //
            this.lblClientInfo.AutoSize = true;
            this.lblClientInfo.Location = new System.Drawing.Point(12, 90);
            this.lblClientInfo.Name = "lblClientInfo";
            this.lblClientInfo.Size = new System.Drawing.Size(89, 12);
            this.lblClientInfo.TabIndex = 6;
            this.lblClientInfo.Text = "客户端: 未连接";
            //
            // btnModbusControl
            //
            this.btnModbusControl.Location = new System.Drawing.Point(93, 41);
            this.btnModbusControl.Name = "btnModbusControl";
            this.btnModbusControl.Size = new System.Drawing.Size(75, 23);
            this.btnModbusControl.TabIndex = 7;
            this.btnModbusControl.Text = "连接重量";
            this.btnModbusControl.UseVisualStyleBackColor = true;
            this.btnModbusControl.Click += new System.EventHandler(this.btnModbusControl_Click_1);
            //
            // lblWeight
            //
            this.lblWeight.AutoSize = true;
            this.lblWeight.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold);
            this.lblWeight.Location = new System.Drawing.Point(323, 90);
            this.lblWeight.Name = "lblWeight";
            this.lblWeight.Size = new System.Drawing.Size(95, 24);
            this.lblWeight.TabIndex = 9;
            this.lblWeight.Text = "重量: 0kg";
            //
            // button1
            //
            this.button1.Location = new System.Drawing.Point(315, 11);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 10;
            this.button1.Text = "设置模板";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click_1);
            //
            // btnProductRules
            //
            this.btnProductRules.Location = new System.Drawing.Point(315, 41);
            this.btnProductRules.Name = "btnProductRules";
            this.btnProductRules.Size = new System.Drawing.Size(75, 23);
            this.btnProductRules.TabIndex = 11;
            this.btnProductRules.Text = "产品规则";
            this.btnProductRules.UseVisualStyleBackColor = true;
            this.btnProductRules.Click += new System.EventHandler(this.btnProductRules_Click);
            //
            // Form1
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.btnProductRules);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.lblWeight);
            this.Controls.Add(this.btnModbusControl);
            this.Controls.Add(this.lblClientInfo);
            this.Controls.Add(this.lblServerInfo);
            this.Controls.Add(this.btnTcpControl);
            this.Controls.Add(this.lblTcpStatus);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btn_select_file);
            this.Controls.Add(this.btn_print);
            this.Name = "Form1";
            this.Text = "打印控制系统";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_print;
        private System.Windows.Forms.Button btn_select_file;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label lblTcpStatus;
        private System.Windows.Forms.Button btnTcpControl;
        private System.Windows.Forms.Label lblServerInfo;
        private System.Windows.Forms.Label lblClientInfo;
        private System.Windows.Forms.Button btnModbusControl;
        private System.Windows.Forms.Label lblWeight;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnProductRules;
    }
}
