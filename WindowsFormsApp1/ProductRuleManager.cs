using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

namespace WindowsFormsApp1
{
    /// <summary>
    /// 产品规则管理器，用于管理产品规则的增删改查和持久化
    /// </summary>
    public class ProductRuleManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly string _filePath;
        private List<ProductRule> _rules;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="filePath">规则文件路径</param>
        public ProductRuleManager(string filePath = "product_rules.json")
        {
            _filePath = filePath;
            _rules = LoadRules();
        }

        /// <summary>
        /// 获取所有规则
        /// </summary>
        /// <returns>规则列表</returns>
        public List<ProductRule> GetAllRules()
        {
            return _rules;
        }

        /// <summary>
        /// 检查规则是否重复
        /// </summary>
        /// <param name="rule">要检查的规则</param>
        /// <param name="excludeId">排除的规则ID（用于更新时排除自身）</param>
        /// <returns>如果重复返回true，否则返回false</returns>
        public bool IsRuleDuplicate(ProductRule rule, int excludeId = 0)
        {
            // 检查是否有相同版面、鸡舍号、客户名和重量范围重叠的规则
            return _rules.Any(r =>
                r.Id != excludeId && // 排除自身
                r.Version == rule.Version && // 相同版面
                (string.IsNullOrEmpty(r.ChickenHouse) || string.IsNullOrEmpty(rule.ChickenHouse) || r.ChickenHouse == rule.ChickenHouse) && // 相同鸡舍号或任一为空
                (string.IsNullOrEmpty(r.CustomerName) || string.IsNullOrEmpty(rule.CustomerName) || r.CustomerName == rule.CustomerName) && // 相同客户名或任一为空
                // 重量范围重叠
                ((r.WeightLowerLimit <= rule.WeightLowerLimit && rule.WeightLowerLimit <= r.WeightUpperLimit) ||
                 (r.WeightLowerLimit <= rule.WeightUpperLimit && rule.WeightUpperLimit <= r.WeightUpperLimit) ||
                 (rule.WeightLowerLimit <= r.WeightLowerLimit && r.WeightUpperLimit <= rule.WeightUpperLimit))
            );
        }

        /// <summary>
        /// 检查特殊规则条件是否重复
        /// </summary>
        /// <param name="rule">所属的主规则</param>
        /// <param name="condition">要检查的特殊规则条件</param>
        /// <returns>如果重复返回true，否则返回false</returns>
        public bool IsSpecialRuleConditionDuplicate(ProductRule rule, SpecialRuleCondition condition)
        {
            if (rule == null || rule.SpecialRules == null)
            {
                return false;
            }

            // 检查是否有相同鸡舍号和重量范围重叠的特殊规则条件
            return rule.SpecialRules.Any(c =>
                c != condition && // 排除自身（引用比较）
                (string.IsNullOrEmpty(c.ChickenHouse) || string.IsNullOrEmpty(condition.ChickenHouse) || c.ChickenHouse == condition.ChickenHouse) && // 相同鸡舍号或任一为空
                // 重量范围重叠
                ((c.WeightLowerLimit <= condition.WeightLowerLimit && condition.WeightLowerLimit <= c.WeightUpperLimit) ||
                 (c.WeightLowerLimit <= condition.WeightUpperLimit && condition.WeightUpperLimit <= c.WeightUpperLimit) ||
                 (condition.WeightLowerLimit <= c.WeightLowerLimit && c.WeightUpperLimit <= condition.WeightUpperLimit))
            );
        }

        /// <summary>
        /// 添加规则
        /// </summary>
        /// <param name="rule">要添加的规则</param>
        /// <returns>添加后的规则ID，如果规则重复则返回-1</returns>
        public int AddRule(ProductRule rule)
        {
            // 检查规则是否重复
            if (IsRuleDuplicate(rule))
            {
                return -1; // 返回-1表示规则重复
            }

            // 生成新ID
            int newId = 1;
            if (_rules.Any())
            {
                newId = _rules.Max(r => r.Id) + 1;
            }

            rule.Id = newId;
            _rules.Add(rule);
            SaveRules();
            return newId;
        }

