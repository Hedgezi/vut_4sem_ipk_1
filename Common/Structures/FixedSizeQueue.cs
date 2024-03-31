namespace vut_ipk1.Common.Structures;

/// <summary>
/// Data structure representing a fixed size queue,
/// for storing receive message IDs, for example. 
/// </summary>
/// <typeparam name="T">
/// Type of the elements stored in the queue
/// </typeparam>
public class FixedSizeQueue<T>
{
    private readonly Queue<T> queue = new Queue<T>();
    private readonly int maxSize;

    public FixedSizeQueue(int maxSize)
    {
        if (maxSize <= 0)
        {
            throw new ArgumentException("Max size must be greater than 0", nameof(maxSize));
        }
        
        this.maxSize = maxSize;
    }

    public void Enqueue(T item)
    {
        if (queue.Count >= maxSize)
        {
            queue.Dequeue(); // Remove the oldest element if the max size is reached
        }
        
        if (queue.Contains(item))
        {
            return;
        }
        
        queue.Enqueue(item); // Add the new item to the queue
    }

    // Dequeue method to remove and return the oldest element from the queue
    public T Dequeue()
    {
        if (queue.Count == 0)
        {
            throw new InvalidOperationException("Queue is empty");
        }
        
        return queue.Dequeue();
    }

    public int Count => queue.Count;

    public bool Contains(T item)
    {
        return queue.Contains(item);
    }
}