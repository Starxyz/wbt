import logging
from typing import Optional, Tuple, List, Dict, Any
import time
import threading

# 配置日志
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(name)s - %(levelname)s - %(message)s')
logger = logging.getLogger('ModbusClient')

try:
    from pymodbus.client.sync import ModbusTcpClient
    from pymodbus.exceptions import ModbusException
    MODBUS_AVAILABLE = True
except ImportError:
    logger.warning("pymodbus 库未安装，将使用模拟重量数据")
    MODBUS_AVAILABLE = False

class ModbusClient:
    def __init__(self, host: str = '127.0.0.1', port: int = 502, 
                 unit: int = 1, weight_register: int = 0):
        """
        初始化Modbus客户端
        
        Args:
            host: Modbus服务器主机地址
            port: Modbus服务器端口
            unit: Modbus单元ID
            weight_register: 重量数据的寄存器地址
        """
        self.host = host
        self.port = port
        self.unit = unit
        self.weight_register = weight_register
        self.client = None
        self.connected = False
        self.last_weight = 0.0
        self.is_running = False
        self.weight_callback = None
        self.poll_interval = 0.5  # 轮询间隔，单位秒
        self._lock = threading.Lock()
        
    def connect(self) -> bool:
        """连接到Modbus服务器，返回是否成功"""
        if not MODBUS_AVAILABLE:
            logger.warning("pymodbus 库未安装，使用模拟连接")
            self.connected = True
            return True
            
        try:
            self.client = ModbusTcpClient(self.host, self.port)
            self.connected = self.client.connect()
            if self.connected:
                logger.info(f"已连接到Modbus服务器 {self.host}:{self.port}")
            else:
                logger.error(f"无法连接到Modbus服务器 {self.host}:{self.port}")
            return self.connected
        except Exception as e:
            logger.error(f"连接Modbus服务器时出错: {str(e)}")
            self.connected = False
            return False
    
    def disconnect(self) -> None:
        """断开与Modbus服务器的连接"""
        if not MODBUS_AVAILABLE:
            self.connected = False
            return
            
        if self.client and self.connected:
            try:
                self.client.close()
                logger.info("已断开与Modbus服务器的连接")
            except Exception as e:
                logger.error(f"断开Modbus连接时出错: {str(e)}")
            finally:
                self.connected = False
    
    def read_weight(self) -> Tuple[bool, float]:
        """
        读取重量数据
        
        Returns:
            (成功标志, 重量值)
        """
        with self._lock:
            if not MODBUS_AVAILABLE:
                # 模拟重量数据
                import random
                weight = round(random.uniform(15.0, 18.0), 1)
                self.last_weight = weight
                return True, weight
                
            if not self.connected:
                logger.warning("未连接到Modbus服务器")
                return False, self.last_weight
                
            try:
                # 读取保持寄存器
                response = self.client.read_holding_registers(
                    self.weight_register, 1, unit=self.unit
                )
                
                if response.isError():
                    logger.error(f"读取重量数据时出错: {response}")
                    return False, self.last_weight
                    
                # 假设重量数据是一个16位整数，需要除以10得到实际重量
                # 根据实际情况调整转换逻辑
                weight = float(response.registers[0]) / 10.0
                self.last_weight = weight
                return True, weight
                
            except Exception as e:
                logger.error(f"读取重量数据时出错: {str(e)}")
                return False, self.last_weight
    
    def start_polling(self, callback=None) -> None:
        """
        开始轮询重量数据
        
        Args:
            callback: 当重量变化时调用的回调函数，接收重量值作为参数
        """
        if self.is_running:
            logger.warning("轮询已经在运行")
            return
            
        self.weight_callback = callback
        self.is_running = True
        
        # 在新线程中轮询
        polling_thread = threading.Thread(target=self._polling_thread)
        polling_thread.daemon = True
        polling_thread.start()
        
        logger.info("开始轮询重量数据")
    
    def stop_polling(self) -> None:
        """停止轮询重量数据"""
        self.is_running = False
        logger.info("停止轮询重量数据")
    
    def _polling_thread(self) -> None:
        """轮询线程函数"""
        last_reported_weight = None
        
        while self.is_running:
            success, weight = self.read_weight()
            
            if success and (last_reported_weight is None or abs(weight - last_reported_weight) >= 0.1):
                # 重量变化超过0.1时才报告
                last_reported_weight = weight
                
                # 调用回调函数
                if self.weight_callback:
                    try:
                        self.weight_callback(weight)
                    except Exception as e:
                        logger.error(f"调用重量回调函数时出错: {str(e)}")
            
            # 等待下一次轮询
            time.sleep(self.poll_interval)
