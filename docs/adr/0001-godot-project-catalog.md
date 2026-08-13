# ADR-0001: 项目发现深化为 GodotProjectCatalog 深层模块

项目发现的全部格式解码（projects.cfg、project.godot、uid 缓存、res:// 图标解析）原先内联在 ListPage 与 ListItem 构造函数里，最近的两次 bug 修复（加载错误、icon 短串崩溃 `icon[6..]`）都落在这条 seam 上。我们决定将其抽成 GodotProjectCatalog：单方法接口 `FindProjects(string godotDataPath) → GodotProject[]`，icon 解析（含 uid→res://→绝对路径决策树与文件存在性检查）折叠进内部，单项目失败以 `GodotProject.Error` 呈现，projects.cfg 读取失败返回空数组。输出保持 cfg 顺序；`config/name` 缺失时 Title 回退到项目目录名。文件访问经窄 `IFileSystem` 接口注入（生产真实适配器 + 测试内存适配器），使"接口即测试表面"，并为该项目首次引入 xunit 测试项目。

**关键权衡**：launch 职责刻意留在页面（独立 launcher 候选）；provider 的 `GetCommandItem` 直读页面 `ProjectItems`（dock 支持）本次不动。
