// Copyright (c) NNicrosopht 1989-2015

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ISharp.Collections
{
    public class ListNode<T>
    {
        public ListNode<T> Next;
        public ListNode<T> Previous;
        public T Data;
        public byte Flags;

        static byte HeaderFlag = 1;

        public ListNode()
        {
            Header = true;
            Previous = this;
            Next = this;
        }

        public ListNode(T t) { Data = t; Header = false; }

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


    public class ListEntry<T> : IEnumerator<T>
    {
        public ListEntry(ListNode<T> N) { _Node = N; }

        public T Value
        {
            get
            {
                if (Header) throw new IsListRootException();
                return _Node.Data;
            }
            set
            {
                if (Header) throw new IsListRootException();
                _Node.Data = value;
            }

        }

        public bool Header { get { return _Node.Header; } }

        public bool MoveNext()
        {
            _Node = _Node.Next;
            return !Header;
        }

        public bool MovePrevious()
        {
            _Node = _Node.Previous;
            return !Header;
        }

        public static ListEntry<T> operator ++(ListEntry<T> Entry)
        {
            Entry._Node = Entry._Node.Next;
            return Entry;
        }

        public static ListEntry<T> operator --(ListEntry<T> Entry)
        {
            Entry._Node = Entry._Node.Previous;
            return Entry;
        }

        public static ListEntry<T> operator +(ListEntry<T> c, ulong Increment)
        {
            ListEntry<T> Result = new ListEntry<T>(c._Node);
            for (ulong i = 0; i < Increment; i++) ++Result;
            return Result;
        }

        public static ListEntry<T> operator +(ulong Increment, ListEntry<T> c)
        {
            ListEntry<T> Result = new ListEntry<T>(c._Node);
            for (ulong i = 0; i < Increment; i++) ++Result;
            return Result;
        }

        public static ListEntry<T> operator -(ListEntry<T> c, ulong Decrement)
        {
            ListEntry<T> Result = new ListEntry<T>(c._Node);
            for (ulong i = 0; i < Decrement; i++) --Result;
            return Result;
        }

        public void Reset()
        { while (!Header) _Node = _Node.Next; }

        object System.Collections.IEnumerator.Current
        { get { return _Node.Data; } }

        T IEnumerator<T>.Current
        { get { return _Node.Data; } }

        public static bool operator ==(ListEntry<T> x, ListEntry<T> y) { return x._Node == y._Node; }
        public static bool operator !=(ListEntry<T> x, ListEntry<T> y) { return x._Node != y._Node; }

        public override int GetHashCode()
        {
            return _Node.Data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return _Node.Data.Equals(obj);
        }

        public static long operator -(ListEntry<T> This, ListEntry<T> Iter)
        {
            long Result = 0;
            while (This._Node != Iter._Node) { Iter.MoveNext(); Result++; }
            return Result;
        }

        public void Dispose() { }

        public ListNode<T> _Node;
    }



    [Serializable]
    public class List<T> : IEnumerable<T>,
                           IEquatable<List<T>>,
                           ICloneable,
                           ISerializable
    {
        public ListNode<T> Header;
        ulong Nodes;
        public event TypeFound<T> Found;
        public event TypeAdded<T> Added;
        public event TypeRemoved<T> Removed;
        public event TypeUpdated<T> Updated;
        public IEqualityComparer<T> EComparer;
        public ICloner<T> TCloner;

        //*** Constructors ***

        public List()
        {
            Nodes = 0;
            Header = new ListNode<T>();
            EComparer = EqualityComparer<T>.Default;
            TCloner = Cloner<T>.Default;
        }

        public List(IEqualityComparer<T> ECompare)
        {
            Nodes = 0;
            Header = new ListNode<T>();
            EComparer = ECompare;
            TCloner = Cloner<T>.Default;
        }

        public List(IEnumerable<T> copy)
        {
            Nodes = 0;
            Header = new ListNode<T>();
            EComparer = EqualityComparer<T>.Default;
            TCloner = Cloner<T>.Default;

            foreach (T e in copy) Add(TCloner.Clone(e));
        }

        public List(IEnumerable<T> copy,
                    IEqualityComparer<T> ECompare)
        {
            Nodes = 0;
            Header = new ListNode<T>();
            EComparer = ECompare;
            TCloner = Cloner<T>.Default;

            foreach (T e in copy) Add(TCloner.Clone(e));
        }

        public List(SerializationInfo si, StreamingContext sc)
        {
            Nodes = 0;
            Header = new ListNode<T>();

            EComparer = (IEqualityComparer<T>)si.GetValue("EComparer", typeof(IEqualityComparer<T>));
            TCloner = (ICloner<T>)si.GetValue("TCloner", typeof(ICloner<T>));

            Type type = typeof(T);

            ulong Count = si.GetUInt64("Count");

            for (ulong i = 0; i < Count; i++)
            {
                object obj = si.GetValue(i.ToString(), type);
                Add((T)obj);
            }
        }

        //*** Properties ***

        public ListEntry<T> Begin { get { return new ListEntry<T>(Header.Next); } }

        public ICloner<T> Cloner
        {
            get { return TCloner; }
            set { TCloner = value; }
        }

        public IEqualityComparer<T> Comparer
        {
            get { return EComparer; }
            set { EComparer = value; }
        }

        public int Count
        { get { return (int)Nodes; } }

        public ListEntry<T> End { get { return new ListEntry<T>(Header); } }

        public ListEntry<T> First { get { return new ListEntry<T>(Header.Next); } }

        public int Hash { get { return GetHashCode(); } }

        public bool IsReadOnly { get { return false; } }

        public bool IsSynchronized { get { return false; } }

        public ListEntry<T> Last { get { return new ListEntry<T>(Header.Previous); } }

        public ulong Length { get { return Nodes; } }

        public object SyncHeader { get { return this; } }

        //*** Operators ***

        public static List<T> operator +(List<T> This, T t)
        {
            This.Add(t);
            return This;
        }

        public static bool operator ==(List<T> A, List<T> B)
        {
            return A.Equals(B);
        }

        public static bool operator !=(List<T> A, List<T> B)
        {
            return !A.Equals(B);
        }


        public override bool Equals(object obj)
        {
            return this == (List<T>)obj;
        }


        //*** Public Methods ***

        public void Add(T Data)
        {
            ListNode<T> NewNode = new ListNode<T>(Data);
            Nodes++;

            if (Header.Next == Header)
            {
                Header.Previous = NewNode;
                Header.Next = NewNode;
                NewNode.Previous = Header;
                NewNode.Next = Header;
                if (Added != null) Added(this, NewNode.Data);
            }
            else
            {
                Header.Previous.Next = NewNode;
                NewNode.Previous = Header.Previous;
                Header.Previous = NewNode;
                NewNode.Next = Header;
                if (Added != null) Added(this, NewNode.Data);
            }
        }

        public void AddFront(T Data)
        {
            ListNode<T> NewNode = new ListNode<T>(Data);
            Nodes++;

            if (Header.Next == Header)
            {
                Header.Previous = NewNode;
                Header.Next = NewNode;
                NewNode.Previous = Header;
                NewNode.Next = Header;
                if (Added != null) Added(this, NewNode.Data);
            }
            else
            {
                ListNode<T> Node = Header.Next;
                Node.Previous.Next = NewNode;
                NewNode.Previous = Node.Previous;
                Node.Previous = NewNode;
                NewNode.Next = Node;
                if (Added != null) Added(this, NewNode.Data);
            }
        }

        ListEntry<T> Add(T t, ListEntry<T> Position)
        {
            return new ListEntry<T>(Add(Position._Node, t));
        }

        public void Clear()
        {
            Remove();
        }

        public object Clone()
        {
            List<T> ListOut = new List<T>(EComparer);
            ListOut.TCloner = TCloner;
            ListOut.Copy(Header);
            return ListOut;
        }

        public bool Contains(ListEntry<T> LE)
        {
            return Contains(LE._Node);
        }

        public bool Contains(T value)
        {
            return Search(value) == null ? false : true;
        }

        public bool Contains(TypePredicate<T> Predicate)
        {
            ListNode<T> Node = PredicateFind(Predicate);
            if (Node == null) return false; else return true;
        }

        public bool Contains<P>(P Data, TypeCondition<T, P> Predicate)
        {
            ListNode<T> Node = PredicateFind<P>(Data, Predicate);
            if (Node == null) return false; else return true;
        }

        public void CopyTo(System.Array Arr, int I)
        {
            ListEntry<T> Start = Begin;
            ListEntry<T> Finish = End;

            while (Start != Finish)
            {
                Arr.SetValue(TCloner.Clone(Start._Node.Data), I);
                I++; Start.MoveNext();
            }
        }

        public bool Equals(List<T> e)
        {
            ListEntry<T> First1 = Begin;
            ListEntry<T> Last1 = End;
            ListEntry<T> First2 = e.Begin;
            ListEntry<T> Last2 = e.End;

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
            ListNode<T> Node = PredicateFind(Predicate);
            if (Node == null) throw new EntryNotFoundException();
            return Node.Data;
        }

        public T Find<P>(P Data, TypeCondition<T, P> Predicate)
        {
            ListNode<T> Node = PredicateFind<P>(Data, Predicate);
            if (Node == null) throw new EntryNotFoundException();
            return Node.Data;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return new ListEntry<T>(Header); }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        { return new ListEntry<T>(Header); }

        public override int GetHashCode()
        {
            int HashCode = 0;

            foreach (T t in this)
                HashCode += EComparer.GetHashCode(t);

            return HashCode;
        }

        public void GetObjectData(SerializationInfo si, StreamingContext sc)
        {
            si.SetType(typeof(ISharp.Collections.List<T>));
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

        public ListEntry<T> Insert(T Data)
        {
            ListNode<T> NewNode = new ListNode<T>(Data);
            Nodes++;

            if (Header.Next == Header)
            {
                Header.Previous = NewNode;
                Header.Next = NewNode;
                NewNode.Previous = Header;
                NewNode.Next = Header;
                if (Added != null) Added(this, NewNode.Data);
                return new ListEntry<T>(NewNode);
            }
            else
            {
                Header.Previous.Next = NewNode;
                NewNode.Previous = Header.Previous;
                Header.Previous = NewNode;
                NewNode.Next = Header;
                if (Added != null) Added(this, NewNode.Data);
                return new ListEntry<T>(NewNode);
            }
        }

        public ListEntry<T> InsertFront(T Data)
        {
            ListNode<T> NewNode = new ListNode<T>(Data);
            Nodes++;

            if (Header.Next == Header)
            {
                Header.Previous = NewNode;
                Header.Next = NewNode;
                NewNode.Previous = Header;
                NewNode.Next = Header;
                if (Added != null) Added(this, NewNode.Data);
                return new ListEntry<T>(NewNode);
            }
            else
            {
                ListNode<T> Node = Header.Next;
                Node.Previous.Next = NewNode;
                NewNode.Previous = Node.Previous;
                Node.Previous = NewNode;
                NewNode.Next = Node;
                if (Added != null) Added(this, NewNode.Data);
                return new ListEntry<T>(NewNode);
            }
        }

        public ListEntry<T> Locate(TypePredicate<T> Predicate)
        {
            ListNode<T> Node = PredicateFind(Predicate);
            if (Node == null) throw new EntryNotFoundException();
            return new ListEntry<T>(Node);
        }

        public ListEntry<T> Locate<P>(P Data, TypeCondition<T, P> Predicate)
        {
            ListNode<T> Node = PredicateFind<P>(Data, Predicate);
            if (Node == null) throw new EntryNotFoundException();
            return new ListEntry<T>(Node);
        }

        public void Notify()
        {
            if (Added != null)
            {
                ListEntry<T> i = Begin;
                ListEntry<T> e = End;
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
            Header.Next = Header;
            Header.Previous = Header;
            return Result;
        }

        public bool Remove(ListEntry<T> Entry)
        {
            Remove(Entry._Node);
            return true;
        }

        public void Remove(TypePredicate<T> Predicate)
        {
            ListEntry<T> i = Begin;
            ListEntry<T> e = End;
            ListEntry<T> j = i + 1;
            while (i != e)
            {
                if (Predicate(i.Value))
                    Remove(i._Node);
                i = j; ++j;
            }
        }

        public void Remove<P>(P Data,
                              TypeCondition<T, P> Predicate)
        {
            ListEntry<T> i = Begin;
            ListEntry<T> e = End;
            ListEntry<T> j = i + 1;
            while (i != e)
            {
                if (Predicate(i.Value, Data))
                    Remove(i._Node);
                i = j; ++j;
            }
        }

        public void Search(TypePredicate<T> Predicate)
        {
            if (Found != null)
            {
                ListEntry<T> i = Begin;
                ListEntry<T> e = End;
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
                ListEntry<T> i = Begin;
                ListEntry<T> e = End;
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

            ListNode<T>[] sublist = new ListNode<T>[64];

            ulong c = 0;

            ListNode<T> p = Header.Next;              // first unsorted item

            while (p != Header)
            {
                c++;
                ulong mergeNodes = Power2(c);
                ListNode<T> q = p;                  // tail of partial (merged) List.
                p = p.Next;
                q.Next = q; q.Previous = q;         // q is a sublist of size 1.
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
            p = p.Next;

            ListNode<T> r = p.Previous; // link Header back into list

            Header.Previous = r; // update Header
            Header.Next = p;

            p.Previous = Header; // update first and last
            r.Next = Header;
        }

        public override String ToString()
        {
            String StringOut = "{";

            ListEntry<T> Start = Begin;
            ListEntry<T> Finish = End;
            ListEntry<T> Last = End - 1;

            while (Start != Finish)
            {
                String NewStringOut = Start.Value.ToString();
                if (Start != Last) NewStringOut = NewStringOut + ",";
                StringOut = StringOut + NewStringOut;
                ++Start;
            }

            StringOut = StringOut + "}";
            return StringOut;
        }

        public void Update(ListEntry<T> Entry, T after)
        {
            Update(Entry._Node, after);
        }


        //*** Private Methods ***

        public void Copy(ListNode<T> clone)
        {
            if (Nodes != 0) Remove();

            ListNode<T> n = clone.Next;
            while (!n.Header)
            {
                Add(Header, TCloner.Clone(n.Data));
                n = n.Next;
            }
        }

        ListNode<T> Add(ListNode<T> Node, T Data)
        {
            if (Header.Next == Header)
            {
                if (Node != Header) throw new InvalidListNodeException();
                ListNode<T> NewNode = new ListNode<T>(Data);
                Nodes++;
                Header.Previous = NewNode;
                Header.Next = NewNode;
                NewNode.Previous = Header;
                NewNode.Next = Header;
                if (Added != null) Added(this, NewNode.Data);
                return NewNode;
            }
            else
            {
                ListNode<T> NewNode = new ListNode<T>(Data);
                Nodes++;
                Node.Previous.Next = NewNode;
                NewNode.Previous = Node.Previous;
                Node.Previous = NewNode;
                NewNode.Next = Node;
                if (Added != null) Added(this, NewNode.Data);
                return NewNode;
            }
        }

        bool Contains(ListNode<T> Node)
        {
            ListEntry<T> i = Begin;
            ListEntry<T> e = End;
            while (i != e)
            {
                if (i._Node == Node) return true;
                ++i;
            }
            return false;
        }

        bool Remove(ListNode<T> Node)
        {
            if (Node.Header) throw new IsListRootException();
            ListNode<T> Next = Node.Next;
            Node.Previous.Next = Next;
            Next.Previous = Node.Previous;
            Nodes--;
            if (Removed != null) Removed(this, Node.Data);
            return true;
        }

        ListNode<T> Search(T value)
        {
            ListNode<T> n = Header.Next;
            while (!n.Header)
            {
                if (EComparer.Equals(n.Value, value)) return n;
                n = n.Next;
            }
            return null;
        }

        ListNode<T> PredicateFind(TypePredicate<T> Predicate)
        {
            ListEntry<T> i = Begin;
            ListEntry<T> e = End;
            while (i != e)
            {
                if (Predicate(i.Value)) return i._Node;
                ++i;
            }
            return null;
        }

        ListNode<T> PredicateFind<P>(P Data,
                                     TypeCondition<T, P> Predicate)
        {
            ListEntry<T> i = Begin;
            ListEntry<T> e = End;
            while (i != e)
            {
                if (Predicate(i.Value, Data)) return i._Node;
                ++i;
            }
            return null;
        }

        void PredicateSelect(TypePredicate<T> Predicate,
                             List<T> Selected)
        {
            ListEntry<T> i = Begin;
            ListEntry<T> e = End;
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
            ListEntry<T> i = Begin;
            ListEntry<T> e = End;
            while (i != e)
            {
                if (Predicate(i.Value, Data)) Selected.Add(i.Value);
                ++i;
            }
        }

        void Update(ListNode<T> Node, T Value)
        {
            if (Updated != null)
            {
                T Saved = Node.Data;
                Node.Data = Value;
                Updated(this, Saved, Value);
            }
            else Node.Data = Value;
        }

        static ListNode<T> Merge(ListNode<T> first,
                                 ListNode<T> second,
                                 IComparer<T> TCompare)
        {
            ListNode<T> a = first.Next;
            ListNode<T> b = second.Next;

            ListNode<T> head;
            ListNode<T> tail;

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
                    a.Previous = tail;
                    tail = a;
                    a = a.Next;
                    if (a == first.Next) end = EndType.TypeA;
                }
                else
                {
                    tail.Next = b;
                    b.Previous = tail;
                    tail = b;
                    b = b.Next;
                    if (b == second.Next) end = EndType.TypeB;
                }
            }
            if (end == EndType.TypeA)
            {
                tail.Next = b;
                b.Previous = tail;
                second.Next = head;
                head.Previous = second;
                return second;
            }
            else
            {
                tail.Next = a;
                a.Previous = tail;
                first.Next = head;
                head.Previous = first;
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

    } // end of class List
  
} // end of namespace ISharp.Collections
