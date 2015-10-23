// Copyright (c) Benedict Bede McNamara, 2000-20015, All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ISharp.Collections
{
    enum UnsignedLimits : ulong
    {
        MaximumULong = 0xffffffffffffffff,
    }

    enum SignedLimits : long
    {
        MaximumLong = 0x7fffffffffffffff,
        MinimumLong = unchecked((long)0x8000000000000000)
    }

    public delegate void TypeFound<T>(object O, T type);
    public delegate void TypeAdded<T>(object O, T type);
    public delegate void TypeRemoved<T>(object O, T type);
    public delegate void TypeUpdated<T>(object O, T before, T after);

    public delegate void Updating<T>(object O, ref T value);
    public delegate void Updating<K, T>(object O, K key, ref T value);

    public delegate bool TypePredicate<T>(T t);
    public delegate bool TypeCondition<T, P>(T t, P p);

    public delegate void KeyTypeFound<K, T>(object O, K key, T type);
    public delegate void KeyTypeAdded<K, T>(object O, K key, T type);
    public delegate void KeyTypeRemoved<K, T>(object O, K key, T type);
    public delegate void KeyTypeUpdated<K, T>(object O, K key, T before, T after);

    public delegate bool KeyTypePredicate<K, T>(K k, T t);
    public delegate bool KeyTypeCondition<K, T, P>(K k, T t, P p);

    enum EndType { None, TypeA, TypeB };

    [Serializable]
    public struct KeyValue<K, T>
    {
        K k;
        T d;

        public KeyValue(K kk, T dd) { k = kk; d = dd; }

        public K Key
        {
            get { return k; }
            set { k = value; }
        }

        public T Value
        {
            get { return d; }
            set { d = value; }
        }

        public override string ToString()
        { return "(" + Key.ToString() + "," + Value.ToString() + ")"; }
    }

    public interface IHasher<T>
    {
        int GetHashCode(T t);
    }

    [Serializable]
    public abstract class Hasher<T> : IHasher<T>
    {
        public static Hasher<T> Default
        {
            get
            {
                return new DefaultHasher<T>();
            }
        }

        public abstract int GetHashCode(T t);
    }

    [Serializable]
    public class DefaultHasher<T> : Hasher<T>
    {
        public override int GetHashCode(T t)
        {
            return t.GetHashCode();
        }
    }

public interface ICloneable<T>
{
    T Clone();
}

public interface ICloner<T>
{
    T Clone(T t);
}

[Serializable]
public abstract class Cloner<T> : ICloner<T>
{
    public static Cloner<T> Default
    {
        get
        {
            Type TypeT = typeof(T);
            Type TypeIC1 = typeof(ICloneable<T>);
            Type TypeIC2 = typeof(ICloneable);
            if (TypeIC1.IsAssignableFrom(TypeT))
                return new DefaultCloner1<T>();
            else if (TypeIC2.IsAssignableFrom(TypeT))
                return new DefaultCloner2<T>();
            else
                return new DefaultNoCloner<T>();
        }
    }

    public static Cloner<T> Invisible
    {
        get
        {
            return new DefaultNoCloner<T>();
        }
    }

    public abstract T Clone(T t);
}

[Serializable]
public class DefaultCloner1<T> : Cloner<T>
{
    public override T Clone(T t)
    {
        ICloneable<T> copier = (ICloneable<T>)t;
        return copier.Clone();
    }
}

[Serializable]
public class DefaultCloner2<T> : Cloner<T>
{
    public override T Clone(T t)
    {
        ICloneable copier = (ICloneable)t;
        return (T)copier.Clone();
    }
}

[Serializable]
public class DefaultNoCloner<T> : Cloner<T>
{
    public override T Clone(T t)
    {
        return t;
    }
}

public interface IComparer<K, T>
{
    int Compare(K k, T t);
}

public enum Direction { FromLeft, FromRight };

public enum State { Header, LeftHigh, Balanced, RightHigh };

public enum SetOperation
{
    Union,
    Intersection,
    SymmetricDifference,
    Difference,
    Equality,
    Inequality,
    Subset,
    Superset
}

public class Node
{
    public Node Left;
    public Node Right;
    public Node Parent;
    public State Balance;

    public Node()
    {
        Left = this;
        Right = this;
        Parent = null;
        Balance = State.Header;
    }

    public Node(Node p)
    {
        Left = null;
        Right = null;
        Parent = p;
        Balance = State.Balanced;
    }

    public bool IsHeader
    { get { return Balance == State.Header; } }
}


class Utility
{
    public static Node minimum(Node x)
    {
        while (x.Left != null) x = x.Left;
        return x;
    }

    public static Node maximum(Node x)
    {
        while (x.Right != null) x = x.Right;
        return x;
    }

    static void RotateLeft(ref Node root)
    {
        Node Parent = root.Parent;
        Node x = root.Right;

        root.Parent = x;
        x.Parent = Parent;
        if (x.Left != null) x.Left.Parent = root;

        root.Right = x.Left;
        x.Left = root;
        root = x;
    }

    static void RotateRight(ref Node root)
    {
        Node Parent = root.Parent;
        Node x = root.Left;

        root.Parent = x;
        x.Parent = Parent;
        if (x.Right != null) x.Right.Parent = root;

        root.Left = x.Right;
        x.Right = root;
        root = x;
    }

    static void BalanceLeft(ref Node root)
    {
        Node left = root.Left;

        switch (left.Balance)
        {
            case State.LeftHigh:
                root.Balance = State.Balanced;
                left.Balance = State.Balanced;
                RotateRight(ref root);
                break;

            case State.RightHigh:
                {
                    Node subRight = left.Right;
                    switch (subRight.Balance)
                    {
                        case State.Balanced:
                            root.Balance = State.Balanced;
                            left.Balance = State.Balanced;
                            break;

                        case State.RightHigh:
                            root.Balance = State.Balanced;
                            left.Balance = State.LeftHigh;
                            break;

                        case State.LeftHigh:
                            root.Balance = State.RightHigh;
                            left.Balance = State.Balanced;
                            break;
                    }
                    subRight.Balance = State.Balanced;
                    RotateLeft(ref left);
                    root.Left = left;
                    RotateRight(ref root);
                }
                break;

            case State.Balanced:
                root.Balance = State.LeftHigh;
                left.Balance = State.RightHigh;
                RotateRight(ref root);
                break;
        }
    }

    static void BalanceRight(ref Node root)
    {
        Node right = root.Right;

        switch (right.Balance)
        {
            case State.RightHigh:
                root.Balance = State.Balanced;
                right.Balance = State.Balanced;
                RotateLeft(ref root);
                break;

            case State.LeftHigh:
                {
                    Node subLeft = right.Left; // Left Subtree of Right
                    switch (subLeft.Balance)
                    {
                        case State.Balanced:
                            root.Balance = State.Balanced;
                            right.Balance = State.Balanced;
                            break;

                        case State.LeftHigh:
                            root.Balance = State.Balanced;
                            right.Balance = State.RightHigh;
                            break;

                        case State.RightHigh:
                            root.Balance = State.LeftHigh;
                            right.Balance = State.Balanced;
                            break;
                    }
                    subLeft.Balance = State.Balanced;
                    RotateRight(ref right);
                    root.Right = right;
                    RotateLeft(ref root);
                }
                break;

            case State.Balanced:
                root.Balance = State.RightHigh;
                right.Balance = State.LeftHigh;
                RotateLeft(ref root);
                break;
        }
    }

    internal static void BalanceSet(Node root, Direction From)
    {
        bool Taller = true;

        while (Taller)
        {
            Node Parent = root.Parent;
            Direction NextFrom = (Parent.Left == root) ? Direction.FromLeft : Direction.FromRight;

            if (From == Direction.FromLeft)
            {
                switch (root.Balance)
                {
                    case State.LeftHigh:
                        if (Parent.IsHeader)
                            BalanceLeft(ref Parent.Parent);
                        else if (Parent.Left == root)
                            BalanceLeft(ref Parent.Left);
                        else
                            BalanceLeft(ref Parent.Right);
                        Taller = false;
                        break;

                    case State.Balanced:
                        root.Balance = State.LeftHigh;
                        Taller = true;
                        break;

                    case State.RightHigh:
                        root.Balance = State.Balanced;
                        Taller = false;
                        break;
                }
            }
            else
            {
                switch (root.Balance)
                {
                    case State.LeftHigh:
                        root.Balance = State.Balanced;
                        Taller = false;
                        break;

                    case State.Balanced:
                        root.Balance = State.RightHigh;
                        Taller = true;
                        break;

                    case State.RightHigh:
                        if (Parent.IsHeader)
                            BalanceRight(ref Parent.Parent);
                        else if (Parent.Left == root)
                            BalanceRight(ref Parent.Left);
                        else
                            BalanceRight(ref Parent.Right);
                        Taller = false;
                        break;
                }
            }

            if (Taller) // skip up a level
            {
                if (Parent.IsHeader)
                    Taller = false;
                else
                {
                    root = Parent;
                    From = NextFrom;
                }
            }
        }
    }

    internal static void BalanceSetRemove(Node root, Direction From)
    {
        if (root.IsHeader) return;

        bool Shorter = true;

        while (Shorter)
        {
            Node Parent = root.Parent;
            Direction NextFrom = (Parent.Left == root) ? Direction.FromLeft : Direction.FromRight;

            if (From == Direction.FromLeft)
            {
                switch (root.Balance)
                {
                    case State.LeftHigh:
                        root.Balance = State.Balanced;
                        Shorter = true;
                        break;

                    case State.Balanced:
                        root.Balance = State.RightHigh;
                        Shorter = false;
                        break;

                    case State.RightHigh:
                        if (root.Right.Balance == State.Balanced)
                            Shorter = false;
                        else
                            Shorter = true;
                        if (Parent.IsHeader)
                            BalanceRight(ref Parent.Parent);
                        else if (Parent.Left == root)
                            BalanceRight(ref Parent.Left);
                        else
                            BalanceRight(ref Parent.Right);
                        break;
                }
            }
            else
            {
                switch (root.Balance)
                {
                    case State.RightHigh:
                        root.Balance = State.Balanced;
                        Shorter = true;
                        break;

                    case State.Balanced:
                        root.Balance = State.LeftHigh;
                        Shorter = false;
                        break;

                    case State.LeftHigh:
                        if (root.Left.Balance == State.Balanced)
                            Shorter = false;
                        else
                            Shorter = true;
                        if (Parent.IsHeader)
                            BalanceLeft(ref Parent.Parent);
                        else if (Parent.Left == root)
                            BalanceLeft(ref Parent.Left);
                        else
                            BalanceLeft(ref Parent.Right);
                        break;
                }
            }

            if (Shorter)
            {
                if (Parent.IsHeader)
                    Shorter = false;
                else
                {
                    From = NextFrom;
                    root = Parent;
                }
            }
        }
    }

    internal static Node PreviousItem(Node Node)
    {
        if (Node.IsHeader) { return Node.Right; }

        if (Node.Left != null)
        {
            Node = Node.Left;
            while (Node.Right != null) Node = Node.Right;
        }
        else
        {
            Node y = Node.Parent;
            if (y.IsHeader) return y;
            while (Node == y.Left) { Node = y; y = y.Parent; }
            Node = y;
        }
        return Node;
    }

    internal static Node NextItem(Node Node)
    {
        if (Node.IsHeader) return Node.Left;

        if (Node.Right != null)
        {
            Node = Node.Right;
            while (Node.Left != null) Node = Node.Left;
        }
        else
        {
            Node y = Node.Parent;
            if (y.IsHeader) return y;
            while (Node == y.Right) { Node = y; y = y.Parent; }
            Node = y;
        }
        return Node;
    }

    internal static ulong Depth(Node root)
    {
        if (root != null)
        {
            ulong Left = root.Left != null ? Depth(root.Left) : 0;
            ulong Right = root.Right != null ? Depth(root.Right) : 0;
            return Left < Right ? Right + 1 : Left + 1;
        }
        else
            return 0;
    }

    public static ulong Paths(Node root, ulong weight)
    {
        if (root != null)
        {
            ulong Left = root.Left != null ? Paths(root.Left, weight + 1) : 0;
            ulong Right = root.Right != null ? Paths(root.Right, weight + 1) : 0;
            return Left + Right + weight;
        }
        else
            return 0;
    }

    static void SwapNodeReference(ref Node first, ref Node second)
    { Node temporary = first; first = second; second = temporary; }

    internal static void SwapNodes(Node A, Node B)
    {
        if (B == A.Left)
        {
            if (B.Left != null) B.Left.Parent = A;
            if (B.Right != null) B.Right.Parent = A;

            if (A.Right != null) A.Right.Parent = B;

            if (!A.Parent.IsHeader)
            {
                if (A.Parent.Left == A)
                    A.Parent.Left = B;
                else
                    A.Parent.Right = B;
            }
            else A.Parent.Parent = B;

            B.Parent = A.Parent;
            A.Parent = B;

            A.Left = B.Left;
            B.Left = A;

            SwapNodeReference(ref A.Right, ref B.Right);
        }
        else if (B == A.Right)
        {
            if (B.Right != null) B.Right.Parent = A;
            if (B.Left != null) B.Left.Parent = A;

            if (A.Left != null) A.Left.Parent = B;

            if (!A.Parent.IsHeader)
            {
                if (A.Parent.Left == A)
                    A.Parent.Left = B;
                else
                    A.Parent.Right = B;
            }
            else A.Parent.Parent = B;

            B.Parent = A.Parent;
            A.Parent = B;

            A.Right = B.Right;
            B.Right = A;

            SwapNodeReference(ref A.Left, ref B.Left);
        }
        else if (A == B.Left)
        {
            if (A.Left != null) A.Left.Parent = B;
            if (A.Right != null) A.Right.Parent = B;

            if (B.Right != null) B.Right.Parent = A;

            if (!B.Parent.IsHeader)
            {
                if (B.Parent.Left == B)
                    B.Parent.Left = A;
                else
                    B.Parent.Right = A;
            }
            else B.Parent.Parent = A;

            A.Parent = B.Parent;
            B.Parent = A;

            B.Left = A.Left;
            A.Left = B;

            SwapNodeReference(ref A.Right, ref B.Right);
        }
        else if (A == B.Right)
        {
            if (A.Right != null) A.Right.Parent = B;
            if (A.Left != null) A.Left.Parent = B;

            if (B.Left != null) B.Left.Parent = A;

            if (!B.Parent.IsHeader)
            {
                if (B.Parent.Left == B)
                    B.Parent.Left = A;
                else
                    B.Parent.Right = A;
            }
            else B.Parent.Parent = A;

            A.Parent = B.Parent;
            B.Parent = A;

            B.Right = A.Right;
            A.Right = B;

            SwapNodeReference(ref A.Left, ref B.Left);
        }
        else
        {
            if (A.Parent == B.Parent)
                SwapNodeReference(ref A.Parent.Left, ref A.Parent.Right);
            else
            {
                if (!A.Parent.IsHeader)
                {
                    if (A.Parent.Left == A)
                        A.Parent.Left = B;
                    else
                        A.Parent.Right = B;
                }
                else A.Parent.Parent = B;

                if (!B.Parent.IsHeader)
                {
                    if (B.Parent.Left == B)
                        B.Parent.Left = A;
                    else
                        B.Parent.Right = A;
                }
                else B.Parent.Parent = A;
            }

            if (B.Left != null) B.Left.Parent = A;
            if (B.Right != null) B.Right.Parent = A;

            if (A.Left != null) A.Left.Parent = B;
            if (A.Right != null) A.Right.Parent = B;

            SwapNodeReference(ref A.Left, ref B.Left);
            SwapNodeReference(ref A.Right, ref B.Right);
            SwapNodeReference(ref A.Parent, ref B.Parent);
        }

        State Balance = A.Balance; A.Balance = B.Balance; B.Balance = Balance;
    }

    internal static Node GetEndItem(Node Node)
    {
        while (!Node.IsHeader) Node = Node.Parent;
        return Node;
    }
}


}