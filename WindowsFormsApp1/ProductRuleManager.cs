using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace WindowsFormsApp1
{
    /// <summary>
    /// 规则匹配结果类，用于存储规则匹配的结果和失败原因
    /// </summary>
    public class RuleMatchingResult
    {
        /// <summary>
        /// 匹配的规则，如果没有匹配则为null
        /// </summary>
        public ProductRule MatchedRule { get; set; }

        /// <summary>
        /// 匹配失败的原因，如果匹配成功则为null
        /// </summary>
        public string FailureReason { get; set; }

        /// <summary>
        /// 匹配失败的阶段
        /// </summary>
        public MatchingStage FailureStage { get; set; }

        /// <summary>
        /// 可用的选项列表（如可用的鸡舍号、客户名或重量范围）
        /// </summary>
        public List<string> AvailableOptions { get; set; } = new List<string>();

        /// <summary>
        /// 是否匹配成功
        /// </summary>
        public bool IsSuccess => MatchedRule != null;
    }

    /// <summary>
    /// 匹配失败的阶段枚举
    /// </summary>
    public enum MatchingStage
    {
        /// <summary>
        /// 版面匹配阶段
        /// </summary>
        Version,

        /// <summary>
        /// 允许打印状态匹配阶段
        /// </summary>
        AllowPrint,

        /// <summary>
        /// 鸡舍号匹配阶段
        /// </summary>
        ChickenHouse,

        /// <summary>
        /// 客户名匹配阶段
        /// </summary>
        CustomerName,

        /// <summary>
        /// 重量匹配阶段
        /// </summary>
        Weight
    }
    /// <summary>
    /// 自定义 JSON 转换器，用于处理旧的 RejectPrint 属性
    /// </summary>
    public class ProductRuleConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ProductRule);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            ProductRule rule = new ProductRule();

            // 设置基本属性
            rule.Id = jo["Id"].Value<int>();
            rule.Version = jo["Version"].Value<string>();
            rule.ProductName = jo["ProductName"].Value<string>();
            rule.Specification = jo["Specification"].Value<string>();
            rule.ChickenHouse = jo["ChickenHouse"]?.Value<string>();
            rule.CustomerName = jo["CustomerName"]?.Value<string>();
            rule.WeightLowerLimit = jo["WeightLowerLimit"].Value<double>();
            rule.WeightUpperLimit = jo["WeightUpperLimit"].Value<double>();
            rule.QRCode = jo["QRCode"]?.Value<string>();
            rule.EnableSpecialRules = jo["EnableSpecialRules"]?.Value<bool>() ?? false;

            // 处理 RejectPrint 到 AllowPrint 的转换
            // 无论如何，默认都设置为不允许打印
            rule.AllowPrint = false;

            // 只有当明确指定 AllowPrint 为 true 时才允许打印
            if (jo["AllowPrint"] != null && jo["AllowPrint"].Value<bool>() == true)
            {
                rule.AllowPrint = true;
            }
            // 如果使用的是旧的 RejectPrint 属性，只有当它明确为 false 时才允许打印
            else if (jo["RejectPrint"] != null && jo["RejectPrint"].Value<bool>() == false)
            {
                rule.AllowPrint = false; // 强制设置为 false，确保默认不允许打印
            }

            // 处理特殊规则
            if (jo["SpecialRules"] != null)
            {
                rule.SpecialRules = new List<SpecialRuleCondition>();
                foreach (JObject specialRuleJo in jo["SpecialRules"])
                {
                    SpecialRuleCondition specialRule = new SpecialRuleCondition();
                    specialRule.ChickenHouse = specialRuleJo["ChickenHouse"]?.Value<string>();
                    specialRule.WeightLowerLimit = specialRuleJo["WeightLowerLimit"].Value<double>();
                    specialRule.WeightUpperLimit = specialRuleJo["WeightUpperLimit"].Value<double>();
                    specialRule.QRCode = specialRuleJo["QRCode"]?.Value<string>();

                    // 处理特殊规则的 RejectPrint 到 AllowPrint 的转换
                    // 无论如何，默认都设置为不允许打印
                    specialRule.AllowPrint = false;

                    // 只有当明确指定 AllowPrint 为 true 时才允许打印
                    if (specialRuleJo["AllowPrint"] != null && specialRuleJo["AllowPrint"].Value<bool>() == true)
                    {
                        specialRule.AllowPrint = true;
                    }
                    // 如果使用的是旧的 RejectPrint 属性，只有当它明确为 false 时才允许打印
                    else if (specialRuleJo["RejectPrint"] != null && specialRuleJo["RejectPrint"].Value<bool>() == false)
                    {
                        specialRule.AllowPrint = false; // 强制设置为 false，确保默认不允许打印
                    }

                    rule.SpecialRules.Add(specialRule);
                }
            }

            return rule;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // 使用默认序列化
            serializer.Serialize(writer, value);
        }

        public override bool CanWrite
        {
            get { return false; }
        }
    }

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
        /// <param name="resetAllowPrintStatus">是否重置所有规则的允许打印状态为不允许打印</param>
        public ProductRuleManager(string filePath = "product_rules.json", bool resetAllowPrintStatus = false)
        {
            _filePath = filePath;
            _rules = LoadRules();

            // 只有在指定需要重置时才重置所有规则的允许打印状态
            if (resetAllowPrintStatus)
            {
                SetAllRulesToDisallowPrint();
                Logger.Info("初始化时重置所有规则的允许打印状态为不允许打印");
            }
            else
            {
                Logger.Info("初始化时保留所有规则的允许打印状态");
            }
        }

        /// <summary>
        /// 将所有规则设置为不允许打印
        /// </summary>
        private void SetAllRulesToDisallowPrint()
        {
            int changedCount = 0;

            // 设置所有主规则为不允许打印
            foreach (var rule in _rules)
            {
                if (rule.AllowPrint)
                {
                    rule.AllowPrint = false;
                    changedCount++;
                }

                // 设置所有特殊规则为不允许打印
                if (rule.SpecialRules != null)
                {
                    foreach (var specialRule in rule.SpecialRules)
                    {
                        if (specialRule.AllowPrint)
                        {
                            specialRule.AllowPrint = false;
                            changedCount++;
                        }
                    }
                }
            }

            if (changedCount > 0)
            {
                Logger.Info($"程序启动时，已将 {changedCount} 个规则的允许打印状态重置为不允许打印");
                // 保存更改
                SaveRules();
            }
            else
            {
                Logger.Info("程序启动时，所有规则已经是不允许打印状态，无需重置");
            }
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
                if (!IsChickenHouseMatch(existingRule, rule))
                {
                    Logger.Debug($"规则ID={existingRule.Id} 鸡舍号不匹配，跳过");
                    continue;
                }

                // 检查客户名是否匹配
                if (!IsCustomerNameMatch(existingRule, rule))
                {
                    Logger.Debug($"规则ID={existingRule.Id} 客户名不匹配，跳过");
                    continue;
                }

                // 检查重量范围是否重叠
                if (!IsWeightRangeOverlap(existingRule, rule))
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
        /// 检查两个规则的鸡舍号是否匹配
        /// </summary>
        /// <param name="rule1">规则1</param>
        /// <param name="rule2">规则2</param>
        /// <returns>如果匹配返回true，否则返回false</returns>
        private bool IsChickenHouseMatch(ProductRule rule1, ProductRule rule2)
        {
            // 如果两个规则都指定了鸡舍号，则必须完全相同才匹配
            // 如果两个规则都未指定鸡舍号，则匹配
            // 如果一个规则指定了鸡舍号，另一个未指定，则不匹配
            bool chickenHouseMatch;
            if (string.IsNullOrEmpty(rule1.ChickenHouse) && string.IsNullOrEmpty(rule2.ChickenHouse))
            {
                // 两者都没有指定鸡舍号，匹配
                chickenHouseMatch = true;
                Logger.Debug($"规则ID={rule1.Id} 鸡舍号比较: 两个规则都未指定鸡舍号，匹配结果=true");
            }
            else if (!string.IsNullOrEmpty(rule1.ChickenHouse) && !string.IsNullOrEmpty(rule2.ChickenHouse))
            {
                // 两者都指定了鸡舍号，必须完全相同
                chickenHouseMatch = (rule1.ChickenHouse == rule2.ChickenHouse);
                Logger.Debug($"规则ID={rule1.Id} 鸡舍号比较: 规则1鸡舍号={rule1.ChickenHouse}, 规则2鸡舍号={rule2.ChickenHouse}, 匹配结果={chickenHouseMatch}");
            }
            else
            {
                // 一个规则指定了鸡舍号，另一个未指定，不匹配
                chickenHouseMatch = false;
                Logger.Debug($"规则ID={rule1.Id} 鸡舍号比较: 规则1鸡舍号={rule1.ChickenHouse ?? "未指定"}, 规则2鸡舍号={rule2.ChickenHouse ?? "未指定"}, 匹配结果=false (一个有鸡舍号，一个没有)");
            }

            return chickenHouseMatch;
        }

        /// <summary>
        /// 检查两个规则的客户名是否匹配
        /// </summary>
        /// <param name="rule1">规则1</param>
        /// <param name="rule2">规则2</param>
        /// <returns>如果匹配返回true，否则返回false</returns>
        private bool IsCustomerNameMatch(ProductRule rule1, ProductRule rule2)
        {
            // 如果两个规则都指定了客户名，则必须完全相同才匹配
            // 如果两个规则都未指定客户名，则匹配
            // 如果一个规则指定了客户名，另一个未指定，则不匹配
            bool customerNameMatch;

            // 清理客户名，删除开头或结尾的空格、回车、换行等不可见字符
            string customerName1 = rule1.CustomerName?.Trim();
            string customerName2 = rule2.CustomerName?.Trim();

            if (string.IsNullOrEmpty(customerName1) && string.IsNullOrEmpty(customerName2))
            {
                // 两者都没有指定客户名，匹配
                customerNameMatch = true;
                Logger.Debug($"规则ID={rule1.Id} 客户名比较: 两个规则都未指定客户名，匹配结果=true");
            }
            else if (!string.IsNullOrEmpty(customerName1) && !string.IsNullOrEmpty(customerName2))
            {
                // 两者都指定了客户名，必须完全相同
                customerNameMatch = (customerName1 == customerName2);
                Logger.Debug($"规则ID={rule1.Id} 客户名比较: 规则1客户名={customerName1}, 规则2客户名={customerName2}, 匹配结果={customerNameMatch}");
            }
            else
            {
                // 一个规则指定了客户名，另一个未指定，不匹配
                customerNameMatch = false;
                Logger.Debug($"规则ID={rule1.Id} 客户名比较: 规则1客户名={customerName1 ?? "未指定"}, 规则2客户名={customerName2 ?? "未指定"}, 匹配结果=false (一个有客户名，一个没有)");
            }

            return customerNameMatch;
        }

        /// <summary>
        /// 检查两个规则的重量范围是否重叠
        /// </summary>
        /// <param name="rule1">规则1</param>
        /// <param name="rule2">规则2</param>
        /// <returns>如果重叠返回true，否则返回false</returns>
        private bool IsWeightRangeOverlap(ProductRule rule1, ProductRule rule2)
        {
            bool weightRangeOverlap = false;

            // 情况1: 规则2的下限在规则1的范围内
            if (rule1.WeightLowerLimit <= rule2.WeightLowerLimit &&
                rule2.WeightLowerLimit <= rule1.WeightUpperLimit)
            {
                weightRangeOverlap = true;
                Logger.Debug($"规则ID={rule1.Id} 重量范围重叠: 规则2下限 {rule2.WeightLowerLimit} 在规则1范围 [{rule1.WeightLowerLimit}-{rule1.WeightUpperLimit}] 内");
            }
            // 情况2: 规则2的上限在规则1的范围内
            else if (rule1.WeightLowerLimit <= rule2.WeightUpperLimit &&
                     rule2.WeightUpperLimit <= rule1.WeightUpperLimit)
            {
                weightRangeOverlap = true;
                Logger.Debug($"规则ID={rule1.Id} 重量范围重叠: 规则2上限 {rule2.WeightUpperLimit} 在规则1范围 [{rule1.WeightLowerLimit}-{rule1.WeightUpperLimit}] 内");
            }
            // 情况3: 规则2完全包含规则1
            else if (rule2.WeightLowerLimit <= rule1.WeightLowerLimit &&
                     rule1.WeightUpperLimit <= rule2.WeightUpperLimit)
            {
                weightRangeOverlap = true;
                Logger.Debug($"规则ID={rule1.Id} 重量范围重叠: 规则2范围 [{rule2.WeightLowerLimit}-{rule2.WeightUpperLimit}] 完全包含规则1范围 [{rule1.WeightLowerLimit}-{rule1.WeightUpperLimit}]");
            }

            return weightRangeOverlap;
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
                if (!IsSpecialRuleChickenHouseMatch(existingCondition, condition, index))
                {
                    Logger.Debug($"特殊规则 #{index} 鸡舍号不匹配，跳过");
                    continue;
                }

                // 检查重量范围是否重叠
                if (!IsSpecialRuleWeightRangeOverlap(existingCondition, condition, index))
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
        /// 检查两个特殊规则条件的鸡舍号是否匹配
        /// </summary>
        /// <param name="condition1">条件1</param>
        /// <param name="condition2">条件2</param>
        /// <param name="index">条件索引，用于日志记录</param>
        /// <returns>如果匹配返回true，否则返回false</returns>
        private bool IsSpecialRuleChickenHouseMatch(SpecialRuleCondition condition1, SpecialRuleCondition condition2, int index)
        {
            // 如果两个条件都指定了鸡舍号，则必须完全相同才匹配
            // 如果两个条件都未指定鸡舍号，则匹配
            // 如果一个条件指定了鸡舍号，另一个未指定，则不匹配
            bool chickenHouseMatch;
            if (string.IsNullOrEmpty(condition1.ChickenHouse) && string.IsNullOrEmpty(condition2.ChickenHouse))
            {
                // 两者都没有指定鸡舍号，匹配
                chickenHouseMatch = true;
                Logger.Debug($"特殊规则 #{index} 鸡舍号比较: 两个条件都未指定鸡舍号，匹配结果=true");
            }
            else if (!string.IsNullOrEmpty(condition1.ChickenHouse) && !string.IsNullOrEmpty(condition2.ChickenHouse))
            {
                // 两者都指定了鸡舍号，必须完全相同
                chickenHouseMatch = (condition1.ChickenHouse == condition2.ChickenHouse);
                Logger.Debug($"特殊规则 #{index} 鸡舍号比较: 条件1鸡舍号={condition1.ChickenHouse}, 条件2鸡舍号={condition2.ChickenHouse}, 匹配结果={chickenHouseMatch}");
            }
            else
            {
                // 一个条件指定了鸡舍号，另一个未指定，不匹配
                chickenHouseMatch = false;
                Logger.Debug($"特殊规则 #{index} 鸡舍号比较: 条件1鸡舍号={condition1.ChickenHouse ?? "未指定"}, 条件2鸡舍号={condition2.ChickenHouse ?? "未指定"}, 匹配结果=false (一个有鸡舍号，一个没有)");
            }

            return chickenHouseMatch;
        }

        /// <summary>
        /// 检查两个特殊规则条件的重量范围是否重叠
        /// </summary>
        /// <param name="condition1">条件1</param>
        /// <param name="condition2">条件2</param>
        /// <param name="index">条件索引，用于日志记录</param>
        /// <returns>如果重叠返回true，否则返回false</returns>
        private bool IsSpecialRuleWeightRangeOverlap(SpecialRuleCondition condition1, SpecialRuleCondition condition2, int index)
        {
            bool weightRangeOverlap = false;

            // 情况1: 条件2的下限在条件1的范围内
            if (condition1.WeightLowerLimit <= condition2.WeightLowerLimit &&
                condition2.WeightLowerLimit <= condition1.WeightUpperLimit)
            {
                weightRangeOverlap = true;
                Logger.Debug($"特殊规则 #{index} 重量范围重叠: 条件2下限 {condition2.WeightLowerLimit} 在条件1范围 [{condition1.WeightLowerLimit}-{condition1.WeightUpperLimit}] 内");
            }
            // 情况2: 条件2的上限在条件1的范围内
            else if (condition1.WeightLowerLimit <= condition2.WeightUpperLimit &&
                     condition2.WeightUpperLimit <= condition1.WeightUpperLimit)
            {
                weightRangeOverlap = true;
                Logger.Debug($"特殊规则 #{index} 重量范围重叠: 条件2上限 {condition2.WeightUpperLimit} 在条件1范围 [{condition1.WeightLowerLimit}-{condition1.WeightUpperLimit}] 内");
            }
            // 情况3: 条件2完全包含条件1
            else if (condition2.WeightLowerLimit <= condition1.WeightLowerLimit &&
                     condition1.WeightUpperLimit <= condition2.WeightUpperLimit)
            {
                weightRangeOverlap = true;
                Logger.Debug($"特殊规则 #{index} 重量范围重叠: 条件2范围 [{condition2.WeightLowerLimit}-{condition2.WeightUpperLimit}] 完全包含条件1范围 [{condition1.WeightLowerLimit}-{condition1.WeightUpperLimit}]");
            }

            return weightRangeOverlap;
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
        /// <returns>规则匹配结果，包含匹配的规则或失败原因</returns>
        public RuleMatchingResult FindMatchingRule(string version, string chickenHouse, string customerName, double weight)
        {
            var startTime = DateTime.Now;
            Logger.Info($"开始查找匹配规则: 版面={version}, 鸡舍={chickenHouse ?? "未指定"}, 客户名={customerName ?? "未指定"}, 重量={weight}");

            var result = new RuleMatchingResult();

            // 按版面筛选规则
            var matchingRules = FilterRulesByVersion(version);
            if (matchingRules.Count == 0)
            {
                return CreateVersionFailureResult(version);
            }

            // 按允许打印状态筛选规则
            matchingRules = FilterRulesByAllowPrint(matchingRules);
            if (matchingRules.Count == 0)
            {
                return CreateAllowPrintFailureResult();
            }

            // 优化：如果只有一个简单规则，直接判断重量
            var simpleRule = CheckForSingleSimpleRule(matchingRules, weight);
            if (simpleRule != null)
            {
                return CreateSuccessResult(simpleRule, startTime);
            }

            // 按鸡舍号筛选规则
            var chickenHouseResult = FilterRulesByChickenHouseWithResult(matchingRules, chickenHouse);
            if (chickenHouseResult.Count == 0)
            {
                return CreateChickenHouseFailureResult(matchingRules, chickenHouse);
            }
            matchingRules = chickenHouseResult;

            // 按客户名筛选规则
            var customerNameResult = FilterRulesByCustomerNameWithResult(matchingRules, customerName);
            if (customerNameResult.Count == 0)
            {
                return CreateCustomerNameFailureResult(matchingRules, customerName);
            }
            matchingRules = customerNameResult;

            // 按重量筛选规则，找出最终匹配的规则
            var finalRule = FindRuleMatchingWeight(matchingRules, chickenHouse, weight);

            var totalMatchDuration = (DateTime.Now - startTime).TotalMilliseconds;
            if (finalRule != null)
            {
                result.MatchedRule = finalRule;
                Logger.Info($"找到匹配的规则: ID={finalRule.Id}, 品名={finalRule.ProductName}, 规格={finalRule.Specification}, 重量范围=[{finalRule.WeightLowerLimit}-{finalRule.WeightUpperLimit}], 耗时: {totalMatchDuration:F0}ms");
                return result;
            }
            else
            {
                Logger.Info($"未找到匹配的规则，查找耗时: {totalMatchDuration:F0}ms");
                return CreateWeightFailureResult(matchingRules, weight);
            }
        }

        /// <summary>
        /// 创建版面匹配失败的结果
        /// </summary>
        /// <param name="version">版面信息</param>
        /// <returns>匹配失败的结果</returns>
        private RuleMatchingResult CreateVersionFailureResult(string version)
        {
            var result = new RuleMatchingResult
            {
                FailureStage = MatchingStage.Version,
                FailureReason = $"系统中没有品类为 {version} 的规则，请先添加相关规则"
            };
            return result;
        }

        /// <summary>
        /// 创建允许打印状态匹配失败的结果
        /// </summary>
        /// <returns>匹配失败的结果</returns>
        private RuleMatchingResult CreateAllowPrintFailureResult()
        {
            var result = new RuleMatchingResult
            {
                FailureStage = MatchingStage.AllowPrint,
                FailureReason = $"未找到允许打印的规则，所有规则都设置为不允许打印"
            };
            return result;
        }

        /// <summary>
        /// 创建匹配成功的结果
        /// </summary>
        /// <param name="rule">匹配的规则</param>
        /// <param name="startTime">开始时间，用于计算耗时</param>
        /// <returns>匹配成功的结果</returns>
        private RuleMatchingResult CreateSuccessResult(ProductRule rule, DateTime startTime)
        {
            var result = new RuleMatchingResult
            {
                MatchedRule = rule
            };
            var totalDuration = (DateTime.Now - startTime).TotalMilliseconds;
            Logger.Info($"找到匹配的规则: ID={rule.Id}, 品名={rule.ProductName}, 规格={rule.Specification}, 重量范围=[{rule.WeightLowerLimit}-{rule.WeightUpperLimit}], 耗时: {totalDuration:F0}ms");
            return result;
        }

        /// <summary>
        /// 创建鸡舍号匹配失败的结果
        /// </summary>
        /// <param name="rules">规则列表</param>
        /// <param name="chickenHouse">鸡舍号</param>
        /// <returns>匹配失败的结果</returns>
        private RuleMatchingResult CreateChickenHouseFailureResult(List<ProductRule> rules, string chickenHouse)
        {
            var result = new RuleMatchingResult
            {
                FailureStage = MatchingStage.ChickenHouse
            };

            // 特殊处理：如果鸡舍号是字符串"null"，当做不需要匹配鸡舍号处理
            if (chickenHouse == "null")
            {
                chickenHouse = null;
            }

            if (!string.IsNullOrEmpty(chickenHouse))
            {
                var availableChickenHouses = rules
                    .Where(r => !string.IsNullOrEmpty(r.ChickenHouse))
                    .Select(r => r.ChickenHouse)
                    .Distinct()
                    .ToList();

                if (availableChickenHouses.Any())
                {
                    result.FailureReason = $"没有鸡舍号为 {chickenHouse} 的规则，当前品类有以下鸡舍号的规则: {string.Join(", ", availableChickenHouses)}";
                    result.AvailableOptions = availableChickenHouses;
                }
                else
                {
                    result.FailureReason = $"没有鸡舍号为 {chickenHouse} 的规则，也没有通用鸡舍号规则";
                }
            }
            else
            {
                var availableChickenHouses = rules
                    .Where(r => !string.IsNullOrEmpty(r.ChickenHouse))
                    .Select(r => r.ChickenHouse)
                    .Distinct()
                    .ToList();

                if (availableChickenHouses.Any())
                {
                    result.FailureReason = $"没有通用鸡舍号规则，需要指定以下鸡舍号之一: {string.Join(", ", availableChickenHouses)}";
                    result.AvailableOptions = availableChickenHouses;
                }
                else
                {
                    result.FailureReason = "没有通用鸡舍号规则，所有规则都需要指定鸡舍号";
                }
            }

            return result;
        }

        /// <summary>
        /// 创建客户名匹配失败的结果
        /// </summary>
        /// <param name="rules">规则列表</param>
        /// <param name="customerName">客户名</param>
        /// <returns>匹配失败的结果</returns>
        private RuleMatchingResult CreateCustomerNameFailureResult(List<ProductRule> rules, string customerName)
        {
            var result = new RuleMatchingResult
            {
                FailureStage = MatchingStage.CustomerName
            };

            if (!string.IsNullOrEmpty(customerName))
            {
                string trimmedCustomerName = customerName.Trim();
                var availableCustomerNames = rules
                    .Where(r => !string.IsNullOrEmpty(r.CustomerName))
                    .Select(r => r.CustomerName.Trim())
                    .Distinct()
                    .ToList();

                if (availableCustomerNames.Any())
                {
                    result.FailureReason = $"没有客户名为 {trimmedCustomerName} 的规则，当前条件下有以下客户名的规则: {string.Join(", ", availableCustomerNames)}";
                    result.AvailableOptions = availableCustomerNames;
                }
                else
                {
                    result.FailureReason = $"没有客户名为 {trimmedCustomerName} 的规则，也没有通用客户名规则";
                }
            }
            else
            {
                var availableCustomerNames = rules
                    .Where(r => !string.IsNullOrEmpty(r.CustomerName))
                    .Select(r => r.CustomerName.Trim())
                    .Distinct()
                    .ToList();

                if (availableCustomerNames.Any())
                {
                    result.FailureReason = $"没有通用客户名规则，当前条件下需要指定以下客户名之一: {string.Join(", ", availableCustomerNames)}";
                    result.AvailableOptions = availableCustomerNames;
                }
                else
                {
                    result.FailureReason = "没有通用客户名规则，所有规则都需要指定客户名";
                }
            }

            return result;
        }

        /// <summary>
        /// 创建重量匹配失败的结果
        /// </summary>
        /// <param name="rules">规则列表</param>
        /// <param name="weight">重量</param>
        /// <returns>匹配失败的结果</returns>
        private RuleMatchingResult CreateWeightFailureResult(List<ProductRule> rules, double weight)
        {
            var result = new RuleMatchingResult
            {
                FailureStage = MatchingStage.Weight
            };

            var availableWeightRanges = rules
                .Select(r => $"[{r.WeightLowerLimit}-{r.WeightUpperLimit}]")
                .Distinct()
                .ToList();

            if (availableWeightRanges.Any())
            {
                result.FailureReason = $"重量 {weight} 不在任何规则的范围内，当前条件下有以下重量范围的规则: {string.Join(", ", availableWeightRanges)}";
                result.AvailableOptions = availableWeightRanges;
            }
            else
            {
                result.FailureReason = $"没有找到适用的重量范围规则";
            }

            return result;
        }

        /// <summary>
        /// 获取匹配的规则（兼容旧版本）
        /// </summary>
        /// <param name="version">版面信息</param>
        /// <param name="chickenHouse">鸡舍号，可能为null</param>
        /// <param name="customerName">客户名，可能为null</param>
        /// <param name="weight">重量</param>
        /// <returns>匹配的规则，如果没找到则返回null</returns>
        public ProductRule GetMatchingRule(string version, string chickenHouse, string customerName, double weight)
        {
            var result = FindMatchingRule(version, chickenHouse, customerName, weight);
            return result.MatchedRule;
        }

        /// <summary>
        /// 按版面筛选规则
        /// </summary>
        /// <param name="version">版面信息</param>
        /// <returns>筛选后的规则列表</returns>
        private List<ProductRule> FilterRulesByVersion(string version)
        {
            var matchingRules = _rules.Where(r => r.Version == version).ToList();
            Logger.Info($"按版面筛选后的规则数量: {matchingRules.Count}");

            if (matchingRules.Count == 0)
            {
                Logger.Info($"【匹配失败】未找到版面为 {version} 的规则，请检查规则配置");
            }

            return matchingRules;
        }

        /// <summary>
        /// 按允许打印状态筛选规则
        /// </summary>
        /// <param name="rules">要筛选的规则列表</param>
        /// <returns>筛选后的规则列表</returns>
        private List<ProductRule> FilterRulesByAllowPrint(List<ProductRule> rules)
        {
            var originalCount = rules.Count;
            var matchingRules = rules.Where(r => r.AllowPrint).ToList();
            Logger.Info($"按允许打印筛选后的规则数量: {matchingRules.Count}/{originalCount}");

            if (matchingRules.Count == 0)
            {
                Logger.Info($"【匹配失败】未找到允许打印的规则，所有规则都设置为不允许打印");
            }

            return matchingRules;
        }

        /// <summary>
        /// 检查是否只有一个简单规则（没有鸡舍号、客户名和特殊规则），如果是则直接判断重量
        /// </summary>
        /// <param name="rules">要检查的规则列表</param>
        /// <param name="weight">重量</param>
        /// <returns>如果找到匹配的简单规则则返回该规则，否则返回null</returns>
        private ProductRule CheckForSingleSimpleRule(List<ProductRule> rules, double weight)
        {
            if (rules.Count == 1)
            {
                var singleRule = rules[0];
                if (string.IsNullOrEmpty(singleRule.ChickenHouse) &&
                    string.IsNullOrEmpty(singleRule.CustomerName) &&
                    (!singleRule.EnableSpecialRules || singleRule.SpecialRules == null || !singleRule.SpecialRules.Any()))
                {
                    Logger.Info($"只有一个规则匹配版面 {singleRule.Version}，且该规则鸡舍号和客户名都为空，没有特殊规则，直接进行重量判断");

                    // 直接判断重量
                    if (singleRule.WeightLowerLimit <= weight && weight <= singleRule.WeightUpperLimit)
                    {
                        Logger.Info($"找到匹配的规则: ID={singleRule.Id}, 品名={singleRule.ProductName}, 规格={singleRule.Specification}, 重量范围=[{singleRule.WeightLowerLimit}-{singleRule.WeightUpperLimit}]");
                        return singleRule;
                    }
                    else
                    {
                        Logger.Info($"【重量不匹配】规则 ID={singleRule.Id} 的重量范围 [{singleRule.WeightLowerLimit}-{singleRule.WeightUpperLimit}] 不匹配当前重量 {weight}");
                        return null;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 按鸡舍号筛选规则
        /// </summary>
        /// <param name="rules">要筛选的规则列表</param>
        /// <param name="chickenHouse">鸡舍号，可能为null</param>
        /// <returns>筛选后的规则列表</returns>
        private List<ProductRule> FilterRulesByChickenHouse(List<ProductRule> rules, string chickenHouse)
        {
            return FilterRulesByChickenHouseWithResult(rules, chickenHouse);
        }

        /// <summary>
        /// 按鸡舍号筛选规则，返回筛选后的规则列表
        /// </summary>
        /// <param name="rules">要筛选的规则列表</param>
        /// <param name="chickenHouse">鸡舍号，可能为null</param>
        /// <returns>筛选后的规则列表</returns>
        private List<ProductRule> FilterRulesByChickenHouseWithResult(List<ProductRule> rules, string chickenHouse)
        {
            var originalRuleCount = rules.Count;
            List<ProductRule> matchingRules;

            // 注意：鸡舍号为字符串"null"的情况已在Form1.cs的OnMessageReceived函数中处理
            // 此处不再需要特殊处理

            if (!string.IsNullOrEmpty(chickenHouse))
            {
                // 如果提供了鸡舍号
                Logger.Info($"开始按鸡舍号 {chickenHouse} 筛选规则");

                // 首先尝试精确匹配鸡舍号
                var exactChickenHouseRules = rules.Where(r => r.ChickenHouse == chickenHouse).ToList();
                if (exactChickenHouseRules.Any())
                {
                    Logger.Info($"找到精确匹配鸡舍号 {chickenHouse} 的规则: {exactChickenHouseRules.Count} 条");
                    matchingRules = exactChickenHouseRules;
                }
                else
                {
                    // 如果规则指定了鸡舍号，必须精确匹配才能作为打印的规则
                    Logger.Info($"【匹配失败】未找到精确匹配鸡舍号 {chickenHouse} 的规则");
                    LogAvailableChickenHouses(rules);
                    // 设置匹配规则为空列表，表示匹配失败
                    matchingRules = new List<ProductRule>();
                }
            }
            else
            {
                // 如果没有提供鸡舍号，只保留那些没有指定鸡舍号的规则
                Logger.Info("未提供鸡舍号，只保留没有指定鸡舍号的规则");
                var nullChickenHouseRules = rules.Where(r => string.IsNullOrEmpty(r.ChickenHouse)).ToList();
                if (nullChickenHouseRules.Any())
                {
                    Logger.Info($"找到没有指定鸡舍号的规则: {nullChickenHouseRules.Count} 条");
                    matchingRules = nullChickenHouseRules;
                }
                else
                {
                    Logger.Info($"【匹配失败】没有找到没有指定鸡舍号的规则，所有规则都需要指定鸡舍号");
                    LogAvailableChickenHouses(rules);
                    matchingRules = new List<ProductRule>();
                }
            }

            return matchingRules;
        }

        /// <summary>
        /// 记录所有可用的鸡舍号
        /// </summary>
        /// <param name="rules">规则列表</param>
        private void LogAvailableChickenHouses(List<ProductRule> rules)
        {
            var availableChickenHouses = rules
                .Where(r => !string.IsNullOrEmpty(r.ChickenHouse))
                .Select(r => r.ChickenHouse)
                .Distinct()
                .ToList();

            if (availableChickenHouses.Any())
            {
                Logger.Info($"【提示】有以下鸡舍号的规则: {string.Join(", ", availableChickenHouses)}");
            }

            // 检查是否有通用规则（没有指定鸡舍号的规则）
            var nullChickenHouseRules = rules.Where(r => string.IsNullOrEmpty(r.ChickenHouse)).ToList();
            if (nullChickenHouseRules.Any())
            {
                Logger.Info($"【提示】有 {nullChickenHouseRules.Count} 条通用鸡舍号规则，但根据新要求不再使用");
            }
        }

        /// <summary>
        /// 按客户名筛选规则
        /// </summary>
        /// <param name="rules">要筛选的规则列表</param>
        /// <param name="customerName">客户名，可能为null</param>
        /// <returns>筛选后的规则列表</returns>
        private List<ProductRule> FilterRulesByCustomerName(List<ProductRule> rules, string customerName)
        {
            return FilterRulesByCustomerNameWithResult(rules, customerName);
        }

        /// <summary>
        /// 按客户名筛选规则，返回筛选后的规则列表
        /// </summary>
        /// <param name="rules">要筛选的规则列表</param>
        /// <param name="customerName">客户名，可能为null</param>
        /// <returns>筛选后的规则列表</returns>
        private List<ProductRule> FilterRulesByCustomerNameWithResult(List<ProductRule> rules, string customerName)
        {
            var originalRuleCount = rules.Count;
            List<ProductRule> matchingRules;

            if (!string.IsNullOrEmpty(customerName))
            {
                // 如果提供了客户名，先清理客户名，删除开头或结尾的空格、回车、换行等不可见字符
                string trimmedCustomerName = customerName.Trim();
                Logger.Info($"开始按客户名 {trimmedCustomerName} 筛选规则 (原始客户名: {customerName})");

                // 首先尝试精确匹配客户名
                var exactCustomerRules = rules.Where(r =>
                    !string.IsNullOrEmpty(r.CustomerName) &&
                    r.CustomerName.Trim() == trimmedCustomerName
                ).ToList();

                if (exactCustomerRules.Any())
                {
                    Logger.Info($"找到精确匹配客户名 {trimmedCustomerName} 的规则: {exactCustomerRules.Count} 条");
                    matchingRules = exactCustomerRules;
                }
                else
                {
                    // 如果规则指定了客户名，必须精确匹配才能作为打印的规则
                    Logger.Info($"【匹配失败】未找到精确匹配客户名 {trimmedCustomerName} 的规则");
                    LogAvailableCustomerNames(rules);
                    // 设置匹配规则为空列表，表示匹配失败
                    matchingRules = new List<ProductRule>();
                }
            }
            else
            {
                // 如果没有提供客户名，只保留那些没有指定客户名的规则
                Logger.Info("未提供客户名，只保留没有指定客户名的规则");
                var nullCustomerRules = rules.Where(r => string.IsNullOrEmpty(r.CustomerName)).ToList();
                if (nullCustomerRules.Any())
                {
                    Logger.Info($"找到没有指定客户名的规则: {nullCustomerRules.Count} 条");
                    matchingRules = nullCustomerRules;
                }
                else
                {
                    Logger.Info($"【匹配失败】没有找到没有指定客户名的规则，所有规则都需要指定客户名");
                    LogAvailableCustomerNames(rules);
                    matchingRules = new List<ProductRule>();
                }
            }

            return matchingRules;
        }

        /// <summary>
        /// 记录所有可用的客户名
        /// </summary>
        /// <param name="rules">规则列表</param>
        private void LogAvailableCustomerNames(List<ProductRule> rules)
        {
            var availableCustomerNames = rules
                .Where(r => !string.IsNullOrEmpty(r.CustomerName))
                .Select(r => r.CustomerName.Trim())
                .Distinct()
                .ToList();

            if (availableCustomerNames.Any())
            {
                Logger.Info($"【提示】当前筛选条件下有以下客户名的规则: {string.Join(", ", availableCustomerNames)}");
            }
        }

        /// <summary>
        /// 查找匹配重量的规则
        /// </summary>
        /// <param name="rules">要筛选的规则列表</param>
        /// <param name="chickenHouse">鸡舍号，可能为null</param>
        /// <param name="weight">重量</param>
        /// <returns>匹配的规则，如果没找到则返回null</returns>
        private ProductRule FindRuleMatchingWeight(List<ProductRule> rules, string chickenHouse, double weight)
        {
            // 注意：鸡舍号为字符串"null"的情况已在Form1.cs的OnMessageReceived函数中处理
            // 此处不再需要特殊处理

            Logger.Info($"筛选后剩余规则数量: {rules.Count}");

            bool anyRuleChecked = false;
            foreach (var rule in rules)
            {
                anyRuleChecked = true;
                Logger.Info($"检查规则 ID={rule.Id}, 品名={rule.ProductName}, 规格={rule.Specification}, 重量范围=[{rule.WeightLowerLimit}-{rule.WeightUpperLimit}], 启用特殊规则={rule.EnableSpecialRules}");

                // 检查是否有特殊规则需要处理
                if (rule.EnableSpecialRules && rule.SpecialRules != null && rule.SpecialRules.Any())
                {
                    Logger.Info($"规则 ID={rule.Id} 启用了特殊规则，共 {rule.SpecialRules.Count} 条特殊规则");

                    // 对于启用了特殊规则的规则，直接处理特殊规则，不检查版面自身的重量范围
                    var specialRule = ProcessSpecialRules(rule, chickenHouse, weight);
                    if (specialRule != null)
                    {
                        return specialRule;
                    }

                    // 如果没有匹配的特殊规则，继续检查下一个规则
                    Logger.Info($"规则 ID={rule.Id} 的特殊规则没有匹配项，继续检查下一个规则");
                    continue;
                }

                // 对于没有特殊规则的规则，检查版面自身的重量范围
                if (rule.WeightLowerLimit <= weight && weight <= rule.WeightUpperLimit)
                {
                    return rule;
                }
                else
                {
                    Logger.Info($"【重量不匹配】规则 ID={rule.Id} 的重量范围 [{rule.WeightLowerLimit}-{rule.WeightUpperLimit}] 不匹配当前重量 {weight}");
                }
            }

            if (anyRuleChecked)
            {
                Logger.Info($"【匹配失败】所有规则的重量范围都不匹配当前重量 {weight}");
                LogAvailableWeightRanges(rules);
            }

            return null;
        }

        /// <summary>
        /// 记录所有可用的重量范围
        /// </summary>
        /// <param name="rules">规则列表</param>
        private void LogAvailableWeightRanges(List<ProductRule> rules)
        {
            var availableWeightRanges = rules
                .Select(r => $"[{r.WeightLowerLimit}-{r.WeightUpperLimit}]")
                .Distinct()
                .ToList();

            if (availableWeightRanges.Any())
            {
                Logger.Info($"【提示】当前筛选条件下有以下重量范围的规则: {string.Join(", ", availableWeightRanges)}");
            }
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

            // 注意：鸡舍号为字符串"null"的情况已在Form1.cs的OnMessageReceived函数中处理
            // 此处不再需要特殊处理

            // 记录日志，帮助调试
            Logger.Info($"开始处理规则 ID={rule.Id} 的特殊规则: 版面={rule.Version}, 鸡舍={chickenHouse ?? "未指定"}, 重量={weight}, 特殊规则数量={rule.SpecialRules.Count}");

            // 优化：先筛选允许打印的特殊规则
            var allowedSpecialRules = rule.SpecialRules.Where(sr => sr.AllowPrint).ToList();
            Logger.Info($"允许打印的特殊规则数量: {allowedSpecialRules.Count}/{rule.SpecialRules.Count}");

            if (allowedSpecialRules.Count == 0)
            {
                Logger.Info($"规则 ID={rule.Id} 没有允许打印的特殊规则，跳过特殊规则处理");
                return null;
            }

            int index = 0;
            foreach (var condition in allowedSpecialRules)
            {
                index++;
                // 记录每个特殊规则的详细信息
                Logger.Info($"检查特殊规则 #{index}: 鸡舍={condition.ChickenHouse ?? "未指定"}, 重量范围=[{condition.WeightLowerLimit}-{condition.WeightUpperLimit}], 二维码={condition.QRCode}, 允许打印={condition.AllowPrint}");

                // 检查鸡舍是否匹配
                // 如果特殊规则指定了鸡舍号，必须精确匹配
                if (!string.IsNullOrEmpty(condition.ChickenHouse))
                {
                    // 如果特殊规则指定了鸡舍号，但没有提供鸡舍号，则不匹配
                    if (string.IsNullOrEmpty(chickenHouse))
                    {
                        Logger.Info($"【特殊规则不匹配】特殊规则 #{index} 指定了鸡舍 {condition.ChickenHouse}，但当前未提供鸡舍信息");
                        continue;
                    }
                    // 如果特殊规则指定了鸡舍号，但与提供的鸡舍号不匹配，则不匹配
                    else if (condition.ChickenHouse != chickenHouse)
                    {
                        Logger.Info($"【特殊规则不匹配】特殊规则 #{index} 鸡舍不匹配: 规则鸡舍={condition.ChickenHouse}, 当前鸡舍={chickenHouse}");
                        continue;
                    }
                    else
                    {
                        Logger.Info($"特殊规则 #{index} 鸡舍匹配: {condition.ChickenHouse}");
                    }
                }
                else
                {
                    // 如果特殊规则没有指定鸡舍号，适用于所有鸡舍
                    Logger.Info($"特殊规则 #{index} 未指定鸡舍，适用于所有鸡舍");
                }

                // 检查重量范围是否匹配
                if (condition.WeightLowerLimit <= weight && weight <= condition.WeightUpperLimit)
                {
                    Logger.Info($"特殊规则 #{index} 重量匹配: 当前重量 {weight} 在范围 [{condition.WeightLowerLimit}-{condition.WeightUpperLimit}] 内");

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
                        // 使用特殊规则的允许打印设置，如果特殊规则有设置的话
                        AllowPrint = condition.AllowPrint,
                        // 使用特殊规则的二维码
                        QRCode = condition.QRCode,
                        EnableSpecialRules = false,
                        SpecialRules = new List<SpecialRuleCondition>()
                    };

                    var duration = (DateTime.Now - startTime).TotalMilliseconds;
                    Logger.Info($"特殊规则处理完成，找到匹配的特殊规则 #{index}，耗时: {duration:F0}ms");
                    Logger.Info($"特殊规则 #{index} 设置为允许打印: 版面={rule.Version}, 鸡舍={chickenHouse ?? "未指定"}, 重量={weight}");

                    return specialRule;
                }
                else
                {
                    Logger.Info($"【特殊规则不匹配】特殊规则 #{index} 重量不匹配: 当前重量 {weight} 不在范围 [{condition.WeightLowerLimit}-{condition.WeightUpperLimit}] 内");
                }
            }

            var totalDuration = (DateTime.Now - startTime).TotalMilliseconds;
            Logger.Info($"【特殊规则处理结果】没有找到匹配的特殊规则，耗时: {totalDuration:F0}ms");

            // 列出所有允许打印的特殊规则的重量范围
            if (allowedSpecialRules.Any())
            {
                var availableWeightRanges = allowedSpecialRules
                    .Select(r => $"[{r.WeightLowerLimit}-{r.WeightUpperLimit}]")
                    .ToList();

                Logger.Info($"【提示】规则 ID={rule.Id} 的允许打印的特殊规则有以下重量范围: {string.Join(", ", availableWeightRanges)}");
            }

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
                    // 使用自定义转换器处理旧的 RejectPrint 属性
                    var settings = new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> { new ProductRuleConverter() }
                    };
                    var rules = JsonConvert.DeserializeObject<List<ProductRule>>(json, settings);
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
                // 使用默认设置序列化，确保使用新的属性名
                string json = JsonConvert.SerializeObject(_rules, Formatting.Indented);
                File.WriteAllText(_filePath, json, Encoding.UTF8);

                // 记录日志，提示所有规则的允许打印状态
                int allowedCount = _rules.Count(r => r.AllowPrint);
                Logger.Info($"保存规则时，共有 {allowedCount}/{_rules.Count} 条规则设置为允许打印");
                Logger.Info($"成功保存 {_rules.Count} 条规则到 {_filePath}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"保存规则到文件 {_filePath} 失败");
            }
        }
    }
}
