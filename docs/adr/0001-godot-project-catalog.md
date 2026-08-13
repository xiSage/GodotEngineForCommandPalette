# ADR-0001: 项目发现深化为 GodotProjectCatalog 深层模块

项目发现的全部格式解码（projects.cfg、project.godot、uid 缓存、res:// 图标解析）原先内联在 ListPage 与 ListItem 构造函数里，最近的两次 bug 修复（加载错误、icon 短串崩溃 `icon[6..]`）都落在这条 seam 上。我们决定将其抽成 GodotProjectCatalog：单方法接口 `FindProjects(string godotDataPath) → GodotProject[]`，icon 解析（含 uid→res://→绝对路径决策树与文件存在性检查）折叠进内部，单项目失败以 `GodotProject.Error` 呈现，projects.cfg 读取失败返回空数组。输出保持 cfg 顺序；`config/name` 缺失时 Title 回退到项目目录名。文件访问经窄 `IFileSystem` 接口注入（生产真实适配器 + 测试内存适配器），使"接口即测试表面"，并为该项目首次引入 xunit 测试项目。

## 后续深化（同轮完成）

- **#4**：`GetCommandItem`（dock 支持）不再直读页面的 `ProjectItems`，改为从 catalog 按项目路径查询；页面 `ProjectItems` 收回 `private`。
- **#5**：launch（编辑/运行 Godot）从 ListItem 静态方法抽成 `GodotLauncher` 适配器，经 `IProcessStarter` 端口启动进程；参数构造成为可测接口。
- **#3**：设置持久化抽成 `GodotSettingsStore`（构造注入文件路径，Load/Save/Changed）；`GodotSettings` 收敛为纯数据 record。原静态 `SettingsChanged` 事件由**共享 store 实例**替代——组合根（命令 provider）创建 store 并注入设置 provider 与页面，使跨实例通知通过同一事件源完成，且每个测试可用独立临时路径。死代码 `ResetToDefaults`/`Reload` 删除。

测试 25 个，覆盖 catalog 解析各分支、store 往返/损坏回退/目录创建、launcher 参数构造与空路径静默。
