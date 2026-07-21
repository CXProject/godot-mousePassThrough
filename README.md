## 更新日志

### v1.1.1

**新增 Control 点击区域支持**

- 新增 `ControlItem`，使用 `Control.GetGlobalRect()` 获取全局矩形范围，支持 `Button`、`ColorRect` 等 `Control` 派生节点的鼠标穿透命中检测
- 新增 `RegisterControlClickArea(Control ctrl)`，用于注册 UI 点击区域
- 新增统一入口 `RegisterClickArea(CanvasItem root)`，可自动识别并注册 `Control`、`Polygon2D`、`CollisionPolygon2D` 和 `CollisionShape2D`
- `UpdateClickArea` 与 `UnregisterClickArea` 的参数类型扩展为 `CanvasItem`，Control 节点也可使用相同的更新和注销流程
- 新增 `HasClickAreaAtPoint(Vector2 pos)`，便于测试和调试已注册区域的命中结果
- 完善 `testScene_with_controlitem.tscn` 示例：按钮点击与鼠标进入色块时会交替换色，并在启动时自动验证 Control 点击区域

### v1.1.0

**fix bug: CollisionShape2DItem memory leak**
- 修复 `CollisionShape2DItem.IsHit()` 方法中的内存泄漏：原实现每次命中检测都通过 `new CircleShape2D` 创建临时点形状对象，高频调用下导致持续分配和 GC 压力
- **优化方案**：将点形状对象提升为类级别 `readonly` 字段 `_point_shape`，在实例化时创建一次并复用，避免重复分配

**fix bug: Camera 属性安全性**
- 修复 `Camera` 公共属性的安全性问题：将内部存储字段改为私有 `_camera`，getter 添加 `GodotObject.IsInstanceValid()` 校验，防止访问已释放的相机节点导致异常

**架构重构：世界空间四叉树 + 相机查询过滤**
- 将四叉树从与相机绑定的视口空间改为独立的世界空间构建，相机仅作为每帧查询过滤器
- 新增 `QueryByRect(Rect2)` 方法，支持按矩形范围批量查询候选区域
- 命中检测改为两阶段流程：先通过相机视口矩形裁剪筛选候选区域，再对候选执行精确的点命中测试
- 移除 `RebuildData` 中对相机位置/缩放的依赖，`SetMainCamera` 不再触发四叉树重建

**四叉树动态扩缩容**
- **延迟初始化**：根节点在首次 Insert 时才按物体范围创建，避免预设大范围浪费深度层
- **动态扩展（ExpandRootToFit）**：物体超出根节点范围时自动向上包裹新父节点，保证至少 2 倍大小以容纳旧子树；若偏移导致无法放入任何象限则兜底重插入所有物品
- **智能收缩（ShrinkRoot）**：所有内容集中在单个子象限时提升为新根；若根节点持有跨象限物品则阻止收缩，防止数据丢失
- 收缩逻辑已集成到 `UnregisterClickArea`、`UpdateClickArea`、`UpdateAllClickArea`

**CanvasLayer 支持**
- 新增双路径存储架构，同时支持屏幕空间物品和世界空间物品：
  - `FollowViewport = false` 的 CanvasLayer 节点归入屏幕空间列表，使用视口坐标命中检测
  - `FollowViewport = true` 的 CanvasLayer 节点归入世界四叉树，使用世界坐标命中检测
- 新增 `CheckCanvasLayer` 方法，自动检测节点所属 CanvasLayer 类型并分类存储
- 命中检测改为两阶段流程：先遍历屏幕空间物品（优先级更高），再通过四叉树查询世界空间物品
- 注册 `FollowViewportScale != 1.0` 的物品时输出警告日志，提示点击检测可能不准确
- `IQuadTreeItem` 接口新增 `RootNode` 属性，支持从物品侧向上查找父级 CanvasLayer

**API 变化**
- **`SetMainCamera` 签名简化**: 移除 `keepExistingAreas` 参数，仅保留 `Camera2D camera` 参数

---

## 已知限制

### CanvasLayer `FollowViewportScale` 非标准值暂不支持

当 `CanvasLayer.FollowViewportEnabled = true` 且 `FollowViewportScale != 1.0` 时，该层内节点的坐标变换为混合坐标系（位置跟随相机但缩放速率不同），与四叉树的世界坐标查询和屏幕坐标查询均无法正确对齐。

