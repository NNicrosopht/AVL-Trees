// Copyright (c) NNicrosopht 1989-2015

using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.Serialization;

namespace ISharp.Collections
{
    public class UnorderedSetNode<T> : Node
    {
        public T Data;
        public uint Index;

        public UnorderedSetNode(uint i, T t, Node Parent)
            : base(Parent)
        {
            Index = i;
            Data = t;
        }
    }

    public struct UnorderedSetEntry<T> : IEnumerator<T>
    {
        public UnorderedSetEntry(Node N) { _Node = N; }

        public UnorderedSetEntry<T> Entry
        { get { return new UnorderedSetEntry<T>(_Node); } }

        public uint Key
        {
            get
            {
                if (_Node.Balance == State.Header) throw new IsEndItemException();
                return ((UnorderedSetNode<T>)_Node).Index;
            }
        }

        public uint Index
        {
            get
            {
                if (_Node.Balance == State.Header) throw new IsEndItemException();
                return ((UnorderedSetNode<T>)_Node).Index;
            }
        }

        public T Value
        {
            get
            {
                if (_Node.Balance == State.Header) throw new IsEndItemException();
                return ((UnorderedSetNode<T>)_Node).Data;
            }
            set
            {
                if (_Node.Balance == State.Header) throw new IsEndItemException();
                ((UnorderedSetNode<T>)_Node).Data = value;
            }
        }

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

        public static UnorderedSetEntry<T> operator ++(UnorderedSetEntry<T> entry)
        {
            entry._Node = Utility.NextItem(entry._Node);
            return entry;
        }

        public static UnorderedSetEntry<T> operator --(UnorderedSetEntry<T> entry)
        {
            entry._Node = Utility.PreviousItem(entry._Node);
            return entry;
        }

        public static UnorderedSetEntry<T> operator +(UnorderedSetEntry<T> C, ulong Increment)
        {
            UnorderedSetEntry<T> Result = new UnorderedSetEntry<T>(C._Node);
            for (ulong i = 0; i < Increment; i++) ++Result;
            return Result;
        }

        public static UnorderedSetEntry<T> operator +(ulong Increment, UnorderedSetEntry<T> C)
        {
            UnorderedSetEntry<T> Result = new UnorderedSetEntry<T>(C._Node);
            for (ulong i = 0; i < Increment; i++) ++Result;
            return Result;
        }

        public static UnorderedSetEntry<T> operator -(UnorderedSetEntry<T> C, ulong Decrement)
        {
            UnorderedSetEntry<T> Result = new UnorderedSetEntry<T>(C._Node);
            for (ulong i = 0; i < Decrement; i++) --Result;
            return Result;
        }

        public void Reset()
        {
            _Node = Utility.GetEndItem(_Node);
        }

        object System.Collections.IEnumerator.Current
        { get { return ((UnorderedSetNode<T>)_Node).Data; } }

        T IEnumerator<T>.Current
        { get { return ((UnorderedSetNode<T>)_Node).Data; } }

        public static bool operator ==(UnorderedSetEntry<T> x, UnorderedSetEntry<T> y) { return x._Node == y._Node; }
        public static bool operator !=(UnorderedSetEntry<T> x, UnorderedSetEntry<T> y) { return x._Node != y._Node; }

        public override int GetHashCode()
        {
            return ((UnorderedSetNode<T>)_Node).Data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ((UnorderedSetNode<T>)_Node).Data.Equals(obj);
        }
        
        public static long operator -(UnorderedSetEntry<T> This, UnorderedSetEntry<T> iter)
        {
            long result = 0;
            while (This._Node != iter._Node) { iter.MoveNext(); result++; }
            return result;
        }
        public override String ToString()
        {
            if (_Node.Balance == State.Header) throw new IsEndItemException();
            return Value.ToString();
        }

        public void Dispose() { }

        public Node _Node;
    }

