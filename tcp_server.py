import socket
import threading
import json
import logging
from typing import Callable, Dict, Any, Optional

# 配置日志
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(name)s - %(levelname)s - %(message)s')
logger = logging.getLogger('TCPServer')

class TCPServer:
    def __init__(self, host: str = '0.0.0.0', port: int = 8888, 
                 message_handler: Optional[Callable] = None):
        """
        初始化TCP服务器
        
        Args:
            host: 服务器主机地址
            port: 服务器端口
            message_handler: 消息处理回调函数，接收解析后的消息字典
        """
        self.host = host
        self.port = port
        self.server_socket = None
        self.is_running = False
        self.message_handler = message_handler
        self.clients = []
    
    def start(self) -> None:
        """启动TCP服务器"""
        if self.is_running:
            logger.warning("服务器已经在运行")
            return
            
        try:
            self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            self.server_socket.bind((self.host, self.port))
            self.server_socket.listen(5)
            self.is_running = True
            
            logger.info(f"TCP服务器已启动，监听 {self.host}:{self.port}")
            
            # 在新线程中接受连接
            accept_thread = threading.Thread(target=self._accept_connections)
            accept_thread.daemon = True
            accept_thread.start()
            
        except Exception as e:
            logger.error(f"启动服务器时出错: {str(e)}")
            self.stop()
    
    def stop(self) -> None:
        """停止TCP服务器"""
        self.is_running = False
        
        # 关闭所有客户端连接
        for client in self.clients:
            try:
                client.close()
            except:
                pass
        self.clients = []
        
        # 关闭服务器套接字
        if self.server_socket:
            try:
                self.server_socket.close()
            except:
                pass
            
        logger.info("TCP服务器已停止")
    
    def _accept_connections(self) -> None:
        """接受客户端连接的线程函数"""
        while self.is_running:
            try:
                client_socket, client_address = self.server_socket.accept()
                logger.info(f"接受来自 {client_address} 的连接")
                
                self.clients.append(client_socket)
                
                # 在新线程中处理客户端
                client_thread = threading.Thread(
                    target=self._handle_client,
                    args=(client_socket, client_address)
                )
                client_thread.daemon = True
                client_thread.start()
                
            except Exception as e:
                if self.is_running:
                    logger.error(f"接受连接时出错: {str(e)}")
    
    def _handle_client(self, client_socket: socket.socket, client_address: tuple) -> None:
        """
        处理客户端连接的线程函数
        
        Args:
            client_socket: 客户端套接字
            client_address: 客户端地址
        """
        try:
            while self.is_running:
                # 接收数据
                data = client_socket.recv(4096)
                if not data:
                    logger.info(f"客户端 {client_address} 断开连接")
                    break
                
                # 解析消息
                try:
                    message = self._parse_message(data)
                    logger.info(f"从 {client_address} 接收消息: {message}")
                    
                    # 调用消息处理回调
                    if self.message_handler:
                        response = self.message_handler(message)
                        
                        # 如果有响应，发送回客户端
                        if response:
                            client_socket.sendall(json.dumps(response).encode('utf-8'))
                            
                except Exception as e:
                    logger.error(f"处理消息时出错: {str(e)}")
                    
        except Exception as e:
            logger.error(f"处理客户端 {client_address} 时出错: {str(e)}")
            
        finally:
            # 关闭客户端连接
            try:
                client_socket.close()
                if client_socket in self.clients:
                    self.clients.remove(client_socket)
            except:
                pass
    
    def _parse_message(self, data: bytes) -> Dict[str, Any]:
        """
        解析接收到的消息
        
        Args:
            data: 接收到的字节数据
            
        Returns:
            解析后的消息字典
        """
        # 尝试解析为JSON
        try:
            message = json.loads(data.decode('utf-8'))
            return message
        except json.JSONDecodeError:
            # 如果不是JSON，尝试解析为自定义格式
            # 假设格式为: 品类|鸡舍|大标签数量|版面状态|二维码
            try:
                text = data.decode('utf-8').strip()
                parts = text.split('|')
                
                message = {}
                if len(parts) >= 1:
                    message["category"] = parts[0]
                if len(parts) >= 2:
                    message["chicken_house"] = parts[1] if parts[1] else None
                if len(parts) >= 3:
                    message["tag_quantity"] = parts[2] if parts[2] else None
                if len(parts) >= 4:
                    message["panel_status"] = parts[3] if parts[3] else None
                if len(parts) >= 5:
                    message["qrcode"] = parts[4] if parts[4] else None
                    
                return message
            except Exception as e:
                logger.error(f"解析消息时出错: {str(e)}")
                return {"raw_data": data.decode('utf-8', errors='ignore')}
    
    def send_to_all(self, message: Dict[str, Any]) -> None:
        """
        向所有连接的客户端发送消息
        
        Args:
            message: 要发送的消息字典
        """
        if not self.clients:
            logger.warning("没有连接的客户端")
            return
            
        data = json.dumps(message).encode('utf-8')
        
        for client in self.clients[:]:  # 使用副本遍历
            try:
                client.sendall(data)
            except Exception as e:
                logger.error(f"发送消息时出错: {str(e)}")
                try:
                    client.close()
                    self.clients.remove(client)
                except:
                    pass
