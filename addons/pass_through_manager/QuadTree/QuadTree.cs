using Godot;
using System;
using System.Collections.Generic;

public interface IQuadTreeItem
{
	/// <summary>
	/// 当前所在的节点，方便快速移除/更新
	/// </summary>
	public QuadTreeNode CurrentNode { get; set; }

	/// <summary>
	/// 物体的包围矩形，用于决定它应该位于哪一个子节点中。
	/// 如果物体跨越多个子节点，这个矩形可能不被任何一个子节点完全包含，
	/// 那么该物体会留在父节点中。
	/// </summary>
	public Rect2 Bounds { get; }

	/// <summary>
	/// 给定一个点，判断该物体是否被命中。四叉树本身并不知道物体的具体形状，
	/// 只依赖于调用者提供的这个方法。
	/// </summary>
	public bool IsHit(Vector2 pos);

	/// <summary>
	/// 物体的标识,标识一致则来自同一元，算一个物体
	/// </summary>
	public ulong ItemID { get; }

	public void Update();
}

/// <summary>
/// 四叉树节点
/// </summary>
public class QuadTreeNode
{
	public Rect2 Rect { get; private set; }
	/// <summary>
	/// 节点包含的物品列表
	/// 如果其不是叶子节点，说明这些物品横跨了数个子节点
	/// </summary>
	public List<IQuadTreeItem> quadTreeItems = new List<IQuadTreeItem>();
	/// <summary>
	/// 父节点
	/// </summary>
	public QuadTreeNode parent;
	/// <summary>
	/// 子节点列表，要么没有（就是叶子节点），要么就有左上，右上，左下，右下四个子节点
	/// </summary>
	public List<QuadTreeNode> children = new List<QuadTreeNode>();

	public QuadTreeNode(Rect2 rect, QuadTreeNode parent = null)
	{
		Rect = rect;
		this.parent = parent;
	}
}

/// <summary>
/// 四叉树实现
/// </summary>
public class QuadTree
{
	public QuadTreeNode Root { get; private set; }

	//叶子节点最大深度，达到这个深度就不会再分割下去。
	public int _maxDepth;

	//每个节点的最大物体数，超过这个数且没达到最大深度就分割下去
	public int _maxItemCount;

	/// <summary>
	/// 创建四叉树，根节点延迟到首次 Insert 时按物体范围自动确定
	/// </summary>
	public QuadTree(int maxDepth, int maxItemCount)
	{
		Root = null;
		_maxDepth = maxDepth;
		_maxItemCount = maxItemCount;
	}

	/// <summary>
	/// 插入新的物体
	/// </summary>
	/// <param name="item"></param>
	public void Insert(IQuadTreeItem item)
	{
		if (item == null)
			return;

		// 如果之前已经在树中，先把它移除
		if (item.CurrentNode != null)
			Remove(item);

		// 延迟初始化或动态扩展根节点以容纳新物体
		if (Root == null)
		{
			Root = new QuadTreeNode(item.Bounds);
			InsertInternal(Root, item, 0);
			return;
		}

		ExpandRootToFit(item.Bounds);
		InsertInternal(Root, item, 0);
	}

	/// <summary>
	/// 动态扩展根节点使其能容纳给定的边界矩形
	/// 通过在旧根上方包裹新的父节点来实现
	/// </summary>
	private void ExpandRootToFit(Rect2 bounds)
	{
		while (!ContainsRect(Root.Rect, bounds))
		{
			var current = Root.Rect;
			var curEnd = current.Position + current.Size;
			var boundsEnd = bounds.Position + bounds.Size;

			// 计算能同时包含当前根和新物体的最小 AABB
			var minX = Math.Min(current.Position.X, bounds.Position.X);
			var minY = Math.Min(current.Position.Y, bounds.Position.Y);
			var maxX = Math.Max(curEnd.X, boundsEnd.X);
			var maxY = Math.Max(curEnd.Y, boundsEnd.Y);

			// 取较大边确保正方形（四叉树要求）
			var sizeX = maxX - minX;
			var sizeY = maxY - minY;
			var maxSize = Math.Max(sizeX, sizeY);

			// 新根必须至少是旧根的2倍（尽量让旧根能放入一个象限）
			var minExpandSize = Math.Max(current.Size.X, current.Size.Y) * 2;
			maxSize = Math.Max(maxSize, minExpandSize);

			var newRect = new Rect2(minX, minY, maxSize, maxSize);

			// 创建新根并分裂为4个子象限
			var newRoot = new QuadTreeNode(newRect);
			Subdivide(newRoot);

			// 尝试找到旧根属于哪个象限，将旧根替换进去（保留其完整子树）
			bool foundQuadrant = false;
			for (int i = 0; i < newRoot.children.Count; i++)
			{
				if (ContainsRect(newRoot.children[i].Rect, current))
				{
					newRoot.children[i] = Root;
					Root.parent = newRoot;
					foundQuadrant = true;
					break;
				}
			}

			if (!foundQuadrant)
			{
				// 旧根无法放入任何单个象限（因偏移导致），收集所有物品重新插入
				var allItems = CollectAllItems(Root);
				Root = newRoot;
				foreach (var item in allItems)
				{
					item.CurrentNode = null;
					InsertInternal(Root, item, 0);
				}
			}
			else
			{
				Root = newRoot;
			}
		}
	}

