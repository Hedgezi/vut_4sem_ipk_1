namespace vut_ipk1.Common.Structures;

public class FixedSizeQueue<T>
{
    private readonly Queue<T> queue = new Queue<T>(); // Internal queue to store elements
    private readonly int maxSize; // Maximum size of the queue

    // Constructor takes the maximum size of the queue
    public FixedSizeQueue(int maxSize)
    {
        if (maxSize <= 0)
        {
            throw new ArgumentException("Max size must be greater than 0", nameof(maxSize));
        }
        
        this.maxSize = maxSize;
    }

    // Enqueue method with logic to maintain queue size
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

    // Property to get the current count of the queue
    public int Count => queue.Count;

    public bool Contains(T item)
    {
        return queue.Contains(item);
    }
}