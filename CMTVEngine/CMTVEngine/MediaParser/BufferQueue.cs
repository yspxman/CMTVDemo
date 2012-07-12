using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Threading;

namespace CMTVEngine
{
    public class BufferQueue <T> :System.IDisposable
    {
             /// <summary>
        /// Our queue of work items
        /// </summary>
        private Queue<T> m_queue;

        /// <summary>
        /// An event which fires whenever the queue has items in it
        /// (or rather, when the queue goes from empty to non-empty)
        /// </summary>
        private ManualResetEvent m_queueHasItemsEvent;

        /// <summary>
        /// Initializes a new instance of the WorkQueue class
        /// </summary>
        public BufferQueue()
        {
            m_queueHasItemsEvent = new ManualResetEvent(false);
            m_queue = new Queue<T>();
        }

        public int Count()
        {
            return m_queue.Count;
        }
        /// <summary>
        /// Implements IDisposable.Dispose()
        /// </summary>
        public void Dispose()
        {
            if (m_queueHasItemsEvent != null)
            {
                m_queueHasItemsEvent.Close();
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Enqueue a new work item
        /// </summary>
        /// <param name="elem">the item to add</param>
        public void Enqueue(T elem)
        {
            lock (m_queue)
            {
                m_queue.Enqueue(elem);
                if (1 == m_queue.Count)
                {
                    m_queueHasItemsEvent.Set();
                }
            }
        }

        /// <summary>
        /// Remove and return an item from the queue
        /// </summary>
        /// <returns>next item from the queue</returns>
        public T Dequeue()
        {
            T elem = default(T); //denull;
            lock (m_queue)
            {
                if (0 != m_queue.Count)
                {
                    elem = m_queue.Dequeue();
                    if (0 == m_queue.Count)
                    {
                        m_queueHasItemsEvent.Reset();
                    }
                }
            }

            return elem;
        }

        /// <summary>
        /// Clear the queue and add 1 item in the same operation. This is useful
        /// for operation that take precedence over all others (like closing and errors)
        /// </summary>
        /// <param name="elem">New item to add</param>
        public void ClearAndEnqueue(T elem)
        {
            lock (m_queue)
            {
                m_queue.Clear();
                m_queue.Enqueue(elem);
                m_queueHasItemsEvent.Set();
            }
        }

        public void Clear()
        {
            lock (m_queue)
            {
                m_queue.Clear();
                m_queueHasItemsEvent.Reset();
            }
        }

        /// <summary>
        /// Wait until the queue has an item in it
        /// </summary>
        public void WaitForWorkItem()
        {
            m_queueHasItemsEvent.WaitOne();
        }
    }

    
}
