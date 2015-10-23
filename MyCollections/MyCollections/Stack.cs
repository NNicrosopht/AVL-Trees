// Copyright (c) Benedict Bede McNamara 1989-2015

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ISharp.Collections
{
    public class StackNode<T>
    {
        public StackNode<T> Next;
        public T Data;
        public byte Flags;

        static byte HeaderFlag = 1;

        public StackNode()
        {
            Header = true;
            Next = this;
        }

        public StackNode(T t) { Data = t; Header = false; }

        public bool Header
        {
            get { return (Flags & HeaderFlag) != 0; }
            set { if (value) Flags = (byte)(Flags | HeaderFlag); else Flags = (byte)(Flags & ~HeaderFlag); }
        }

        public T Value
        {
            get
            {
                if (Header) throw new IsListRootException();
                return Data;
            }
            set
            {
                if (Header) throw new IsListRootException();
                Data = value;
            }
        }
    }


    public class StackEntry<T> : IEnumerator<T>
    {
        public StackEntry(StackNode<T> N) { Node = N; }

        public T Value
        {
            get
            {
                if (Header) throw new IsListRootException();
                return Node.Data;
            }
            set
            {
                if (Header) throw new IsListRootException();
                Node.Data = value;
            }

        }

        public bool Header { get { return Node.Header; } }

        public bool MoveNext()
        {
            Node = Node.Next;
            return !Header;
        }

        public static StackEntry<T> operator ++(StackEntry<T> Entry)
        {
            Entry.Node = Entry.Node.Next;
            return Entry;
        }

        public static StackEntry<T> operator +(StackEntry<T> c, ulong Increment)
        {
            StackEntry<T> Result = new StackEntry<T>(c.Node);
            for (ulong i = 0; i < Increment; i++) ++Result;
            return Result;
        }

        public static StackEntry<T> operator +(ulong Increment, StackEntry<T> c)
        {
            StackEntry<T> Result = new StackEntry<T>(c.Node);
            for (ulong i = 0; i < Increment; i++) ++Result;
            return Result;
        }

        public void Reset()
        { while (!Header) Node = Node.Next; }

        object System.Collections.IEnumerator.Current
        { get { return Node.Data; } }

        T IEnumerator<T>.Current
        { get { return Node.Data; } }

        public static bool operator ==(StackEntry<T> x, StackEntry<T> y) { return x.Node == y.Node; }
        public static bool operator !=(StackEntry<T> x, StackEntry<T> y) { return x.Node != y.Node; }

        public override int GetHashCode()
        {
            return Node.Data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Node.Data.Equals(obj);
        }

        public static long operator -(StackEntry<T> This, StackEntry<T> Iter)
        {
            long Result = 0;
            while (This.Node != Iter.Node) { Iter.MoveNext(); Result++; }
            return Result;
        }

        public void Dispose() { }

        public StackNode<T> Node;
    }

    [Serializable]
    public class Stack<T> : IEnumerable<T>,
                            IEquatable<Stack<T>>,
                            ICloneable,
                            ISerializable
    {
        public StackNode<T> Header;
        ulong Nodes;
        public event TypeFound<T> Found;
        public event TypeAdded<T> Added;
        public event TypeRemoved<T> Removed;
        public IEqualityComparer<T> EComparer;
        public ICloner<T> TCloner;

        //*** Constructors ***

        public Stack()
        {
            Nodes = 0;
            Header = new StackNode<T>();
            EComparer = EqualityComparer<T>.Default;
            TCloner = Cloner<T>.Default;
        }

        public Stack(IEqualityComparer<T> ECompare)
        {
            Nodes = 0;
            Header = new StackNode<T>();
            EComparer = ECompare;
            TCloner = Cloner<T>.Default;
        }

        public Stack(IEnumerable<T> copy)
        {
            Nodes = 0;
            Header = new StackNode<T>();
            EComparer = EqualityComparer<T>.Default;
            TCloner = Cloner<T>.Default;

            foreach (T e in copy) Add(TCloner.Clone(e));
        }

        public Stack(IEnumerable<T> copy,
                     IEqualityComparer<T> ECompare)
        {
            Nodes = 0;
            Header = new StackNode<T>();
            EComparer = ECompare;
            TCloner = Cloner<T>.Default;

            foreach (T e in copy) Add(TCloner.Clone(e));
        }

        public Stack(SerializationInfo si, StreamingContext sc)
        {
            Nodes = 0;
            Header = new StackNode<T>();

            EComparer = (IEqualityComparer<T>)si.GetValue("EComparer", typeof(IEqualityComparer<T>));
            TCloner = (ICloner<T>)si.GetValue("TCloner", typeof(ICloner<T>));

            Type type = typeof(T);

            ulong Count = si.GetUInt64("Count");

            StackNode<T> Last = Header;

            for (ulong i = 0; i < Count; i++)
            {
                object obj = si.GetValue(i.ToString(), type);
                StackNode<T> NewNode = new StackNode<T>((T)obj);
                Nodes++;
                NewNode.Next = Header;
                Last.Next = NewNode;
                if (Added != null) Added(this, NewNode.Data);
                Last = NewNode;
            }
        }

        //*** Properties ***

        public StackEntry<T> Begin { get { return new StackEntry<T>(Header.Next); } }

        public StackEntry<T> End { get { return new StackEntry<T>(Header); } }

        public ICloner<T> Cloner
        {
            get { return TCloner; }
            set { TCloner = value; }
        }

        public IEqualityComparer<T> Comparer
        {
            get
            {
                return EComparer;
            }
            set
            {
                EComparer = value;
            }
        }

        public int Count { get { return (int)Nodes; } }

        public int Hash { get { return GetHashCode(); } }

        public bool IsReadOnly { get { return false; } }

        public bool IsSynchronized { get { return false; } }

        public ulong Length { get { return Nodes; } }

        public object SyncRoot { get { return this; } }

        //*** Operators ***

        public static bool operator ==(Stack<T> A, Stack<T> B)
        {
            if ((object)A == null && (object)B != null) return false;
            if ((object)A != null && (object)B == null) return false;
            if ((object)A == null && (object)B == null) return true;

            return A.Equals(B);
        }

        public static bool operator !=(Stack<T> A, Stack<T> B)
        {
            if ((object)A == null && (object)B != null) return true;
            if ((object)A != null && (object)B == null) return true;
            if ((object)A == null && (object)B == null) return false;

            return !A.Equals(B);
        }

        public override bool Equals(object obj)
        {
            return this == (Stack<T>)obj;
        }

        //*** Public Methods ***

        public void Add(T Data)
        {
            StackNode<T> NewNode = new StackNode<T>(Data);
            Nodes++;

            if (Header.Next == Header)
            {
                Header.Next = NewNode;
                NewNode.Next = Header;
                if (Added != null) Added(this, NewNode.Data);
            }
            else
            {
                NewNode.Next = Header.Next;
                Header.Next = NewNode;
                if (Added != null) Added(this, NewNode.Data);
            }
        }

        public void Push(T Data)
        {
            StackNode<T> NewNode = new StackNode<T>(Data);
            Nodes++;

            if (Header.Next == Header)
            {
                Header.Next = NewNode;
                NewNode.Next = Header;
                if (Added != null) Added(this, NewNode.Data);
            }
            else
            {
                NewNode.Next = Header.Next;
                Header.Next = NewNode;
                if (Added != null) Added(this, NewNode.Data);
            }
        }

        public T Peek()
        {
            if (Header.Next == Header)
            {
                throw new IsListRootException();
            }
            else
            {
                return Header.Next.Data;
            }
        }


        public T Pop()
        {
            if (Header.Next == Header)
            {
                throw new IsListRootException();
            }
            else
            {
                T ReturnData = Header.Next.Data;
                StackNode<T> Current = Header.Next;
                Header.Next = Header.Next.Next;
                Nodes--;
                if (Removed != null) Removed(this, Current.Data);
                return ReturnData;
            }
        }

        //        public ulong Add(IEnumerable<T> Copy)
        //        {
        //            ulong Nodes = 0;
        //            foreach (T t in Copy)
        //            {
        //                Add(TCloner.Clone(t));
        //                Nodes++;
        //            }
        //            return Nodes;
        //        }

        public Stack<T> Reverse()
        {
            Stack<T> Reversed = new Stack<T>();

            Reversed.Cloner = Cloner;
            Reversed.Comparer = Comparer;

            foreach (T t in this)
            {
                Reversed.Add(TCloner.Clone(t));
            }

            return Reversed;
        }

        public void Clear()
        {
            Remove();
        }

        public object Clone()
        {
            Stack<T> StackOut = new Stack<T>(EComparer);
            StackOut.TCloner = TCloner;
            StackOut.Copy(Header);
            return StackOut;
        }

        public bool Contains(T value)
        {
            return Search(value) == null ? false : true;
        }

        public bool Contains(TypePredicate<T> Predicate)
        {
            StackNode<T> Node = PredicateFind(Predicate);
            if (Node == null) return false; else return true;
        }

        public bool Contains<P>(P Data, TypeCondition<T, P> Predicate)
        {
            StackNode<T> Node = PredicateFind<P>(Data, Predicate);
            if (Node == null) return false; else return true;
        }

        public bool Equals(Stack<T> e)
        {
            StackEntry<T> First1 = Begin;
            StackEntry<T> Last1 = End;
            StackEntry<T> First2 = e.Begin;
            StackEntry<T> Last2 = e.End;

            bool Equals = true;

            while (First1 != Last1 && First2 != Last2)
            {
                if (EComparer.Equals(First1.Value, First2.Value))
                { First1.MoveNext(); First2.MoveNext(); }
                else
                { Equals = false; break; }
            }

            if (Equals)
            {
                if (First1 != Last1) Equals = false;
                if (First2 != Last2) Equals = false;
            }

            return Equals;
        }

        public T Find(TypePredicate<T> Predicate)
        {
            StackNode<T> Node = PredicateFind(Predicate);
            if (Node == null) throw new EntryNotFoundException();
            return Node.Data;
        }

        public T Find<P>(P Data, TypeCondition<T, P> Predicate)
        {
            StackNode<T> Node = PredicateFind<P>(Data, Predicate);
            if (Node == null) throw new EntryNotFoundException();
            return Node.Data;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return new StackEntry<T>(Header); }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        { return new StackEntry<T>(Header); }

        public override int GetHashCode()
        {
            int HashCode = 0;

            foreach (T t in this)
                HashCode += EComparer.GetHashCode(t);

            return HashCode;
        }

        public void GetObjectData(SerializationInfo si, StreamingContext sc)
        {
            si.SetType(typeof(ISharp.Collections.Stack<T>));
            Type type = typeof(T);
            ulong Index = 0;
            foreach (T e in this)
            {
                si.AddValue(Index.ToString(), e, type);
                Index++;
            }
            si.AddValue("Count", Index);
            si.AddValue("EComparer", EComparer, EComparer.GetType());
            si.AddValue("TCloner", TCloner, TCloner.GetType());
        }

        public StackEntry<T> Locate(TypePredicate<T> Predicate)
        {
            StackNode<T> Node = PredicateFind(Predicate);
            if (Node == null) throw new EntryNotFoundException();
            return new StackEntry<T>(Node);
        }

        public StackEntry<T> Locate<P>(P Data, TypeCondition<T, P> Predicate)
        {
            StackNode<T> Node = PredicateFind<P>(Data, Predicate);
            if (Node == null) throw new EntryNotFoundException();
            return new StackEntry<T>(Node);
        }

        public void Notify()
        {
            if (Added != null)
            {
                StackEntry<T> i = Begin;
                StackEntry<T> e = End;
                while (i != e)
                {
                    Added(this, i.Value);
                    ++i;
                }
            }
        }

        public ulong Remove()
        {
            if (Removed != null)
                foreach (T t in this)
                {
                    Removed(this, t);
                }
            ulong Result = Nodes;
            StackNode<T> n = Header.Next;
            while (!n.Header)
            {
                StackNode<T> Next = n.Next;
                Nodes--;
                if (Removed != null) Removed(this, n.Data);
                n = Next;
            }
            Header.Next = Header;
            return Result;
        }

        public void Search(TypePredicate<T> Predicate)
        {
            if (Found != null)
            {
                StackEntry<T> i = Begin;
                StackEntry<T> e = End;
                while (i != e)
                {
                    if (Predicate(i.Value)) Found(this, i.Value);
                    ++i;
                }
            }
        }

        public void Search<P>(P Data,
                              TypeCondition<T, P> Predicate)
        {
            if (Found != null)
            {
                StackEntry<T> i = Begin;
                StackEntry<T> e = End;
                while (i != e)
                {
                    if (Predicate(i.Value, Data)) Found(this, i.Value);
                    ++i;
                }
            }
        }

        public List<T> Select(TypePredicate<T> Predicate)
        {
            List<T> Selected = new List<T>(EComparer);
            Selected.TCloner = TCloner;
            Selected.EComparer = EComparer;
            PredicateSelect(Predicate, Selected);
            return Selected;
        }

        public List<T> Select<P>(P Data,
                                 TypeCondition<T, P> Predicate)
        {
            List<T> Selected = new List<T>(EComparer);
            Selected.TCloner = TCloner;
            Selected.EComparer = EComparer;
            PredicateSelect<P>(Data, Predicate, Selected);
            return Selected;
        }

        public void Sort(IComparer<T> TCompare)
        {
            if (Header.Next == Header ||
                Header.Next.Next == Header) return;  // at least two

            StackNode<T>[] sublist = new StackNode<T>[64];

            ulong c = 0;

            StackNode<T> p = Header.Next;              // first unsorted item

            while (p != Header)
            {
                c++;
                ulong mergeNodes = Power2(c);
                StackNode<T> q = p;                  // tail of partial (merged) List.
                p = p.Next;
                q.Next = q;                         // q is a sublist of size 1.
                for (ulong i = 0; i < mergeNodes; i++)
                    q = Merge(q, sublist[i], TCompare);
                sublist[mergeNodes] = q;
            }

            long MergeNodes = -1;
            while (c != 0)
            {
                long d = (long)(c % 2);
                c /= 2;
                MergeNodes++;
                if (d != 0)
                    if (p == Header)
                        p = sublist[MergeNodes];
                    else
                        p = Merge(p, sublist[MergeNodes], TCompare);
            }

            StackNode<T> r = p; // link Header back into list
            p = p.Next;
            Header.Next = p;
            r.Next = Header;
        }

        public override String ToString()
        {
            String StringOut = "{";

            StackEntry<T> Start = Begin;
            StackEntry<T> Finish = End;

            while (Start != Finish)
            {
                String NewStringOut = Start.Value.ToString();
                if (Start + 1 != End) NewStringOut = NewStringOut + ",";
                StringOut = StringOut + NewStringOut;
                ++Start;
            }

            StringOut = StringOut + "}";
            return StringOut;
        }

        //*** Private Methods ***

        public void Copy(StackNode<T> clone)
        {
            if (Nodes != 0) Remove();

            StackNode<T> n = clone.Next;
            StackNode<T> Last = Header;

            while (!n.Header)
            {
                StackNode<T> NewNode = new StackNode<T>(TCloner.Clone(n.Data));
                Nodes++;
                NewNode.Next = Header;
                Last.Next = NewNode;
                if (Added != null) Added(this, NewNode.Data);
                Last = NewNode;
                n = n.Next;
            }
        }

        bool Contains(StackNode<T> Node)
        {
            StackEntry<T> i = Begin;
            StackEntry<T> e = End;
            while (i != e)
            {
                if (i.Node == Node) return true;
                ++i;
            }
            return false;
        }

        StackNode<T> Search(T value)
        {
            StackNode<T> n = Header.Next;
            while (!n.Header)
            {
                if (EComparer.Equals(n.Value, value)) return n;
                n = n.Next;
            }
            return null;
        }

        StackNode<T> PredicateFind(TypePredicate<T> Predicate)
        {
            StackEntry<T> i = Begin;
            StackEntry<T> e = End;
            while (i != e)
            {
                if (Predicate(i.Value)) return i.Node;
                ++i;
            }
            return null;
        }

        StackNode<T> PredicateFind<P>(P Data,
                                      TypeCondition<T, P> Predicate)
        {
            StackEntry<T> i = Begin;
            StackEntry<T> e = End;
            while (i != e)
            {
                if (Predicate(i.Value, Data)) return i.Node;
                ++i;
            }
            return null;
        }

        void PredicateSelect(TypePredicate<T> Predicate,
                             List<T> Selected)
        {
            StackEntry<T> i = Begin;
            StackEntry<T> e = End;
            while (i != e)
            {
                if (Predicate(i.Value)) Selected.Add(i.Value);
                ++i;
            }
        }

        void PredicateSelect<P>(P Data,
                                TypeCondition<T, P> Predicate,
                                List<T> Selected)
        {
            StackEntry<T> i = Begin;
            StackEntry<T> e = End;
            while (i != e)
            {
                if (Predicate(i.Value, Data)) Selected.Add(i.Value);
                ++i;
            }
        }

        static StackNode<T> Merge(StackNode<T> first,
                                  StackNode<T> second,
                                  IComparer<T> TCompare)
        {
            StackNode<T> a = first.Next;
            StackNode<T> b = second.Next;

            StackNode<T> head;
            StackNode<T> tail;

            EndType end = EndType.None;

            if (TCompare.Compare(a.Data, b.Data) <= 0)
            {
                head = tail = a;
                a = a.Next;
                if (a == first.Next) end = EndType.TypeA;
            }
            else
            {
                head = tail = b;
                b = b.Next;
                if (b == second.Next) end = EndType.TypeB;
            }

            while (end == EndType.None)
            {
                if (TCompare.Compare(a.Data, b.Data) <= 0)
                {
                    tail.Next = a;
                    tail = a;
                    a = a.Next;
                    if (a == first.Next) end = EndType.TypeA;
                }
                else
                {
                    tail.Next = b;
                    tail = b;
                    b = b.Next;
                    if (b == second.Next) end = EndType.TypeB;
                }
            }
            if (end == EndType.TypeA)
            {
                tail.Next = b;
                second.Next = head;
                return second;
            }
            else
            {
                tail.Next = a;
                first.Next = head;
                return first;
            }
        }

        public static ulong Power2(ulong c)
        {
            ulong level = 0;
            while (c % 2 != 1)
            {
                c /= 2;
                level++;
            }
            return level;
        }

    } // end of class Stack

} // end of namespace ISharp.Collections
