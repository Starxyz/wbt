import logging
from typing import Dict, Optional, Any, List, Tuple
from data_manager import DataManager
from modbus_client import ModbusClient

# 配置日志
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(name)s - %(levelname)s - %(message)s')
logger = logging.getLogger('BusinessLogic')

class BusinessLogic:
    def __init__(self, data_manager: DataManager, modbus_client: ModbusClient):
        """
        初始化业务逻辑处理器
        
        Args:
            data_manager: 数据管理器实例
            modbus_client: Modbus客户端实例
        """
        self.data_manager = data_manager
        self.modbus_client = modbus_client
        self.current_weight = 0.0
        self.current_message = {}
        self.result_callback = None
        
        # 设置重量变化回调
        self.modbus_client.start_polling(callback=self._on_weight_changed)
    
    def set_result_callback(self, callback) -> None:
        """设置结果回调函数"""
        self.result_callback = callback
    
    def process_message(self, message: Dict[str, Any]) -> Dict[str, Any]:
        """
        处理接收到的消息
        
        Args:
            message: 接收到的消息字典
            
        Returns:
            处理结果
        """
        logger.info(f"处理消息: {message}")
        
        # 保存当前消息
        self.current_message = message
        
        # 提取关键信息
        version = message.get("panel_status")
        chicken_house = message.get("chicken_house")
        customer_name = message.get("customer_name")
        
        # 如果没有版面信息，无法处理
        if not version:
            result = {
                "success": False,
                "error": "缺少版面信息",
                "message": message
            }
            self._notify_result(result)
            return result
        
        # 使用当前重量查找匹配的产品
        product = self.data_manager.find_product(
            version=version,
            chicken_house=chicken_house,
            customer_name=customer_name,
            weight=self.current_weight
        )
        
        if product:
            result = {
                "success": True,
                "product": product,
                "weight": self.current_weight,
                "message": message
            }
        else:
            result = {
                "success": False,
                "error": "未找到匹配的产品",
                "weight": self.current_weight,
                "message": message
            }
        
        # 通知结果
        self._notify_result(result)
        return result
    
    def _on_weight_changed(self, weight: float) -> None:
        """
        重量变化回调函数
        
        Args:
            weight: 新的重量值
        """
        self.current_weight = weight
        logger.debug(f"重量更新: {weight}")
        
        # 如果有当前消息，重新处理
        if self.current_message:
            self.process_message(self.current_message)
    
    def _notify_result(self, result: Dict[str, Any]) -> None:
        """
        通知处理结果
        
        Args:
            result: 处理结果
        """
        if self.result_callback:
            try:
                self.result_callback(result)
            except Exception as e:
                logger.error(f"调用结果回调函数时出错: {str(e)}")
    
    def shutdown(self) -> None:
        """关闭业务逻辑处理器"""
        self.modbus_client.stop_polling()
        self.modbus_client.disconnect()
