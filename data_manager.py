import json
import os
from typing import Dict, List, Optional, Any, Tuple

class DataManager:
    def __init__(self, file_path: str = "product_data.json"):
        self.file_path = file_path
        self.data = self._load_data()
        
    def _load_data(self) -> Dict:
        """从文件加载数据，如果文件不存在则创建默认结构"""
        if os.path.exists(self.file_path):
            try:
                with open(self.file_path, 'r', encoding='utf-8') as f:
                    return json.load(f)
            except json.JSONDecodeError:
                print(f"错误：{self.file_path} 文件格式不正确")
                return {"products": []}
        else:
            return {"products": []}
    
    def save_data(self) -> None:
        """保存数据到文件"""
        with open(self.file_path, 'w', encoding='utf-8') as f:
            json.dump(self.data, f, ensure_ascii=False, indent=2)
    
    def get_all_products(self) -> List[Dict]:
        """获取所有产品数据"""
        return self.data.get("products", [])
    
    def add_product(self, product: Dict) -> int:
        """添加新产品，返回新产品ID"""
        products = self.data.get("products", [])
        # 生成新ID
        new_id = 1
        if products:
            new_id = max(p.get("id", 0) for p in products) + 1
        
        product["id"] = new_id
        products.append(product)
        self.data["products"] = products
        self.save_data()
        return new_id
    
    def update_product(self, product_id: int, updated_product: Dict) -> bool:
        """更新产品信息，返回是否成功"""
        products = self.data.get("products", [])
        for i, product in enumerate(products):
            if product.get("id") == product_id:
                updated_product["id"] = product_id  # 确保ID不变
                products[i] = updated_product
                self.data["products"] = products
                self.save_data()
                return True
        return False
    
    def delete_product(self, product_id: int) -> bool:
        """删除产品，返回是否成功"""
        products = self.data.get("products", [])
        for i, product in enumerate(products):
            if product.get("id") == product_id:
                products.pop(i)
                self.data["products"] = products
                self.save_data()
                return True
        return False
    
    def find_product(self, version: str, chicken_house: Optional[str], 
                    customer_name: Optional[str], weight: float) -> Optional[Dict]:
        """
        根据版面信息、鸡舍号、客户名和重量查找匹配的产品
        
        Args:
            version: 版面信息
            chicken_house: 鸡舍号，可能为None
            customer_name: 客户名，可能为None
            weight: 重量值
            
        Returns:
            匹配的产品信息，如果没找到则返回None
        """
        products = self.data.get("products", [])
        
        # 首先筛选版面匹配的产品
        matching_products = [p for p in products if p.get("version") == version]
        
        # 进一步筛选鸡舍号匹配的产品（如果提供了鸡舍号）
        if chicken_house is not None:
            matching_products = [p for p in matching_products 
                               if p.get("chicken_house") == chicken_house or p.get("chicken_house") is None]
        
        # 进一步筛选客户名匹配的产品（如果提供了客户名）
        if customer_name is not None:
            matching_products = [p for p in matching_products 
                               if p.get("customer_name") == customer_name or p.get("customer_name") is None]
        
        # 最后筛选重量在范围内的产品
        for product in matching_products:
            lower = product.get("weight_lower_limit")
            upper = product.get("weight_upper_limit")
            
            if lower <= weight <= upper:
                # 检查是否有特殊规则需要处理
                if product.get("special_rules", {}).get("enabled", False):
                    # 处理特殊规则
                    special_info = self._process_special_rules(product, chicken_house, weight)
                    if special_info:
                        # 将特殊规则处理结果合并到产品信息中
                        product_copy = product.copy()
                        product_copy.update(special_info)
                        return product_copy
                
                return product
                
        return None
    
    def _process_special_rules(self, product: Dict, chicken_house: Optional[str], 
                              weight: float) -> Optional[Dict]:
        """处理特殊规则，返回特殊处理的结果"""
        if not product.get("special_rules", {}).get("enabled", False):
            return None
            
        conditions = product.get("special_rules", {}).get("conditions", [])
        
        for condition in conditions:
            # 检查鸡舍是否匹配
            if chicken_house and condition.get("chicken_house") != chicken_house:
                continue
                
            # 检查重量范围是否匹配
            weight_range = condition.get("weight_range", [0, 0])
            if not (weight_range[0] <= weight <= weight_range[1]):
                continue
                
            # 找到匹配的条件，返回特殊处理结果
            return {
                "qrcode": condition.get("qrcode", ""),
                "special_processed": True
            }
            
        return None
