using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;

namespace WindowsFormsApp1
{
    public partial class ProductRuleForm : Form
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private ProductRuleManager _ruleManager;
        private ProductRule _currentRule;
        private BindingList<ProductRule> _ruleBindingList;
        private BindingList<SpecialRuleCondition> _specialRuleBindingList;

        public ProductRuleForm()
        {
            InitializeComponent();
            _ruleManager = new ProductRuleManager();
            LoadRules();
            SetupDataGridViews();
        }

        private void LoadRules()
        {
            var rules = _ruleManager.GetAllRules();
            _ruleBindingList = new BindingList<ProductRule>(rules);
            dgvRules.DataSource = _ruleBindingList;
        }

        private void SetupDataGridViews()
        {
            // 设置主规则表格
            dgvRules.AutoGenerateColumns = false;
            dgvRules.Columns.Clear();
            dgvRules.EditMode = DataGridViewEditMode.EditProgrammatically;
            dgvRules.ReadOnly = true;

            dgvRules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Id",
                HeaderText = "序号",
                Width = 50,
                ReadOnly = true
            });

            dgvRules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Version",
                HeaderText = "版面",
                Width = 100,
                ReadOnly = true
            });

            dgvRules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ProductName",
                HeaderText = "品名",
                Width = 150,
                ReadOnly = true
            });

            dgvRules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Specification",
                HeaderText = "规格",
                Width = 150,
                ReadOnly = true
            });

            dgvRules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ChickenHouse",
                HeaderText = "鸡舍号",
                Width = 80,
                ReadOnly = true
            });

            dgvRules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CustomerName",
                HeaderText = "客户名",
                Width = 100,
                ReadOnly = true
            });

            dgvRules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "WeightLowerLimit",
                HeaderText = "重量下限",
                Width = 80,
                ReadOnly = true
            });

            dgvRules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "WeightUpperLimit",
                HeaderText = "重量上限",
                Width = 80,
                ReadOnly = true
            });

            var rejectPrintColumn = new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "RejectPrint",
                HeaderText = "拒绝打印",
                Width = 80,
                ReadOnly = true
            };
            dgvRules.Columns.Add(rejectPrintColumn);

            dgvRules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "QRCode",
                HeaderText = "二维码",
                Width = 100,
                ReadOnly = true
            });

            var enableSpecialRulesColumn = new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "EnableSpecialRules",
                HeaderText = "启用特殊规则",
                Width = 100,
                ReadOnly = true
            };
            dgvRules.Columns.Add(enableSpecialRulesColumn);

            // 设置特殊规则表格
            dgvSpecialRules.AutoGenerateColumns = false;
            dgvSpecialRules.Columns.Clear();
            dgvSpecialRules.EditMode = DataGridViewEditMode.EditProgrammatically;
            dgvSpecialRules.ReadOnly = true;

            dgvSpecialRules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ChickenHouse",
                HeaderText = "鸡舍号",
                Width = 80,
                ReadOnly = true
            });

            dgvSpecialRules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "WeightLowerLimit",
                HeaderText = "重量下限",
                Width = 80,
                ReadOnly = true
            });

            dgvSpecialRules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "WeightUpperLimit",
                HeaderText = "重量上限",
                Width = 80,
                ReadOnly = true
            });

            dgvSpecialRules.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "QRCode",
                HeaderText = "二维码",
                Width = 100,
                ReadOnly = true
            });

            var specialRejectPrintColumn = new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "RejectPrint",
                HeaderText = "拒绝打印",
                Width = 80,
                ReadOnly = true
            };
            dgvSpecialRules.Columns.Add(specialRejectPrintColumn);
        }

        private void dgvRules_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvRules.SelectedRows.Count > 0)
            {
                _currentRule = dgvRules.SelectedRows[0].DataBoundItem as ProductRule;
                if (_currentRule != null)
                {
                    // 设置加载标志，防止触发CheckedChanged事件
                    _isLoading = true;

                    // 更新表单字段
                    txtVersion.Text = _currentRule.Version;
                    txtProductName.Text = _currentRule.ProductName;
                    txtSpecification.Text = _currentRule.Specification;
                    txtChickenHouse.Text = _currentRule.ChickenHouse;
                    txtCustomerName.Text = _currentRule.CustomerName;
                    txtWeightLowerLimit.Text = _currentRule.WeightLowerLimit.ToString();
                    txtWeightUpperLimit.Text = _currentRule.WeightUpperLimit.ToString();
                    chkRejectPrint.Checked = _currentRule.RejectPrint;
                    txtQRCode.Text = _currentRule.QRCode;
                    chkEnableSpecialRules.Checked = _currentRule.EnableSpecialRules;

                    // 重置加载标志
                    _isLoading = false;

                    // 更新特殊规则表格
                    _specialRuleBindingList = new BindingList<SpecialRuleCondition>(_currentRule.SpecialRules);
                    dgvSpecialRules.DataSource = _specialRuleBindingList;

                    // 启用编辑和删除按钮
                    btnUpdateRule.Enabled = true;
                    btnDeleteRule.Enabled = true;
                    grpSpecialRules.Enabled = true;
                }
            }
            else
            {
                // 设置加载标志，防止触发CheckedChanged事件
                _isLoading = true;

                ClearForm();

                // 重置加载标志
                _isLoading = false;

                _currentRule = null;
                btnUpdateRule.Enabled = false;
                btnDeleteRule.Enabled = false;
                grpSpecialRules.Enabled = false;
                dgvSpecialRules.DataSource = null;
            }
        }

        private void ClearForm()
        {
            txtVersion.Text = string.Empty;
            txtProductName.Text = string.Empty;
            txtSpecification.Text = string.Empty;
            txtChickenHouse.Text = string.Empty;
            txtCustomerName.Text = string.Empty;
            txtWeightLowerLimit.Text = string.Empty;
            txtWeightUpperLimit.Text = string.Empty;
            chkRejectPrint.Checked = false;
            txtQRCode.Text = string.Empty;
            chkEnableSpecialRules.Checked = false;
            txtSpecialChickenHouse.Text = string.Empty;
            txtSpecialWeightLowerLimit.Text = string.Empty;
            txtSpecialWeightUpperLimit.Text = string.Empty;
            txtSpecialQRCode.Text = string.Empty;
            chkSpecialRejectPrint.Checked = false;
        }

        private void btnAddRule_Click(object sender, EventArgs e)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(txtVersion.Text))
                {
                    MessageBox.Show("版面不能为空", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!double.TryParse(txtWeightLowerLimit.Text, out double lowerLimit))
                {
                    MessageBox.Show("重量下限必须是有效的数字", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!double.TryParse(txtWeightUpperLimit.Text, out double upperLimit))
                {
                    MessageBox.Show("重量上限必须是有效的数字", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (lowerLimit > upperLimit)
                {
                    MessageBox.Show("重量下限不能大于重量上限", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 创建新规则
                var newRule = new ProductRule
                {
                    Version = txtVersion.Text,
                    ProductName = txtProductName.Text,
                    Specification = txtSpecification.Text,
                    ChickenHouse = txtChickenHouse.Text,
                    CustomerName = txtCustomerName.Text,
                    WeightLowerLimit = lowerLimit,
                    WeightUpperLimit = upperLimit,
                    RejectPrint = chkRejectPrint.Checked,
                    QRCode = txtQRCode.Text,
                    EnableSpecialRules = chkEnableSpecialRules.Checked,
                    SpecialRules = new List<SpecialRuleCondition>()
                };

                // 添加规则
                int newId = _ruleManager.AddRule(newRule);

                if (newId == -1)
                {
                    // 规则重复
                    MessageBox.Show("添加失败：已存在相同的规则（版面、鸡舍号、客户名和重量范围重叠）", "规则重复", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Logger.Info($"添加了新规则，ID: {newId}");

                // 刷新列表
                LoadRules();
                ClearForm();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "添加规则失败");
                MessageBox.Show($"添加规则失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpdateRule_Click(object sender, EventArgs e)
        {
            if (_currentRule == null)
            {
                MessageBox.Show("请先选择一个规则", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(txtVersion.Text))
                {
                    MessageBox.Show("版面不能为空", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!double.TryParse(txtWeightLowerLimit.Text, out double lowerLimit))
                {
                    MessageBox.Show("重量下限必须是有效的数字", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!double.TryParse(txtWeightUpperLimit.Text, out double upperLimit))
                {
                    MessageBox.Show("重量上限必须是有效的数字", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (lowerLimit > upperLimit)
                {
                    MessageBox.Show("重量下限不能大于重量上限", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 更新规则
                _currentRule.Version = txtVersion.Text;
                _currentRule.ProductName = txtProductName.Text;
                _currentRule.Specification = txtSpecification.Text;
                _currentRule.ChickenHouse = txtChickenHouse.Text;
                _currentRule.CustomerName = txtCustomerName.Text;
                _currentRule.WeightLowerLimit = lowerLimit;
                _currentRule.WeightUpperLimit = upperLimit;
                _currentRule.RejectPrint = chkRejectPrint.Checked;
                _currentRule.QRCode = txtQRCode.Text;
                _currentRule.EnableSpecialRules = chkEnableSpecialRules.Checked;

                // 保存更新
                bool updateSuccess = _ruleManager.UpdateRule(_currentRule);

                if (!updateSuccess)
                {
                    // 规则重复
                    MessageBox.Show("更新失败：已存在相同的规则（版面、鸡舍号、客户名和重量范围重叠）", "规则重复", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // 重新加载规则，恢复原始状态
                    LoadRules();
                    return;
                }

                Logger.Info($"更新了规则，ID: {_currentRule.Id}");

                // 保存当前规则的ID，以便在刷新后重新选择
                int currentRuleId = _currentRule.Id;
                bool rejectPrintStatus = _currentRule.RejectPrint;

                // 刷新列表
                LoadRules();

                // 重新选择之前的规则
                SelectRuleById(currentRuleId);

                // 确保拒绝打印状态与更新前一致
                if (_currentRule != null)
                {
                    _currentRule.RejectPrint = rejectPrintStatus;
                    chkRejectPrint.Checked = rejectPrintStatus;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "更新规则失败");
                MessageBox.Show($"更新规则失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDeleteRule_Click(object sender, EventArgs e)
        {
            if (_currentRule == null)
            {
                MessageBox.Show("请先选择一个规则", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // 确认删除
                DialogResult result = MessageBox.Show($"确定要删除规则 {_currentRule.Id} 吗？", "确认删除",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // 删除规则
                    _ruleManager.DeleteRule(_currentRule.Id);
                    Logger.Info($"删除了规则，ID: {_currentRule.Id}");

                    // 刷新列表
                    LoadRules();
                    ClearForm();
                    _currentRule = null;
                    btnUpdateRule.Enabled = false;
                    btnDeleteRule.Enabled = false;
                    grpSpecialRules.Enabled = false;
                    dgvSpecialRules.DataSource = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "删除规则失败");
                MessageBox.Show($"删除规则失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddSpecialRule_Click(object sender, EventArgs e)
        {
            if (_currentRule == null)
            {
                MessageBox.Show("请先选择一个主规则", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // 验证输入
                if (!double.TryParse(txtSpecialWeightLowerLimit.Text, out double lowerLimit))
                {
                    MessageBox.Show("特殊规则重量下限必须是有效的数字", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!double.TryParse(txtSpecialWeightUpperLimit.Text, out double upperLimit))
                {
                    MessageBox.Show("特殊规则重量上限必须是有效的数字", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (lowerLimit > upperLimit)
                {
                    MessageBox.Show("特殊规则重量下限不能大于重量上限", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtSpecialQRCode.Text))
                {
                    MessageBox.Show("特殊规则二维码不能为空", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 创建新的特殊规则条件
                var newCondition = new SpecialRuleCondition
                {
                    ChickenHouse = txtSpecialChickenHouse.Text,
                    WeightLowerLimit = lowerLimit,
                    WeightUpperLimit = upperLimit,
                    QRCode = txtSpecialQRCode.Text,
                    RejectPrint = chkSpecialRejectPrint.Checked
                };

                // 检查特殊规则是否重复
                if (_ruleManager.IsSpecialRuleConditionDuplicate(_currentRule, newCondition))
                {
                    MessageBox.Show("添加失败：已存在相同的特殊规则条件（鸡舍号和重量范围重叠）", "规则重复", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 添加到当前规则
                _currentRule.SpecialRules.Add(newCondition);
                _currentRule.EnableSpecialRules = true;
                chkEnableSpecialRules.Checked = true;

                // 更新规则
                bool updateSuccess = _ruleManager.UpdateRule(_currentRule);
                if (!updateSuccess)
                {
                    MessageBox.Show("更新失败：添加特殊规则后与其他规则冲突", "规则重复", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // 移除刚添加的特殊规则
                    _currentRule.SpecialRules.Remove(newCondition);
                    return;
                }

                Logger.Info($"为规则 {_currentRule.Id} 添加了特殊规则条件");

                // 刷新特殊规则表格
                _specialRuleBindingList = new BindingList<SpecialRuleCondition>(_currentRule.SpecialRules);
                dgvSpecialRules.DataSource = _specialRuleBindingList;

                // 清空特殊规则输入框
                txtSpecialChickenHouse.Text = string.Empty;
                txtSpecialWeightLowerLimit.Text = string.Empty;
                txtSpecialWeightUpperLimit.Text = string.Empty;
                txtSpecialQRCode.Text = string.Empty;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "添加特殊规则条件失败");
                MessageBox.Show($"添加特殊规则条件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDeleteSpecialRule_Click(object sender, EventArgs e)
        {
            if (_currentRule == null)
            {
                MessageBox.Show("请先选择一个主规则", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (dgvSpecialRules.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择一个特殊规则条件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // 获取选中的特殊规则条件
                var selectedCondition = dgvSpecialRules.SelectedRows[0].DataBoundItem as SpecialRuleCondition;

                // 从当前规则中移除
                _currentRule.SpecialRules.Remove(selectedCondition);

                // 如果没有特殊规则条件了，禁用特殊规则
                if (_currentRule.SpecialRules.Count == 0)
                {
                    _currentRule.EnableSpecialRules = false;
                    chkEnableSpecialRules.Checked = false;
                }

                // 更新规则
                _ruleManager.UpdateRule(_currentRule);
                Logger.Info($"从规则 {_currentRule.Id} 中删除了特殊规则条件");

                // 刷新特殊规则表格
                _specialRuleBindingList = new BindingList<SpecialRuleCondition>(_currentRule.SpecialRules);
                dgvSpecialRules.DataSource = _specialRuleBindingList;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "删除特殊规则条件失败");
                MessageBox.Show($"删除特殊规则条件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
            _currentRule = null;
            btnUpdateRule.Enabled = false;
            btnDeleteRule.Enabled = false;
            grpSpecialRules.Enabled = false;
            dgvSpecialRules.DataSource = null;
            dgvRules.ClearSelection();
        }

        // 标志位，用于防止在加载规则时触发CheckedChanged事件
        private bool _isLoading = false;

        private void chkRejectPrint_CheckedChanged(object sender, EventArgs e)
        {
            // 如果正在加载规则，不处理CheckedChanged事件
            if (_isLoading) return;

            if (_currentRule != null)
            {
                _currentRule.RejectPrint = chkRejectPrint.Checked;

                // 保存更改
                _ruleManager.UpdateRule(_currentRule);

                Logger.Debug($"拒绝打印状态已更改为: {chkRejectPrint.Checked} 并已保存");
            }
        }

        private void chkEnableSpecialRules_CheckedChanged(object sender, EventArgs e)
        {
            // 如果正在加载规则，不处理CheckedChanged事件
            if (_isLoading) return;

            if (_currentRule != null)
            {
                _currentRule.EnableSpecialRules = chkEnableSpecialRules.Checked;
                Logger.Debug($"启用特殊规则状态已更改为: {chkEnableSpecialRules.Checked}");

                // 如果禁用特殊规则，但仍有特殊规则条件，提示用户
                if (!chkEnableSpecialRules.Checked && _currentRule.SpecialRules.Count > 0)
                {
                    DialogResult result = MessageBox.Show(
                        "当前规则包含特殊规则条件，但您已禁用特殊规则。是否要清空特殊规则条件？",
                        "确认",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        _currentRule.SpecialRules.Clear();
                        _specialRuleBindingList = new BindingList<SpecialRuleCondition>(_currentRule.SpecialRules);
                        dgvSpecialRules.DataSource = _specialRuleBindingList;
                        Logger.Info("已清空特殊规则条件");
                    }
                }

                // 保存更改
                _ruleManager.UpdateRule(_currentRule);
                Logger.Debug($"启用特殊规则状态已保存");
            }
        }

        // 移除了表格单元格点击事件处理程序，因为表格已设置为只读

        private void chkSpecialRejectPrint_CheckedChanged(object sender, EventArgs e)
        {
            // 如果正在加载规则，不处理CheckedChanged事件
            if (_isLoading) return;

            // 检查是否有选中的特殊规则
            if (_currentRule != null && dgvSpecialRules.SelectedRows.Count > 0)
            {
                var selectedCondition = dgvSpecialRules.SelectedRows[0].DataBoundItem as SpecialRuleCondition;
                if (selectedCondition != null)
                {
                    // 更新特殊规则的拒绝打印状态
                    selectedCondition.RejectPrint = chkSpecialRejectPrint.Checked;

                    // 保存更改
                    _ruleManager.UpdateRule(_currentRule);

                    // 刷新特殊规则表格以显示更新后的状态
                    _specialRuleBindingList = new BindingList<SpecialRuleCondition>(_currentRule.SpecialRules);
                    dgvSpecialRules.DataSource = _specialRuleBindingList;

                    // 重新选择之前选中的行
                    if (dgvSpecialRules.Rows.Count > 0)
                    {
                        for (int i = 0; i < dgvSpecialRules.Rows.Count; i++)
                        {
                            var condition = dgvSpecialRules.Rows[i].DataBoundItem as SpecialRuleCondition;
                            if (condition == selectedCondition)
                            {
                                dgvSpecialRules.Rows[i].Selected = true;
                                break;
                            }
                        }
                    }

                    Logger.Debug($"特殊规则拒绝打印状态已更改为: {chkSpecialRejectPrint.Checked} 并已保存");
                }
            }
            else
            {
                Logger.Debug($"特殊规则拒绝打印状态已更改为: {chkSpecialRejectPrint.Checked}，但没有选中的特殊规则，未保存");
            }
        }

        /// <summary>
        /// 根据ID选择规则
        /// </summary>
        /// <param name="ruleId">要选择的规则ID</param>
        private void SelectRuleById(int ruleId)
        {
            // 查找规则在DataGridView中的索引
            for (int i = 0; i < dgvRules.Rows.Count; i++)
            {
                var rule = dgvRules.Rows[i].DataBoundItem as ProductRule;
                if (rule != null && rule.Id == ruleId)
                {
                    // 选择该行
                    dgvRules.ClearSelection();
                    dgvRules.Rows[i].Selected = true;

                    // 确保该行可见
                    dgvRules.FirstDisplayedScrollingRowIndex = i;

                    // 设置当前规则
                    _currentRule = rule;

                    Logger.Debug($"已重新选择规则，ID: {ruleId}");
                    return;
                }
            }

            Logger.Debug($"未找到ID为 {ruleId} 的规则");
        }

        /// <summary>
        /// 特殊规则表格选择变更事件处理程序
        /// </summary>
        private void dgvSpecialRules_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvSpecialRules.SelectedRows.Count > 0)
            {
                var selectedCondition = dgvSpecialRules.SelectedRows[0].DataBoundItem as SpecialRuleCondition;
                if (selectedCondition != null)
                {
                    // 设置加载标志，防止触发CheckedChanged事件
                    _isLoading = true;

                    // 更新特殊规则输入框
                    txtSpecialChickenHouse.Text = selectedCondition.ChickenHouse;
                    txtSpecialWeightLowerLimit.Text = selectedCondition.WeightLowerLimit.ToString();
                    txtSpecialWeightUpperLimit.Text = selectedCondition.WeightUpperLimit.ToString();
                    txtSpecialQRCode.Text = selectedCondition.QRCode;
                    chkSpecialRejectPrint.Checked = selectedCondition.RejectPrint;

                    // 重置加载标志
                    _isLoading = false;

                    Logger.Debug($"已选择特殊规则条件: 鸡舍={selectedCondition.ChickenHouse ?? "未指定"}, 重量范围=[{selectedCondition.WeightLowerLimit}-{selectedCondition.WeightUpperLimit}]");
                }
            }
        }
    }
}
