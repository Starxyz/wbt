using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    /// <summary>
    /// 产品规则类，用于存储产品的特殊处理规则
    /// </summary>
    public class ProductRule
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 版面信息
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 品名
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// 规格
        /// </summary>
        public string Specification { get; set; }

        /// <summary>
        /// 鸡舍号
        /// </summary>
        public string ChickenHouse { get; set; }

        /// <summary>
        /// 客户名
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        /// 重量下限
        /// </summary>
        public double WeightLowerLimit { get; set; }

        /// <summary>
        /// 重量上限
        /// </summary>
        public double WeightUpperLimit { get; set; }

        /// <summary>
        /// 是否允许打印（默认为 false，即不允许打印）
        /// </summary>
        public bool AllowPrint { get; set; } = false;

        /// <summary>
        /// 二维码
        /// </summary>
        public string QRCode { get; set; }

        /// <summary>
        /// 是否启用特殊规则
        /// </summary>
        public bool EnableSpecialRules { get; set; }

        /// <summary>
        /// 特殊规则条件列表
        /// </summary>
        public List<SpecialRuleCondition> SpecialRules { get; set; } = new List<SpecialRuleCondition>();
    }

    /// <summary>
    /// 特殊规则条件类
    /// </summary>
    public class SpecialRuleCondition
    {
        /// <summary>
        /// 鸡舍号
        /// </summary>
        public string ChickenHouse { get; set; }

        /// <summary>
        /// 重量下限
        /// </summary>
        public double WeightLowerLimit { get; set; }

        /// <summary>
        /// 重量上限
        /// </summary>
        public double WeightUpperLimit { get; set; }

        /// <summary>
        /// 二维码
        /// </summary>
        public string QRCode { get; set; }

        /// <summary>
        /// 是否允许打印（默认为 false，即不允许打印）
        /// </summary>
        public bool AllowPrint { get; set; } = false;
    }
}
