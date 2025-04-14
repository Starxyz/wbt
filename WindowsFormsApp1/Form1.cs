﻿﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Seagull.BarTender.Print;
using NLog;

namespace WindowsFormsApp1
{
    public partial class Form1: Form
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public Engine engine = new Engine();//打印机 引擎
        public LabelFormatDocument format = null;//获取 模板内容
        private string selectedFilePath = null;
        public Form1()
        {
            InitializeComponent();
        }
        public void Pint_model(int printnum)
        {
            Logger.Info($"开始打印，数量: {printnum}");

            for (int i = 0; i < printnum; i++)
            {
                try
                {
                    engine.Start();
                    btn_print.Enabled = false;
                    Logger.Debug($"打开模板文件: {selectedFilePath}");
                    format = engine.Documents.Open(selectedFilePath);

                    if (true)

                    {

                        format.SubStrings["品名"].Value = "品名：46无抗鲜鸡蛋";
                        format.SubStrings["规格"].Value = "规格：360枚";
                        format.SubStrings["斤数"].Value = "斤数：12";
                        format.SubStrings["生产日期"].Value = "生产日期：20250204";
                        format.SubStrings["二维码"].Value = "5678";


                    }

                    Result rel = format.Print();//获取打印状态

                    if (rel == Result.Success)
                    {
                        Logger.Info("打印成功");
                       
                    }
                    else
                    {
                        Logger.Error("打印失败");
                        
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, ex.ToString() + " 打印异常");
                    
                }
            }

            btn_print.Enabled = true;
            engine.Stop();
            Logger.Info("打印任务完成");

        }

        private void btn_print_Click(object sender, EventArgs e)
        {
            Pint_model(1);
        }

        private void btn_select_file_Click(object sender, EventArgs e)
        {
            Logger.Debug("开始选择文件");
            // 创建 OpenFileDialog 实例
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                // 设置初始目录为程序运行目录
                openFileDialog.InitialDirectory = Application.StartupPath;

                // 可选：设置对话框标题
                openFileDialog.Title = "请选择文件";

                // 可选：设置文件过滤器
                openFileDialog.Filter = "BarTender 模板文件 (*.btw)|*.btw";

                // 可选：设置默认过滤器索引
                openFileDialog.FilterIndex = 1;

                // 可选：是否恢复上次选择的目录
                openFileDialog.RestoreDirectory = true;

                // 显示对话框并检查用户是否选择了文件
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // 获取选择的文件路径
                    selectedFilePath = openFileDialog.FileName;
                    Logger.Info($"已选择文件: {selectedFilePath}");

                    // 在这里处理所选文件
                    MessageBox.Show($"选择的文件: {selectedFilePath}", "文件已选择",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

    }
}
