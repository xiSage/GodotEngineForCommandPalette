# ADR-0002: 项目列表顺序是展示关切，收藏只读

项目列表此前恒为 cfg 顺序（ADR-0001）。新增"修改排序方式"功能时，我们决定：排序逻辑放在页面层（纯函数 `ProjectSorter.Sort`），catalog 输出契约（cfg 顺序）保持不变。收藏标志（`projects.cfg` 的 `favorite`）由 Godot 编辑器维护，catalog 只读解析为 `GodotProject.IsFavorite`，扩展从不写回该文件。

理由：排序是展示关切而非发现关切，塞进 catalog 会污染其深层模块边界、并迫使按 id 查询的 `GetCommandItem` 处理无关排序参数。收藏归属编辑器，写回会与编辑器冲突且破坏 catalog 只读性。

行为约定：错误项目恒钉列表底部；"收藏置顶"默认开启，收藏组内跟随所选排序方式；排序采用文化感知比较器，同名按路径平局。
