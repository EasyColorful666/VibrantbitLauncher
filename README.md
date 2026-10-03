# VibrantbitLauncher

一款基于 **WPF / .NET 10** 打造的 **Minecraft: Java Edition** Windows 启动器，采用 Fluent Design 风格界面（WPF-UI），集成版本下载、模组浏览与一键联机等常用功能。

> 本项目仍在快速迭代中，部分功能可能随版本调整。

## 功能特性



* **多账户管理**


  * 微软正版账户 OAuth 登录（Microsoft Store / Xbox Live 授权流程）

  * 离线账户（盗版 / 离线模式）

  * Yggdrasil 外置登录（支持 LittleSkin 等第三方认证服务）

  * 多账户本地持久化，一键切换当前账户

* **版本下载与安装**


  * 一键下载并安装原版 Minecraft（Vanilla）全部版本

  * 后台自动检测本地 Java 运行环境

* **模组中心**


  * 接入 [Modrinth](https://modrinth.com) 公开 API，在线浏览、搜索模组

  * 按 Minecraft 版本与加载器筛选：**Fabric / Forge / Quilt / NeoForge**

* **游戏启动**


  * 选择本地已安装版本与对应 Java 直接启动

  * 独立的游戏版本设置窗口

* **多人联机**


  * 内置 [EasyTier](https://easytier.cn) P2P 组网，一键下载部署，与好友建立虚拟局域网联机

* **主页与资讯**


  * 主页聚合 Mojang 官方新闻

* **个性化设置**


  * 亮 / 暗主题切换、强调色

  * 自定义 `.minecraft` 目录与 Java 路径

## 界面预览

截图待补充。

## 目录结构



```
VibrantbitLauncher/

├── Assets/            # 应用背景图、字体等资源

├── Helpers/           # 新闻抓取等辅助工具

├── Models/            # 配置与数据模型

├── Resources/         # 翻译等资源

├── Services/          # 主机、设置持久化等服务

├── ViewModels/        # MVVM 视图模型（页面 / 窗口）

└── Views/

\&#x20;   ├── Pages/         # 主页、账户、下载、模组、运行、多人、设置

\&#x20;   └── Windows/       # 主窗口、各类登录窗口、游戏设置窗口
```

## 从源码构建

### 环境要求



* Windows 10 / 11（x64）

* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### 编译运行



```
git clone https://github.com/\\\<your-org>/VibrantbitLauncher.git

cd VibrantbitLauncher

dotnet restore

dotnet run --project VibrantbitLauncher.csproj
```

### 发布单文件可执行程序

项目已配置单文件发布（非自包含，运行机器需安装 .NET 10 Desktop Runtime）：



```
dotnet publish VibrantbitLauncher.csproj -c Release -r win-x64 ^

\&#x20; -p:PublishSingleFile=true -p:SelfContained=false
```

产物输出于 `bin/Release/net10.0-windows7.0/win-x64/publish/`。

## 使用说明



1. 首次启动后进入 **设置**，确认或选择 `.minecraft` 目录与 Java 路径。

2. 在 **账户** 页登录你的微软 / 离线 / Yggdrasil 账户。

3. 在 **下载** 页选择需要的 Minecraft 版本进行安装。

4. 需要模组时进入 **模组** 页，按版本和加载器筛选并下载。

5. 在 **运行** 页选择版本点击启动；多人游戏可在 **多人** 页按提示安装 EasyTier 后与好友组网。

## 致谢



* [lepoco/wpfui](https://github.com/lepoco/wpfui) — Fluent 风格 WPF 控件库

* MinecraftLaunch（NuGet 包）— Minecraft 启动核心库，负责版本解析、账户认证与进程拉起

* [EasyTier](https://github.com/EasyTier/EasyTier) — 去中心化 P2P 组网工具

* [Modrinth](https://modrinth.com) — 模组分发平台

> Minecraft 是 Mojang Studios 的注册商标。本项目与 Mojang / Microsoft 无隶属关系。

## 许可证

本项目基于 **GNU Affero 通用公共许可证 v3.0（AGPL-3.0）** 开源，详见 [LICENSE.txt](./LICENSE.txt)。