using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class TemplateManagerForm : Form
    {
        private List<Template> templates;

        public TemplateManagerForm()
        {
            InitializeComponent();
            LoadTemplates();
        }

        // 加载模板数据
        private void LoadTemplates()
        {
            try
            {
                string json = File.ReadAllText("templates.json", Encoding.UTF8);
                templates = JsonConvert.DeserializeObject<List<Template>>(json);
                dataGridViewTemplates.DataSource = templates; // 显示模板列表
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载模板失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 保存模板数据到JSON文件
        private void SaveTemplates()
        {
            try
            {
                string json = JsonConvert.SerializeObject(templates, Formatting.Indented);
                File.WriteAllText("templates.json", json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存模板失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 添加模板按钮点击事件
        private void btnAddTemplate_Click(object sender, EventArgs e)
        {
            var newTemplate = new Template
            {
                key = txtKey.Text,
                productName = txtProductName.Text,
                spec = txtSpec.Text,
                qrcode = txtQRCode.Text,
                category = txtCategory.Text,
                slot = txtSlot.Text,
                weightRange = txtWeightRange.Text
            };

            templates.Add(newTemplate);
            SaveTemplates();
            LoadTemplates(); // 重新加载数据
        }

        // 删除模板按钮点击事件
        private void btnDeleteTemplate_Click(object sender, EventArgs e)
        {
            if (dataGridViewTemplates.SelectedRows.Count > 0)
            {
                var selectedRow = dataGridViewTemplates.SelectedRows[0];
                var templateToDelete = selectedRow.DataBoundItem as Template;

                templates.Remove(templateToDelete);
                SaveTemplates();
                LoadTemplates(); // 重新加载数据
            }
            else
            {
                MessageBox.Show("请先选择一个模板进行删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // 编辑模板按钮点击事件
        private void btnEditTemplate_Click(object sender, EventArgs e)
        {
            if (dataGridViewTemplates.SelectedRows.Count > 0)
            {
                var selectedRow = dataGridViewTemplates.SelectedRows[0];
                var templateToEdit = selectedRow.DataBoundItem as Template;

                templateToEdit.productName = txtProductName.Text;
                templateToEdit.spec = txtSpec.Text;
                templateToEdit.qrcode = txtQRCode.Text;
                templateToEdit.category = txtCategory.Text;
                templateToEdit.slot = txtSlot.Text;
                templateToEdit.weightRange = txtWeightRange.Text;

                SaveTemplates();
                LoadTemplates(); // 重新加载数据
            }
            else
            {
                MessageBox.Show("请先选择一个模板进行编辑", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    public class Template
    {
        public string key { get; set; }
        public string productName { get; set; }
        public string spec { get; set; }
        public string qrcode { get; set; }
        public string category { get; set; }
        public string slot { get; set; }
        public string weightRange { get; set; }
    }
}