    [Serializable]
    public class UnorderedSet<T> : ICollection<T>,
                                   IEquatable<UnorderedSet<T>>,
                                   ICloneable,
                                   ISerializable
    {
        protected Node Header;
        protected ulong Nodes;
        public event TypeAdded<T> Added;
        public event TypeRemoved<T> Removed;
        public event TypeUpdated<T> Updated;
        protected IEqualityComparer<T> TComparer;
        protected ICloner<T> TCloner;

        //*** Constructor ***

        public UnorderedSet()
        {
            Nodes = 0;
            Header = new Node();
            TComparer = EqualityComparer<T>.Default;
            TCloner = Cloner<T>.Default;
        }

        public UnorderedSet(IEqualityComparer<T> ECompare)
        {
            Nodes = 0;
            Header = new Node();
            TComparer = ECompare;
            TCloner = Cloner<T>.Default;
        }

        public UnorderedSet(UnorderedSet<T> UnorderedSetToCopy)
        {
            Nodes = 0;
            Header = new Node();
            TComparer = UnorderedSetToCopy.TComparer;
            TCloner = UnorderedSetToCopy.TCloner;
            Copy((UnorderedSetNode<T>)UnorderedSetToCopy.Root);
        }

        public UnorderedSet(SerializationInfo si, StreamingContext sc)
        {
            Nodes = 0;
            Header = new Node();
            TComparer = (IEqualityComparer<T>)si.GetValue("TComparer", typeof(IEqualityComparer<T>));
            TCloner = (ICloner<T>)si.GetValue("TCloner", typeof(ICloner<T>));

            Type ValueType = typeof(T);

            ulong Count = si.GetUInt64("Count");

            for (ulong i = 0; i < Count; i++)
            {
                String iString = i.ToString();
                object value = si.GetValue("V" + iString, ValueType);
                Add((T)value);
            }
        }

        public UnorderedSet(IEnumerable<T> c)
        {
            Nodes = 0;
            Header = new Node();
            TComparer = EqualityComparer<T>.Default;
            TCloner = Cloner<T>.Default;

            foreach (T t in c)
            {
                Add(TCloner.Clone(t));
            }
        }

        public UnorderedSet(IEnumerable<T> c,
                            IEqualityComparer<T> ECompare)
        {
            Nodes = 0;
            Header = new Node();
            TComparer = ECompare;
            TCloner = Cloner<T>.Default;

            foreach (T t in c)
            {
                Add(TCloner.Clone(t));
            }
        }

        public UnorderedSet(UnorderedSet<T> A,
                            UnorderedSet<T> B,
                            SetOperation operation)
        {
            Nodes = 0;
            Header = new Node();
            TComparer = A.TComparer;
            TCloner = A.TCloner;
            CombineSets(A, B, this, operation);
        }


        //*** Primary Indexer ***

        public bool this[T Data]
        {
            get
            {
                if (Root == null)
                    return false;
                else
                {
                    uint Index = (uint)TComparer.GetHashCode(Data);

                    Node Search = Root;

                    do
                    {
                        if (Index < ((UnorderedSetNode<T>)Search).Index) Search = Search.Left;

                        else if (Index > ((UnorderedSetNode<T>)Search).Index) Search = Search.Right;

                        else break;

                    } while (Search != null);

                    if (Search == null) return false;

                    Node Locate = Find(Data, Search);

                    if (Locate == null) return false;

                    return true;
                }
            }
        }

        //*** Properties ***

        public UnorderedSetEntry<T> Begin
        { get { return new UnorderedSetEntry<T>(LeftMost); } }

        public ICloner<T> Cloner
        {
            get { return TCloner; }
            set { TCloner = value; }
        }

        public IEqualityComparer<T> Comparer
        { get { return TComparer; } }

        public virtual int Count { get { return (int)Nodes; } }

        public ulong Depth { get { return Utility.Depth(Root); } }

        public UnorderedSetEntry<T> End
        { get { return new UnorderedSetEntry<T>(Header); } }

        public int Hash { get { return GetHashCode(); } }

        public virtual bool IsReadOnly { get { return false; } }

        public virtual bool IsSynchronized { get { return false; } }

        public ulong Length
        { get { return Nodes; } }

        public Node LeftMost
        {
            get { return Header.Left; }
            set { Header.Left = value; }
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

        //*** Operators ***

        public static UnorderedSet<T> operator +(UnorderedSet<T> U, T Key)
        {
            U.Add(Key);
            return U;
        }


        public static UnorderedSet<T> operator -(UnorderedSet<T> U, T Key)
        {
            U.Remove(Key);
            return U;
        }

        public static UnorderedSet<T> operator |(UnorderedSet<T> A, UnorderedSet<T> B)
        {
            UnorderedSet<T> U = new UnorderedSet<T>(A.TComparer);
            U.TCloner = A.TCloner;
            CombineSets(A, B, U, SetOperation.Union);
            return U;
        }

        public static UnorderedSet<T> operator &(UnorderedSet<T> A, UnorderedSet<T> B)
        {
            UnorderedSet<T> I = new UnorderedSet<T>(A.TComparer);
            I.TCloner = A.TCloner;
            CombineSets(A, B, I, SetOperation.Intersection);
            return I;
        }

        public static UnorderedSet<T> operator ^(UnorderedSet<T> A, UnorderedSet<T> B)
        {
            UnorderedSet<T> S = new UnorderedSet<T>(A.TComparer);
            S.TCloner = A.TCloner;
            CombineSets(A, B, S, SetOperation.SymmetricDifference);
            return S;
        }

        public static UnorderedSet<T> operator -(UnorderedSet<T> A, UnorderedSet<T> B)
        {
            UnorderedSet<T> S = new UnorderedSet<T>(A.TComparer);
            S.TCloner = A.TCloner;
            CombineSets(A, B, S, SetOperation.Difference);
            return S;
        }

        public static bool operator ==(UnorderedSet<T> A, UnorderedSet<T> B)
        {
            return A.Equals(B);
        }

        public static bool operator !=(UnorderedSet<T> A, UnorderedSet<T> B)
        {
            return !A.Equals(B);
        }

        public static bool operator <(UnorderedSet<T> A, UnorderedSet<T> B)
        {
            return CheckSets(A, B, SetOperation.Subset);
        }

        public static bool operator >(UnorderedSet<T> A, UnorderedSet<T> B)
        {
            return CheckSets(A, B, SetOperation.Superset);
        }

        //*** Public Methods ***

        bool Exists(T Data, Node _Node)
        {
            Node Previous = Utility.PreviousItem(_Node);
            Node Save = _Node;

            while (!Previous.IsHeader && ((UnorderedSetNode<T>)Previous).Index == ((UnorderedSetNode<T>)_Node).Index)
            {
                Save = Previous;
                Previous = Utility.PreviousItem(Previous);
            }

            _Node = Save;
            if (TComparer.Equals(Data, ((UnorderedSetNode<T>)_Node).Data)) return true;

            Node Next = Utility.NextItem(_Node);
            while (!Next.IsHeader && ((UnorderedSetNode<T>)Next).Index == ((UnorderedSetNode<T>)_Node).Index)
            {
                _Node = Next;
                if (TComparer.Equals(Data, ((UnorderedSetNode<T>)_Node).Data)) return true;
                Next = Utility.NextItem(_Node);
            }

            return false;
        }

        Node Find(T Data, Node _Node)
        {
            Node Previous = Utility.PreviousItem(_Node);
            Node Save = _Node;

            while (!Previous.IsHeader && ((UnorderedSetNode<T>)Previous).Index == ((UnorderedSetNode<T>)_Node).Index)
            {
                Save = Previous;
                Previous = Utility.PreviousItem(Previous);
            }

            _Node = Save;
            if (TComparer.Equals(Data, ((UnorderedSetNode<T>)_Node).Data)) return _Node;

            Node Next = Utility.NextItem(_Node);
            while (!Next.IsHeader && ((UnorderedSetNode<T>)Next).Index == ((UnorderedSetNode<T>)_Node).Index)
            {
                _Node = Next;
                if (TComparer.Equals(Data, ((UnorderedSetNode<T>)_Node).Data)) return _Node;
                Next = Utility.NextItem(_Node);
            }

            return null;
        }

        public void Add(T Data)
        {
            Node root = Root;

            uint Index = (uint)TComparer.GetHashCode(Data);

            if (root == null)
            {
                Root = new UnorderedSetNode<T>(Index, Data, Header);
                Nodes++;
                LeftMost = Root;
                RightMost = Root;
                if (Added != null) Added(this, ((UnorderedSetNode<T>)Root).Data);
            }
            else
            {
                for (; ; )
                {
                    if (Index == ((UnorderedSetNode<T>)root).Index)
                    {
                        if (Exists(Data, root))
                            throw new EntryAlreadyExistsException();
                    }

                    if (Index < ((UnorderedSetNode<T>)root).Index)
                    {
                        if (root.Left != null)
                            root = root.Left;
                        else
                        {
                            UnorderedSetNode<T> NewNode = new UnorderedSetNode<T>(Index, Data, root);
                            Nodes++;
                            root.Left = NewNode;
                            if (LeftMost == root) LeftMost = NewNode;
                            Utility.BalanceSet(root, Direction.FromLeft);
                            if (Added != null) Added(this, NewNode.Data);
                            break;
                        }
                    }
                    else
                    {
                        if (root.Right != null)
                            root = root.Right;
                        else
                        {
                            UnorderedSetNode<T> NewNode = new UnorderedSetNode<T>(Index, Data, root);
                            Nodes++;
                            root.Right = NewNode;
                            if (RightMost == root) RightMost = NewNode;
                            Utility.BalanceSet(root, Direction.FromRight);
                            if (Added != null) Added(this, NewNode.Data);
                            break;
                        }
                    }
                }
            }
        }

        protected void CallRemoved(T Data)
        {
            if (Removed != null) Removed(this, Data);
        }

        public void Clear()
        {
            Remove();
        }

        public virtual object Clone()
        {
            UnorderedSet<T> UnorderedSetOut = new UnorderedSet<T>();
            UnorderedSetOut.TComparer = TComparer;
            UnorderedSetOut.TCloner = TCloner;
            foreach (T t in this)
                UnorderedSetOut.Add(TCloner.Clone(t));
            return UnorderedSetOut;
        }

        public virtual bool Contains(T Data)
        {
            if (Root == null)
                return false;
            else
            {
                uint Index = (uint)TComparer.GetHashCode(Data);

                Node Search = Root;

                do
                {
                    if (Index < ((UnorderedSetNode<T>)Search).Index) Search = Search.Left;

                    else if (Index > ((UnorderedSetNode<T>)Search).Index) Search = Search.Right;

                    else break;

                } while (Search != null);

                if (Search == null) return false;

                Node Locate = Find(Data, Search);

                if (Locate == null) return false;

                return true;
            }
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            int i = 0;
            foreach (T t in this)
            {
                array[arrayIndex + i] = t;
                i++;
            }
        }

        public bool Equals(UnorderedSet<T> Compare)
        {
            foreach (T t in this)
                if (!Compare[t]) return false;

            foreach (T t in Compare)
                if (!this[t]) return false;

            return true;
        }
        
        public override bool Equals(Object compare)
        {
            UnorderedSet<T> Compare = (UnorderedSet<T>)compare;

            foreach (T t in this)
                if (!Compare[t]) return false;

            foreach (T t in Compare)
                if (!this[t]) return false;

            return true;
        }

        public T Find(T Key)
        {
            Node _Node = Search(Key);
            if (_Node == null)
                throw new EntryNotFoundException();
            else
                return ((UnorderedSetNode<T>)_Node).Data;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return new UnorderedSetEntry<T>(Header); }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        { return new UnorderedSetEntry<T>(Header); }

        public virtual void GetObjectData(SerializationInfo si, StreamingContext sc)
        {
            si.SetType(typeof(ISharp.Collections.UnorderedSet<T>));
            Type ValueType = typeof(T);
            ulong Index = 0;
            foreach (T t in this)
            {
                String iString = Index.ToString();
                si.AddValue("V" + iString, t, ValueType);
                Index++;
            }
            si.AddValue("Count", Index);
            si.AddValue("TComparer", TComparer, TComparer.GetType());
            si.AddValue("TCloner", TCloner, TCloner.GetType());
        }

        public void Notify()
        {
            if (Added != null) Notify(Root);
        }

        public ulong Remove()
        {
            if (Removed != null)
                foreach (T t in this)
                {
                    Removed(this, t);
                }
            Root = null;
            LeftMost = Header;
            RightMost = Header;
            ulong count = Nodes;
            Nodes = 0;
            return count;
        }

        public bool Remove(T Data)
        {
            Node root = Root;

            uint Index = (uint)TComparer.GetHashCode(Data);

            for (; ; )
            {
                if (root == null)
                    throw new EntryNotFoundException();

                if (Index < ((UnorderedSetNode<T>)root).Index)
                    root = root.Left;

                else if (Index > ((UnorderedSetNode<T>)root).Index)
                    root = root.Right;

                else // Item is found
                {
                    root = Find(Data, root);
                    if (root == null) throw new EntryNotFoundException();

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
                        Node N = Utility.NextItem(root);

                        if (N.IsHeader)
                        { LeftMost = Header; RightMost = Header; }
                        else
                            LeftMost = N;
                    }
                    else if (RightMost == root)
                    {
                        Node P = Utility.PreviousItem(root);

                        if (P.IsHeader)
                        { LeftMost = Header; RightMost = Header; }
                        else
                            RightMost = P;
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
                    Nodes--;
                    if (Removed != null) Removed(this, ((UnorderedSetNode<T>)root).Data);
                    break;
                }
            }

            return true;
        }

        public Node Search(T Data)
        {
            if (Root == null)
                return null;
            else
            {
                uint Index = (uint)TComparer.GetHashCode(Data);

                Node Search = Root;

                do
                {
                    if (Index < ((UnorderedSetNode<T>)Search).Index) Search = Search.Left;

                    else if (Index > ((UnorderedSetNode<T>)Search).Index) Search = Search.Right;

                    else break;

                } while (Search != null);

                if (Search == null) return null;

                return Find(Data, Search);
            }
        }

        public override String ToString()
        {
            String StringOut = "{";
            Node LastNode = (End - 1)._Node;

            for (UnorderedSetEntry<T> item = Begin; item != End; ++item)
            {
                String NewStringOut = item.Value.ToString();
                if (!TComparer.Equals(((UnorderedSetNode<T>)LastNode).Data, ((UnorderedSetNode<T>)item._Node).Data)) NewStringOut = NewStringOut + ",";
                StringOut = StringOut + NewStringOut;
            }
            StringOut = StringOut + "}";
            return StringOut;
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

            Validate((UnorderedSetNode<T>)Root);

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

        void Copy(UnorderedSetNode<T> CopyRoot)
        {
            if (Root != null) Remove();
            if (CopyRoot != null)
            {
                Copy(ref Header.Parent, CopyRoot, Header);
                LeftMost = GetFirst();
                RightMost = GetLast();
            }
        }

        void Copy(ref Node root, UnorderedSetNode<T> CopyRoot, Node Parent)
        {
            root = new UnorderedSetNode<T>(CopyRoot.Index, TCloner.Clone(CopyRoot.Data), Parent);
            Nodes++;

            root.Balance = CopyRoot.Balance;

            if (CopyRoot.Left != null)
                Copy(ref root.Left, (UnorderedSetNode<T>)CopyRoot.Left, (UnorderedSetNode<T>)root);

            if (CopyRoot.Right != null)
                Copy(ref root.Right, (UnorderedSetNode<T>)CopyRoot.Right, (UnorderedSetNode<T>)root);

            if (Added != null) Added(this, ((UnorderedSetNode<T>)root).Data);
        }

        Node GetFirst()
        {
            if (Root == null)
                return Header;

            else
            {
                Node Search = Root;
                while (Search.Left != null) Search = Search.Left;
                return Search;
            }
        }

        Node GetLast()
        {
            if (Root == null)
                return Header;

            else
            {
                Node Search = Root;
                while (Search.Right != null) Search = Search.Right;
                return Search;
            }
        }

        void Notify(Node root)
        {
            if (root != null)
            {
                if (root.Left != null)
                    Notify(root.Left);

                Added(this, ((UnorderedSetNode<T>)root).Data);

                if (root.Right != null)
                    Notify(root.Right);
            }
        }

        public void Remove(UnorderedSetNode<T> root)
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
            Nodes--;
            if (Removed != null) Removed(this, root.Data);
        }

        void Validate(UnorderedSetNode<T> root)
        {
            if (root == null) return;

            if (root.Left != null)
            {
                UnorderedSetNode<T> Left = (UnorderedSetNode<T>)root.Left;

                if (Left.Index > root.Index)
                    throw new OutOfKeyOrderException();

                if (Left.Parent != root)
                    throw new TreeInvalidParentException();

                Validate((UnorderedSetNode<T>)root.Left);
            }

            if (root.Right != null)
            {
                UnorderedSetNode<T> Right = (UnorderedSetNode<T>)root.Right;

                if (Right.Index < root.Index)
                    throw new OutOfKeyOrderException();

                if (Right.Parent != root)
                    throw new TreeInvalidParentException();

                Validate((UnorderedSetNode<T>)root.Right);
            }

            ulong DepthLeft = root.Left != null ? Utility.Depth(root.Left) : 0;
            ulong DepthRight = root.Right != null ? Utility.Depth(root.Right) : 0;

            if (DepthLeft > DepthRight && DepthLeft - DepthRight > 2)
                throw new TreeOutOfBalanceException();

            if (DepthLeft < DepthRight && DepthRight - DepthLeft > 2)
                throw new TreeOutOfBalanceException();
        }

        public static void CombineSets(UnorderedSet<T> A,
                                       UnorderedSet<T> B,
                                       UnorderedSet<T> R,
                                       SetOperation operation)
        {
            ICloner<T> TCloner = R.TCloner;

            switch (operation)
            {
                case SetOperation.Union:
                    foreach (T t in A)
                        R.Add(TCloner.Clone(t));

                    foreach (T t in B)
                        if (!R[t]) R.Add(TCloner.Clone(t));
                    return;

                case SetOperation.Intersection:
                    foreach (T t in A)
                        if (B[t]) R.Add(TCloner.Clone(t));

                    foreach (T t in B)
                        if (A[t] && !R[t]) R.Add(TCloner.Clone(t));
                    return;

                case SetOperation.SymmetricDifference:
                    foreach (T t in A)
                        if (!B[t]) R.Add(TCloner.Clone(t));

                    foreach (T t in B)
                        if (!A[t] && !R[t]) R.Add(TCloner.Clone(t));
                    return;

                case SetOperation.Difference:
                    foreach (T t in A)
                        if (!B[t]) R.Add(TCloner.Clone(t));
                    return;

            }

            throw new InvalidSetOperationException();
        }

        public static bool CheckSets(UnorderedSet<T> A,
                                     UnorderedSet<T> B,
                                     SetOperation operation)
        {
            switch (operation)
            {
                case SetOperation.Subset:
                    foreach (T t in A)
                        if (!B[t]) return false;
                    return true;


                case SetOperation.Superset:
                    foreach (T t in B)
                        if (!A[t]) return false;
                    return true;
            }

            throw new InvalidSetOperationException();
        }

        public void Update(T value)
        {
            Node search = Search(value);

            if (search == null) throw new EntryNotFoundException();

            if (Updated != null)
            {
                T saved = ((UnorderedSetNode<T>)search).Data;
                ((UnorderedSetNode<T>)search).Data = value;
                Updated(this, saved, value);
            }
            else ((UnorderedSetNode<T>)search).Data = value;
        }

        public override int GetHashCode()
        {
            return GetHashCode((UnorderedSetNode<T>)Header.Parent);
        }

        int GetHashCode(UnorderedSetNode<T> Root)
        {
            if (Root != null)
            {
                int HashCode = (int)Root.Index;

                if (Root.Left != null)
                    HashCode += GetHashCode((UnorderedSetNode<T>)Root.Left);

                if (Root.Right != null)
                    HashCode += GetHashCode((UnorderedSetNode<T>)Root.Right);

                return HashCode;
            }

            return 0;
        }

    }

} // end of namespace ISharp
