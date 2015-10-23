using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ISharp.Collections
{
    public class ArrayNode<T> : Node
    {
        public T Data;
        public long Index;

        public ArrayNode(long i, T t, Node Parent)
            : base(Parent)
        {
            Index = i;
            Data = t;
        }
    }

    public struct SubArrayEnumerator<T> : IEnumerator<KeyValue<long, T>>
    {
        public SubArrayEnumerator(Node c, Node e)
        {
            Finish = e;
            Start = Utility.PreviousItem(c); _Node = Start;
        }

        public SubArrayEnumerator(ArrayEntry<T> c, ArrayEntry<T> e)
        {
            Finish = e._Node;
            Start = Utility.PreviousItem(c._Node); _Node = Start;
        }

        public bool MoveNext()
        {
            _Node = Utility.NextItem(_Node);
            return _Node != Finish;
        }

        public bool MovePrevious()
        {
            _Node = Utility.PreviousItem(_Node);
            return _Node != Start;
        }

        public void Reset()
        {
            _Node = Start;
        }

        object System.Collections.IEnumerator.Current
        { get { return new KeyValue<long, T>(((ArrayNode<T>)_Node).Index, ((ArrayNode<T>)_Node).Data); } }

        KeyValue<long, T> IEnumerator<KeyValue<long, T>>.Current
        { get { return new KeyValue<long, T>(((ArrayNode<T>)_Node).Index, ((ArrayNode<T>)_Node).Data); } }

        public void Dispose() { }

        Node Start;
        Node _Node;
        Node Finish;
    }

    public struct SubArray<T> : IEnumerable<KeyValue<long, T>>
    {
        public SubArray(Node lower, Node upper)
        { Start = lower; Finish = upper; }

        public SubArray(ArrayEntry<T> lower, ArrayEntry<T> upper)
        { Start = lower._Node; Finish = upper._Node; }

        public ArrayEntry<T> Begin
        {
            get { return new ArrayEntry<T>(Start); }
        }

        public ArrayEntry<T> End
        {
            get { return new ArrayEntry<T>(Finish); }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return new SubArrayEnumerator<T>(Start, Finish); }

        IEnumerator<KeyValue<long, T>> IEnumerable<KeyValue<long, T>>.GetEnumerator()
        { return new SubArrayEnumerator<T>(Start, Finish); }

        public IEnumerator<KeyValue<long, T>> GetEnumerator()
        { return new SubArrayEnumerator<T>(Start, Finish); }

        public override string ToString()
        {
            string StringOut = "{";
            ArrayEntry<T> last = new ArrayEntry<T>(Finish); --last;
            foreach (KeyValue<long, T> e in this)
            {
                string NewStringOut = "(" + e.Key.ToString() + "," + e.Value.ToString() + ")";
                if (e.Key.CompareTo(((ArrayNode<T>)last._Node).Index) != 0) NewStringOut = NewStringOut + ",";
                StringOut = StringOut + NewStringOut;
            }
            StringOut = StringOut + "}";
            return StringOut;
        }

        public Node Start;
        public Node Finish;
    }


    public struct ArrayEntry<T> : IEnumerator<KeyValue<long, T>>
    {
        public ArrayEntry(Node n) { _Node = n; }

        public ArrayEntry<T> Entry
        { get { return new ArrayEntry<T>(_Node); } }

        public long Key
        {
            get
            {
                if (_Node.Balance == State.Header) throw new IsEndItemException();
                return ((ArrayNode<T>)_Node).Index;
            }
        }

        public long Index
        {
            get
            {
                if (_Node.Balance == State.Header) throw new IsEndItemException();
                return ((ArrayNode<T>)_Node).Index;
            }
        }

        public T Value
        {
            get
            {
                if (_Node.Balance == State.Header) throw new IsEndItemException();
                return ((ArrayNode<T>)_Node).Data;
            }
            set
            {
                if (_Node.Balance == State.Header) throw new IsEndItemException();
                ((ArrayNode<T>)_Node).Data = value;
            }
        }

        public bool IsHeader { get { return _Node.IsHeader; } }

        public bool MoveNext()
        {
            _Node = Utility.NextItem(_Node);
            return _Node.IsHeader ? false : true;
        }

        public bool MovePrevious()
        {
            _Node = Utility.PreviousItem(_Node);
            return _Node.IsHeader ? false : true;
        }

        public static ArrayEntry<T> operator ++(ArrayEntry<T> entry)
        {
            entry._Node = Utility.NextItem(entry._Node);
            return entry;
        }

        public static ArrayEntry<T> operator --(ArrayEntry<T> entry)
        {
            entry._Node = Utility.PreviousItem(entry._Node);
            return entry;
        }

        public static ArrayEntry<T> operator +(ArrayEntry<T> c, ulong increment)
        {
            ArrayEntry<T> result = new ArrayEntry<T>(c._Node);
            for (ulong i = 0; i < increment; i++) ++result;
            return result;
        }

        public static ArrayEntry<T> operator +(ulong increment, ArrayEntry<T> c)
        {
            ArrayEntry<T> result = new ArrayEntry<T>(c._Node);
            for (ulong i = 0; i < increment; i++) ++result;
            return result;
        }

        public static ArrayEntry<T> operator -(ArrayEntry<T> c, ulong decrement)
        {
            ArrayEntry<T> result = new ArrayEntry<T>(c._Node);
            for (ulong i = 0; i < decrement; i++) --result;
            return result;
        }

        public void Reset()
        {
            _Node = Utility.GetEndItem(_Node);
        }

        object System.Collections.IEnumerator.Current
        { get { return new KeyValue<long, T>(((ArrayNode<T>)_Node).Index, ((ArrayNode<T>)_Node).Data); } }

        KeyValue<long, T> IEnumerator<KeyValue<long, T>>.Current
        { get { return new KeyValue<long, T>(((ArrayNode<T>)_Node).Index, ((ArrayNode<T>)_Node).Data); } }

        public static bool operator ==(ArrayEntry<T> x, ArrayEntry<T> y) { return x._Node == y._Node; }
        public static bool operator !=(ArrayEntry<T> x, ArrayEntry<T> y) { return x._Node != y._Node; }

        public override int GetHashCode()
        {
            return ((ArrayNode<T>)_Node).Data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ((ArrayNode<T>)_Node).Data.Equals(obj);
        }

        public static long operator -(ArrayEntry<T> This, ArrayEntry<T> iter)
        {
            long result = 0;
            while (This._Node != iter._Node) { iter.MoveNext(); result++; }
            return result;
        }
        public override String ToString()
        {
            if (_Node.Balance == State.Header) throw new IsEndItemException();
            return "(" + Index.ToString() + "," + Value.ToString() + ")";
        }

        public void Dispose() { }

        public Node _Node;
    };


    class ArrayUtility<T>
    {
        public static void SortArray(Node first,
                                        Node last,
                                        IComparer<T> TComparer)
        {
            ArrayEntry<T> First = new ArrayEntry<T>(first);
            ArrayEntry<T> Last = new ArrayEntry<T>(last);

            Stack<T> ListToSort = new Stack<T>();

            for (ArrayEntry<T> f = First; f != Last; f.MoveNext())
                ListToSort.Push(f.Value);

            ListToSort.Sort(TComparer);

            while (First != Last) { ((ArrayNode<T>)First._Node).Data = ListToSort.Pop(); First.MoveNext(); }
        }
    }

    [Serializable]
    public class Array<T> : IEnumerable<KeyValue<long, T>>,
                            ICloneable,
                            ISerializable
    {
        private Node Header;
        private ulong Nodes;
        private long Index;
        private ulong Amount;
        private ulong HashCode;
        public event KeyTypeFound<long, T> Found;
        public event KeyTypeAdded<long, T> Added;
        public event KeyTypeRemoved<long, T> Removed;
        public event KeyTypeUpdated<long, T> Updated;
        private IEqualityComparer<T> EComparer;
        private ICloner<T> TCloner;

        //*** Constructors ***

        public Array()
        {
            Nodes = 0;
            HashCode = 0;
            Header = new Node();
            EComparer = EqualityComparer<T>.Default;
            TCloner = Cloner<T>.Default;
        }

        public Array(IEqualityComparer<T> ECompare)
        {
            Nodes = 0;
            HashCode = 0;
            Header = new Node();
            EComparer = ECompare;
            TCloner = Cloner<T>.Default;
        }

        public Array(Array<T> ArrayToCopy)
        {
            HashCode = 0;
            Nodes = 0;
            Header = new Node();
            EComparer = ArrayToCopy.EComparer;
            TCloner = ArrayToCopy.TCloner;
            Copy((ArrayNode<T>)ArrayToCopy.Root);
        }

        public Array(SerializationInfo si, StreamingContext sc)
        {
            Nodes = 0;
            HashCode = 0;
            Header = new Node();
            EComparer = (IEqualityComparer<T>)si.GetValue("EComparer", typeof(IEqualityComparer<T>));
            TCloner = (ICloner<T>)si.GetValue("TCloner", typeof(ICloner<T>));

            Type KeyType = typeof(long);
            Type ValueType = typeof(T);

            ulong Count = si.GetUInt64("Count");

            for (ulong i = 0; i < Count; i++)
            {
                String iString = i.ToString();
                object key = si.GetValue("K" + iString, KeyType);
                object value = si.GetValue("V" + iString, ValueType);
                Add((long)key, (T)value, false);
            }
        }

        public Array(IEnumerable<T> c)
        {
            Nodes = 0;
            HashCode = 0;
            Header = new Node();
            EComparer = EqualityComparer<T>.Default;
            TCloner = Cloner<T>.Default;
            foreach (T t in c)
            {
                long Index = High;
                Add(Index, TCloner.Clone(t), false);
            }
        }

        public Array(IEnumerable<T> c,
                     IEqualityComparer<T> ECompare)
        {
            Nodes = 0;
            HashCode = 0;
            Header = new Node();
            EComparer = ECompare;
            TCloner = Cloner<T>.Default;
            foreach (T t in c)
            {
                long Index = High;
                Add(Index, TCloner.Clone(t), false);
            }
        }

        //*** Indexers ***

        public T this[long index]
        {
            get
            {
                ArrayNode<T> Node = Search(index);
                return Node.Data;
            }

            set
            {
                Add(index, value, true);
            }
        }

        public SubArray<T> this[long key1, long key2]
        {
            get
            {
                Node Node1 = Search(key1);
                Node Node2 = Search(key2);

                ArrayEntry<T> upper = new ArrayEntry<T>(Node2); upper.MoveNext();

                return new SubArray<T>(Node1, upper._Node);
            }
        }

        //*** Properties ***

        public ArrayEntry<T> Begin
        { get { return new ArrayEntry<T>(LeftMost); } }

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
                Rehash();
            }
        }

        public int Count { get { return (int)Nodes; } }

        public ulong Depth { get { return Utility.Depth(Root); } }

        public double Path { get { return (double)Utility.Paths(Root, 1) / (double)Nodes; } }

        public ArrayEntry<T> End
        { get { return new ArrayEntry<T>(Header); } }

        public ArrayEntry<T> First
        {
            get
            {
                return new ArrayEntry<T>(LeftMost);
            }
        }

        public int Hash
        { get { ulong shifted = HashCode >> 32; return (int)(shifted ^ HashCode); } }

        public int High
        { get { if (Nodes == 0) return 0; else return (int)(((ArrayNode<T>)RightMost).Index + 1); } }

        public bool IsReadOnly { get { return false; } }

        public bool IsSynchronized { get { return false; } }


        public Set<long> Keys
        {
            get
            {
                Set<long> keys = new Set<long>();

                ArrayEntry<T> begin = Begin;
                ArrayEntry<T> end = End;
                while (begin != end)
                {
                    keys += begin.Key;
                    ++begin;
                }
                return keys;
            }
        }

        public ArrayEntry<T> Last
        {
            get
            {
                return new ArrayEntry<T>(RightMost);
            }
        }

        public Node LeftMost
        {
            get { return Header.Left; }
            set { Header.Left = value; }
        }

        public ulong Length { get { return Nodes; } }

        public Node RightMost
        {
            get { return Header.Right; }
            set { Header.Right = value; }
        }

        public Node Root
        {
            get { return Header.Parent; }
            set { Header.Parent = value; }
        }

        public object SyncRoot { get { return this; } }

        public List<T> Values
        {
            get
            {
                List<T> values = new List<T>(EComparer);

                ArrayEntry<T> begin = Begin;
                ArrayEntry<T> end = End;
                while (begin != end)
                {
                    values += begin.Value;
                    ++begin;
                }
                return values;
            }
        }

        //*** Operators ***

        public static Array<T> operator -(Array<T> array, long key)
        {
            array.Remove(key);
            return array;
        }

        public static bool operator ==(Array<T> A, Array<T> B)
        {
            if ((object)A == null && (object)B != null) return false;
            if ((object)A != null && (object)B == null) return false;
            if ((object)A == null && (object)B == null) return true;

            if (A.Hash != B.Hash) return false;
            return A.Equals(B);
        }

        public static bool operator !=(Array<T> A, Array<T> B)
        {
            if ((object)A == null && (object)B != null) return true;
            if ((object)A != null && (object)B == null) return true;
            if ((object)A == null && (object)B == null) return false;

            if (A.Hash != B.Hash) return true;
            return !A.Equals(B);
        }

        //*** Public Methods ***

        public ArrayEntry<T> After(long key, bool equals)
        {
            return new ArrayEntry<T>(equals ? AfterEquals(key) : After(key));
        }

        public virtual long Add(T t)
        {
            long Index = High;
            Add(Index, t, false);
            return Index;
        }

        public void Add(long Index, T Data)
        {
            Add(Index, Data, false);
        }

        public void Add(ArrayEntry<T> se)
        {
            Add(se.Key, TCloner.Clone(se.Value), false);
        }

        public void Add(KeyValue<long, T> kv)
        {
            Add(kv.Key, kv.Value, false);
        }

        public ulong Add(SubArray<T> subarray)
        {
            return Add((ArrayNode<T>)subarray.Start, (ArrayNode<T>)subarray.Finish);
        }

        public ArrayEntry<T> Before(long key, bool equals)
        {
            return new ArrayEntry<T>(equals ? BeforeEquals(key) : Before(key));
        }

        public void Clear()
        {
            Remove();
        }

        public object Clone()
        {
            Array<T> ArrayOut = new Array<T>();
            ArrayOut.EComparer = EComparer;
            ArrayOut.TCloner = TCloner;
            ArrayOut.Copy((ArrayNode<T>)Root);
            return ArrayOut;
        }

        public void Compress() { Index = 0; Compress((ArrayNode<T>)Root); }

        public bool Contains(KeyTypePredicate<long, T> predicate)
        {
            if (Nodes != 0)
            {
                ArrayEntry<T> i = Begin;
                ArrayEntry<T> e = End;
                while (i != e && !predicate(i.Key, i.Value)) ++i;
                if (i != e) return true; else return false;
            }
            else return false;
        }

        public bool Contains<P>(P Data, KeyTypeCondition<long, T, P> predicate)
        {
            if (Nodes != 0)
            {
                ArrayEntry<T> i = Begin;
                ArrayEntry<T> e = End;
                while (i != e && !predicate(i.Key, i.Value, Data)) ++i;
                if (i != e) return true; else return false;
            }
            else return false;
        }

        public bool Contains(KeyValue<long, T> kv)
        {
            ArrayNode<T> Node = Find(kv.Key);
            if (Node == null)
                return false;
            else
                return EComparer.Equals(kv.Value, Node.Data);
        }

        public bool Contains(T value)
        {
            ArrayEntry<T> begin = Begin;
            ArrayEntry<T> end = End;
            while (begin != end)
            {
                if (EComparer.Equals(value, begin.Value)) return true;
                ++begin;
            }
            return false;
        }

        public bool ContainsKey(long Index)
        {
            ArrayNode<T> Node = Find(Index);
            if (Node == null)
                return false;
            else
                return true;
        }

        public virtual void CopyTo(System.Array arr, int i)
        {
            ArrayEntry<T> begin = Begin;
            ArrayEntry<T> end = End;

            while (begin != end)
            {
                arr.SetValue(new KeyValue<long, T>(begin.Key, TCloner.Clone(begin.Value)), i);
                i++; begin.MoveNext();
            }
        }

        public override bool Equals(object obj)
        {
            Array<T> compare = (Array<T>)obj;

            ArrayEntry<T> first1 = Begin;
            ArrayEntry<T> last1 = End;
            ArrayEntry<T> first2 = compare.Begin;
            ArrayEntry<T> last2 = compare.End;

            bool equals = true;

            while (first1 != last1 && first2 != last2)
            {
                if (first1.Key == first2.Key)
                {
                    if (EComparer.Equals(first1.Value, first2.Value))
                    { first1.MoveNext(); first2.MoveNext(); }
                    else
                    { equals = false; break; }
                }
                else
                { equals = false; break; }
            }

            if (equals)
            {
                if (first1 != last1) equals = false;
                if (first2 != last2) equals = false;
            }

            return equals;
        }

        public KeyValue<long, T> Find(KeyTypePredicate<long, T> predicate)
        {
            ArrayNode<T> Node = PredicateFind(predicate);
            return new KeyValue<long, T>(Node.Index, Node.Data);
        }

        public KeyValue<long, T> Find<P>(P Data, KeyTypeCondition<long, T, P> predicate)
        {
            ArrayNode<T> Node = PredicateFind<P>(Data, predicate);
            return new KeyValue<long, T>(Node.Index, Node.Data);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return new ArrayEntry<T>(Header); }

        IEnumerator<KeyValue<long, T>> IEnumerable<KeyValue<long, T>>.GetEnumerator()
        { return new ArrayEntry<T>(Header); }

        public override int GetHashCode() { return Hash; }

        public void GetObjectData(SerializationInfo si, StreamingContext sc)
        {
            si.SetType(typeof(ISharp.Collections.Array<T>));
            Type KeyType = typeof(long);
            Type ValueType = typeof(T);
            ulong Index = 0;
            foreach (KeyValue<long, T> kv in this)
            {
                String iString = Index.ToString();
                si.AddValue("K" + iString, kv.Key, KeyType);
                si.AddValue("V" + iString, kv.Value, ValueType);
                Index++;
            }
            si.AddValue("Count", Index);
            si.AddValue("EComparer", EComparer, EComparer.GetType());
            si.AddValue("TCloner", TCloner, TCloner.GetType());
        }

        public ArrayEntry<T> Insert(long Index, T Data)
        {
            ArrayNode<T> root = (ArrayNode<T>)Root;

            if (root == null)
            {
                Root = AllocateNode(Index, Data, Header);
                LeftMost = Root;
                RightMost = Root;
                if (Added != null) Added(this, ((ArrayNode<T>)Root).Index, ((ArrayNode<T>)Root).Data);
                return new ArrayEntry<T>(Root);
            }
            else
            {
                for (; ; )
                {
                    int compare = Index.CompareTo(root.Index);

                    if (compare == 0)
                        throw new EntryAlreadyExistsException();

                    else if (compare < 0)
                    {
                        if (root.Left != null)
                            root = (ArrayNode<T>)root.Left;
                        else
                        {
                            ArrayNode<T> NewNode = AllocateNode(Index, Data, root);
                            root.Left = NewNode;
                            if (LeftMost == root) LeftMost = NewNode;
                            Utility.BalanceSet(root, Direction.FromLeft);
                            if (Added != null) Added(this, NewNode.Index, NewNode.Data);
                            return new ArrayEntry<T>(NewNode);
                        }
                    }
                    else
                    {
                        if (root.Right != null)
                            root = (ArrayNode<T>)root.Right;
                        else
                        {
                            ArrayNode<T> NewNode = AllocateNode(Index, Data, root);
                            root.Right = NewNode;
                            if (RightMost == root) RightMost = NewNode;
                            Utility.BalanceSet(root, Direction.FromRight);
                            if (Added != null) Added(this, NewNode.Index, NewNode.Data);
                            return new ArrayEntry<T>(NewNode);
                        }
                    }
                }
            }
        }

        public ArrayEntry<T> Locate(long key)
        {
            return new ArrayEntry<T>(Search(key));
        }

        public ArrayEntry<T> Locate(KeyTypePredicate<long, T> predicate)
        {
            return new ArrayEntry<T>(PredicateFind(predicate));
        }

        public ArrayEntry<T> Locate<P>(P Data, KeyTypeCondition<long, T, P> predicate)
        {
            return new ArrayEntry<T>(PredicateFind<P>(Data, predicate));
        }

        public void Notify()
        {
            if (Added != null) Notify((ArrayNode<T>)Root);
        }

        public ulong Remove()
        {
            if (Removed != null)
                foreach (KeyValue<long, T> kv in this)
                {
                    Removed(this, kv.Key, kv.Value);
                }
            Root = null;
            LeftMost = Header;
            RightMost = Header;
            ulong count = Nodes;
            Nodes = 0;
            HashCode = 0;
            return count;
        }

        public void Remove(long Index)
        {
            Node root = Root;

            for (; ; )
            {
                if (root == null)
                    throw new EntryNotFoundException();

                if (Index < ((ArrayNode<T>)root).Index)
                    root = root.Left;

                else if (Index > ((ArrayNode<T>)root).Index)
                    root = root.Right;

                else // Item is found
                {
                    if (root.Left != null && root.Right != null)
                    {
                        Node replace = root.Left;
                        while (replace.Right != null) replace = replace.Right;
                        Utility.SwapNodes(root, replace);
                    }

                    Node Parent = root.Parent;

                    Direction from = (Parent.Left == root) ? Direction.FromLeft : Direction.FromRight;

                    if (LeftMost == root)
                    {
                        Node n = Utility.NextItem(root);

                        if (n.IsHeader)
                        { LeftMost = Header; RightMost = Header; }
                        else
                            LeftMost = n;
                    }
                    else if (RightMost == root)
                    {
                        Node p = Utility.PreviousItem(root);

                        if (p.IsHeader)
                        { LeftMost = Header; RightMost = Header; }
                        else
                            RightMost = p;
                    }

                    if (root.Left == null)
                    {
                        if (Parent == Header)
                            Header.Parent = root.Right;
                        else if (Parent.Left == root)
                            Parent.Left = root.Right;
                        else
                            Parent.Right = root.Right;

                        if (root.Right != null) root.Right.Parent = Parent;
                    }
                    else
                    {
                        if (Parent == Header)
                            Header.Parent = root.Left;
                        else if (Parent.Left == root)
                            Parent.Left = root.Left;
                        else
                            Parent.Right = root.Left;

                        if (root.Left != null) root.Left.Parent = Parent;
                    }

                    Utility.BalanceSetRemove(Parent, from);
                    FreeNode((ArrayNode<T>)root);
                    break;
                }
            }
        }

        void Rehash()
        {
            HashCode = 0;
            foreach (KeyValue<long, T> entry in this)
            {
                HashCode += (ulong)entry.Key;
                HashCode += (ulong)(EComparer.GetHashCode(entry.Value));
            }
        }

        public bool Remove(ArrayEntry<T> se)
        {
            Remove(se.Key);
            return true;
        }

        public ulong Remove(KeyTypePredicate<long, T> predicate)
        {
            ulong count = 0;
            ArrayEntry<T> j = new ArrayEntry<T>();
            ArrayEntry<T> i = Begin;
            ArrayEntry<T> e = End;
            while (i != e)
            {
                if (predicate(i.Key, i.Value))
                {
                    j = i + 1;
                    Remove(((ArrayNode<T>)i._Node).Index);
                    count++;
                    i = j;
                }
                else ++i;
            }
            return count;
        }

        public ulong Remove<P>(P Data,
                               KeyTypeCondition<long, T, P> predicate)
        {
            ulong count = 0;
            ArrayEntry<T> j = new ArrayEntry<T>();
            ArrayEntry<T> i = Begin;
            ArrayEntry<T> e = End;
            while (i != e)
            {
                if (predicate(i.Key, i.Value, Data))
                {
                    j = i + 1;
                    Remove(((ArrayNode<T>)i._Node).Index);
                    count++;
                    i = j;
                }
                else ++i;
            }
            return count;
        }

        public ulong Remove(SubArray<T> subarray)
        {
            return Remove((ArrayNode<T>)subarray.Start, (ArrayNode<T>)subarray.Finish);
        }

        public void Search(KeyTypePredicate<long, T> predicate)
        {
            if (Found != null)
            {
                ArrayEntry<T> i = Begin;
                ArrayEntry<T> e = End;
                while (i != e)
                {
                    if (predicate(i.Key, i.Value)) Found(this, i.Key, i.Value);
                    ++i;
                }
            }
        }

        public void Search<P>(P Data,
                              KeyTypeCondition<long, T, P> predicate)
        {
            if (Found != null)
            {
                ArrayEntry<T> i = Begin;
                ArrayEntry<T> e = End;
                while (i != e)
                {
                    if (predicate(i.Key, i.Value, Data)) Found(this, i.Key, i.Value);
                    ++i;
                }
            }
        }

        public ArrayEntry<T> Search(T key, IComparer<T> TComparer) // Binary Search
        {
            if (Nodes != 0)
            {
                long top = ((ArrayNode<T>)RightMost).Index;
                long bottom = ((ArrayNode<T>)LeftMost).Index;
                bool Found = false;
                ArrayNode<T> result = null;
                ArrayEntry<T> entry;
                while (!Found && top >= bottom)
                {
                    long middle = BeforeEquals((top + bottom) / 2, out result);
                    int c = TComparer.Compare(key, result.Data);
                    if (c == 0)
                        Found = true;
                    else if (c < 0)
                    {
                        entry._Node = result; --entry;
                        top = ((ArrayNode<T>)entry._Node).Index;
                    }
                    else
                    {
                        entry._Node = result; ++entry;
                        bottom = ((ArrayNode<T>)entry._Node).Index;
                    }
                }
                if (Found)
                    return new ArrayEntry<T>(result);
                else
                    throw new EntryNotFoundException();
            }
            else
                throw new EntryNotFoundException();
        }

        public Array<T> Select(KeyTypePredicate<long, T> predicate)
        {
            Array<T> results = new Array<T>();
            results.EComparer = EComparer;
            results.TCloner = TCloner;
            PredicateSelect(predicate, results);
            return results;
        }

        public Array<T> Select<P>(P Data,
                                  KeyTypeCondition<long, T, P> predicate)
        {
            Array<T> results = new Array<T>();
            results.EComparer = EComparer;
            results.TCloner = TCloner;
            PredicateSelect<P>(Data, predicate, results);
            return results;
        }

        public void Shift(long Start,
                          ulong Amount,
                          bool Up)
        {
            if ((ulong)SignedLimits.MaximumLong < Amount)
                throw new InvalidShiftParametersException();

            if (Up)
                ShiftUp(Start, Amount);
            else
                ShiftDown(Start, Amount);
        }

        void ShiftUp(long Start,
                     ulong AmountSet)
        {
            if ((long)SignedLimits.MaximumLong - (long)AmountSet < Start)
                throw new InvalidShiftParametersException();

            Index = Start;
            Amount = AmountSet;

            ShiftUp((ArrayNode<T>)Root);
        }

        void ShiftUp(ArrayNode<T> root)
        {
            if (root != null)
            {
                if (root.Left != null)
                    ShiftUp((ArrayNode<T>)root.Left);

                if (root.Right != null)
                    ShiftUp((ArrayNode<T>)root.Right);

                if (root.Index >= Index)
                    root.Index += (long)Amount;
            }
        }

        void ShiftDown(long Start,
                       ulong AmountSet)
        {
            if ((long)SignedLimits.MinimumLong + (long)AmountSet > Start)
                throw new InvalidShiftParametersException();

            Index = Start;
            Amount = AmountSet;

            ShiftDown((ArrayNode<T>)Root);
        }

        void ShiftDown(ArrayNode<T> root)
        {
            if (root != null)
            {
                if (root.Left != null)
                    ShiftDown((ArrayNode<T>)root.Left);

                if (root.Right != null)
                    ShiftDown((ArrayNode<T>)root.Right);

                if (root.Index <= Index)
                    root.Index -= (long)Amount;
            }
        }

        public void Sort(IComparer<T> TComparer)
        { ArrayUtility<T>.SortArray(LeftMost, Header, TComparer); }

        public override String ToString()
        {
            String StringOut = "{";
            foreach (KeyValue<long, T> e in this)
            {
                String NewStringOut = "(" + e.Key.ToString() + "," + e.Value.ToString() + ")";
                if (e.Key != Last.Key) NewStringOut = NewStringOut + ",";
                StringOut = StringOut + NewStringOut;
            }
            StringOut = StringOut + "}";
            return StringOut;
        }

        public bool TryGetValue(long key,
                                out T value)
        {
            ArrayNode<T> Node = Find(key);
            if (Node == null)
            { value = default(T); return false; }
            else
            {
                value = Node.Data;
                return true;
            }
        }

        public void Update(ArrayEntry<T> entry, T value)
        {
            Update((ArrayNode<T>)entry._Node, value);
        }

        public void Validate()
        {
            if (Nodes == 0 || Root == null)
            {
                if (Nodes != 0) throw new InvalidEmptyTreeException();
                if (Root != null) throw new InvalidEmptyTreeException();
                if (LeftMost != Header) throw new InvalidEndItemException();
                if (RightMost != Header) throw new InvalidEndItemException();
            }

            Validate((ArrayNode<T>)Root);

            if (Root != null)
            {
                Node x = Root;
                while (x.Left != null) x = x.Left;

                if (LeftMost != x) throw new InvalidEndItemException();

                Node y = Root;
                while (y.Right != null) y = y.Right;

                if (RightMost != y) throw new InvalidEndItemException();
            }
        }

        //*** Private Methods ***

        Node After(long Index)
        {
            Node y = Header;
            Node x = Root;

            while (x != null)
                if (Index < ((ArrayNode<T>)x).Index)
                { y = x; x = x.Left; }
                else
                    x = x.Right;

            return y;
        }

        Node AfterEquals(long Index)
        {
            Node y = Header;
            Node x = Root;

            while (x != null)
            {
                if (Index == ((ArrayNode<T>)x).Index)
                { y = x; break; }
                else if (Index < ((ArrayNode<T>)x).Index)
                { y = x; x = x.Left; }
                else
                    x = x.Right;
            }

            return y;
        }

        public void Add(long Index, T Data, bool existsOk)
        {
            Node root = Root;

            if (root == null)
            {
                Root = AllocateNode(Index, Data, Header);
                LeftMost = Root;
                RightMost = Root;
                if (Added != null) Added(this, ((ArrayNode<T>)Root).Index, ((ArrayNode<T>)Root).Data);
            }
            else
            {
                for (; ; )
                {
                    if (Index == ((ArrayNode<T>)root).Index)
                    {
                        if (existsOk)
                        {
                            if (Updated != null)
                            {
                                T saved = ((ArrayNode<T>)root).Data;
                                ((ArrayNode<T>)root).Data = Data;
                                Updated(this, Index, saved, Data);
                            }
                            else ((ArrayNode<T>)root).Data = Data;
                            break;
                        }
                        else
                            throw new EntryAlreadyExistsException();
                    }

                    else if (Index < ((ArrayNode<T>)root).Index)
                    {
                        if (root.Left != null)
                            root = root.Left;
                        else
                        {
                            ArrayNode<T> NewNode = AllocateNode(Index, Data, root);
                            root.Left = NewNode;
                            if (LeftMost == root) LeftMost = NewNode;
                            Utility.BalanceSet(root, Direction.FromLeft);
                            if (Added != null) Added(this, NewNode.Index, NewNode.Data);
                            break;
                        }
                    }
                    else
                    {
                        if (root.Right != null)
                            root = root.Right;
                        else
                        {
                            ArrayNode<T> NewNode = AllocateNode(Index, Data, root);
                            root.Right = NewNode;
                            if (RightMost == root) RightMost = NewNode;
                            Utility.BalanceSet(root, Direction.FromRight);
                            if (Added != null) Added(this, NewNode.Index, NewNode.Data);
                            break;
                        }
                    }
                }
            }
        }

        ulong Add(ArrayNode<T> begin, ArrayNode<T> end)
        {
            bool success = true;
            ulong count = 0;

            ArrayEntry<T> i = new ArrayEntry<T>(begin);

            while (success && i._Node != end)
            {
                if (!i._Node.IsHeader)
                {
                    try
                    {
                        Add(i.Key, TCloner.Clone(i.Value), false);
                        count++;
                        i.MoveNext();
                    }
                    catch (Exception) { success = false; }
                }
                else i.MoveNext();
            }
            if (!success)
            {
                if (count != 0)
                {
                    i.MovePrevious();
                    ArrayEntry<T> start = new ArrayEntry<T>(begin); start.MovePrevious();

                    while (i != start)
                    {
                        ArrayEntry<T> j = new ArrayEntry<T>(i._Node); j.MovePrevious();
                        if (!i._Node.IsHeader) Remove(((ArrayNode<T>)i._Node).Index);
                        i = j;
                    }
                }
                throw new AddSubTreeFailedException();
            }
            return count;
        }

        ArrayNode<T> AllocateNode(long Index, T Data, Node Parent)
        {
            ArrayNode<T> Node = new ArrayNode<T>(Index, Data, Parent);
            Nodes++;
            HashCode += (ulong)Index;
            HashCode += (ulong)(EComparer.GetHashCode(Node.Data));
            return Node;
        }

        long BeforeEquals(long Index,
                          out ArrayNode<T> Node)  // For Binary Search
        {
            Node y = null;
            Node x = Root;

            while (x != null)
            {
                if (Index == ((ArrayNode<T>)x).Index)
                { y = x; break; }
                else if (Index < ((ArrayNode<T>)x).Index)
                    x = x.Left;
                else
                { y = x; x = x.Right; }
            }

            if (y == null) throw new EntryNotFoundException();
            Node = (ArrayNode<T>)y;
            return ((ArrayNode<T>)y).Index;
        }

        Node Before(long Index)
        {
            Node y = Header;
            Node x = Root;

            while (x != null)
                if (Index <= ((ArrayNode<T>)x).Index)
                { x = x.Left; }
                else
                { y = x; x = x.Right; }

            return y;
        }

        Node BeforeEquals(long Index)
        {
            Node y = Header;
            Node x = Root;

            while (x != null)
            {
                if (Index == ((ArrayNode<T>)x).Index)
                { y = x; break; }
                else if (Index < ((ArrayNode<T>)x).Index)
                    x = x.Left;
                else
                { y = x; x = x.Right; }
            }

            return y;
        }

        void Compress(ArrayNode<T> root)
        {
            if (root.Left != null)
                Compress((ArrayNode<T>)root.Left);

            root.Index = Index;
            Index++;

            if (root.Right != null)
                Compress((ArrayNode<T>)root.Right);
        }

        public void Copy(ArrayNode<T> CopyRoot)
        {
            if (Root != null) Remove();
            if (CopyRoot != null)
            {
                Copy(ref Header.Parent, CopyRoot, Header);
                LeftMost = GetFirst();
                RightMost = GetLast();
            }
        }

        void Copy(ref Node root, ArrayNode<T> CopyRoot, Node Parent)
        {
            root = AllocateNode(CopyRoot.Index, TCloner.Clone(CopyRoot.Data), Parent);

            root.Balance = CopyRoot.Balance;

            if (CopyRoot.Left != null)
                Copy(ref root.Left, (ArrayNode<T>)CopyRoot.Left, (ArrayNode<T>)root);

            if (CopyRoot.Right != null)
                Copy(ref root.Right, (ArrayNode<T>)CopyRoot.Right, (ArrayNode<T>)root);

            if (Added != null) Added(this, ((ArrayNode<T>)root).Index, ((ArrayNode<T>)root).Data);
        }

        public ArrayNode<T> Find(long Index)
        {
            if (Root == null)
                return null;
            else
            {
                ArrayNode<T> search = (ArrayNode<T>)Root;

                do
                {
                    if (Index < search.Index) search = (ArrayNode<T>)search.Left;

                    else if (Index > search.Index) search = (ArrayNode<T>)search.Right;

                    else break;

                } while (search != null);

                return search;
            }
        }

        void FreeNode(ArrayNode<T> Node)
        {
            HashCode -= (ulong)(Node.Index);
            HashCode -= (ulong)(EComparer.GetHashCode(Node.Data));
            Nodes--;
            if (Removed != null) Removed(this, Node.Index, Node.Data);
        }

        Node GetFirst()
        {
            if (Root == null)
                return Header;

            else
            {
                Node search = Root;
                while (search.Left != null) search = search.Left;
                return search;
            }
        }

        Node GetLast()
        {
            if (Root == null)
                return Header;

            else
            {
                Node search = Root;
                while (search.Right != null) search = search.Right;
                return search;
            }
        }

        void Notify(ArrayNode<T> root)
        {
            if (root != null)
            {
                if (root.Left != null)
                    Notify((ArrayNode<T>)root.Left);

                Added(this, root.Index, root.Data);

                if (root.Right != null)
                    Notify((ArrayNode<T>)root.Right);
            }
        }

        public void PredicateSelect(KeyTypePredicate<long, T> predicate,
                                    Array<T> results)
        {
            ArrayEntry<T> i = Begin;
            ArrayEntry<T> e = End;
            while (i != e)
            {
                if (predicate(i.Key, i.Value)) results.Add(i.Key, i.Value, false);
                ++i;
            }
        }

        public void PredicateSelect<P>(P Data,
                                       KeyTypeCondition<long, T, P> predicate,
                                       Array<T> results)
        {
            ArrayEntry<T> i = Begin;
            ArrayEntry<T> e = End;
            while (i != e)
            {
                if (predicate(i.Key, i.Value, Data)) results.Add(i.Key, i.Value, false);
                ++i;
            }
        }

        public ArrayNode<T> PredicateFind(KeyTypePredicate<long, T> predicate)
        {
            if (Nodes != 0)
            {
                ArrayEntry<T> i = Begin;
                ArrayEntry<T> e = End;
                while (i != e && !predicate(i.Key, i.Value)) ++i;
                if (i != e) return (ArrayNode<T>)i._Node;
                else throw new EntryNotFoundException();
            }
            else throw new EntryNotFoundException();
        }

        public ArrayNode<T> PredicateFind<P>(P Data,
                                             KeyTypeCondition<long, T, P> predicate)
        {
            if (Nodes != 0)
            {
                ArrayEntry<T> i = Begin;
                ArrayEntry<T> e = End;
                while (i != e && !predicate(i.Key, i.Value, Data)) ++i;
                if (i != e) return (ArrayNode<T>)i._Node;
                else throw new EntryNotFoundException();
            }
            else throw new EntryNotFoundException();
        }

        public void Remove(ArrayNode<T> root)
        {
            if (root.Left != null && root.Right != null)
            {
                Node replace = root.Left;
                while (replace.Right != null) replace = replace.Right;
                Utility.SwapNodes(root, replace);
            }

            Node Parent = root.Parent;

            Direction from = (Parent.Left == root) ? Direction.FromLeft : Direction.FromRight;

            if (LeftMost == root)
            {
                Node n = Utility.NextItem(root);

                if (n.IsHeader)
                { LeftMost = Header; RightMost = Header; }
                else
                    LeftMost = n;
            }
            else if (RightMost == root)
            {
                Node p = Utility.PreviousItem(root);

                if (p.IsHeader)
                { LeftMost = Header; RightMost = Header; }
                else
                    RightMost = p;
            }

            if (root.Left == null)
            {
                if (Parent == Header)
                    Header.Parent = root.Right;
                else if (Parent.Left == root)
                    Parent.Left = root.Right;
                else
                    Parent.Right = root.Right;

                if (root.Right != null) root.Right.Parent = Parent;
            }
            else
            {
                if (Parent == Header)
                    Header.Parent = root.Left;
                else if (Parent.Left == root)
                    Parent.Left = root.Left;
                else
                    Parent.Right = root.Left;

                if (root.Left != null) root.Left.Parent = Parent;
            }

            Utility.BalanceSetRemove(Parent, from);
            FreeNode(root);
        }

        public ulong Remove(ArrayNode<T> i, ArrayNode<T> j)
        {
            if (i == LeftMost && j == Header)
                return Remove();
            else
            {
                ulong count = 0;
                while (i != j)
                {
                    ArrayEntry<T> iter = new ArrayEntry<T>(i); iter.MoveNext();
                    if (i != Header) { Remove(i.Index); count++; }
                    i = (ArrayNode<T>)iter._Node;
                }
                return count;
            }
        }

        public void Update(ArrayNode<T> Node, T value)
        {
            HashCode -= (ulong)(EComparer.GetHashCode(Node.Data));
            HashCode += (ulong)(EComparer.GetHashCode(value));

            if (Updated != null)
            {
                T saved = Node.Data;
                Node.Data = value;
                Updated(this, Node.Index, saved, value);
            }
            else Node.Data = value;
        }

        void Validate(ArrayNode<T> root)
        {
            if (root == null) return;

            if (root.Left != null)
            {
                ArrayNode<T> Left = (ArrayNode<T>)root.Left;

                if (Left.Index >= root.Index)
                    throw new OutOfKeyOrderException();

                if (Left.Parent != root)
                    throw new TreeInvalidParentException();

                Validate((ArrayNode<T>)root.Left);
            }

            if (root.Right != null)
            {
                ArrayNode<T> Right = (ArrayNode<T>)root.Right;

                if (Right.Index <= root.Index)
                    throw new OutOfKeyOrderException();

                if (Right.Parent != root)
                    throw new TreeInvalidParentException();

                Validate((ArrayNode<T>)root.Right);
            }

            ulong DepthLeft = root.Left != null ? Utility.Depth(root.Left) : 0;
            ulong DepthRight = root.Right != null ? Utility.Depth(root.Right) : 0;

            if (DepthLeft > DepthRight && DepthLeft - DepthRight > 2)
                throw new TreeOutOfBalanceException();

            if (DepthLeft < DepthRight && DepthRight - DepthLeft > 2)
                throw new TreeOutOfBalanceException();
        }

        public ArrayNode<T> Search(long Index)
        {
            if (Root == null)
                throw new EntryNotFoundException();
            else
            {
                ArrayNode<T> search = (ArrayNode<T>)Root;

                do
                {
                    if (Index < search.Index) search = (ArrayNode<T>)search.Left;

                    else if (Index > search.Index) search = (ArrayNode<T>)search.Right;

                    else break;

                } while (search != null);

                if (search == null) throw new EntryNotFoundException();

                return search;
            }
        }
    } // end of class Array

} // end of namespace ISharp.Collections

