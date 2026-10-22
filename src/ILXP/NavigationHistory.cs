using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ILXP
{
    /// <summary>
    /// Simple back/forward navigation history with a bounded ring buffer.
    /// </summary>
    public sealed class NavigationHistory
    {
        /// <summary>
        /// Fixed-capacity ring buffer used by the history. Kept public (as in the
        /// original code) because the type is generic and self-contained.
        /// </summary>
        public sealed class RingStack<T>
        {
            private readonly object[] items;
            private int head = -1;
            private int count;
            private int capacity;

            public RingStack()
                : this(100)
            {
            }

            public RingStack(int capacity)
            {
                if (capacity < 1)
                {
                    throw new ArgumentOutOfRangeException("capacity", "Capacity must be greater than 0");
                }
                this.capacity = capacity;
                items = new object[capacity];
            }

            public int Count
            {
                get { return count; }
            }

            public T this[int index]
            {
                get
                {
                    if (index < 0 || index >= count)
                    {
                        throw new ArgumentOutOfRangeException("index", "Stack is empty");
                    }
                    int position = head - index;
                    if (position < 0)
                    {
                        position += capacity;
                    }
                    return (T)items[position];
                }
            }

            public void Push(T value)
            {
                head++;
                if (head >= capacity)
                {
                    head -= capacity;
                }
                if (count < capacity)
                {
                    count++;
                }
                items[head] = value;
            }

            public T Pop()
            {
                if (count <= 0)
                {
                    throw new InvalidOperationException("Stack is empty");
                }
                T value = (T)items[head];
                items[head] = null;
                head--;
                if (head < 0)
                {
                    head += capacity;
                }
                count--;
                return value;
            }

            public void Clear()
            {
                for (int i = 0; i < count; i++)
                {
                    items[head] = null;
                    head--;
                    if (head < 0)
                    {
                        head = capacity - 1;
                    }
                }
                head = -1;
                count = 0;
            }
        }

        private readonly RingStack<string> backStack;
        private readonly RingStack<string> forwardStack;

        public NavigationHistory()
        {
            backStack = new RingStack<string>(300);
            forwardStack = new RingStack<string>(300);
        }

        public void Clear()
        {
            backStack.Clear();
            forwardStack.Clear();
        }

        /// <summary>
        /// Records a new location. Re-visiting the current location is a no-op.
        /// </summary>
        public void Navigate(string location)
        {
            if (backStack.Count > 0)
            {
                string current = backStack.Pop();
                backStack.Push(current);
                if (string.Equals(location, current, StringComparison.Ordinal))
                {
                    return;
                }
            }
            backStack.Push(location);
            forwardStack.Clear();
        }

        public bool CanGoBack
        {
            get { return backStack.Count > 1; }
        }

        public bool CanGoForward
        {
            get { return forwardStack.Count > 0; }
        }

        public string Back()
        {
            forwardStack.Push(backStack.Pop());
            string location = backStack.Pop();
            backStack.Push(location);
            return location;
        }

        public string Forward()
        {
            string location = forwardStack.Pop();
            backStack.Push(location);
            return location;
        }
    }
}
