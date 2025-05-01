namespace WindowsFormsApp1
{
    partial class TemplateManagerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataGridView dataGridViewTemplates;
        private System.Windows.Forms.Button btnAddTemplate;
        private System.Windows.Forms.Button btnDeleteTemplate;
        private System.Windows.Forms.Button btnEditTemplate;
        private System.Windows.Forms.TextBox txtKey;
        private System.Windows.Forms.TextBox txtProductName;
        private System.Windows.Forms.TextBox txtSpec;
        private System.Windows.Forms.TextBox txtQRCode;
        private System.Windows.Forms.TextBox txtCategory;
        private System.Windows.Forms.TextBox txtSlot;
        private System.Windows.Forms.TextBox txtWeightRange;

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
            this.dataGridViewTemplates = new System.Windows.Forms.DataGridView();
            this.btnAddTemplate = new System.Windows.Forms.Button();
            this.btnDeleteTemplate = new System.Windows.Forms.Button();
            this.btnEditTemplate = new System.Windows.Forms.Button();
            this.txtKey = new System.Windows.Forms.TextBox();
            this.txtProductName = new System.Windows.Forms.TextBox();
            this.txtSpec = new System.Windows.Forms.TextBox();
            this.txtQRCode = new System.Windows.Forms.TextBox();
            this.txtCategory = new System.Windows.Forms.TextBox();
            this.txtSlot = new System.Windows.Forms.TextBox();
            this.txtWeightRange = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewTemplates)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridViewTemplates
            // 
            this.dataGridViewTemplates.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewTemplates.Location = new System.Drawing.Point(12, 12);
            this.dataGridViewTemplates.Name = "dataGridViewTemplates";
            this.dataGridViewTemplates.Size = new System.Drawing.Size(760, 150);
            this.dataGridViewTemplates.TabIndex = 0;
            // 
            // btnAddTemplate
            // 
            this.btnAddTemplate.Location = new System.Drawing.Point(12, 200);
            this.btnAddTemplate.Name = "btnAddTemplate";
            this.btnAddTemplate.Size = new System.Drawing.Size(75, 23);
            this.btnAddTemplate.TabIndex = 1;
            this.btnAddTemplate.Text = "添加模板";
            this.btnAddTemplate.UseVisualStyleBackColor = true;
            this.btnAddTemplate.Click += new System.EventHandler(this.btnAddTemplate_Click);
            // 
            // btnDeleteTemplate
            // 
            this.btnDeleteTemplate.Location = new System.Drawing.Point(93, 200);
            this.btnDeleteTemplate.Name = "btnDeleteTemplate";
            this.btnDeleteTemplate.Size = new System.Drawing.Size(75, 23);
            this.btnDeleteTemplate.TabIndex = 2;
            this.btnDeleteTemplate.Text = "删除模板";
            this.btnDeleteTemplate.UseVisualStyleBackColor = true;
            this.btnDeleteTemplate.Click += new System.EventHandler(this.btnDeleteTemplate_Click);
            // 
            // btnEditTemplate
            // 
            this.btnEditTemplate.Location = new System.Drawing.Point(174, 200);
            this.btnEditTemplate.Name = "btnEditTemplate";
            this.btnEditTemplate.Size = new System.Drawing.Size(75, 23);
            this.btnEditTemplate.TabIndex = 3;
            this.btnEditTemplate.Text = "编辑模板";
            this.btnEditTemplate.UseVisualStyleBackColor = true;
            this.btnEditTemplate.Click += new System.EventHandler(this.btnEditTemplate_Click);
            // 
            // TemplateManagerForm
            // 
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnEditTemplate);
            this.Controls.Add(this.btnDeleteTemplate);
            this.Controls.Add(this.btnAddTemplate);
            this.Controls.Add(this.dataGridViewTemplates);
            this.Name = "TemplateManagerForm";
            this.Text = "模板管理";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewTemplates)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion
    }
}