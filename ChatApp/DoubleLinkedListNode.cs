using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp
{
    public class DoubleLinkedList<T> : IEnumerable<T>
    {
        private DoubleLinkedListNode<T> Head { get; set; }
        private DoubleLinkedListNode<T> Tail { get; set; }
        public DoubleLinkedListNode<T> AddLast(T value) 
        {
            var node = new DoubleLinkedListNode<T>(value);
            if (Head == null)
            {
                Head = node;
                Tail = Head;
            }
            else
            {
                node.Previous = Tail;
                Tail.Next = node;
                Tail = node;
            }
            return node;
        }

        public void Remove(DoubleLinkedListNode<T> node) 
        {
            if (Head == node)
            {
                if (node.Next != null)
                {
                    node.Next.Previous = null;
                    Head = node.Next;
                }
                else 
                {
                    Head = null;
                    Tail = null;
                }
                return;
            }
            var previous = node.Previous;
            previous.Next = node.Next;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (Head == null)
            {
                yield break;
            }

            yield return Head.Value;

            for (var item = Head.Next; item != null; item = item.Next)
            {
                yield return item.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }

    public class DoubleLinkedListNode<TModel>
    {
        public DoubleLinkedListNode<TModel> Next { get; set; }
        public DoubleLinkedListNode<TModel> Previous { get; set; }
        public TModel Value { get; set; }

        public DoubleLinkedListNode(TModel value)
        {
            Value = value;
        }

    }
}
