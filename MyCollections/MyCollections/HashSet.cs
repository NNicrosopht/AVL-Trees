// Copyright (c) NNicrosopht 1989-2015
﻿// HashSet

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ISharp.Collections
{
    enum HashValues { Default = 17, Threshold = 1, Multiplier = 2, Shrink = 5 };

    public static class Primes
    {
        public static bool IsPrime(ulong n)
        {
            bool is_prime = true;
            ulong limit = (ulong)Math.Sqrt(n);

            for (ulong j = 2; j <= limit; j++)
                if (n % j == 0)
                {
                    is_prime = false;
                    break;
                }

            return is_prime;
        }

        public static ulong NextPrime(ulong n)
        {
            while (!IsPrime(n)) n++;
            return n;
        }
    }

     public class HashSetNode<T>
    {
        public HashSetNode<T> Next;
        public HashSetNode<T> Previous;
        public HashSetNode<T> Link;
        public T Data;
        public int Hash;
        public bool Header;

        public HashSetNode()
        {
            Header = true;
            Previous = this;
            Next = this;
        }

        public HashSetNode(T t, int HashCode)
        {
            Data = t;
            Header = false;
            Hash = HashCode;
        }

        public T Value
        {
            get { return Data; }
            set { Data = value; }
        }

        public override int GetHashCode() { return Hash; }
    }

    public struct HashSetEntry<T> : IEnumerator<T>
    {
        public HashSetEntry(HashSetNode<T> N) { Node = N; }

        public bool Header { get { return Node.Header; } }

        public T Value
        {
            get
            {
                if (Header) throw new IsListRootException();
                return Node.Data;
            }
        }

        public bool MoveNext()
        {
            Node = Node.Next;
            return !Header;
        }

        public bool MovePrevious()
        {
            Node = Node.Previous;
            return !Header;
        }

        public static HashSetEntry<T> operator ++(HashSetEntry<T> Entry)
        {
            Entry.Node = Entry.Node.Next;
            return Entry;
        }

        public static HashSetEntry<T> operator --(HashSetEntry<T> Entry)
        {
            Entry.Node = Entry.Node.Previous;
            return Entry;
        }

        public void Reset()
        { while (!Header) Node = Node.Next; }

        object System.Collections.IEnumerator.Current
        { get { return Node.Value; } }

        T IEnumerator<T>.Current
        { get { return Node.Value; } }

        public static bool operator ==(HashSetEntry<T> x, HashSetEntry<T> y) { return x.Node == y.Node; }
        public static bool operator !=(HashSetEntry<T> x, HashSetEntry<T> y) { return x.Node != y.Node; }

        public override bool Equals(object obj)
        { return Node == ((HashSetEntry<T>)obj).Node; }

        public override int GetHashCode() { return Node.GetHashCode(); }

        public static long operator -(HashSetEntry<T> This, HashSetEntry<T> Iter)
        {
            long Result = 0;
            while (This.Node != Iter.Node) { Iter.MoveNext(); Result++; }
            return Result;
        }

        public override string ToString() { return Value.ToString(); }

        public void Dispose() { }

        public HashSetNode<T> Node;
    }

    [Serializable]
    public class HashSet<T> : ICollection<T>,
                              ICloneable,
                              ISerializable,
                              IEquatable<HashSet<T>>
    {
        protected HashSetNode<T> Header;
        protected ulong Nodes;
        protected IEqualityComparer<T> TComparer;
        protected ICloner<T> TCloner;
        private HashSetNode<T>[] HashTable;
        private ulong HashEntries;
        private ulong HashThreshold;
        private ulong HashShrink;

        //*** Constructors ***

        public HashSet()
        {
            Nodes = 0;
            Header = new HashSetNode<T>();
            TComparer = EqualityComparer<T>.Default;
            TCloner = Cloner<T>.Default;
            InitializeHashTable();
        }

        public HashSet(IEqualityComparer<T> C)
        {
            Nodes = 0;
            Header = new HashSetNode<T>();
            TComparer = C;
            TCloner = Cloner<T>.Default;
            InitializeHashTable();
        }

        public HashSet(SerializationInfo si, StreamingContext sc)
        {
            Nodes = 0;
            Header = new HashSetNode<T>();

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
        
        //*** Properties ***

        public ICloner<T> Cloner
        {
            get { return TCloner; }
            set { TCloner = value; }
        }

        public HashSetEntry<T> Begin
        { get { return new HashSetEntry<T>(Header.Next); } }

        public HashSetEntry<T> End
        { get { return new HashSetEntry<T>(Header); } }

        public virtual int Count { get { return (int)Nodes; } }

        public bool IsReadOnly { get { return false; } }

        //*** Operators ***

        public bool this[T key]
        {
            get
            {
                if (Header == null)
                    return false;
                else
                {
                    int HashCode = TComparer.GetHashCode(key);
                    ulong HashIndex = (ulong)HashCode % HashEntries;
                    HashSetNode<T> Node = HashTable[HashIndex];

                    while (Node != null && !TComparer.Equals(key, Node.Data))
                        Node = Node.Link;

                    return Node != null;
                }
            }
        }

        public static HashSet<T> operator +(HashSet<T> set, T t)
        {
            set.Add(t);
            return set;
        }

        public static bool operator ==(HashSet<T> A, HashSet<T> B)
        {
            return A.Equals(B);
        }

        public static bool operator !=(HashSet<T> A, HashSet<T> B)
        {
            return !A.Equals(B);
        }

        public static HashSet<T> operator |(HashSet<T> A, HashSet<T> B)
        {

            HashSet<T> U = new HashSet<T>(A.TComparer);
            CombineSets(A, B, U, SetOperation.Union);
            return U;
        }

        public static HashSet<T> operator &(HashSet<T> A, HashSet<T> B)
        {
            HashSet<T> I = new HashSet<T>(A.TComparer);
            CombineSets(A, B, I, SetOperation.Intersection);
            return I;
        }

        public static HashSet<T> operator ^(HashSet<T> A, HashSet<T> B)
        {
            HashSet<T> S = new HashSet<T>(A.TComparer);
            CombineSets(A, B, S, SetOperation.SymmetricDifference);
            return S;
        }

        public static HashSet<T> operator -(HashSet<T> A, HashSet<T> B)
        {
            HashSet<T> S = new HashSet<T>(A.TComparer);
            CombineSets(A, B, S, SetOperation.Difference);
            return S;
        }

        //*** Methods ***

        public void Add(T Data)
        {
            int KeyHash = TComparer.GetHashCode(Data);
            ulong HashIndex = (ulong)KeyHash % HashEntries;
            HashSetNode<T> Node = HashTable[HashIndex];

            while (Node != null && !TComparer.Equals(Data, Node.Data))
                Node = Node.Link;

            if (Node != null)
                throw new EntryAlreadyExistsException();

            HashSetNode<T> NewNode = AllocateNode(Data);
            if (Header.Next == Header)
            {
                Header.Previous = NewNode;
                Header.Next = NewNode;
                NewNode.Previous = Header;
                NewNode.Next = Header;
            }
            else
            {
                Header.Previous.Next = NewNode;
                NewNode.Previous = Header.Previous;
                Header.Previous = NewNode;
                NewNode.Next = Header;
            }
        }

        HashSetNode<T> AllocateNode(T Data)
        {
            int HashCode = TComparer.GetHashCode(Data);
            HashSetNode<T> Node = new HashSetNode<T>(Data, HashCode);
            Nodes++;
            if (Nodes == HashThreshold) ExpandHash();
            ulong HashIndex = (ulong)HashCode % HashEntries;
            Node.Link = HashTable[HashIndex];
            HashTable[HashIndex] = Node;
            return Node;
        }

        public void Clear() { Remove(); }

        public object Clone()
        {
            HashSet<T> Out = new HashSet<T>(TComparer);
            Out.Cloner = TCloner;
            foreach (T t in this) Out.Add(TCloner.Clone(t));
            return Out;
        }

        public bool Contains(T t) { return this[t]; }

        public void CopyTo(T[] array, int arrayIndex)
        {
            int i=0;
            foreach(T t in this)
            {
                array[arrayIndex+i] = t;
                i++;
            }
        }

        public virtual bool Equals(HashSet<T> o)
        {
            foreach (T t in this) if (!o[t]) return false;
            return true;
        }
        
        public override bool Equals(object o)
        {
            HashSet<T> In = (HashSet<T>)o;
            foreach (T t in this) if (!In[t]) return false;
            return true;
        }

        void ExpandHash()
        {
            HashEntries = Primes.NextPrime((ulong)HashValues.Multiplier * HashEntries);
            HashThreshold = (ulong)HashValues.Threshold * HashEntries;
            HashShrink = HashEntries / (ulong)HashValues.Shrink;
            HashTable = new HashSetNode<T>[HashEntries];

            HashSetNode<T> begin = Header.Next;
            HashSetNode<T> end = Header;

            while (begin != end)
            {
                ulong HashIndex = (ulong)begin.Hash % HashEntries;
                begin.Link = HashTable[HashIndex];
                HashTable[HashIndex] = begin;
                begin = begin.Next;
            }
        }

        public HashSetNode<T> Find(T Data)
        {
            if (Header == null)
                return null;
            else
            {
                int HashCode = TComparer.GetHashCode(Data);
                ulong HashIndex = (ulong)HashCode % HashEntries;
                HashSetNode<T> Node = HashTable[HashIndex];

                while (Node != null && !TComparer.Equals(Data, Node.Data))
                    Node = Node.Link;

                return Node;
            }
        }

        void FreeNode(HashSetNode<T> Node)
        {
            Nodes--;
            if (Nodes < HashShrink) ShrinkHash();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return new HashSetEntry<T>(Header); }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        { return new HashSetEntry<T>(Header); }

        public override int GetHashCode()
        {
            int HashCode = 0;

            HashSetEntry<T> HashBegin = Begin;
            HashSetEntry<T> HashEnd = End;

            while (HashBegin != HashEnd)
            {
                HashCode += HashBegin.Node.Hash;
                HashBegin++;
            }

            return HashCode;
        }

        public virtual void GetObjectData(SerializationInfo si, StreamingContext sc)
        {
            si.SetType(typeof(HashSet<T>));
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

        void InitializeHashTable()
        {
            HashTable = new HashSetNode<T>[(ulong)HashValues.Default];
            HashEntries = (ulong)HashValues.Default;
            HashThreshold = HashEntries * (ulong)HashValues.Threshold;
            HashShrink = 0;
        }

        public ulong Remove()
        {
            ulong count = Nodes;
            Header = new HashSetNode<T>();
            Header.Header = true;
            Header.Previous = Header;
            Header.Next = Header;
            HashTable = new HashSetNode<T>[(ulong)HashValues.Default];
            HashEntries = (ulong)HashValues.Default;
            HashThreshold = HashEntries * (ulong)HashValues.Threshold;
            HashShrink = 0;
            Nodes = 0;
            return count;
        }

        public bool Remove(T key)
        {
            if (Header == null)
                throw new EntryNotFoundException();
            else
            {
                int HashCode = TComparer.GetHashCode(key);
                ulong HashIndex = (ulong)HashCode % HashEntries;
                HashSetNode<T> Trailer = null;
                HashSetNode<T> Node = HashTable[HashIndex];

                while (Node != null && !TComparer.Equals(key, Node.Data))
                { Trailer = Node; Node = Node.Link; }

                if (Node == null)
                    throw new EntryNotFoundException();

                if (Trailer != null)
                    Trailer.Link = Node.Link;
                else
                    HashTable[HashIndex] = Node.Link;

                Remove(Node);
                return true;
            }
        }

        void Remove(HashSetNode<T> Node)
        {
            if (Node.Header) throw new IsListRootException();
            HashSetNode<T> Next = Node.Next;
            Node.Previous.Next = Next;
            Next.Previous = Node.Previous;
            FreeNode(Node);
        }

        public HashSetNode<T> Search(T key)
        {
            if (Header == null)
                throw new EntryNotFoundException();
            else
            {
                int HashCode = TComparer.GetHashCode(key);
                ulong HashIndex = (ulong)HashCode % HashEntries;
                HashSetNode<T> Node = HashTable[HashIndex];

                while (Node != null && !TComparer.Equals(key, Node.Data))
                    Node = Node.Link;

                if (Node == null) throw new EntryNotFoundException();
                return Node;
            }
        }

        void ShrinkHash()
        {
            HashEntries = HashEntries / (ulong)HashValues.Multiplier;
            HashThreshold = (ulong)HashValues.Threshold * HashEntries;
            HashShrink = HashEntries / (ulong)HashValues.Shrink;
            if (HashShrink < (ulong)HashValues.Default / 2) HashShrink = 0;
            HashTable = new HashSetNode<T>[HashEntries];

            HashSetNode<T> begin = Header.Next;
            HashSetNode<T> end = Header;

            while (begin != end)
            {
                ulong HashIndex = (ulong)begin.Hash % HashEntries;
                begin.Link = HashTable[HashIndex];
                HashTable[HashIndex] = begin;
                begin = begin.Next;
            }
        }

        public HashSetNode<T> Update(T Data)
        {
            int HashCode = TComparer.GetHashCode(Data);
            ulong HashIndex = (ulong)HashCode % HashEntries;
            HashSetNode<T> Node = HashTable[HashIndex];

            while (Node != null && !TComparer.Equals(Data, Node.Data))
                Node = Node.Link;

            if (Node != null) // Node Found - Update It
            {
                Node.Data = Data;
                return Node;
            }

            else  // Node Not Found - Create It
            {
                // Allocate Node

                HashSetNode<T> NewNode = new HashSetNode<T>(Data, HashCode);
                Nodes++;
                if (Nodes == HashThreshold)
                {
                    ExpandHash();
                    HashIndex = (ulong)HashCode % HashEntries;
                }
                NewNode.Link = HashTable[HashIndex];
                HashTable[HashIndex] = NewNode;

                // Add Node to List

                if (Header.Next == Header)
                {
                    Header.Previous = NewNode;
                    Header.Next = NewNode;
                    NewNode.Previous = Header;
                    NewNode.Next = Header;
                    return NewNode;
                }
                else
                {
                    Header.Previous.Next = NewNode;
                    NewNode.Previous = Header.Previous;
                    Header.Previous = NewNode;
                    NewNode.Next = Header;
                    return NewNode;
                }
            }
        }

        public void Update(HashSetNode<T> Node, T value)
        {
            Node.Data = value;
        }

        //*** Static Methods ***

        public static void CombineSets(HashSet<T> A,
                                       HashSet<T> B,
                                       HashSet<T> R,
                                       SetOperation operation)
        {
            switch (operation)
            {
                case SetOperation.Union:
                    foreach (T t in A)
                        R.Add(t);

                    foreach (T t in B)
                        if (R.Find(t) == null)
                            R.Add(t);
                    return;

                case SetOperation.Intersection:
                    foreach (T t in A)
                        if (B.Find(t) != null)
                            R.Add(t);

                    foreach (T t in B)
                        if (A.Find(t) != null)
                            if (R.Find(t) == null)
                                R.Add(t);
                    return;

                case SetOperation.SymmetricDifference:
                    foreach (T t in A)
                        if (B.Find(t) == null)
                            R.Add(t);

                    foreach (T t in B)
                        if (A.Find(t) == null && R.Find(t) == null)
                            R.Add(t);
                    return;

                case SetOperation.Difference:
                    foreach (T t in A)
                        if (B.Find(t) == null)
                            R.Add(t);
                    return;
            }

            throw new InvalidSetOperationException();
        }

        public static bool CheckSets(HashSet<T> A,
                                     HashSet<T> B,
                                     SetOperation operation)
        {
            switch (operation)
            {
                case SetOperation.Subset:
                    foreach (T t in A)
                        if (B.Find(t) == null)
                            return false;
                    return true;

                case SetOperation.Superset:
                    foreach (T t in B)
                        if (A.Find(t) == null)
                            return false;
                    return true;
            }

            throw new InvalidSetOperationException();
        }

    }

} // end of namespace ISharp.Collections


