namespace CMS.Collections
{
    /// <summary>
    /// Implementing this interface makes the class usable in <see cref="ObjectPool{T}"/>
    /// </summary>
    public interface IPoolable
    {
        void Reset();
    }
}
