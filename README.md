# C# Modbus 上位机数据采集系统

基于 **C# / WPF / .NET Framework 4.8** 开发的工控上位机系统软件。用于与 Modbus TCP/RTU 从站设备通信，实现设备数据的实时采集、处理、可视化展示与历史数据持久化存储。

## 📌 项目特性

- **多协议支持**：支持 Modbus TCP 与 Modbus RTU 两种通信方式，可在运行时自由切换。
- **实时数据监控**：异步轮询读取设备寄存器数据，UI 界面实时刷新。
- **历史数据存储**：接入本地 SQLite 数据库，记录点位历史数据，支持按标签与时间范围查询。
- **高可用性设计**：内置断线重连机制与异常隔离，保障系统在工业现场环境下的稳定运行。
- **防数据膨胀**：采用“变化才存 + 定时清理”策略，避免数据库无限增大。

## 🛠️ 技术栈

- **开发语言**：C#
- **UI 框架**：WPF (MVVM 模式)
- **运行环境**：.NET Framework 4.8
- **通信协议**：Modbus (基于 NModbus 库)
- **数据库**：SQLite
- **多线程**：Task / Async / Await / Dispatcher

## 🏗️ 架构设计

系统采用 MVVM 分层架构，保证各层职责单一，彻底解耦 UI 与底层通信/存储逻辑：

- **Models**：设备配置与点位数据模型（如 `PointItem`）。
- **Services**：底层服务，包含 Modbus 通信（`IModbusService`）与 SQLite 存储（`DatabaseService`）。
- **ViewModels**：业务逻辑核心，处理轮询控制、命令绑定、异常处理与数据中转。
- **Views**：XAML 界面，仅负责数据展示与用户交互。
- **Database**：本地 SQLite 数据文件与操作日志txt文件存储。

### 接口抽象

系统抽象了 `IModbusService` 接口，底层提供 TCP 和 RTU 两种实现类（通过工厂模式按配置创建）。View 层和 ViewModel 层仅依赖接口，做到**换通信协议无需修改 UI 代码**，同时也方便在单元测试中进行 Mock 测试。

## ⚙️ 核心技术点解析

### 异步与线程安全
- 轮询采集任务通过 `Task.Run` 运行在后台线程，避免阻塞 UI 主线程导致界面卡顿。
- 跨线程更新 UI 集合时，统一通过 `Dispatcher.Invoke` 封送回 UI 线程执行，防止引发跨线程访问异常。

### 异常处理与容错
- **通信隔离**：单次轮询异常不会导致采集循环终止，保证系统持续运行。
- **存储隔离**：数据库写入失败仅记录日志，不影响正常的通信与界面展示。

### 数据库并发控制
- 针对 SQLite 单文件数据库的并发写入限制，在数据库服务中使用 `lock` 进行串行化保护，避免出现 `database is locked` 错误。
- 采用参数化查询（Parameterized Queries），有效防止 SQL 注入。

## 🚀 如何运行

1. 克隆本项目到本地：
   ```bash
   git clone https://github.com/LGR486/C-.git
2. 确保本地已安装 Visual Studio 2022 及 .NET Framework 4.8 开发环境。
3. 启动一个 Modbus 模拟器（如 Modbus Slave），配置 IP 为 127.0.0.1，端口 502，站号 1。
4. 使用 VS 打开解决方案 WinApp1.sln，编译并运行。
5. 在界面中点击“连接”，即可看到数据实时刷新。历史查询功能会自动读取本地 SQLite 数据。
<img width="1470" height="552" alt="image" src="https://github.com/user-attachments/assets/2c6a5535-e0a5-4ef8-8c22-3bfc39be7dee" />