	/// <summary>
	/// 收集树中所有物品（用于扩展失败时的兜底重插入）
	/// </summary>
	private List<IQuadTreeItem> CollectAllItems(QuadTreeNode node)
	{
		var items = new List<IQuadTreeItem>();
		CollectAllItemsInternal(node, items);
		return items;
	}

	private void CollectAllItemsInternal(QuadTreeNode node, List<IQuadTreeItem> items)
	{
		items.AddRange(node.quadTreeItems);
		foreach (var child in node.children)
		{
			CollectAllItemsInternal(child, items);
		}
	}

	/// <summary>
	/// 移除物体
	/// </summary>
	/// <param name="item"></param>
	public void Remove(IQuadTreeItem item)
	{
		if (item == null || item.CurrentNode == null) return;

		var node = item.CurrentNode;
		if (node.quadTreeItems.Remove(item))
		{
			item.CurrentNode = null;
		}

		// 判定是否需要合并
		// 先对当前节点进行判断
		TryMarge(node);
		// 再对父节点进行判断
		if (node.parent == null) return;
		TryMarge(node.parent);

		void TryMarge(QuadTreeNode n)
		{
			ids.Clear();
			GetItemIdsInternal(n, ids);
			if (ids.Count <= _maxItemCount)
			{
				//合并子节点
				MargeNode(n, n);
				n.children.Clear();
			}
		}
	}

	/// <summary>
	/// 收缩根节点：当所有物品都集中在某个子象限时，将该子象限提升为新的根。
	/// 可递归收缩多层，直到物品分散在多个象限或到达叶子节点。
	/// 通常在 Remove/Unregister 后调用以保持树紧凑。
	/// </summary>
	public void ShrinkRoot()
	{
		if (Root == null || Root.children.Count == 0) return;

		while (Root.children.Count > 0)
		{
			// 找到唯一包含内容的子象限（其他3个必须完全为空）
			QuadTreeNode activeChild = null;
			bool canShrink = true;

			for (int i = 0; i < Root.children.Count; i++)
			{
				var child = Root.children[i];
				if (HasAnyContent(child))
				{
					if (activeChild == null)
					{
						activeChild = child;
					}
					else
					{
						// 多个子象限有内容，无法再收缩
						canShrink = false;
						break;
					}
				}
			}

			if (!canShrink || activeChild == null) break;

			// 根节点自身持有跨象限的物品，不允许收缩
			if (Root.quadTreeItems.Count > 0) break;

			// 将活跃子象限提升为新根
			activeChild.parent = null;
			Root = activeChild;
		}
	}

	private bool HasAnyContent(QuadTreeNode node)
	{
		if (node.quadTreeItems.Count > 0) return true;
		foreach (var child in node.children)
		{
			if (HasAnyContent(child)) return true;
		}
		return false;
	}

	/// <summary>
	/// 更新物体的位置
	/// </summary>
	/// <param name="item"></param>
	public bool Update(IQuadTreeItem item)
	{
		//先判定是否还在Node中，如果不在，则更新
		if (ContainsRect(item.CurrentNode.Rect, item.Bounds))
		{
			//还在节点内，但是如果有子节点，再判定是否可以移动到子节点中，可以的话就要更新
			var child = FindContainingChild(item.CurrentNode, item.Bounds);
			if (child == null) return false;
		}
		// 物体可能已经移动或者大小改变，最简单的处理方式是重新插入
		Remove(item);
		Insert(item);
		return true;
	}

	/// <summary>
	/// 获取第一个命中物体
	/// </summary>
	/// <param name="pos"></param>
	/// <returns></returns>
	public IQuadTreeItem GetHitItem(Vector2 pos)
	{
		if (Root == null) return null;
		return SearchFirst(Root, pos);
	}

	/// <summary>
	/// 获取所有命中物体列表
	/// </summary>
	/// <param name="pos"></param>
	/// <returns></returns>
	public List<IQuadTreeItem> GetHitItems(Vector2 pos)
	{
		var items = new List<IQuadTreeItem>();
		if (Root == null) return items;
		Search(Root, pos, items);
		return items;
	}

	/// <summary>
	/// 按矩形范围查询所有与该矩形相交的物体（用于相机视口裁剪查询）
	/// </summary>
	public List<IQuadTreeItem> QueryByRect(Rect2 queryRect)
	{
		var items = new List<IQuadTreeItem>();
		if (Root == null) return items;
		var checkedIds = new HashSet<ulong>();
		QueryByRectInternal(Root, queryRect, items, checkedIds);
		return items;
	}

	private void QueryByRectInternal(QuadTreeNode node, Rect2 queryRect, List<IQuadTreeItem> results, HashSet<ulong> _checkedId)
	{
		if (!node.Rect.Intersects(queryRect))
			return;

		foreach (var item in node.quadTreeItems)
		{
			if (_checkedId.Contains(item.ItemID) == false)
			{
				results.Add(item);
				_checkedId.Add(item.ItemID);
			}
		}

		if (node.children.Count > 0)
		{
			foreach (var child in node.children)
			{
				QueryByRectInternal(child, queryRect, results, _checkedId);
			}
		}
	}

