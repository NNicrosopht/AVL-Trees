// Copyright (c) NNicrosopht 1989-2015

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

//**************************************************************************
//******************************** Sets ************************************
//**************************************************************************

namespace ISharp.Collections
{

    public class SetNode<T> : Node
    {
        public T Data;

        public SetNode(T dataType, Node Parent)
            : base(Parent)
        {
            Data = dataType;
        }
    }

    public struct SetEntry<T> : IEnumerator<T>
    {
        public SetEntry(Node n) { _Node = n; }

        public T Value
        {
            get
            {
                if (_Node.Balance == State.Header) throw new IsEndItemException();
                return ((SetNode<T>)_Node).Data;
            }
            set
            {
                if (_Node.Balance == State.Header) throw new IsEndItemException();
                ((SetNode<T>)_Node).Data = value;
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

        public static SetEntry<T> operator ++(SetEntry<T> entry)
        {
            entry._Node = Utility.NextItem(entry._Node);
            return entry;
        }

        public static SetEntry<T> operator --(SetEntry<T> entry)
        {
            entry._Node = Utility.PreviousItem(entry._Node);
            return entry;
        }

        public static SetEntry<T> operator +(SetEntry<T> C, ulong Increment)
        {
            SetEntry<T> Result = new SetEntry<T>(C._Node);
            for (ulong i = 0; i < Increment; i++) ++Result;
            return Result;
        }

        public static SetEntry<T> operator +(ulong Increment, SetEntry<T> C)
        {
            SetEntry<T> Result = new SetEntry<T>(C._Node);
            for (ulong i = 0; i < Increment; i++) ++Result;
            return Result;
        }

        public static SetEntry<T> operator -(SetEntry<T> C, ulong Decrement)
        {
            SetEntry<T> Result = new SetEntry<T>(C._Node);
            for (ulong i = 0; i < Decrement; i++) --Result;
            return Result;
        }

        public void Reset()
        {
            _Node = Utility.GetEndItem(_Node);
        }

        object System.Collections.IEnumerator.Current
        { get { return ((SetNode<T>)_Node).Data; } }

        T IEnumerator<T>.Current
        { get { return ((SetNode<T>)_Node).Data; } }

        public static bool operator ==(SetEntry<T> x, SetEntry<T> y) { return x._Node == y._Node; }
        public static bool operator !=(SetEntry<T> x, SetEntry<T> y) { return x._Node != y._Node; }

        public override int GetHashCode()
        {
            return ((SetNode<T>)_Node).Data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ((SetNode<T>)_Node).Data.Equals(obj);
        }


        public static long operator -(SetEntry<T> This, SetEntry<T> iter)
        {
            long Result = 0;
            while (This._Node != iter._Node) { iter.MoveNext(); Result++; }
            return Result;
        }

        public override string ToString()
        {
            if (_Node.Balance == State.Header) throw new IsEndItemException();
            return Value.ToString();
        }

        public void Dispose() { }

        public Node _Node;
    }

    [Serializable]
    public class Set<T> : ICollection<T>,
                          ICloneable,
                          ISerializable,
                          IComparable<Set<T>>,
                          IEquatable<Set<T>>
    {
        protected Node Header;
        protected ulong Nodes;
        public event TypeAdded<T> Added;
        public event TypeRemoved<T> Removed;
        public event TypeUpdated<T> Updated;
        protected IComparer<T> TComparer;
        protected ICloner<T> TCloner;
        protected IHasher<T> THasher;

        //*** Constructors ***

        public Set()
        {
            Nodes = 0;
            Header = new Node();
            TComparer = Comparer<T>.Default;
            TCloner = Cloner<T>.Default;
            THasher = Hasher<T>.Default;
        }

        public Set(IComparer<T> TCompare)
        {
            Nodes = 0;
            Header = new Node();
            TComparer = TCompare;
            TCloner = Cloner<T>.Default;
            THasher = Hasher<T>.Default;
        }

        public Set(Set<T> SetToCopy)
        {
            Nodes = 0;
            Header = new Node();
            TComparer = SetToCopy.TComparer;
            TCloner = SetToCopy.TCloner;
            THasher = SetToCopy.THasher;
            Copy((SetNode<T>)SetToCopy.Root);
        }

        public Set(IEnumerable<T> Collection)
        {
            Nodes = 0;
            Header = new Node();
            TComparer = Comparer<T>.Default;
            TCloner = Cloner<T>.Default;
            THasher = Hasher<T>.Default;
            foreach (T t in Collection)
                Add(TCloner.Clone(t));
        }

        public Set(IEnumerable<T> Collection,
                   IComparer<T> TCompare)
        {
            Nodes = 0;
            Header = new Node();
            TComparer = TCompare;
            TCloner = Cloner<T>.Default;
            THasher = Hasher<T>.Default;
            foreach (T t in Collection)
                Add(TCloner.Clone(t));
        }

        public Set(Set<T> A,
                   Set<T> B,
                   SetOperation operation)
        {
            Nodes = 0;
            Header = new Node();
            TComparer = A.TComparer;
            TCloner = A.TCloner;
            THasher = A.THasher;
            CombineSets(A, B, this, operation);
        }

        public Set(SerializationInfo si, StreamingContext sc)
        {
            IComparer<T> TCompare = (IComparer<T>)si.GetValue("TComparer", typeof(IComparer<T>));
            ICloner<T> TClone = (ICloner<T>)si.GetValue("TCloner", typeof(ICloner<T>));
            IHasher<T> THasher = (IHasher<T>)si.GetValue("THasher", typeof(IHasher<T>));

            Nodes = 0;
            Header = new Node();
            TComparer = TCompare;
            TCloner = TClone;

            Type type = typeof(T);

            ulong LoadCount = si.GetUInt64("Count");

            for (ulong i = 0; i < LoadCount; i++)
            {
                object obj = si.GetValue(i.ToString(), type);
                Add((T)obj, false);
            }
        }

        //*** Operators ***

        public static Set<T> operator |(Set<T> A, Set<T> B)
        {
            Set<T> U = new Set<T>(A.TComparer);
            U.TCloner = A.TCloner;
            U.THasher = A.THasher;
            CombineSets(A, B, U, SetOperation.Union);
            return U;
        }

        public static Set<T> operator &(Set<T> A, Set<T> B)
        {
            Set<T> I = new Set<T>(A.TComparer);
            I.TCloner = A.TCloner;
            I.THasher = A.THasher;
            CombineSets(A, B, I, SetOperation.Intersection);
            return I;
        }

        public static Set<T> operator ^(Set<T> A, Set<T> B)
        {
            Set<T> S = new Set<T>(A.TComparer);
            S.TCloner = A.TCloner;
            S.THasher = A.THasher;
            CombineSets(A, B, S, SetOperation.SymmetricDifference);
            return S;
        }

        public static Set<T> operator -(Set<T> A, Set<T> B)
        {
            Set<T> S = new Set<T>(A.TComparer);
            S.TCloner = A.TCloner;
            S.THasher = A.THasher;
            CombineSets(A, B, S, SetOperation.Difference);
            return S;
        }

        public static bool operator ==(Set<T> A, Set<T> B)
        {
            return CheckSets(A, B, SetOperation.Equality);
        }

        public override bool Equals(object obj)
        {
            return this == (Set<T>)obj;
        }

        public static bool operator !=(Set<T> A, Set<T> B)
        {
            return CheckSets(A, B, SetOperation.Inequality);
        }

        public static bool operator <(Set<T> A, Set<T> B)
        {
            return CheckSets(A, B, SetOperation.Subset);
        }

        public static bool operator >(Set<T> A, Set<T> B)
        {
            return CheckSets(A, B, SetOperation.Superset);
        }

        public bool this[T key]
        {
            get
            {
                if (Root == null)
                    return false;
                else
                {
                    Node search = Root;

                    do
                    {
                        int Result = TComparer.Compare(key, ((SetNode<T>)search).Data);

                        if (Result < 0) search = search.Left;

                        else if (Result > 0) search = search.Right;

                        else break;

                    } while (search != null);

                    return search != null;
                }

            }
        }

        public static Set<T> operator +(Set<T> set, T t)
        {
            set.Add(t, false);
            return set;
        }

        public static Set<T> operator -(Set<T> set, T t)
        {
            set.Remove(t);
            return set;
        }

        //*** Properties ***

        public SetEntry<T> Begin
        { get { return new SetEntry<T>(Header.Left); } }

        public ICloner<T> Cloner
        {
            get { return TCloner; }
            set { TCloner = value; }
        }

        public IComparer<T> Comparer
        {
            get { return TComparer; }
        }

        public int Count { get { return (int)Nodes; } }

        public ulong Depth { get { return Utility.Depth(Root); } }

        public SetEntry<T> End
        { get { return new SetEntry<T>(Header); } }

        public T First
        { get { return ((SetNode<T>)LeftMost).Data; } }

        public int Hash { get { return GetHashCode(); } }

        public IHasher<T> Hasher
        {
            get { return THasher; }
            set { THasher = value; }
        }

        public bool IsReadOnly { get { return false; } }

        public bool IsSynchronized { get { return false; } }

        public T Last
        { get { return ((SetNode<T>)RightMost).Data; } }

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

        //*** Public Methods ***

        public SetEntry<T> After(T value, bool equals)
        {
            return new SetEntry<T>(equals ? AfterEquals(value) : After(value));
        }

        public virtual void Add(T t)
        {
            Add(t, false);
        }

        public void Add(SetEntry<T> cse)
        {
            Add(TCloner.Clone(cse.Value), false);
        }

        public ulong Add(IEnumerable<T> copy)
        {
            ulong count = 0;
            foreach (T t in copy)
            {
                Add(TCloner.Clone(t), false);
                count++;
            }
            return count;
        }

        public SetEntry<T> Before(T value, bool equals)
        {
            return new SetEntry<T>(equals ? BeforeEquals(value) : Before(value));
        }

        public virtual void Clear()
        {
            Remove();
        }

        public void CallRemoved(T data)
        {
            if (Removed != null) Removed(this, data);
        }

        public virtual object Clone()
        {
            Set<T> setOut = new Set<T>(TComparer);
            setOut.TCloner = TCloner;
            setOut.THasher = THasher;
            setOut.Copy((SetNode<T>)Root);
            return setOut;
        }

        public int CompareTo(Set<T> B)
        {
            return CompareSets(this, B);
        }

        public virtual bool Contains(T t)
        {
            Node found = Search(t);
            return found != null ? true : false;
        }

        public virtual bool Contains(Set<T> ss)
        {
            foreach (T t in ss)
                if (Search(t) == null)
                    return false;
            return true;
        }

        public virtual void CopyTo(T[] arr, int i)
        {
            SetEntry<T> begin = new SetEntry<T>((SetNode<T>)Header.Left);
            SetEntry<T> end = new SetEntry<T>(Header);

            while (begin != end)
            {
                arr.SetValue(TCloner.Clone(((SetNode<T>)begin._Node).Data), i);
                i++; begin.MoveNext();
            }
        }


        public virtual void CopyTo(System.Array arr, int i)
        {
            SetEntry<T> begin = new SetEntry<T>((SetNode<T>)Header.Left);
            SetEntry<T> end = new SetEntry<T>(Header);

            while (begin != end)
            {
                arr.SetValue(TCloner.Clone(((SetNode<T>)begin._Node).Data), i);
                i++; begin.MoveNext();
            }
        }

        public bool Equals(Set<T> compare)
        {
            SetEntry<T> first1 = Begin;
            SetEntry<T> last1 = End;
            SetEntry<T> first2 = compare.Begin;
            SetEntry<T> last2 = compare.End;

            bool equals = true;

            while (first1 != last1 && first2 != last2)
            {
                if (TComparer.Compare(first1.Value, first2.Value) == 0)
                { first1.MoveNext(); first2.MoveNext(); }
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

        public T Find(T value)
        {
            Node _Node = Search(value);
            if (_Node == null)
                throw new EntryNotFoundException();
            return ((SetNode<T>)_Node).Data;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return new SetEntry<T>(Header); }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        { return new SetEntry<T>(Header); }

        public override int GetHashCode()
        {
            int HashCode = 0;

            foreach (T t in this)
                HashCode += THasher.GetHashCode(t);

            return HashCode;
        }

        public virtual void GetObjectData(SerializationInfo si, StreamingContext sc)
        {
            si.SetType(typeof(ISharp.Collections.Set<T>));
            Type type = typeof(T);
            ulong index = 0;
            foreach (T e in this)
            {
                si.AddValue(index.ToString(), e, type);
                index++;
            }
            si.AddValue("Count", index);
            si.AddValue("TComparer", TComparer, TComparer.GetType());
            si.AddValue("TCloner", TCloner, TCloner.GetType());
            si.AddValue("THasher", THasher, THasher.GetType());
        }

        public SetEntry<T> Insert(T t)
        {
            return new SetEntry<T>(Add(t, false));
        }

        public SetEntry<T> Locate(T value)
        {
            Node _Node = Search(value);
            if (_Node == null)
                throw new EntryNotFoundException();
            return new SetEntry<T>(_Node);
        }


        public void Notify()
        {
            if (Added != null) Notify((SetNode<T>)Root);
        }

        public ulong Remove()
        {
            if (Removed != null)
                foreach (T t in this)
                {
                    Removed(this, t);
                }
            ulong count = Nodes;
            Root = null;
            LeftMost = Header;
            RightMost = Header;
            Nodes = 0;
            return count;
        }

        public virtual bool Remove(T data)
        {
            Node root = Root;

            for (; ; )
            {
                if (root == null)
                    throw new EntryNotFoundException();

                int compare = TComparer.Compare(data, ((SetNode<T>)root).Data);

                if (compare < 0)
                    root = root.Left;

                else if (compare > 0)
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

                    Direction From = (Parent.Left == root) ? Direction.FromLeft : Direction.FromRight;

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

                    Utility.BalanceSetRemove(Parent, From);

                    Nodes--;
                    if (Removed != null) Removed(this, ((SetNode<T>)root).Data);

                    break;
                }
            }

            return true;
        }

        public void Remove(SetEntry<T> i)
        {
            Remove(i._Node);
        }

        public Node Search(T data)
        {
            if (Root == null)
                return null;
            else
            {
                Node search = Root;

                do
                {
                    int Result = TComparer.Compare(data, ((SetNode<T>)search).Data);

                    if (Result < 0) search = search.Left;

                    else if (Result > 0) search = search.Right;

                    else break;

                } while (search != null);

                return search;
            }
        }

        public override string ToString()
        {
            string StringOut = "{";

            SetEntry<T> start = Begin;
            SetEntry<T> end = End;
            SetEntry<T> last = End - 1;

            while (start != end)
            {
                string NewStringOut = start.Value.ToString();
                if (start != last) NewStringOut = NewStringOut + ",";
                StringOut = StringOut + NewStringOut;
                ++start;
            }

            StringOut = StringOut + "}";
            return StringOut;
        }

        public void Update(T value)
        {
            if (Root == null)
                throw new EntryNotFoundException();
            else
            {
                Node search = Root;

                do
                {
                    int Result = TComparer.Compare(value, ((SetNode<T>)search).Data);

                    if (Result < 0) search = search.Left;

                    else if (Result > 0) search = search.Right;

                    else break;

                } while (search != null);

                if (search == null) throw new EntryNotFoundException();

                if (Updated != null)
                {
                    T saved = ((SetNode<T>)search).Data;
                    ((SetNode<T>)search).Data = value;
                    Updated(this, saved, value);
                }
                else ((SetNode<T>)search).Data = value;
            }
        }

        public void Update(SetEntry<T> entry, T after)
        {
            Update((SetNode<T>)entry._Node, after);
        }

        public void Validate()
        {
            if (Nodes == 0 || Root == null)
            {
                if (Nodes != 0) { throw new InvalidEmptyTreeException(); }
                if (Root != null) { throw new InvalidEmptyTreeException(); }
                if (LeftMost != Header) { throw new InvalidEndItemException(); }
                if (RightMost != Header) { throw new InvalidEndItemException(); }
            }

            Validate((SetNode<T>)Root);

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

        Node After(T data)
        {
            Node y = Header;
            Node x = Root;

            while (x != null)
                if (TComparer.Compare(data, ((SetNode<T>)x).Data) < 0)
                { y = x; x = x.Left; }
                else
                    x = x.Right;

            return y;
        }

        Node AfterEquals(T data)
        {
            Node y = Header;
            Node x = Root;

            while (x != null)
            {
                int c = TComparer.Compare(data, ((SetNode<T>)x).Data);
                if (c == 0)
                { y = x; break; }
                else if (c < 0)
                { y = x; x = x.Left; }
                else
                    x = x.Right;
            }

            return y;
        }

        protected SetNode<T> Add(T data, bool exist)
        {
            Node root = Root;

            if (root == null)
            {
                Root = new SetNode<T>(data, Header);
                Nodes++;
                LeftMost = Root;
                RightMost = Root;
                if (Added != null) Added(this, ((SetNode<T>)Root).Data);
                return (SetNode<T>)Root;
            }
            else
            {
                for (; ; )
                {
                    int compare = TComparer.Compare(data, ((SetNode<T>)root).Data);

                    if (compare == 0) // Item Exists
                    {
                        if (exist)
                        {
                            if (Updated != null)
                            {
                                T saved = ((SetNode<T>)root).Data;
                                ((SetNode<T>)root).Data = data;
                                Updated(this, saved, data);
                            }
                            else ((SetNode<T>)root).Data = data;
                            return (SetNode<T>)root;
                        }
                        else
                            throw new EntryAlreadyExistsException();
                    }

                    else if (compare < 0)
                    {
                        if (root.Left != null)
                            root = root.Left;
                        else
                        {
                            SetNode<T> NewNode = new SetNode<T>(data, root);
                            Nodes++;
                            root.Left = NewNode;
                            if (LeftMost == root) LeftMost = NewNode;
                            Utility.BalanceSet(root, Direction.FromLeft);
                            if (Added != null) Added(this, NewNode.Data);
                            return NewNode;
                        }
                    }

                    else
                    {
                        if (root.Right != null)
                            root = root.Right;
                        else
                        {
                            SetNode<T> NewNode = new SetNode<T>(data, root);
                            Nodes++;
                            root.Right = NewNode;
                            if (RightMost == root) RightMost = NewNode;
                            Utility.BalanceSet(root, Direction.FromRight);
                            if (Added != null) Added(this, NewNode.Data);
                            return NewNode;
                        }
                    }
                }
            }
        }

        ulong Add(Node begin, Node end)
        {
            bool success = true;
            ulong count = 0;

            SetEntry<T> i = new SetEntry<T>(begin);

            while (success && i._Node != end)
            {
                if (!i._Node.IsHeader)
                {
                    try
                    {
                        Add(TCloner.Clone(i.Value), false);
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
                    SetEntry<T> start = new SetEntry<T>(begin); start.MovePrevious();

                    while (i != start)
                    {
                        SetEntry<T> j = new SetEntry<T>(i._Node); j.MovePrevious();
                        if (!i._Node.IsHeader) Remove(i.Value);
                        i = j;
                    }
                }
                throw new AddSubTreeFailedException();
            }
            return count;
        }

        Node Before(T data)
        {
            Node y = Header;
            Node x = Root;

            while (x != null)
                if (TComparer.Compare(data, ((SetNode<T>)x).Data) <= 0)
                    x = x.Left;
                else
                { y = x; x = x.Right; }

            return y;
        }

        Node BeforeEquals(T data)
        {
            Node y = Header;
            Node x = Root;

            while (x != null)
            {
                int c = TComparer.Compare(data, ((SetNode<T>)x).Data);
                if (c == 0)
                { y = x; break; }
                else if (c < 0)
                    x = x.Left;
                else
                { y = x; x = x.Right; }
            }

            return y;
        }

        void Bounds()
        {
            LeftMost = GetFirst();
            RightMost = GetLast();
        }

        protected void Copy(SetNode<T> CopyRoot)
        {
            if (Root != null) Remove();
            if (CopyRoot != null)
            {
                Copy(ref Header.Parent, CopyRoot, Header);
                LeftMost = GetFirst();
                RightMost = GetLast();
            }
        }

        void Copy(ref Node root, SetNode<T> CopyRoot, Node Parent)
        {
            root = new SetNode<T>(TCloner.Clone(CopyRoot.Data), Parent);
            Nodes++;

            root.Balance = CopyRoot.Balance;

            if (CopyRoot.Left != null)
                Copy(ref root.Left, (SetNode<T>)CopyRoot.Left, (SetNode<T>)root);

            if (CopyRoot.Right != null)
                Copy(ref root.Right, (SetNode<T>)CopyRoot.Right, (SetNode<T>)root);

            if (Added != null) Added(this, ((SetNode<T>)root).Data);
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

        void Import(SetNode<T> n)
        {
            if (n != null) ImportTree(n);
        }

        void ImportTree(SetNode<T> n)
        {
            if (n.Left != null) ImportTree((SetNode<T>)n.Left);
            Add(n.Data, false);
            if (n.Right != null) ImportTree((SetNode<T>)n.Right);
        }

        void Notify(SetNode<T> root)
        {
            if (root != null)
            {
                if (root.Left != null)
                    Notify((SetNode<T>)root.Left);

                Added(this, root.Data);

                if (root.Right != null)
                    Notify((SetNode<T>)root.Right);
            }
        }

        void Remove(Node root)
        {
            if (root.Left != null && root.Right != null)
            {
                Node replace = root.Left;
                while (replace.Right != null) replace = replace.Right;
                Utility.SwapNodes(root, replace);
            }

            Node Parent = root.Parent;

            Direction From = (Parent.Left == root) ? Direction.FromLeft : Direction.FromRight;

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

            Utility.BalanceSetRemove(Parent, From);
            Nodes--;
            if (Removed != null) Removed(this, ((SetNode<T>)root).Data);
        }

        ulong Remove(Node i, Node j)
        {
            if (i == LeftMost && j == Header)
                return Remove();
            else
            {
                ulong count = 0;
                while (i != j)
                {
                    SetEntry<T> iter = new SetEntry<T>(i); iter.MoveNext();
                    if (i != Header) { Remove(i); count++; }
                    i = iter._Node;
                }
                return count;
            }
        }

        void Update(SetNode<T> Node, T value)
        {
            if (TComparer.Compare(Node.Data, value) != 0) throw new DifferentKeysException();

            if (Updated != null)
            {
                T saved = Node.Data;
                Node.Data = value;
                Updated(this, saved, value);
            }
            else Node.Data = value;
        }


        void Validate(SetNode<T> root)
        {
            if (root == null) return;

            if (root.Left != null)
            {
                SetNode<T> Left = (SetNode<T>)root.Left;

                if (TComparer.Compare(Left.Data, root.Data) >= 0)
                    throw new OutOfKeyOrderException();

                if (Left.Parent != root)
                    throw new TreeInvalidParentException();

                Validate((SetNode<T>)root.Left);
            }

            if (root.Right != null)
            {
                SetNode<T> Right = (SetNode<T>)root.Right;

                if (TComparer.Compare(Right.Data, root.Data) <= 0)
                    throw new OutOfKeyOrderException();

                if (Right.Parent != root)
                    throw new TreeInvalidParentException();

                Validate((SetNode<T>)root.Right);
            }

            ulong DepthLeft = root.Left != null ? Utility.Depth(root.Left) : 0;
            ulong DepthRight = root.Right != null ? Utility.Depth(root.Right) : 0;

            if (DepthLeft > DepthRight && DepthLeft - DepthRight > 2)
                throw new TreeOutOfBalanceException();

            if (DepthLeft < DepthRight && DepthRight - DepthLeft > 2)
                throw new TreeOutOfBalanceException();
        }

        //*** Static Methods

        public static void CombineSets(Set<T> A,
                                       Set<T> B,
                                       Set<T> R,
                                       SetOperation operation)
        {
            IComparer<T> TComparer = R.TComparer;
            ICloner<T> TCloner = R.TCloner;
            SetEntry<T> first1 = A.Begin;
            SetEntry<T> last1 = A.End;
            SetEntry<T> first2 = B.Begin;
            SetEntry<T> last2 = B.End;

            switch (operation)
            {
                case SetOperation.Union:
                    while (first1 != last1 && first2 != last2)
                    {
                        int order = TComparer.Compare(first1.Value, first2.Value);

                        if (order < 0)
                        {
                            R.Add(TCloner.Clone(first1.Value));
                            first1.MoveNext();
                        }

                        else if (order > 0)
                        {
                            R.Add(TCloner.Clone(first2.Value));
                            first2.MoveNext();
                        }

                        else
                        {
                            R.Add(TCloner.Clone(first1.Value));
                            first1.MoveNext();
                            first2.MoveNext();
                        }
                    }
                    while (first1 != last1)
                    {
                        R.Add(TCloner.Clone(first1.Value));
                        first1.MoveNext();
                    }
                    while (first2 != last2)
                    {
                        R.Add(TCloner.Clone(first2.Value));
                        first2.MoveNext();
                    }
                    return;

                case SetOperation.Intersection:
                    while (first1 != last1 && first2 != last2)
                    {
                        int order = TComparer.Compare(first1.Value, first2.Value);

                        if (order < 0)
                            first1.MoveNext();

                        else if (order > 0)
                            first2.MoveNext();

                        else
                        {
                            R.Add(TCloner.Clone(first1.Value));
                            first1.MoveNext();
                            first2.MoveNext();
                        }
                    }
                    return;

                case SetOperation.SymmetricDifference:
                    while (first1 != last1 && first2 != last2)
                    {
                        int order = TComparer.Compare(first1.Value, first2.Value);

                        if (order < 0)
                        {
                            R.Add(TCloner.Clone(first1.Value));
                            first1.MoveNext();
                        }

                        else if (order > 0)
                        {
                            R.Add(TCloner.Clone(first2.Value));
                            first2.MoveNext();
                        }

                        else
                        { first1.MoveNext(); first2.MoveNext(); }
                    }

                    while (first1 != last1)
                    {
                        R.Add(TCloner.Clone(first1.Value));
                        first1.MoveNext();
                    }

                    while (first2 != last2)
                    {
                        R.Add(TCloner.Clone(first2.Value));
                        first2.MoveNext();
                    }
                    return;

                case SetOperation.Difference:
                    while (first1 != last1 && first2 != last2)
                    {
                        int order = TComparer.Compare(first1.Value, first2.Value);

                        if (order < 0)
                        {
                            R.Add(TCloner.Clone(first1.Value));
                            first1.MoveNext();
                        }

                        else if (order > 0)
                        {
                            R.Add(TCloner.Clone(first1.Value));
                            first1.MoveNext();
                            first2.MoveNext();
                        }

                        else
                        { first1.MoveNext(); first2.MoveNext(); }
                    }

                    while (first1 != last1)
                    {
                        R.Add(TCloner.Clone(first1.Value));
                        first1.MoveNext();
                    }
                    return;
            }

            throw new InvalidSetOperationException();
        }

        public static bool CheckSets(Set<T> A,
                                     Set<T> B,
                                     SetOperation operation)
        {
            IComparer<T> TComparer = A.TComparer;
            SetEntry<T> first1 = A.Begin;
            SetEntry<T> last1 = A.End;
            SetEntry<T> first2 = B.Begin;
            SetEntry<T> last2 = B.End;

            switch (operation)
            {
                case SetOperation.Equality:
                case SetOperation.Inequality:
                    {
                        bool equals = true;

                        while (first1 != last1 && first2 != last2)
                        {
                            if (TComparer.Compare(first1.Value, first2.Value) == 0)
                            { first1.MoveNext(); first2.MoveNext(); }
                            else
                            { equals = false; break; }
                        }

                        if (equals)
                        {
                            if (first1 != last1) equals = false;
                            if (first2 != last2) equals = false;
                        }

                        if (operation == SetOperation.Equality)
                            return equals;
                        else
                            return !equals;
                    }

                case SetOperation.Subset:
                case SetOperation.Superset:
                    {
                        bool subset = true;

                        while (first1 != last1 && first2 != last2)
                        {
                            int order = TComparer.Compare(first1.Value, first2.Value);

                            if (order < 0)
                            { subset = false; break; }

                            else if (order > 0)
                                first2.MoveNext();

                            else
                            { first1.MoveNext(); first2.MoveNext(); }
                        }

                        if (subset)
                            if (first1 != last1) subset = false;

                        if (operation == SetOperation.Subset)
                            return subset;
                        else
                            return !subset;
                    }
            }

            throw new InvalidSetOperationException();
        }

        public static int CompareSets(Set<T> A,
                                      Set<T> B)
        {
            IComparer<T> TComparer = A.TComparer;
            SetEntry<T> first1 = A.Begin;
            SetEntry<T> last1 = A.End;
            SetEntry<T> first2 = B.Begin;
            SetEntry<T> last2 = B.End;

            int Result = 0;

            while (first1 != last1 && first2 != last2)
            {
                Result = TComparer.Compare(first1.Value, first2.Value);
                if (Result == 0)
                { first1.MoveNext(); first2.MoveNext(); }
                else
                    return Result;
            }

            if (first1 != last1) return 1;
            if (first2 != last2) return -1;

            return 0;
        }
    }

    public class CompactSet<T> : IEnumerable<T>,
                                 ICloneable,
                                 IComparable<CompactSet<T>> where T : ICloneable
    {
        protected Node Header;
        protected IComparer<T> TComparer;

        //*** Constructors ***

        public CompactSet()
        {
            Header = new Node();
            TComparer = Comparer<T>.Default;
        }

        public CompactSet(IComparer<T> TCompare)
        {
            Header = new Node();
            TComparer = TCompare;
        }

        public CompactSet(CompactSet<T> SetToCopy)
        {
            Header = new Node();
            TComparer = SetToCopy.TComparer;
            Copy((SetNode<T>)SetToCopy.Root);
        }

        public CompactSet(IEnumerable<T> Collection)
        {
            Header = new Node();
            TComparer = Comparer<T>.Default;
            foreach (T t in Collection)
                Add((T)t.Clone());
        }

        public CompactSet(IEnumerable<T> Collection,
                          IComparer<T> TCompare)
        {
            Header = new Node();
            TComparer = TCompare;
            foreach (T t in Collection)
                Add((T)t.Clone());
        }

        public CompactSet(CompactSet<T> A,
                          CompactSet<T> B,
                          SetOperation operation)
        {
            Header = new Node();
            TComparer = A.TComparer;
            CombineSets(A, B, this, operation);
        }

        public CompactSet(SerializationInfo si, StreamingContext sc)
        {
            IComparer<T> TCompare = (IComparer<T>)si.GetValue("TComparer", typeof(IComparer<T>));

            Header = new Node();
            TComparer = TCompare;

            Type type = typeof(T);

            ulong LoadCount = si.GetUInt64("Count");

            for (ulong i = 0; i < LoadCount; i++)
            {
                object obj = si.GetValue(i.ToString(), type);
                Add((T)obj, false);
            }
        }

        //*** Operators ***

        public static CompactSet<T> operator |(CompactSet<T> A, CompactSet<T> B)
        {
            CompactSet<T> U = new CompactSet<T>(A.TComparer);
            CombineSets(A, B, U, SetOperation.Union);
            return U;
        }

        public static CompactSet<T> operator &(CompactSet<T> A, CompactSet<T> B)
        {
            CompactSet<T> I = new CompactSet<T>(A.TComparer);
            CombineSets(A, B, I, SetOperation.Intersection);
            return I;
        }

        public static CompactSet<T> operator ^(CompactSet<T> A, CompactSet<T> B)
        {
            CompactSet<T> S = new CompactSet<T>(A.TComparer);
            CombineSets(A, B, S, SetOperation.SymmetricDifference);
            return S;
        }

        public static CompactSet<T> operator -(CompactSet<T> A, CompactSet<T> B)
        {
            CompactSet<T> S = new CompactSet<T>(A.TComparer);
            CombineSets(A, B, S, SetOperation.Difference);
            return S;
        }

        public static bool operator ==(CompactSet<T> A, CompactSet<T> B)
        {
            if ((object)A == null && (object)B != null) return false;
            if ((object)A != null && (object)B == null) return false;
            if ((object)A == null && (object)B == null) return true;

            return CheckSets(A, B, SetOperation.Equality);
        }

        public static bool operator !=(CompactSet<T> A, CompactSet<T> B)
        {
            if ((object)A == null && (object)B != null) return true;
            if ((object)A != null && (object)B == null) return true;
            if ((object)A == null && (object)B == null) return false;

            return CheckSets(A, B, SetOperation.Inequality);
        }


        public override bool Equals(object obj)
        {
            return this == (CompactSet<T>)obj;
        }


        public static bool operator <(CompactSet<T> A, CompactSet<T> B)
        {
            return CheckSets(A, B, SetOperation.Subset);
        }

        public static bool operator >(CompactSet<T> A, CompactSet<T> B)
        {
            return CheckSets(A, B, SetOperation.Superset);
        }

        public bool this[T key]
        {
            get
            {
                if (Root == null)
                    return false;
                else
                {
                    Node search = Root;

                    do
                    {
                        int Result = TComparer.Compare(key, ((SetNode<T>)search).Data);

                        if (Result < 0) search = search.Left;

                        else if (Result > 0) search = search.Right;

                        else break;

                    } while (search != null);

                    return search != null;
                }

            }
        }

        public static CompactSet<T> operator +(CompactSet<T> set, T t)
        {
            set.Add(t, false);
            return set;
        }

        public static CompactSet<T> operator -(CompactSet<T> set, T t)
        {
            set.Remove(t);
            return set;
        }

        //*** Properties ***

        public SetEntry<T> Begin
        { get { return new SetEntry<T>(Header.Left); } }

        public IComparer<T> Comparer
        {
            get { return TComparer; }
        }

        public int Count { get { return (int)Length; } }

        public ulong Depth { get { return Utility.Depth(Root); } }

        public SetEntry<T> End
        { get { return new SetEntry<T>(Header); } }

        public T First
        { get { return ((SetNode<T>)LeftMost).Data; } }

        public int Hash { get { return GetHashCode(); } }

        public bool IsReadOnly { get { return false; } }

        public bool IsSynchronized { get { return false; } }

        public T Last
        { get { return ((SetNode<T>)RightMost).Data; } }

        public Node LeftMost
        {
            get { return Header.Left; }
            set { Header.Left = value; }
        }

        public ulong Length
        {
            get
            {
                ulong Result = 0;
                foreach (T t in this) Result++;
                return Result;
            }
        }

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

        //*** Public Methods ***

        public SetEntry<T> After(T value, bool equals)
        {
            return new SetEntry<T>(equals ? AfterEquals(value) : After(value));
        }

        public void Add(T t)
        {
            Add(t, false);
        }

        public ulong Add(IEnumerable<T> copy)
        {
            ulong count = 0;
            foreach (T t in copy)
            {
                Add((T)t.Clone(), false);
                count++;
            }
            return count;
        }

        public SetEntry<T> Before(T value, bool equals)
        {
            return new SetEntry<T>(equals ? BeforeEquals(value) : Before(value));
        }

        public virtual void Clear()
        {
            Remove();
        }

        public virtual object Clone()
        {
            CompactSet<T> setOut = new CompactSet<T>(TComparer);
            setOut.Copy((SetNode<T>)Root);
            return setOut;
        }

        public int CompareTo(CompactSet<T> B)
        {
            return CompareSets(this, B);
        }

        public bool Equals(CompactSet<T> compare)
        {
            SetEntry<T> first1 = Begin;
            SetEntry<T> last1 = End;
            SetEntry<T> first2 = compare.Begin;
            SetEntry<T> last2 = compare.End;

            bool equals = true;

            while (first1 != last1 && first2 != last2)
            {
                if (TComparer.Compare(first1.Value, first2.Value) == 0)
                { first1.MoveNext(); first2.MoveNext(); }
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

        public T Find(T value)
        {
            Node _Node = Search(value);
            if (_Node == null)
                throw new EntryNotFoundException();
            return ((SetNode<T>)_Node).Data;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return new SetEntry<T>(Header); }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        { return new SetEntry<T>(Header); }

        public override int GetHashCode()
        {
            int HashCode = 0;

            foreach (T t in this)
                HashCode += t.GetHashCode();

            return HashCode;
        }

        public virtual void GetObjectData(SerializationInfo si, StreamingContext sc)
        {
            si.SetType(typeof(ISharp.Collections.CompactSet<T>));
            Type type = typeof(T);
            ulong index = 0;
            foreach (T e in this)
            {
                si.AddValue(index.ToString(), e, type);
                index++;
            }
            si.AddValue("Count", index);
            si.AddValue("TComparer", TComparer, TComparer.GetType());
        }

        public SetEntry<T> Insert(T t)
        {
            return new SetEntry<T>(Add(t, false));
        }

        public SetEntry<T> Locate(T value)
        {
            Node _Node = Search(value);
            if (_Node == null)
                throw new EntryNotFoundException();
            return new SetEntry<T>(_Node);
        }

        public void Remove()
        {
            Root = null;
            LeftMost = Header;
            RightMost = Header;
        }

        public void Remove(T data)
        {
            Node root = Root;

            for (; ; )
            {
                if (root == null)
                    throw new EntryNotFoundException();

                int compare = TComparer.Compare(data, ((SetNode<T>)root).Data);

                if (compare < 0)
                    root = root.Left;

                else if (compare > 0)
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

                    Direction From = (Parent.Left == root) ? Direction.FromLeft : Direction.FromRight;

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

                    Utility.BalanceSetRemove(Parent, From);
                    break;
                }
            }
        }

        public void Remove(SetEntry<T> i)
        {
            Remove(i._Node);
        }

        public Node Search(T data)
        {
            if (Root == null)
                return null;
            else
            {
                Node search = Root;

                do
                {
                    int Result = TComparer.Compare(data, ((SetNode<T>)search).Data);

                    if (Result < 0) search = search.Left;

                    else if (Result > 0) search = search.Right;

                    else break;

                } while (search != null);

                return search;
            }
        }

        public override string ToString()
        {
            string StringOut = "{";

            SetEntry<T> start = Begin;
            SetEntry<T> end = End;
            SetEntry<T> last = End - 1;

            while (start != end)
            {
                string NewStringOut = start.Value.ToString();
                if (start != last) NewStringOut = NewStringOut + ",";
                StringOut = StringOut + NewStringOut;
                ++start;
            }

            StringOut = StringOut + "}";
            return StringOut;
        }

        public void Update(T value)
        {
            if (Root == null)
                throw new EntryNotFoundException();
            else
            {
                Node search = Root;

                do
                {
                    int Result = TComparer.Compare(value, ((SetNode<T>)search).Data);

                    if (Result < 0) search = search.Left;

                    else if (Result > 0) search = search.Right;

                    else break;

                } while (search != null);

                if (search == null) throw new EntryNotFoundException();

                ((SetNode<T>)search).Data = value;
            }
        }

        //*** Private Methods ***

        Node After(T data)
        {
            Node y = Header;
            Node x = Root;

            while (x != null)
                if (TComparer.Compare(data, ((SetNode<T>)x).Data) < 0)
                { y = x; x = x.Left; }
                else
                    x = x.Right;

            return y;
        }

        Node AfterEquals(T data)
        {
            Node y = Header;
            Node x = Root;

            while (x != null)
            {
                int c = TComparer.Compare(data, ((SetNode<T>)x).Data);
                if (c == 0)
                { y = x; break; }
                else if (c < 0)
                { y = x; x = x.Left; }
                else
                    x = x.Right;
            }

            return y;
        }

        protected SetNode<T> Add(T data, bool exist)
        {
            Node root = Root;

            if (root == null)
            {
                Root = new SetNode<T>(data, Header);
                LeftMost = Root;
                RightMost = Root;
                return (SetNode<T>)Root;
            }
            else
            {
                for (; ; )
                {
                    int compare = TComparer.Compare(data, ((SetNode<T>)root).Data);

                    if (compare == 0) // Item Exists
                    {
                        if (exist)
                        {
                            ((SetNode<T>)root).Data = data;
                            return (SetNode<T>)root;
                        }
                        else
                            throw new EntryAlreadyExistsException();
                    }

                    else if (compare < 0)
                    {
                        if (root.Left != null)
                            root = root.Left;
                        else
                        {
                            SetNode<T> NewNode = new SetNode<T>(data, root);
                            root.Left = NewNode;
                            if (LeftMost == root) LeftMost = NewNode;
                            Utility.BalanceSet(root, Direction.FromLeft);
                            return NewNode;
                        }
                    }

                    else
                    {
                        if (root.Right != null)
                            root = root.Right;
                        else
                        {
                            SetNode<T> NewNode = new SetNode<T>(data, root);
                            root.Right = NewNode;
                            if (RightMost == root) RightMost = NewNode;
                            Utility.BalanceSet(root, Direction.FromRight);
                            return NewNode;
                        }
                    }
                }
            }
        }

        ulong Add(Node begin, Node end)
        {
            bool success = true;
            ulong count = 0;

            SetEntry<T> i = new SetEntry<T>(begin);

            while (success && i._Node != end)
            {
                if (!i._Node.IsHeader)
                {
                    try
                    {
                        Add((T)i.Value.Clone(), false);
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
                    SetEntry<T> start = new SetEntry<T>(begin); start.MovePrevious();

                    while (i != start)
                    {
                        SetEntry<T> j = new SetEntry<T>(i._Node); j.MovePrevious();
                        if (!i._Node.IsHeader) Remove(i.Value);
                        i = j;
                    }
                }
                throw new AddSubTreeFailedException();
            }
            return count;
        }

        Node Before(T data)
        {
            Node y = Header;
            Node x = Root;

            while (x != null)
                if (TComparer.Compare(data, ((SetNode<T>)x).Data) <= 0)
                    x = x.Left;
                else
                { y = x; x = x.Right; }

            return y;
        }

        Node BeforeEquals(T data)
        {
            Node y = Header;
            Node x = Root;

            while (x != null)
            {
                int c = TComparer.Compare(data, ((SetNode<T>)x).Data);
                if (c == 0)
                { y = x; break; }
                else if (c < 0)
                    x = x.Left;
                else
                { y = x; x = x.Right; }
            }

            return y;
        }

        void Bounds()
        {
            LeftMost = GetFirst();
            RightMost = GetLast();
        }

        protected void Copy(SetNode<T> CopyRoot)
        {
            if (Root != null) Remove();
            if (CopyRoot != null)
            {
                Copy(ref Header.Parent, CopyRoot, Header);
                LeftMost = GetFirst();
                RightMost = GetLast();
            }
        }

        void Copy(ref Node root, SetNode<T> CopyRoot, Node Parent)
        {
            root = new SetNode<T>((T)CopyRoot.Data.Clone(), Parent);

            root.Balance = CopyRoot.Balance;

            if (CopyRoot.Left != null)
                Copy(ref root.Left, (SetNode<T>)CopyRoot.Left, (SetNode<T>)root);

            if (CopyRoot.Right != null)
                Copy(ref root.Right, (SetNode<T>)CopyRoot.Right, (SetNode<T>)root);
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

        void Import(SetNode<T> n)
        {
            if (n != null) ImportTree(n);
        }

        void ImportTree(SetNode<T> n)
        {
            if (n.Left != null) ImportTree((SetNode<T>)n.Left);
            Add(n.Data, false);
            if (n.Right != null) ImportTree((SetNode<T>)n.Right);
        }

        void Remove(Node root)
        {
            if (root.Left != null && root.Right != null)
            {
                Node replace = root.Left;
                while (replace.Right != null) replace = replace.Right;
                Utility.SwapNodes(root, replace);
            }

            Node Parent = root.Parent;

            Direction From = (Parent.Left == root) ? Direction.FromLeft : Direction.FromRight;

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

            Utility.BalanceSetRemove(Parent, From);
        }

        void Update(SetNode<T> Node, T value)
        {
            if (TComparer.Compare(Node.Data, value) != 0) throw new DifferentKeysException();
            Node.Data = value;
        }


        //*** Static Methods

        public static void CombineSets(CompactSet<T> A,
                                       CompactSet<T> B,
                                       CompactSet<T> R,
                                       SetOperation operation)
        {
            IComparer<T> TComparer = R.TComparer;

            SetEntry<T> first1 = A.Begin;
            SetEntry<T> last1 = A.End;
            SetEntry<T> first2 = B.Begin;
            SetEntry<T> last2 = B.End;

            switch (operation)
            {
                case SetOperation.Union:
                    while (first1 != last1 && first2 != last2)
                    {
                        int order = TComparer.Compare(first1.Value, first2.Value);

                        if (order < 0)
                        {
                            R.Add((T)first1.Value.Clone());
                            first1.MoveNext();
                        }

                        else if (order > 0)
                        {
                            R.Add((T)first2.Value.Clone());
                            first2.MoveNext();
                        }

                        else
                        {
                            R.Add((T)first1.Value.Clone());
                            first1.MoveNext();
                            first2.MoveNext();
                        }
                    }
                    while (first1 != last1)
                    {
                        R.Add((T)first1.Value.Clone());
                        first1.MoveNext();
                    }
                    while (first2 != last2)
                    {
                        R.Add((T)first2.Value.Clone());
                        first2.MoveNext();
                    }
                    return;

                case SetOperation.Intersection:
                    while (first1 != last1 && first2 != last2)
                    {
                        int order = TComparer.Compare(first1.Value, first2.Value);

                        if (order < 0)
                            first1.MoveNext();

                        else if (order > 0)
                            first2.MoveNext();

                        else
                        {
                            R.Add((T)first1.Value.Clone());
                            first1.MoveNext();
                            first2.MoveNext();
                        }
                    }
                    return;

                case SetOperation.SymmetricDifference:
                    while (first1 != last1 && first2 != last2)
                    {
                        int order = TComparer.Compare(first1.Value, first2.Value);

                        if (order < 0)
                        {
                            R.Add((T)first1.Value.Clone());
                            first1.MoveNext();
                        }

                        else if (order > 0)
                        {
                            R.Add((T)first2.Value.Clone());
                            first2.MoveNext();
                        }

                        else
                        { first1.MoveNext(); first2.MoveNext(); }
                    }

                    while (first1 != last1)
                    {
                        R.Add((T)first1.Value.Clone());
                        first1.MoveNext();
                    }

                    while (first2 != last2)
                    {
                        R.Add((T)first2.Value.Clone());
                        first2.MoveNext();
                    }
                    return;

                case SetOperation.Difference:
                    while (first1 != last1 && first2 != last2)
                    {
                        int order = TComparer.Compare(first1.Value, first2.Value);

                        if (order < 0)
                        {
                            R.Add((T)first1.Value.Clone());
                            first1.MoveNext();
                        }

                        else if (order > 0)
                        {
                            R.Add((T)first1.Value.Clone());
                            first1.MoveNext();
                            first2.MoveNext();
                        }

                        else
                        { first1.MoveNext(); first2.MoveNext(); }
                    }

                    while (first1 != last1)
                    {
                        R.Add((T)first1.Value.Clone());
                        first1.MoveNext();
                    }
                    return;
            }

            throw new InvalidSetOperationException();
        }

        public static bool CheckSets(CompactSet<T> A,
                                     CompactSet<T> B,
                                     SetOperation operation)
        {
            IComparer<T> TComparer = A.TComparer;

            SetEntry<T> first1 = A.Begin;
            SetEntry<T> last1 = A.End;
            SetEntry<T> first2 = B.Begin;
            SetEntry<T> last2 = B.End;

            switch (operation)
            {
                case SetOperation.Equality:
                case SetOperation.Inequality:
                    {
                        bool equals = true;

                        while (first1 != last1 && first2 != last2)
                        {
                            if (TComparer.Compare(first1.Value, first2.Value) == 0)
                            { first1.MoveNext(); first2.MoveNext(); }
                            else
                            { equals = false; break; }
                        }

                        if (equals)
                        {
                            if (first1 != last1) equals = false;
                            if (first2 != last2) equals = false;
                        }

                        if (operation == SetOperation.Equality)
                            return equals;
                        else
                            return !equals;
                    }

                case SetOperation.Subset:
                case SetOperation.Superset:
                    {
                        bool subset = true;

                        while (first1 != last1 && first2 != last2)
                        {
                            int order = TComparer.Compare(first1.Value, first2.Value);

                            if (order < 0)
                            { subset = false; break; }

                            else if (order > 0)
                                first2.MoveNext();

                            else
                            { first1.MoveNext(); first2.MoveNext(); }
                        }

                        if (subset)
                            if (first1 != last1) subset = false;

                        if (operation == SetOperation.Subset)
                            return subset;
                        else
                            return !subset;
                    }
            }

            throw new InvalidSetOperationException();
        }

        public static int CompareSets(CompactSet<T> A,
                                      CompactSet<T> B)
        {
            IComparer<T> TComparer = A.TComparer;

            SetEntry<T> first1 = A.Begin;
            SetEntry<T> last1 = A.End;
            SetEntry<T> first2 = B.Begin;
            SetEntry<T> last2 = B.End;

            int Result = 0;

            while (first1 != last1 && first2 != last2)
            {
                Result = TComparer.Compare(first1.Value, first2.Value);
                if (Result == 0)
                { first1.MoveNext(); first2.MoveNext(); }
                else
                    return Result;
            }

            if (first1 != last1) return 1;
            if (first2 != last2) return -1;

            return 0;
        }
    }

} // end of namespace ISharp