**当前行为**：此类物品会被归入四叉树（世界空间），但命中检测可能不准确。
**建议**：如需使用非 1.0 的 `FollowViewportScale`，暂时将 `FollowViewportEnabled` 设为 `false`，物品将走屏幕空间路径正常工作。

## TODO

- [ ] 支持 CanvasLayer `FollowViewport=true, Scale!=1.0` 的混合坐标系点击检测

---

## Godot 要求
- Godot.net 4.6 或更高版本
## Setup
1. 将pass_through_manager文件夹复制到目标项目的addon文件夹下（没有就新建一个）。
2. 点击一下Build project小锤子按钮（Alt+B）编译项目。
3. 在项目设置/插件中启用PassThroughManager插件。
4. 在项目设置/全局中添加：res://addons/pass_through_manager/PassthroughManager.cs脚本作为全局变量。

## 类 API

### 属性 (Properties)

#### `static PassthroughManager Instance { get; }`
- **类型**: PassthroughManager（静态属性）
- **说明**: 获取 PassthroughManager 的单例实例
- **用途**: 全局访问入口点
- **示例**:
  ```csharp
  PassthroughManager.Instance.Initialize(GetViewport().GetWindow());
  ```

#### `QuadTree QuadTree { get; }`
- **类型**: QuadTree（只读属性）
- **说明**: 获取四叉树数据结构实例，用于存储和查询点击区域
- **用途**: 内部数据结构，用于高效的空间查询和碰撞检测
- **示例**:
  ```csharp
  var hitItem = PassthroughManager.Instance.QuadTree.GetHitItem(mousePos);
  ```

#### `bool ForceClickable { get; }`
- **类型**: bool（只读属性）
- **说明**: 判断是否有强制可点击的节点存在
- **返回**: `true` 如果有强制可点击节点，`false` 否则
- **用途**: 在鼠标穿透逻辑中判断是否忽略穿透计算

---

### 信号 (Signals)

#### `QuadTreeUpdate`
- **代理**: `delegate void QuadTreeUpdateEventHandler()`
- **说明**: 当四叉树更新时触发的信号
- **用途**: 外部系统可以监听此信号以响应点击区域的变化
- **示例**:
  ```csharp
  PassthroughManager.Instance.QuadTreeUpdate += OnQuadTreeUpdated;
  ```

---

### 方法 (Methods)

#### `Initialize(Window window, Camera2D camera = null, int maxDepth = 7, int maxItemCount = 1, bool keepExistingAreas = true)`
- **参数**:
  - `window` (Window): 目标窗口对象
  - `camera` (Camera2D, 可选): 主相机对象，默认 null
  - `maxDepth` (int, 可选): 四叉树最大深度，默认 7
  - `maxItemCount` (int, 可选): 单个节点最大项目数，默认 1
  - `keepExistingAreas` (bool, 可选): 是否保留现有区域，默认 true
- **返回值**: void
- **说明**: 重置或重建 PassthroughManager，初始化四叉树和平台相关的穿透提供者
- **用途**: 在场景加载或重启时初始化鼠标穿透功能
- **备注**: 
  - Windows 平台使用 `WindowsPassthroughProvider`
  - 其他平台使用 `DefaultPassthroughProvider`
- **示例**:
  ```csharp
  PassthroughManager.Instance.Initialize(GetViewport().GetWindow());
  ```

#### `SetMainCamera(Camera2D camera)`
- **参数**:
  - `camera` (Camera2D): 主相机对象
- **返回值**: void
- **说明**: 设置主相机用于视口裁剪查询
- **用途**: 四叉树以世界空间构建，不与相机绑定。相机仅作为每帧的查询过滤器，将世界坐标的鼠标位置转换为相机视口矩形进行区域筛选
- **示例**:
  ```csharp
  var newCamera = GetNode<Camera2D>("NewCamera");
  PassthroughManager.Instance.SetMainCamera(newCamera);
  ```

#### `RegisterPolygon2DClickArea(Polygon2D poly)`
- **参数**:
  - `poly` (Polygon2D): 要注册的 Polygon2D 节点
