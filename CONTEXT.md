# GodotEngineForCommandPalette

Windows 命令面板扩展：从 Godot 的应用数据目录发现已安装的 Godot 项目，供用户一键打开编辑器或直接运行。

## Language

**GodotProject**:
一个已被发现的 Godot 工程，可从命令面板打开或运行。含标题、绝对路径、图标路径。
_Avoid_: project item, project entry, project file

**Project Discovery / 项目发现**:
从 Godot data path 下的 `projects.cfg` 扫描项目，并逐个解析其 `project.godot` 元数据的过程。项目级失败以单个记录上的 Error 呈现，不中断整体发现。
_Avoid_: scanning, loading, project listing

**GodotProjectCatalog**:
负责项目发现的模块。输入 data path，输出 `GodotProject` 记录数组；内部隐藏 cfg/uid 缓存/图标解析的全部格式知识。
_Avoid_: project reader, project scanner

**Godot data path / Godot editor path**:
用户在设置中配置的两个路径。Godot data path 指向 Godot 应用数据目录（含 `projects.cfg`）；Godot editor path 指向 Godot 编辑器可执行文件，用于打开/运行项目。
_Avoid_: data folder, exe path

**Project icon / 项目图标**:
`project.godot` 的 `config/icon` 声明的图标。经 uid 缓存与 `res://` 前缀解析为可显示的文件路径；解析失败或文件缺失则为无图标。
_Avoid_: icon path, icon string

**Project favorite / 项目收藏**:
`projects.cfg` 中由 Godot 编辑器维护的 `favorite` 布尔标志。扩展只读该标志，仅用于排序；从不写回该文件。
_Avoid_: pinned, starred, favorite list

**Project list order / 项目列表顺序**:
项目列表的显示顺序，由排序方式与收藏置顶共同决定。catalog 输出恒为 cfg 顺序，排序发生在页面层。
_Avoid_: sorted list, ordering

**Sort mode / 排序方式**:
用户选择的排序规则：配置顺序（默认，即 `projects.cfg` 中的出现顺序）或名称/路径的 A→Z/Z→A。比较器为文化感知。
_Avoid_: sort order setting, ordering mode
