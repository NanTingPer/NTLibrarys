using System.Collections.Generic;

namespace AgentNexus.Core.Models;
/// <summary>
/// 创建带锁的List，在不使用<see cref="values"/>调用的情况下，都是带有lock的
/// </summary>
/// <typeparam name="T"></typeparam>
public class LockList<T>
{
    /// <summary>
    /// 实际维护的集合
    /// </summary>
    public readonly List<T> values;
    private readonly object lockLock = new();
    /// <summary>
    /// 创建带锁的List
    /// </summary>
    public LockList()
    {
        values = new List<T>();
    }
    /// <summary>
    /// 删除给定元素
    /// </summary>
    /// <param name="content">要被删除的元素</param>
    /// <returns>如果成功则返回true, 否则返回false </returns>
    public bool Remove(T content)
    {
        bool b;
        lock (lockLock) {
            b = values.Remove(content);
        }
        return b;
    }

    /// <summary>
    /// 将给定的元素添加到集合
    /// </summary>
    /// <param name="content">要被添加的元素</param>
    public void Add(T content)
    {
        lock (lockLock) {
            values.Add(content);
        }
    }
    /// <summary>
    /// 将此类型隐式转换为<see cref="List{T}"/>
    /// </summary>
    /// <param name="value">此类维护的数组</param>
    public static implicit operator List<T>(LockList<T> value) => value.values;

    /// <summary>
    /// 使用索引获取数据
    /// </summary>
    /// <param name="index">目标索引</param>
    /// <returns></returns>
    public T this[int index]
    {
        get
        {
            return values[index];
        }
    }

    /// <summary>
    /// 删除给定索引的数据
    /// </summary>
    /// <param name="index">要被删除数据的索引</param>
    public void Remove(int index)
    {
        lock (lockLock) {
            values.RemoveAt(index);
        }
    }

    /// <summary>
    /// 获取内容数量
    /// </summary>
    /// <returns></returns>
    public int Count => values.Count;

    /// <summary>
    /// 清空<see cref="values"/>存储的内容
    /// <para> 如果要线程安全定调用此方法 </para>
    /// </summary>
    public void Clear()
    {
        lock (lockLock) {
            values.Clear();
        }
    }
}