        /// <summary>
        /// 更新规则
        /// </summary>
        /// <param name="rule">要更新的规则</param>
        /// <returns>是否更新成功，如果规则重复则返回false</returns>
        public bool UpdateRule(ProductRule rule)
        {
            int index = _rules.FindIndex(r => r.Id == rule.Id);
            if (index >= 0)
            {
                // 检查规则是否重复，排除自身
                if (IsRuleDuplicate(rule, rule.Id))
                {
                    return false; // 规则重复
                }

                _rules[index] = rule;
                SaveRules();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 删除规则
        /// </summary>
        /// <param name="id">要删除的规则ID</param>
        /// <returns>是否删除成功</returns>
        public bool DeleteRule(int id)
        {
            int index = _rules.FindIndex(r => r.Id == id);
            if (index >= 0)
            {
                _rules.RemoveAt(index);
                SaveRules();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 查找匹配的规则
        /// </summary>
        /// <param name="version">版面信息</param>
        /// <param name="chickenHouse">鸡舍号，可能为null</param>
        /// <param name="customerName">客户名，可能为null</param>
        /// <param name="weight">重量</param>
        /// <returns>匹配的规则，如果没找到则返回null</returns>
        public ProductRule FindMatchingRule(string version, string chickenHouse, string customerName, double weight)
        {
            // 首先筛选版面匹配的规则
            var matchingRules = _rules.Where(r => r.Version == version).ToList();

            // 进一步筛选鸡舍号匹配的规则（如果提供了鸡舍号）
            if (!string.IsNullOrEmpty(chickenHouse))
            {
                // 首先尝试精确匹配鸡舍号
                var exactChickenHouseRules = matchingRules.Where(r => r.ChickenHouse == chickenHouse).ToList();
                if (exactChickenHouseRules.Any())
                {
                    matchingRules = exactChickenHouseRules;
                }
                else
                {
                    // 如果没有精确匹配，尝试匹配没有指定鸡舍号的规则
                    var nullChickenHouseRules = matchingRules.Where(r => string.IsNullOrEmpty(r.ChickenHouse)).ToList();
                    if (nullChickenHouseRules.Any())
                    {
                        matchingRules = nullChickenHouseRules;
                    }
                }
            }

            // 进一步筛选客户名匹配的规则（如果提供了客户名）
            if (!string.IsNullOrEmpty(customerName))
            {
                // 首先尝试精确匹配客户名
                var exactCustomerRules = matchingRules.Where(r => r.CustomerName == customerName).ToList();
                if (exactCustomerRules.Any())
                {
                    matchingRules = exactCustomerRules;
                }
                else
                {
                    // 如果没有精确匹配，尝试匹配没有指定客户名的规则
                    var nullCustomerRules = matchingRules.Where(r => r.CustomerName == null).ToList();
                    if (nullCustomerRules.Any())
                    {
                        matchingRules = nullCustomerRules;
                    }
                }
            }

            // 最后筛选重量在范围内的规则
            foreach (var rule in matchingRules)
            {
                // 检查是否有特殊规则需要处理
                if (rule.EnableSpecialRules && rule.SpecialRules != null && rule.SpecialRules.Any())
                {
                    // 对于启用了特殊规则的规则，直接处理特殊规则，不检查版面自身的重量范围
                    var specialRule = ProcessSpecialRules(rule, chickenHouse, weight);
                    if (specialRule != null)
                    {
                        return specialRule;
                    }

                    // 如果没有匹配的特殊规则，继续检查下一个规则
                    continue;
                }

                // 对于没有特殊规则的规则，检查版面自身的重量范围
                if (rule.WeightLowerLimit <= weight && weight <= rule.WeightUpperLimit)
                {
                    return rule;
                }
            }

            return null;
        }

        /// <summary>
        /// 处理特殊规则
        /// </summary>
        /// <param name="rule">原始规则</param>
        /// <param name="chickenHouse">鸡舍号</param>
        /// <param name="weight">重量</param>
        /// <returns>处理后的规则，如果没有匹配的特殊规则则返回null</returns>
        private ProductRule ProcessSpecialRules(ProductRule rule, string chickenHouse, double weight)
        {
            // 记录日志，帮助调试
            Logger.Debug($"处理特殊规则: 版面={rule.Version}, 鸡舍={chickenHouse}, 重量={weight}, 特殊规则数量={rule.SpecialRules.Count}");

            foreach (var condition in rule.SpecialRules)
            {
                // 记录每个特殊规则的详细信息
                Logger.Debug($"检查特殊规则: 鸡舍={condition.ChickenHouse}, 重量范围=[{condition.WeightLowerLimit}-{condition.WeightUpperLimit}], 二维码={condition.QRCode}");

                // 检查鸡舍是否匹配
                if (!string.IsNullOrEmpty(chickenHouse) && !string.IsNullOrEmpty(condition.ChickenHouse) &&
                    condition.ChickenHouse != chickenHouse)
                {
                    Logger.Debug($"鸡舍不匹配: {condition.ChickenHouse} != {chickenHouse}");
                    continue;
                }

                // 检查重量范围是否匹配
                if (condition.WeightLowerLimit <= weight && weight <= condition.WeightUpperLimit)
                {
                    Logger.Debug($"找到匹配的特殊规则: 重量 {weight} 在范围 [{condition.WeightLowerLimit}-{condition.WeightUpperLimit}] 内");

                    // 创建一个新规则，复制原规则的属性
                    var specialRule = new ProductRule
                    {
                        Id = rule.Id,
                        Version = rule.Version,
                        ProductName = rule.ProductName,
                        Specification = rule.Specification,
                        ChickenHouse = rule.ChickenHouse,
                        CustomerName = rule.CustomerName,
                        // 使用特殊规则的重量范围，而不是版面自身的重量范围
                        WeightLowerLimit = condition.WeightLowerLimit,
                        WeightUpperLimit = condition.WeightUpperLimit,
                        // 使用特殊规则的拒绝打印设置，如果特殊规则有设置的话
                        RejectPrint = condition.RejectPrint,
                        // 使用特殊规则的二维码
                        QRCode = condition.QRCode,
                        EnableSpecialRules = false,
                        SpecialRules = new List<SpecialRuleCondition>()
                    };

                    return specialRule;
                }
                else
                {
                    Logger.Debug($"重量不匹配: {weight} 不在范围 [{condition.WeightLowerLimit}-{condition.WeightUpperLimit}] 内");
                }
            }

            Logger.Debug("没有找到匹配的特殊规则");
            return null;
        }

        /// <summary>
        /// 从文件加载规则
        /// </summary>
        /// <returns>规则列表</returns>
        private List<ProductRule> LoadRules()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath, Encoding.UTF8);
                    var rules = JsonConvert.DeserializeObject<List<ProductRule>>(json);
                    Logger.Info($"成功从 {_filePath} 加载了 {rules.Count} 条规则");
                    return rules;
                }
                else
                {
                    Logger.Info($"规则文件 {_filePath} 不存在，将创建新文件");
                    return new List<ProductRule>();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"加载规则文件 {_filePath} 失败");
                return new List<ProductRule>();
            }
        }

        /// <summary>
        /// 保存规则到文件
        /// </summary>
        private void SaveRules()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_rules, Formatting.Indented);
                File.WriteAllText(_filePath, json, Encoding.UTF8);
                Logger.Info($"成功保存 {_rules.Count} 条规则到 {_filePath}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"保存规则到文件 {_filePath} 失败");
            }
        }
    }
}
