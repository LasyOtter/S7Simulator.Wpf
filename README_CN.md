# S7 Simulator (WPF)

一个基于 WPF 和 Sharp7 构建的强大西门子 S7-1200/1500 PLC 仿真器。该工具允许开发人员和工程师模拟 PLC 服务器，从 Excel 导入 DB 结构，并通过标准 S7 协议与内存区域（DB、M、I、Q）进行交互。

## 功能特性

- **S7 服务器仿真**：作为全功能的 Snap7 服务器运行，监听 102 端口。
- **内存区域**：支持对以下区域的读/写操作：
  - **DB**：数据块（基于导入动态注册）
  - **MB**：中间继电器 / 标志位
  - **IB**：输入
  - **QB**：输出
- **Excel 导入**：轻松从 Excel 文件导入 DB 定义和变量标签。
- **持久化**：自动将 DB 结构和变量值保存到本地 SQLite 数据库，确保重启后数据不丢失。
- **实时监控**：通过现代化的 WPF 界面实时查看和编辑变量值。
- **数据类型支持**：
  - **BOOL**：位访问
  - **BYTE, INT, DINT**：整数类型
  - **REAL**：浮点数
  - **STRING**：S7 字符串（支持 UTF-8 中文）

## 技术栈

- **框架**：.NET 8 (WPF)
- **架构**：MVVM (Model-View-ViewModel)，使用 `CommunityToolkit.Mvvm`
- **PLC 通讯**：`Sharp7` (Snap7 的 C# 移植版)
- **数据库**：`SQLite` (`Microsoft.Data.Sqlite`)
- **Excel 处理**：`EPPlus`

## 快速开始

### 前置要求

- .NET 8 SDK
- Visual Studio 2022 或兼容的 IDE

### 安装

1. Clone 仓库。
2. 打开 `S7Simulator.Wpf.sln`。
3. 还原 NuGet 包并生成解决方案。

### 使用说明

1. **启动仿真器**：运行应用程序。状态栏将显示 "S7 Server 已启动"。
2. **导入 DB**：
   - 点击 **"导入 Excel (DB/Tags)"**。
   - 选择包含 DB 定义的 Excel 文件。
   - **Excel 格式**：
     - 第 1 列：DB 号
     - 第 2 列：变量名
     - 第 3 列：数据类型 (BOOL, INT, REAL, STRING 等)
     - 第 4 列：字节偏移量
     - 第 5 列：位偏移量 (BOOL 类型使用，其他为空或 -1)
     - 第 6 列：初始值
     - 第 7 列：注释
3. **连接客户端**：
   - 使用任何 S7 客户端（如 HMI、SCADA、其他 PLC 或 Snap7 Client）。
   - 连接到本机的 IP 地址（或 `127.0.0.1` 进行本地测试）。
   - 机架 (Rack): 0, 插槽 (Slot): 1 (标准 S7-1200/1500 寻址)。
4. **监控与编辑**：
   - 双击列表中的 DB 打开 **详情视图**。
   - 查看每 100ms 刷新一次的实时值。
   - 直接在网格中编辑值以模拟现场变化。

## 项目结构

- **`S7Simulator.Wpf`**：主应用程序项目。
  - **`Models`**：数据实体 (`DbInfo`, `VariableInfo`)。
  - **`ViewModels`**：应用逻辑 (`MainViewModel`)。
  - **`Views`**：UI 组件 (`MainWindow`, `DBDetailWindow`)。
  - **`Services`**：
    - `DatabaseService`：用于持久化的异步 SQLite 操作。
    - `ExcelImporter`：异步 Excel 解析逻辑。
  - **`Plc`**：
    - `PlcServer`：管理 Snap7 服务器生命周期和区域注册。
    - `PlcMemory`：线程安全的内存缓冲区管理和类型转换。

## 贡献

欢迎提交 Pull Request 或针对 Bug 和功能请求提交 Issue。

## 许可证

本项目基于 MIT 许可证开源。
