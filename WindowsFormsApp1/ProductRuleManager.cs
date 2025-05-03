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
            Logger.Debug($"开始检查规则是否重复: 版面={rule.Version}, 鸡舍={rule.ChickenHouse ?? "未指定"}, 客户名={rule.CustomerName ?? "未指定"}, 重量范围=[{rule.WeightLowerLimit}-{rule.WeightUpperLimit}], 排除ID={excludeId}");

            // 首先筛选出相同版面的规则，排除自身
            var sameVersionRules = _rules.Where(r =>
                r.Id != excludeId &&
                r.Version == rule.Version
            ).ToList();

            Logger.Debug($"找到相同版面的规则数量: {sameVersionRules.Count}");

            foreach (var existingRule in sameVersionRules)
            {
                // 检查鸡舍号是否匹配
                // 如果两个规则都指定了鸡舍号，则必须完全相同才匹配
                // 如果两个规则都未指定鸡舍号，则匹配
                // 如果一个规则指定了鸡舍号，另一个未指定，则不匹配
                bool chickenHouseMatch;
                if (string.IsNullOrEmpty(existingRule.ChickenHouse) && string.IsNullOrEmpty(rule.ChickenHouse))
                {
                    // 两者都没有指定鸡舍号，匹配
                    chickenHouseMatch = true;
                    Logger.Debug($"规则ID={existingRule.Id} 鸡舍号比较: 两个规则都未指定鸡舍号，匹配结果=true");
                }
                else if (!string.IsNullOrEmpty(existingRule.ChickenHouse) && !string.IsNullOrEmpty(rule.ChickenHouse))
                {
                    // 两者都指定了鸡舍号，必须完全相同
                    chickenHouseMatch = (existingRule.ChickenHouse == rule.ChickenHouse);
                    Logger.Debug($"规则ID={existingRule.Id} 鸡舍号比较: 现有规则鸡舍号={existingRule.ChickenHouse}, 新规则鸡舍号={rule.ChickenHouse}, 匹配结果={chickenHouseMatch}");
                }
                else
                {
                    // 一个规则指定了鸡舍号，另一个未指定，不匹配
                    chickenHouseMatch = false;
                    Logger.Debug($"规则ID={existingRule.Id} 鸡舍号比较: 现有规则鸡舍号={existingRule.ChickenHouse ?? "未指定"}, 新规则鸡舍号={rule.ChickenHouse ?? "未指定"}, 匹配结果=false (一个有鸡舍号，一个没有)");
                }

                if (!chickenHouseMatch)
                {
                    Logger.Debug($"规则ID={existingRule.Id} 鸡舍号不匹配，跳过");
                    continue;
                }

                // 检查客户名是否匹配
                // 如果两个规则都指定了客户名，则必须完全相同才匹配
                // 如果两个规则都未指定客户名，则匹配
                // 如果一个规则指定了客户名，另一个未指定，则不匹配
                bool customerNameMatch;
                if (string.IsNullOrEmpty(existingRule.CustomerName) && string.IsNullOrEmpty(rule.CustomerName))
                {
                    // 两者都没有指定客户名，匹配
                    customerNameMatch = true;
                    Logger.Debug($"规则ID={existingRule.Id} 客户名比较: 两个规则都未指定客户名，匹配结果=true");
                }
                else if (!string.IsNullOrEmpty(existingRule.CustomerName) && !string.IsNullOrEmpty(rule.CustomerName))
                {
                    // 两者都指定了客户名，必须完全相同
                    customerNameMatch = (existingRule.CustomerName == rule.CustomerName);
                    Logger.Debug($"规则ID={existingRule.Id} 客户名比较: 现有规则客户名={existingRule.CustomerName}, 新规则客户名={rule.CustomerName}, 匹配结果={customerNameMatch}");
                }
                else
                {
                    // 一个规则指定了客户名，另一个未指定，不匹配
                    customerNameMatch = false;
                    Logger.Debug($"规则ID={existingRule.Id} 客户名比较: 现有规则客户名={existingRule.CustomerName ?? "未指定"}, 新规则客户名={rule.CustomerName ?? "未指定"}, 匹配结果=false (一个有客户名，一个没有)");
                }

                if (!customerNameMatch)
                {
                    Logger.Debug($"规则ID={existingRule.Id} 客户名不匹配，跳过");
                    continue;
                }

                // 检查重量范围是否重叠
                bool weightRangeOverlap = false;

                // 情况1: 新规则的下限在现有规则的范围内
                if (existingRule.WeightLowerLimit <= rule.WeightLowerLimit &&
                    rule.WeightLowerLimit <= existingRule.WeightUpperLimit)
                {
                    weightRangeOverlap = true;
                    Logger.Debug($"规则ID={existingRule.Id} 重量范围重叠: 新规则下限 {rule.WeightLowerLimit} 在现有规则范围 [{existingRule.WeightLowerLimit}-{existingRule.WeightUpperLimit}] 内");
                }
                // 情况2: 新规则的上限在现有规则的范围内
                else if (existingRule.WeightLowerLimit <= rule.WeightUpperLimit &&
                         rule.WeightUpperLimit <= existingRule.WeightUpperLimit)
                {
                    weightRangeOverlap = true;
                    Logger.Debug($"规则ID={existingRule.Id} 重量范围重叠: 新规则上限 {rule.WeightUpperLimit} 在现有规则范围 [{existingRule.WeightLowerLimit}-{existingRule.WeightUpperLimit}] 内");
                }
                // 情况3: 新规则完全包含现有规则
                else if (rule.WeightLowerLimit <= existingRule.WeightLowerLimit &&
                         existingRule.WeightUpperLimit <= rule.WeightUpperLimit)
                {
                    weightRangeOverlap = true;
                    Logger.Debug($"规则ID={existingRule.Id} 重量范围重叠: 新规则范围 [{rule.WeightLowerLimit}-{rule.WeightUpperLimit}] 完全包含现有规则范围 [{existingRule.WeightLowerLimit}-{existingRule.WeightUpperLimit}]");
                }

                if (!weightRangeOverlap)
                {
                    Logger.Debug($"规则ID={existingRule.Id} 重量范围不重叠，跳过");
                    continue;
                }

                // 如果所有条件都匹配，则规则重复
                Logger.Info($"发现重复规则: ID={existingRule.Id}, 版面={existingRule.Version}, 鸡舍={existingRule.ChickenHouse ?? "未指定"}, 客户名={existingRule.CustomerName ?? "未指定"}, 重量范围=[{existingRule.WeightLowerLimit}-{existingRule.WeightUpperLimit}]");
                return true;
            }

            Logger.Debug("未发现重复规则");
            return false;
        }

        /// <summary>
        /// 检查特殊规则条件是否重复
        /// </summary>
        /// <param name="rule">所属的主规则</param>
        /// <param name="condition">要检查的特殊规则条件</param>
        /// <returns>如果重复返回true，否则返回false</returns>
        public bool IsSpecialRuleConditionDuplicate(ProductRule rule, SpecialRuleCondition condition)
        {
            Logger.Debug($"开始检查特殊规则条件是否重复: 鸡舍={condition.ChickenHouse ?? "未指定"}, 重量范围=[{condition.WeightLowerLimit}-{condition.WeightUpperLimit}]");

            if (rule == null || rule.SpecialRules == null)
            {
                Logger.Debug("主规则为空或没有特殊规则，不存在重复");
                return false;
            }

            Logger.Debug($"主规则ID={rule.Id}，特殊规则数量={rule.SpecialRules.Count}");

            int index = 0;
            foreach (var existingCondition in rule.SpecialRules)
            {
                index++;

                // 排除自身（引用比较）
                if (existingCondition == condition)
                {
                    Logger.Debug($"特殊规则 #{index} 是自身，跳过");
                    continue;
                }

                // 检查鸡舍号是否匹配
                // 如果两个条件都指定了鸡舍号，则必须完全相同才匹配
                // 如果两个条件都未指定鸡舍号，则匹配
                // 如果一个条件指定了鸡舍号，另一个未指定，则不匹配
                bool chickenHouseMatch;
                if (string.IsNullOrEmpty(existingCondition.ChickenHouse) && string.IsNullOrEmpty(condition.ChickenHouse))
                {
                    // 两者都没有指定鸡舍号，匹配
                    chickenHouseMatch = true;
                    Logger.Debug($"特殊规则 #{index} 鸡舍号比较: 两个条件都未指定鸡舍号，匹配结果=true");
                }
                else if (!string.IsNullOrEmpty(existingCondition.ChickenHouse) && !string.IsNullOrEmpty(condition.ChickenHouse))
                {
                    // 两者都指定了鸡舍号，必须完全相同
                    chickenHouseMatch = (existingCondition.ChickenHouse == condition.ChickenHouse);
                    Logger.Debug($"特殊规则 #{index} 鸡舍号比较: 现有条件鸡舍号={existingCondition.ChickenHouse}, 新条件鸡舍号={condition.ChickenHouse}, 匹配结果={chickenHouseMatch}");
                }
                else
                {
                    // 一个条件指定了鸡舍号，另一个未指定，不匹配
                    chickenHouseMatch = false;
                    Logger.Debug($"特殊规则 #{index} 鸡舍号比较: 现有条件鸡舍号={existingCondition.ChickenHouse ?? "未指定"}, 新条件鸡舍号={condition.ChickenHouse ?? "未指定"}, 匹配结果=false (一个有鸡舍号，一个没有)");
                }

                if (!chickenHouseMatch)
                {
                    Logger.Debug($"特殊规则 #{index} 鸡舍号不匹配，跳过");
                    continue;
                }

                // 检查重量范围是否重叠
                bool weightRangeOverlap = false;

                // 情况1: 新条件的下限在现有条件的范围内
                if (existingCondition.WeightLowerLimit <= condition.WeightLowerLimit &&
                    condition.WeightLowerLimit <= existingCondition.WeightUpperLimit)
                {
                    weightRangeOverlap = true;
                    Logger.Debug($"特殊规则 #{index} 重量范围重叠: 新条件下限 {condition.WeightLowerLimit} 在现有条件范围 [{existingCondition.WeightLowerLimit}-{existingCondition.WeightUpperLimit}] 内");
                }
                // 情况2: 新条件的上限在现有条件的范围内
                else if (existingCondition.WeightLowerLimit <= condition.WeightUpperLimit &&
                         condition.WeightUpperLimit <= existingCondition.WeightUpperLimit)
                {
                    weightRangeOverlap = true;
                    Logger.Debug($"特殊规则 #{index} 重量范围重叠: 新条件上限 {condition.WeightUpperLimit} 在现有条件范围 [{existingCondition.WeightLowerLimit}-{existingCondition.WeightUpperLimit}] 内");
                }
                // 情况3: 新条件完全包含现有条件
                else if (condition.WeightLowerLimit <= existingCondition.WeightLowerLimit &&
                         existingCondition.WeightUpperLimit <= condition.WeightUpperLimit)
                {
                    weightRangeOverlap = true;
                    Logger.Debug($"特殊规则 #{index} 重量范围重叠: 新条件范围 [{condition.WeightLowerLimit}-{condition.WeightUpperLimit}] 完全包含现有条件范围 [{existingCondition.WeightLowerLimit}-{existingCondition.WeightUpperLimit}]");
                }

                if (!weightRangeOverlap)
                {
                    Logger.Debug($"特殊规则 #{index} 重量范围不重叠，跳过");
                    continue;
                }

                // 如果所有条件都匹配，则特殊规则条件重复
                Logger.Info($"发现重复的特殊规则条件: 鸡舍={existingCondition.ChickenHouse ?? "未指定"}, 重量范围=[{existingCondition.WeightLowerLimit}-{existingCondition.WeightUpperLimit}]");
                return true;
            }

            Logger.Debug("未发现重复的特殊规则条件");
            return false;
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
            var startTime = DateTime.Now;
            Logger.Debug($"开始查找匹配规则: 版面={version}, 鸡舍={chickenHouse ?? "未指定"}, 客户名={customerName ?? "未指定"}, 重量={weight}");

            // 首先筛选版面匹配的规则
            var matchingRules = _rules.Where(r => r.Version == version).ToList();
            Logger.Debug($"按版面筛选后的规则数量: {matchingRules.Count}");

            if (matchingRules.Count == 0)
            {
                Logger.Debug($"未找到版面为 {version} 的规则");
                return null;
            }

            // 进一步筛选鸡舍号匹配的规则（如果提供了鸡舍号）
            if (!string.IsNullOrEmpty(chickenHouse))
            {
                Logger.Debug($"开始按鸡舍号 {chickenHouse} 筛选规则");

                // 首先尝试精确匹配鸡舍号
                var exactChickenHouseRules = matchingRules.Where(r => r.ChickenHouse == chickenHouse).ToList();
                if (exactChickenHouseRules.Any())
                {
                    Logger.Debug($"找到精确匹配鸡舍号 {chickenHouse} 的规则: {exactChickenHouseRules.Count} 条");
                    matchingRules = exactChickenHouseRules;
                }
                else
                {
                    // 如果没有精确匹配，尝试匹配没有指定鸡舍号的规则
                    var nullChickenHouseRules = matchingRules.Where(r => string.IsNullOrEmpty(r.ChickenHouse)).ToList();
                    if (nullChickenHouseRules.Any())
                    {
                        Logger.Debug($"未找到精确匹配鸡舍号的规则，使用通用鸡舍号规则: {nullChickenHouseRules.Count} 条");
                        matchingRules = nullChickenHouseRules;
                    }
                    else
                    {
                        Logger.Debug("既没有精确匹配鸡舍号的规则，也没有通用鸡舍号规则");
                    }
                }
            }
            else
            {
                Logger.Debug("未提供鸡舍号，跳过鸡舍号筛选");
            }

            // 进一步筛选客户名匹配的规则（如果提供了客户名）
            if (!string.IsNullOrEmpty(customerName))
            {
                Logger.Debug($"开始按客户名 {customerName} 筛选规则");

                // 首先尝试精确匹配客户名
                var exactCustomerRules = matchingRules.Where(r => r.CustomerName == customerName).ToList();
                if (exactCustomerRules.Any())
                {
                    Logger.Debug($"找到精确匹配客户名 {customerName} 的规则: {exactCustomerRules.Count} 条");
                    matchingRules = exactCustomerRules;
                }
                else
                {
                    // 如果没有精确匹配，尝试匹配没有指定客户名的规则
                    var nullCustomerRules = matchingRules.Where(r => string.IsNullOrEmpty(r.CustomerName)).ToList();
                    if (nullCustomerRules.Any())
                    {
                        Logger.Debug($"未找到精确匹配客户名的规则，使用通用客户名规则: {nullCustomerRules.Count} 条");
                        matchingRules = nullCustomerRules;
                    }
                    else
                    {
                        Logger.Debug("既没有精确匹配客户名的规则，也没有通用客户名规则");
                    }
                }
            }
            else
            {
                Logger.Debug("未提供客户名，跳过客户名筛选");
            }

            Logger.Debug($"筛选后剩余规则数量: {matchingRules.Count}");

            // 最后筛选重量在范围内的规则
            foreach (var rule in matchingRules)
            {
                Logger.Debug($"检查规则 ID={rule.Id}, 品名={rule.ProductName}, 规格={rule.Specification}, 重量范围=[{rule.WeightLowerLimit}-{rule.WeightUpperLimit}], 启用特殊规则={rule.EnableSpecialRules}");

                // 检查是否有特殊规则需要处理
                if (rule.EnableSpecialRules && rule.SpecialRules != null && rule.SpecialRules.Any())
                {
                    Logger.Debug($"规则 ID={rule.Id} 启用了特殊规则，共 {rule.SpecialRules.Count} 条特殊规则");

                    // 对于启用了特殊规则的规则，直接处理特殊规则，不检查版面自身的重量范围
                    var specialRule = ProcessSpecialRules(rule, chickenHouse, weight);
                    if (specialRule != null)
                    {
                        var duration = (DateTime.Now - startTime).TotalMilliseconds;
                        Logger.Info($"找到匹配的特殊规则: ID={specialRule.Id}, 品名={specialRule.ProductName}, 规格={specialRule.Specification}, 重量范围=[{specialRule.WeightLowerLimit}-{specialRule.WeightUpperLimit}], 耗时: {duration:F0}ms");
                        return specialRule;
                    }

                    // 如果没有匹配的特殊规则，继续检查下一个规则
                    Logger.Debug($"规则 ID={rule.Id} 的特殊规则没有匹配项，继续检查下一个规则");
                    continue;
                }

                // 对于没有特殊规则的规则，检查版面自身的重量范围
                if (rule.WeightLowerLimit <= weight && weight <= rule.WeightUpperLimit)
                {
                    var duration = (DateTime.Now - startTime).TotalMilliseconds;
                    Logger.Info($"找到匹配的规则: ID={rule.Id}, 品名={rule.ProductName}, 规格={rule.Specification}, 重量范围=[{rule.WeightLowerLimit}-{rule.WeightUpperLimit}], 耗时: {duration:F0}ms");
                    return rule;
                }
                else
                {
                    Logger.Debug($"规则 ID={rule.Id} 的重量范围 [{rule.WeightLowerLimit}-{rule.WeightUpperLimit}] 不匹配当前重量 {weight}");
                }
            }

            var totalDuration = (DateTime.Now - startTime).TotalMilliseconds;
            Logger.Info($"未找到匹配的规则，查找耗时: {totalDuration:F0}ms");
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
            var startTime = DateTime.Now;
            // 记录日志，帮助调试
            Logger.Debug($"开始处理规则 ID={rule.Id} 的特殊规则: 版面={rule.Version}, 鸡舍={chickenHouse ?? "未指定"}, 重量={weight}, 特殊规则数量={rule.SpecialRules.Count}");

            int index = 0;
            foreach (var condition in rule.SpecialRules)
            {
                index++;
                // 记录每个特殊规则的详细信息
                Logger.Debug($"检查特殊规则 #{index}: 鸡舍={condition.ChickenHouse ?? "未指定"}, 重量范围=[{condition.WeightLowerLimit}-{condition.WeightUpperLimit}], 二维码={condition.QRCode}, 拒绝打印={condition.RejectPrint}");

                // 检查鸡舍是否匹配
                if (!string.IsNullOrEmpty(chickenHouse) && !string.IsNullOrEmpty(condition.ChickenHouse) &&
                    condition.ChickenHouse != chickenHouse)
                {
                    Logger.Debug($"特殊规则 #{index} 鸡舍不匹配: 规则鸡舍={condition.ChickenHouse}, 当前鸡舍={chickenHouse}");
                    continue;
                }
                else
                {
                    if (string.IsNullOrEmpty(condition.ChickenHouse))
                    {
                        Logger.Debug($"特殊规则 #{index} 未指定鸡舍，适用于所有鸡舍");
                    }
                    else if (string.IsNullOrEmpty(chickenHouse))
                    {
                        Logger.Debug($"特殊规则 #{index} 指定了鸡舍 {condition.ChickenHouse}，但当前未提供鸡舍信息");
                    }
                    else
                    {
                        Logger.Debug($"特殊规则 #{index} 鸡舍匹配: {condition.ChickenHouse}");
                    }
                }

                // 检查重量范围是否匹配
                if (condition.WeightLowerLimit <= weight && weight <= condition.WeightUpperLimit)
                {
                    Logger.Debug($"特殊规则 #{index} 重量匹配: 当前重量 {weight} 在范围 [{condition.WeightLowerLimit}-{condition.WeightUpperLimit}] 内");

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

                    var duration = (DateTime.Now - startTime).TotalMilliseconds;
                    Logger.Debug($"特殊规则处理完成，找到匹配的特殊规则 #{index}，耗时: {duration:F0}ms");

                    if (condition.RejectPrint)
                    {
                        Logger.Info($"特殊规则 #{index} 设置为拒绝打印: 版面={rule.Version}, 鸡舍={chickenHouse ?? "未指定"}, 重量={weight}");
                    }

                    return specialRule;
                }
                else
                {
                    Logger.Debug($"特殊规则 #{index} 重量不匹配: 当前重量 {weight} 不在范围 [{condition.WeightLowerLimit}-{condition.WeightUpperLimit}] 内");
                }
            }

            var totalDuration = (DateTime.Now - startTime).TotalMilliseconds;
            Logger.Debug($"特殊规则处理完成，没有找到匹配的特殊规则，耗时: {totalDuration:F0}ms");
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