- **返回值**: void
- **说明**: 注册一个 Polygon2D 节点为可点击区域
- **用途**: 将 Polygon2D 形状添加到点击检测系统
- **备注**: 若已注册则自动更新区域而非重复添加
- **示例**:
  ```csharp
  var polygon = GetNode<Polygon2D>("MyPolygon");
  PassthroughManager.Instance.RegisterPolygon2DClickArea(polygon);
  ```

#### `RegisterControlClickArea(Control ctrl)`
- **参数**:
  - `ctrl` (Control): 要注册的 Control 节点
- **返回值**: void
- **说明**: 使用 Control 的全局矩形范围注册 UI 点击区域
- **用途**: 支持 `Button`、`ColorRect` 等 Control 派生节点的鼠标穿透命中检测
- **备注**: Control 的位置或尺寸变化后，需要调用 `UpdateClickArea` 刷新区域
- **示例**:
  ```csharp
  var button = GetNode<Button>("MyButton");
  PassthroughManager.Instance.RegisterControlClickArea(button);
  ```

#### `RegisterClickArea(CanvasItem root)`
- **参数**:
  - `root` (CanvasItem): 要注册的点击区域节点
- **返回值**: void
- **说明**: 根据节点类型自动选择对应的注册方法
- **支持类型**: `Control`、`Polygon2D`、`CollisionPolygon2D`、`CollisionShape2D`
- **示例**:
  ```csharp
  var clickable = GetNode<CanvasItem>("Clickable");
  PassthroughManager.Instance.RegisterClickArea(clickable);
  ```

#### `RegisterCollisionPolygon2DClickArea(CollisionPolygon2D poly)`
- **参数**:
  - `poly` (CollisionPolygon2D): 要注册的 CollisionPolygon2D 节点
- **返回值**: void
- **说明**: 注册一个 CollisionPolygon2D 节点为可点击区域
- **用途**: 将碰撞多边形形状添加到点击检测系统
- **备注**: 若已注册则自动更新区域而非重复添加
- **示例**:
  ```csharp
  var collisionPoly = GetNode<CollisionPolygon2D>("MyCollisionPoly");
  PassthroughManager.Instance.RegisterCollisionPolygon2DClickArea(collisionPoly);
  ```

#### `RegisterCollisionShape2DClickArea(CollisionShape2D shape)`
- **参数**:
  - `shape` (CollisionShape2D): 要注册的 CollisionShape2D 节点
- **返回值**: void
- **说明**: 注册一个 CollisionShape2D 节点为可点击区域
- **用途**: 将碰撞形状添加到点击检测系统
- **备注**: 若已注册则忽略重复注册
- **示例**:
  ```csharp
  var shape = GetNode<CollisionShape2D>("MyShape");
  PassthroughManager.Instance.RegisterCollisionShape2DClickArea(shape);
  ```

#### `UnregisterClickArea(CanvasItem root)`
- **参数**:
  - `root` (CanvasItem): 要注销的节点
- **返回值**: void
- **说明**: 注销一个已注册的可点击区域
- **用途**: 从点击检测系统移除节点（如节点被删除或不再需要点击）
- **备注**: 若节点未注册则不做任何操作
- **示例**:
  ```csharp
  PassthroughManager.Instance.UnregisterClickArea(node);
  ```

#### `UpdateClickArea(CanvasItem root)`
- **参数**:
  - `root` (CanvasItem): 要更新的节点
- **返回值**: void
- **说明**: 更新单个已注册节点的点击区域
- **用途**: 当节点位置、旋转或形状改变时调用，确保碰撞检测信息最新
- **备注**: 若节点未注册则不做任何操作
- **示例**:
  ```csharp
  PassthroughManager.Instance.UpdateClickArea(node);
  ```

#### `UpdateAllClickArea()`
- **参数**: 无
- **返回值**: void
- **说明**: 更新所有已注册的点击区域
- **用途**: 在需要刷新整个点击系统时调用（如大规模更新）
- **示例**:
  ```csharp
  PassthroughManager.Instance.UpdateAllClickArea();
  ```

#### `HasClickAreaAtPoint(Vector2 pos)`
- **参数**:
  - `pos` (Vector2): 要检测的坐标点
- **返回值**: bool
- **说明**: 检查指定点是否命中任一已注册点击区域，主要用于测试和调试
- **示例**:
  ```csharp
  bool isClickable = PassthroughManager.Instance.HasClickAreaAtPoint(mousePosition);
  ```

