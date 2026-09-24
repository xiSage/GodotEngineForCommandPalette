# GodotEngineForCommandPalette

Windows 命令面板扩展：从 Godot 的应用数据目录发现已安装的 Godot 项目，供用户一键打开编辑器或直接运行。

## Language

**GodotProject**:
一个已被发现的 Godot 工程，可从命令面板打开或运行。含标题、绝对路径、图标路径，以及从 `project.godot` 读到的元数据（Godot 版本、features、主场景、配置文件格式，见「项目元数据契约」）。
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

**Project details / 项目详情**:
项目列表中被选中项旁边自动展开的详情面板，展示 catalog 从 `project.godot` 读到的元数据：项目图标、标题、绝对路径、Godot 版本、features 标签、主场景、配置文件格式。元数据在发现阶段就解析好，面板本身不做 I/O；缺失的键不渲染对应元素，图标解析不出则不显示图标，从不写「未知」或空占位。
_Avoid_: details view, info pane, properties

**Godot version / Godot 版本**:
`project.godot` 的 `application/config/features` 首项，形如 `4.4`。其余 features 项（渲染器、`C#`、`Double Precision` 等）作为特性标签按原顺序展示。
_Avoid_: engine version string, version tag

**Project metadata contract / 项目元数据契约**:
catalog 从 `project.godot` 读取的键及其在 `GodotProject` 上的落点；除此之外的键与 catalog 无关。

| `project.godot` 键 | `GodotProject` 成员 | 缺失时 |
| --- | --- | --- |
| `application/config/name` | `Title` | 回退为目录名 |
| `application/config/icon` | `IconPath` | 无图标 |
| `application/config/features` | `Features`（首项另存到 `GodotVersion`） | 不渲染版本与特性 |
| `application/run/main_scene` | `MainScene` | 不渲染主场景 |
| `config_version`（顶层键，无 section） | `ConfigVersion` | 不渲染配置文件格式 |

_Avoid_: metadata schema, field list
