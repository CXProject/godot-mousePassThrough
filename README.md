## Godot 要求
- Godot.net 4.6 或更高版本
## Setup
1. 将pass_through_manager文件夹复制到目标项目的addon文件夹下（没有就新建一个）。
2. 点击一下Build project小锤子按钮（Alt+B）编译项目。
3. 在项目设置/插件中启用PassThroughManager插件。
4. 在项目设置/全局中添加：res://addons/pass_through_manager/PassthroughManager.cs脚本作为全局变量。

## API
1. PassthroughManager.Initialize(window，max_depth, max_item_count)：  
   初始化或重置函数，window为Godot窗口对象，max_depth为最大深度，max_item_count为最大物品数量。
在游戏主场景开始时或者窗口大小改变时，调用此函数。
  
2. public void RegisterPolygon2DClickArea(Polygon2D poly)：  
   注册一个Polygon2D作为可点击不可穿透区域。
3. public void UnregisterPolygon2DClickArea(Polygon2D poly)：  
   注销一个Polygon2D作为可点击不可穿透区域。
4. public void UpdatePolygon2DClickArea(Polygon2D poly)：  
   注销一个Polygon2D作为可点击不可穿透区域。
  
5. public void RegisterCollisionPolygon2DClickArea(CollisionPolygon2D poly)：  
   注册一个CollisionPolygon2D作为可点击不可穿透区域。
6. public void UnregisterCollisionPolygon2DClickArea(CollisionPolygon2D poly)：  
   注销一个CollisionPolygon2D作为可点击不可穿透区域。
7. public void UpdateCollisionPolygon2DClickArea(CollisionPolygon2D poly)：  
   注销一个CollisionPolygon2D作为可点击不可穿透区域。
  
8.  public bool ForceClickable{get;set;}
   强制鼠标不可穿透，默认值为false。

## 参考
https://github.com/Darnoman/Godot-Clickthrough-Addon