	#region internal helpers

	private HashSet<ulong> ids = new HashSet<ulong>();
	private void InsertInternal(QuadTreeNode node, IQuadTreeItem item, int depth)
	{
		// 如果已经有子节点，优先尝试插入到一个完全包含item的子节点中
		if (node.children.Count > 0)
		{
			var child = FindContainingChild(node, item.Bounds);
			if (child != null)
			{
				InsertInternal(child, item, depth + 1);
				return;
			}
		}

		// 不能放入子节点，或者当前无子节点，放在本节点
		node.quadTreeItems.Add(item);
		item.CurrentNode = node;

		// 检查是否需要分裂

		if (node.children.Count == 0 && depth < _maxDepth)
		{
			ids.Clear();
			foreach (var it in node.quadTreeItems)
			{
				ids.Add(it.ItemID);
			}
			if (ids.Count > _maxItemCount)
			{
				Subdivide(node);
				// 尝试把现有的物品移动到子节点中
				for (int i = node.quadTreeItems.Count - 1; i >= 0; i--)
				{
					var it = node.quadTreeItems[i];
					var child = FindContainingChild(node, it.Bounds);
					if (child != null)
					{
						InsertInternal(child, it, depth + 1);
						node.quadTreeItems.RemoveAt(i);
					}
				}
			}

		}
	}

	private QuadTreeNode FindContainingChild(QuadTreeNode node, Rect2 bounds)
	{
		foreach (var child in node.children)
		{
			if (ContainsRect(child.Rect, bounds))
				return child;
		}
		return null;
	}

	private bool ContainsRect(Rect2 outer, Rect2 inner)
	{
		// 判断 inner 是否完全包含在 outer 中
		return outer.Position.X <= inner.Position.X &&
			outer.Position.Y <= inner.Position.Y &&
			outer.Position.X + outer.Size.X >= inner.Position.X + inner.Size.X &&
			outer.Position.Y + outer.Size.Y >= inner.Position.Y + inner.Size.Y;
	}

	private void Subdivide(QuadTreeNode node)
	{
		var r = node.Rect;
		var half = r.Size / 2f;

		// 左上
		node.children.Add(new QuadTreeNode(new Rect2(r.Position, half), node));
		// 右上
		node.children.Add(new QuadTreeNode(new Rect2(r.Position + new Vector2(half.X, 0), half), node));
		// 左下
		node.children.Add(new QuadTreeNode(new Rect2(r.Position + new Vector2(0, half.Y), half), node));
		// 右下
		node.children.Add(new QuadTreeNode(new Rect2(r.Position + half, half), node));
	}

	private IQuadTreeItem SearchFirst(QuadTreeNode node, Vector2 pos, HashSet<ulong> _checkedId = null)
	{
		if (_checkedId == null) _checkedId = new HashSet<ulong>();
		if (!node.Rect.HasPoint(pos))
			return null;

		foreach (var item in node.quadTreeItems)
		{
			if (_checkedId.Contains(item.ItemID) == false && item.IsHit(pos))
				return item;
			else
				_checkedId.Add(item.ItemID);
		}

		if (node.children.Count > 0)
		{
			foreach (var child in node.children)
			{
				if (child.Rect.HasPoint(pos))
				{
					var res = SearchFirst(child, pos, _checkedId);
					if (res != null)
						return res;
				}
			}
		}
		return null;
	}

	private void Search(QuadTreeNode node, Vector2 pos, List<IQuadTreeItem> results, HashSet<ulong> _checkedId = null)
	{
		if (!node.Rect.HasPoint(pos))
			return;

		foreach (var item in node.quadTreeItems)
		{
			if (_checkedId.Contains(item.ItemID) == false && item.IsHit(pos))
				results.Add(item);
			_checkedId.Add(item.ItemID);
		}

		if (node.children.Count > 0)
		{
			foreach (var child in node.children)
			{
				if (child.Rect.HasPoint(pos))
					Search(child, pos, results);
			}
		}
	}

	private void GetItemIdsInternal(QuadTreeNode node, HashSet<ulong> itemIds)
	{
		foreach (var item in node.quadTreeItems)
		{
			itemIds.Add(item.ItemID);
		}

		if (node.children.Count == 0)
			return;

		foreach (var child in node.children)
		{
			GetItemIdsInternal(child, itemIds);
		}
	}

	private void MargeNode(QuadTreeNode node, QuadTreeNode targetNode)
	{
		if (node.children.Count == 0) return;
		foreach (var child in node.children)
		{
			for (var i = child.quadTreeItems.Count - 1; i >= 0; i--)
			{
				child.quadTreeItems[i].CurrentNode = targetNode;
				targetNode.quadTreeItems.Add(child.quadTreeItems[i]);
			}
			MargeNode(child, targetNode);
		}
	}

	#endregion
}
