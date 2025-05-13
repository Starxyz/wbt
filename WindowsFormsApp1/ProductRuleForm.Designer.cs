namespace WindowsFormsApp1
{
    partial class ProductRuleForm
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
            this.dgvRules = new System.Windows.Forms.DataGridView();
            this.grpRuleDetails = new System.Windows.Forms.GroupBox();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnDeleteRule = new System.Windows.Forms.Button();
            this.btnUpdateRule = new System.Windows.Forms.Button();
            this.btnAddRule = new System.Windows.Forms.Button();
            this.chkEnableSpecialRules = new System.Windows.Forms.CheckBox();
            this.chkRejectPrint = new System.Windows.Forms.CheckBox();
            this.txtQRCode = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtWeightUpperLimit = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txtWeightLowerLimit = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtCustomerName = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtChickenHouse = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtSpecification = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtProductName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtVersion = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.grpSpecialRules = new System.Windows.Forms.GroupBox();
            this.btnDeleteSpecialRule = new System.Windows.Forms.Button();
            this.btnUpdateSpecialRule = new System.Windows.Forms.Button();
            this.btnAddSpecialRule = new System.Windows.Forms.Button();
            this.txtSpecialQRCode = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.txtSpecialWeightUpperLimit = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.txtSpecialWeightLowerLimit = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.txtSpecialChickenHouse = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.dgvSpecialRules = new System.Windows.Forms.DataGridView();
            this.chkSpecialRejectPrint = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRules)).BeginInit();
            this.grpRuleDetails.SuspendLayout();
            this.grpSpecialRules.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSpecialRules)).BeginInit();
            this.SuspendLayout();
            //
            // dgvRules
            //
            this.dgvRules.AllowUserToAddRows = false;
            this.dgvRules.AllowUserToDeleteRows = false;
            this.dgvRules.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvRules.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvRules.Location = new System.Drawing.Point(12, 12);
            this.dgvRules.Name = "dgvRules";
            this.dgvRules.RowHeadersWidth = 51;
            this.dgvRules.RowTemplate.Height = 24;
            this.dgvRules.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvRules.Size = new System.Drawing.Size(1158, 250);
            this.dgvRules.TabIndex = 0;
            this.dgvRules.SelectionChanged += new System.EventHandler(this.dgvRules_SelectionChanged);
            //
            // grpRuleDetails
            //
            this.grpRuleDetails.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpRuleDetails.Controls.Add(this.btnClear);
            this.grpRuleDetails.Controls.Add(this.btnDeleteRule);
            this.grpRuleDetails.Controls.Add(this.btnUpdateRule);
            this.grpRuleDetails.Controls.Add(this.btnAddRule);
            this.grpRuleDetails.Controls.Add(this.chkEnableSpecialRules);
            this.grpRuleDetails.Controls.Add(this.chkRejectPrint);
            this.grpRuleDetails.Controls.Add(this.txtQRCode);
            this.grpRuleDetails.Controls.Add(this.label9);
            this.grpRuleDetails.Controls.Add(this.txtWeightUpperLimit);
            this.grpRuleDetails.Controls.Add(this.label8);
            this.grpRuleDetails.Controls.Add(this.txtWeightLowerLimit);
            this.grpRuleDetails.Controls.Add(this.label7);
            this.grpRuleDetails.Controls.Add(this.txtCustomerName);
            this.grpRuleDetails.Controls.Add(this.label6);
            this.grpRuleDetails.Controls.Add(this.txtChickenHouse);
            this.grpRuleDetails.Controls.Add(this.label5);
            this.grpRuleDetails.Controls.Add(this.txtSpecification);
            this.grpRuleDetails.Controls.Add(this.label4);
            this.grpRuleDetails.Controls.Add(this.txtProductName);
            this.grpRuleDetails.Controls.Add(this.label3);
            this.grpRuleDetails.Controls.Add(this.txtVersion);
            this.grpRuleDetails.Controls.Add(this.label2);
            this.grpRuleDetails.Controls.Add(this.label1);
            this.grpRuleDetails.Location = new System.Drawing.Point(12, 268);
            this.grpRuleDetails.Name = "grpRuleDetails";
            this.grpRuleDetails.Size = new System.Drawing.Size(1158, 200);
            this.grpRuleDetails.TabIndex = 1;
            this.grpRuleDetails.TabStop = false;
            this.grpRuleDetails.Text = "规则详情";
            //
            // btnClear
            //
            this.btnClear.Location = new System.Drawing.Point(1026, 156);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(120, 30);
            this.btnClear.TabIndex = 22;
            this.btnClear.Text = "清空";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            //
            // btnDeleteRule
            //
            this.btnDeleteRule.Enabled = false;
            this.btnDeleteRule.Location = new System.Drawing.Point(1026, 120);
            this.btnDeleteRule.Name = "btnDeleteRule";
            this.btnDeleteRule.Size = new System.Drawing.Size(120, 30);
            this.btnDeleteRule.TabIndex = 21;
            this.btnDeleteRule.Text = "删除规则";
            this.btnDeleteRule.UseVisualStyleBackColor = true;
            this.btnDeleteRule.Click += new System.EventHandler(this.btnDeleteRule_Click);
            //
            // btnUpdateRule
            //
            this.btnUpdateRule.Enabled = false;
            this.btnUpdateRule.Location = new System.Drawing.Point(1026, 84);
            this.btnUpdateRule.Name = "btnUpdateRule";
            this.btnUpdateRule.Size = new System.Drawing.Size(120, 30);
            this.btnUpdateRule.TabIndex = 20;
            this.btnUpdateRule.Text = "更新规则";
            this.btnUpdateRule.UseVisualStyleBackColor = true;
            this.btnUpdateRule.Click += new System.EventHandler(this.btnUpdateRule_Click);
            //
            // btnAddRule
            //
            this.btnAddRule.Location = new System.Drawing.Point(1026, 48);
            this.btnAddRule.Name = "btnAddRule";
            this.btnAddRule.Size = new System.Drawing.Size(120, 30);
            this.btnAddRule.TabIndex = 19;
            this.btnAddRule.Text = "添加规则";
            this.btnAddRule.UseVisualStyleBackColor = true;
            this.btnAddRule.Click += new System.EventHandler(this.btnAddRule_Click);
            //
            // chkEnableSpecialRules
            //
            this.chkEnableSpecialRules.AutoSize = true;
            this.chkEnableSpecialRules.Location = new System.Drawing.Point(800, 156);
            this.chkEnableSpecialRules.Name = "chkEnableSpecialRules";
            this.chkEnableSpecialRules.Size = new System.Drawing.Size(119, 21);
            this.chkEnableSpecialRules.TabIndex = 18;
            this.chkEnableSpecialRules.Text = "启用特殊规则";
            this.chkEnableSpecialRules.UseVisualStyleBackColor = true;
            this.chkEnableSpecialRules.CheckedChanged += new System.EventHandler(this.chkEnableSpecialRules_CheckedChanged);
            //
            // chkRejectPrint
            //
            this.chkRejectPrint.AutoSize = true;
            this.chkRejectPrint.Location = new System.Drawing.Point(800, 120);
            this.chkRejectPrint.Name = "chkRejectPrint";
            this.chkRejectPrint.Size = new System.Drawing.Size(89, 21);
            this.chkRejectPrint.TabIndex = 17;
            this.chkRejectPrint.Text = "允许打印";
            this.chkRejectPrint.UseVisualStyleBackColor = true;
            this.chkRejectPrint.CheckedChanged += new System.EventHandler(this.chkRejectPrint_CheckedChanged);
            //
            // txtQRCode
            //
            this.txtQRCode.Location = new System.Drawing.Point(800, 84);
            this.txtQRCode.Name = "txtQRCode";
            this.txtQRCode.Size = new System.Drawing.Size(200, 22);
            this.txtQRCode.TabIndex = 16;
            //
            // label9
            //
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(730, 87);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(64, 17);
            this.label9.TabIndex = 15;
            this.label9.Text = "二维码：";
            //
            // txtWeightUpperLimit
            //
            this.txtWeightUpperLimit.Location = new System.Drawing.Point(500, 84);
            this.txtWeightUpperLimit.Name = "txtWeightUpperLimit";
            this.txtWeightUpperLimit.Size = new System.Drawing.Size(200, 22);
            this.txtWeightUpperLimit.TabIndex = 14;
            //
            // label8
            //
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(430, 87);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(64, 17);
            this.label8.TabIndex = 13;
            this.label8.Text = "重量上限：";
            //
            // txtWeightLowerLimit
            //
            this.txtWeightLowerLimit.Location = new System.Drawing.Point(500, 48);
            this.txtWeightLowerLimit.Name = "txtWeightLowerLimit";
            this.txtWeightLowerLimit.Size = new System.Drawing.Size(200, 22);
            this.txtWeightLowerLimit.TabIndex = 12;
            //
            // label7
            //
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(430, 51);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(64, 17);
            this.label7.TabIndex = 11;
            this.label7.Text = "重量下限：";
            //
            // txtCustomerName
            //
            this.txtCustomerName.Location = new System.Drawing.Point(500, 120);
            this.txtCustomerName.Name = "txtCustomerName";
            this.txtCustomerName.Size = new System.Drawing.Size(200, 22);
            this.txtCustomerName.TabIndex = 10;
            //
            // label6
            //
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(430, 123);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(64, 17);
            this.label6.TabIndex = 9;
            this.label6.Text = "客户名：";
            //
            // txtChickenHouse
            //
            this.txtChickenHouse.Location = new System.Drawing.Point(800, 48);
            this.txtChickenHouse.Name = "txtChickenHouse";
            this.txtChickenHouse.Size = new System.Drawing.Size(200, 22);
            this.txtChickenHouse.TabIndex = 8;
            //
            // label5
            //
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(730, 51);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 17);
            this.label5.TabIndex = 7;
            this.label5.Text = "鸡舍号：";
            //
            // txtSpecification
            //
            this.txtSpecification.Location = new System.Drawing.Point(200, 156);
            this.txtSpecification.Name = "txtSpecification";
            this.txtSpecification.Size = new System.Drawing.Size(200, 22);
            this.txtSpecification.TabIndex = 6;
            //
            // label4
            //
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(130, 159);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(64, 17);
            this.label4.TabIndex = 5;
            this.label4.Text = "规格：";
            //
            // txtProductName
            //
            this.txtProductName.Location = new System.Drawing.Point(200, 120);
            this.txtProductName.Name = "txtProductName";
            this.txtProductName.Size = new System.Drawing.Size(200, 22);
            this.txtProductName.TabIndex = 4;
            //
            // label3
            //
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(130, 123);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(64, 17);
            this.label3.TabIndex = 3;
            this.label3.Text = "品名：";
            //
            // txtVersion
            //
            this.txtVersion.Location = new System.Drawing.Point(200, 84);
            this.txtVersion.Name = "txtVersion";
            this.txtVersion.Size = new System.Drawing.Size(200, 22);
            this.txtVersion.TabIndex = 2;
            //
            // label2
            //
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(130, 87);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 17);
            this.label2.TabIndex = 1;
            this.label2.Text = "版面：";
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(20, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "基本信息";
            //
            // grpSpecialRules
            //
            this.grpSpecialRules.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpSpecialRules.Controls.Add(this.btnDeleteSpecialRule);
            this.grpSpecialRules.Controls.Add(this.btnUpdateSpecialRule);
            this.grpSpecialRules.Controls.Add(this.btnAddSpecialRule);
            this.grpSpecialRules.Controls.Add(this.txtSpecialQRCode);
            this.grpSpecialRules.Controls.Add(this.label13);
            this.grpSpecialRules.Controls.Add(this.txtSpecialWeightUpperLimit);
            this.grpSpecialRules.Controls.Add(this.label12);
            this.grpSpecialRules.Controls.Add(this.txtSpecialWeightLowerLimit);
            this.grpSpecialRules.Controls.Add(this.label11);
            this.grpSpecialRules.Controls.Add(this.txtSpecialChickenHouse);
            this.grpSpecialRules.Controls.Add(this.label10);
            this.grpSpecialRules.Controls.Add(this.dgvSpecialRules);
            this.grpSpecialRules.Controls.Add(this.chkSpecialRejectPrint);
            this.grpSpecialRules.Enabled = false;
            this.grpSpecialRules.Location = new System.Drawing.Point(12, 474);
            this.grpSpecialRules.Name = "grpSpecialRules";
            this.grpSpecialRules.Size = new System.Drawing.Size(1158, 250);
            this.grpSpecialRules.TabIndex = 2;
            this.grpSpecialRules.TabStop = false;
            this.grpSpecialRules.Text = "特殊规则";
            //
            // btnDeleteSpecialRule
            //
            this.btnDeleteSpecialRule.Location = new System.Drawing.Point(1026, 156);
            this.btnDeleteSpecialRule.Name = "btnDeleteSpecialRule";
            this.btnDeleteSpecialRule.Size = new System.Drawing.Size(120, 30);
            this.btnDeleteSpecialRule.TabIndex = 10;
            this.btnDeleteSpecialRule.Text = "删除特殊规则";
            this.btnDeleteSpecialRule.UseVisualStyleBackColor = true;
            this.btnDeleteSpecialRule.Click += new System.EventHandler(this.btnDeleteSpecialRule_Click);
            //
            // btnUpdateSpecialRule
            //
            this.btnUpdateSpecialRule.Enabled = false;
            this.btnUpdateSpecialRule.Location = new System.Drawing.Point(1026, 120);
            this.btnUpdateSpecialRule.Name = "btnUpdateSpecialRule";
            this.btnUpdateSpecialRule.Size = new System.Drawing.Size(120, 30);
            this.btnUpdateSpecialRule.TabIndex = 12;
            this.btnUpdateSpecialRule.Text = "更新特殊规则";
            this.btnUpdateSpecialRule.UseVisualStyleBackColor = true;
            this.btnUpdateSpecialRule.Click += new System.EventHandler(this.btnUpdateSpecialRule_Click);
            //
            // btnAddSpecialRule
            //
            this.btnAddSpecialRule.Location = new System.Drawing.Point(1026, 84);
            this.btnAddSpecialRule.Name = "btnAddSpecialRule";
            this.btnAddSpecialRule.Size = new System.Drawing.Size(120, 30);
            this.btnAddSpecialRule.TabIndex = 9;
            this.btnAddSpecialRule.Text = "添加特殊规则";
            this.btnAddSpecialRule.UseVisualStyleBackColor = true;
            this.btnAddSpecialRule.Click += new System.EventHandler(this.btnAddSpecialRule_Click);
            //
            // txtSpecialQRCode
            //
            this.txtSpecialQRCode.Location = new System.Drawing.Point(800, 120);
            this.txtSpecialQRCode.Name = "txtSpecialQRCode";
            this.txtSpecialQRCode.Size = new System.Drawing.Size(200, 22);
            this.txtSpecialQRCode.TabIndex = 8;
            //
            // label13
            //
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(730, 123);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(64, 17);
            this.label13.TabIndex = 7;
            this.label13.Text = "二维码：";
            //
            // chkSpecialRejectPrint
            //
            this.chkSpecialRejectPrint.AutoSize = true;
            this.chkSpecialRejectPrint.Location = new System.Drawing.Point(800, 156);
            this.chkSpecialRejectPrint.Name = "chkSpecialRejectPrint";
            this.chkSpecialRejectPrint.Size = new System.Drawing.Size(89, 21);
            this.chkSpecialRejectPrint.TabIndex = 11;
            this.chkSpecialRejectPrint.Text = "允许打印";
            this.chkSpecialRejectPrint.UseVisualStyleBackColor = true;
            this.chkSpecialRejectPrint.CheckedChanged += new System.EventHandler(this.chkSpecialRejectPrint_CheckedChanged);
            //
            // txtSpecialWeightUpperLimit
            //
            this.txtSpecialWeightUpperLimit.Location = new System.Drawing.Point(500, 120);
            this.txtSpecialWeightUpperLimit.Name = "txtSpecialWeightUpperLimit";
            this.txtSpecialWeightUpperLimit.Size = new System.Drawing.Size(200, 22);
            this.txtSpecialWeightUpperLimit.TabIndex = 6;
            //
            // label12
            //
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(430, 123);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(64, 17);
            this.label12.TabIndex = 5;
            this.label12.Text = "重量上限：";
            //
            // txtSpecialWeightLowerLimit
            //
            this.txtSpecialWeightLowerLimit.Location = new System.Drawing.Point(500, 84);
            this.txtSpecialWeightLowerLimit.Name = "txtSpecialWeightLowerLimit";
            this.txtSpecialWeightLowerLimit.Size = new System.Drawing.Size(200, 22);
            this.txtSpecialWeightLowerLimit.TabIndex = 4;
            //
            // label11
            //
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(430, 87);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(64, 17);
            this.label11.TabIndex = 3;
            this.label11.Text = "重量下限：";
            //
            // txtSpecialChickenHouse
            //
            this.txtSpecialChickenHouse.Location = new System.Drawing.Point(800, 84);
            this.txtSpecialChickenHouse.Name = "txtSpecialChickenHouse";
            this.txtSpecialChickenHouse.Size = new System.Drawing.Size(200, 22);
            this.txtSpecialChickenHouse.TabIndex = 2;
            //
            // label10
            //
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(730, 87);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(64, 17);
            this.label10.TabIndex = 1;
            this.label10.Text = "鸡舍号：";
            //
            // dgvSpecialRules
            //
            this.dgvSpecialRules.AllowUserToAddRows = false;
            this.dgvSpecialRules.AllowUserToDeleteRows = false;
            this.dgvSpecialRules.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSpecialRules.Location = new System.Drawing.Point(6, 21);
            this.dgvSpecialRules.Name = "dgvSpecialRules";
            this.dgvSpecialRules.RowHeadersWidth = 51;
            this.dgvSpecialRules.RowTemplate.Height = 24;
            this.dgvSpecialRules.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvSpecialRules.Size = new System.Drawing.Size(400, 223);
            this.dgvSpecialRules.TabIndex = 0;
            this.dgvSpecialRules.SelectionChanged += new System.EventHandler(this.dgvSpecialRules_SelectionChanged);
            //
            // ProductRuleForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1182, 736);
            this.Controls.Add(this.grpSpecialRules);
            this.Controls.Add(this.grpRuleDetails);
            this.Controls.Add(this.dgvRules);
            this.Name = "ProductRuleForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "产品规则管理";
            ((System.ComponentModel.ISupportInitialize)(this.dgvRules)).EndInit();
            this.grpRuleDetails.ResumeLayout(false);
            this.grpRuleDetails.PerformLayout();
            this.grpSpecialRules.ResumeLayout(false);
            this.grpSpecialRules.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSpecialRules)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvRules;
        private System.Windows.Forms.GroupBox grpRuleDetails;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnDeleteRule;
        private System.Windows.Forms.Button btnUpdateRule;
        private System.Windows.Forms.Button btnAddRule;
        private System.Windows.Forms.CheckBox chkEnableSpecialRules;
        private System.Windows.Forms.CheckBox chkRejectPrint;
        private System.Windows.Forms.TextBox txtQRCode;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtWeightUpperLimit;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtWeightLowerLimit;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtCustomerName;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtChickenHouse;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtSpecification;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtProductName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtVersion;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox grpSpecialRules;
        private System.Windows.Forms.Button btnDeleteSpecialRule;
        private System.Windows.Forms.Button btnUpdateSpecialRule;
        private System.Windows.Forms.Button btnAddSpecialRule;
        private System.Windows.Forms.TextBox txtSpecialQRCode;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtSpecialWeightUpperLimit;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtSpecialWeightLowerLimit;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txtSpecialChickenHouse;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.DataGridView dgvSpecialRules;
        private System.Windows.Forms.CheckBox chkSpecialRejectPrint;
    }
}
