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

#### `Initialize(Window window, int maxDepth = 7, int maxItemCount = 1, bool keepExistingAreas = true)`
- **参数**:
  - `window` (Window): 目标窗口对象
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

#### `UnregisterClickArea(Node2D root)`
- **参数**:
  - `root` (Node2D): 要注销的节点
- **返回值**: void
- **说明**: 注销一个已注册的可点击区域
- **用途**: 从点击检测系统移除节点（如节点被删除或不再需要点击）
- **备注**: 若节点未注册则不做任何操作
- **示例**:
  ```csharp
  PassthroughManager.Instance.UnregisterClickArea(node);
  ```

#### `UpdateClickArea(Node2D root)`
- **参数**:
  - `root` (Node2D): 要更新的节点
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

- **_clickAreas**: 存储所有注册的点击区域（字典，key为实例ID）
- **_provider**: 平台相关的穿透提供者实例
- **_forceClickableNodes**: 强制可点击节点集合
- **_isUpdated**: 标记四叉树是否需要更新

---

## 关键特性

✅ **四叉树优化**: 使用四叉树实现高效的空间查询  
✅ **跨平台支持**: Windows 使用原生 API，其他平台有默认实现  
✅ **单例模式**: 全局唯一实例，便于访问  
✅ **信号系统**: 集成 Godot 信号，方便外部监听  
✅ **灵活注册**: 支持多种碰撞形状（Polygon2D、CollisionPolygon2D、CollisionShape2D）  

## 参考
https://github.com/Darnoman/Godot-Clickthrough-Addon