---

## 接口 API

### `IPassthroughProvider` 接口

提供平台相关的鼠标穿透实现

#### `void Initialize(Window window)`
- **参数**: `window` (Window) - 目标窗口
- **说明**: 初始化穿透提供者
- **用途**: 与操作系统的窗口系统交互

#### `void SetClickthrough(bool clickthrough)`
- **参数**: `clickthrough` (bool) - true 启用穿透，false 禁用穿透
- **说明**: 设置窗口是否启用鼠标穿透
- **用途**: 控制操作系统级别的鼠标穿透行为

---

## 工作流示例

### 基本初始化
```csharp
// 1. 初始化管理器
PassthroughManager.Instance.Initialize(GetViewport().GetWindow());

// 2. 注册可点击区域
var polygon = GetNode<Polygon2D>("ClickableArea");
PassthroughManager.Instance.RegisterPolygon2DClickArea(polygon);

// 3. 监听更新信号
PassthroughManager.Instance.QuadTreeUpdate += OnClickAreasUpdated;
```

### 动态更新区域
```csharp
// 当节点移动时，更新其点击区域
if (nodePosition != previousPosition)
{
	PassthroughManager.Instance.UpdateClickArea(node);
}
```

### 注销区域
```csharp
// 当节点删除时
public override void _ExitTree()
{
	PassthroughManager.Instance.UnregisterClickArea(this);
}
```

---

## 内部实现细节

### 架构概览

插件采用 **世界空间四叉树 + 相机查询过滤** 的两阶段架构：

1. **四叉树（QuadTree）**: 以世界坐标构建，存储所有注册的点击区域，与相机完全解耦
2. **相机（Camera）**: 仅作为每帧的查询过滤器，不参与四叉树结构
3. **两阶段命中检测**:
   - **阶段一**：根据相机视口的世界矩形 (`QueryByRect`) 快速筛选候选区域
   - **阶段二**：对候选区域内的物品执行精确的点命中测试 (`IsHit`)

### 四叉树特性

- **延迟初始化**：根节点在首次 `Insert` 时才创建，避免预设大范围浪费深度
- **动态扩展（ExpandRootToFit）**：当新物体超出当前根节点范围时，自动向上包裹新的父节点
  - 保证新根至少为旧根 2 倍大小，确保旧子树能放入单个象限
  - 若因偏移导致无法放入任何象限，触发 `CollectAllItems` 兜底重插入所有物品
- **智能收缩（ShrinkRoot）**：当所有内容集中在单个子象限时，提升该子象限为新根
  - **安全约束**：若根节点自身持有跨象限物品（`quadTreeItems` 不为空），则阻止收缩，防止丢失数据
- **跨象限处理**：无法放入任何单个子象限的物品会保留在父节点的 `quadTreeItems` 中

### 核心字段

- **_clickAreas**: 存储所有注册的点击区域（字典，key为实例ID）
- **_provider**: 平台相关的穿透提供者实例（Windows 使用 `WindowsPassthroughProvider`，其他平台使用 `DefaultPassthroughProvider`）
- **_forceClickableNodes**: 强制可点击节点集合
- **QuadTree**: 世界空间四叉树实例，支持动态扩缩容

---

## 关键特性

✅ **世界空间四叉树**: 与相机完全解耦，以世界坐标构建，支持动态扩缩容  
✅ **两阶段查询**: 视口矩形裁剪 → 精确命中测试，高效且准确  
✅ **动态根节点扩展/收缩**: 自动适应物体分布，保持四叉树紧凑和高效  
✅ **跨平台支持**: Windows 使用原生 API (`WS_EX_TRANSPARENT`)，其他平台使用 Godot 的 `MousePassthroughPolygon`  
✅ **单例模式**: 全局唯一实例（AutoLoad），便于访问  
✅ **信号系统**: 集成 Godot 信号（`QuadTreeUpdate`），方便外部监听  
✅ **灵活注册**: 支持 UI 与多种碰撞形状（`Control`、`Polygon2D`、`CollisionPolygon2D`、`CollisionShape2D`）

## 参考
https://github.com/Darnoman/Godot-Clickthrough-Addon
