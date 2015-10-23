using System;

namespace ISharp.Collections
{

    public class EntryAlreadyExistsException : Exception
    {
        static String message = "An entry already exists in the collection.";

        public EntryAlreadyExistsException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class OutOfKeyOrderException : Exception
    {
        static String message = "A tree was found to be out of key order.";

        public OutOfKeyOrderException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class TreeInvalidParentException : Exception
    {
        static String message = "The validation routines detected that the parent structure of a tree is invalid.";

        public TreeInvalidParentException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class TreeOutOfBalanceException : Exception
    {
        static String message = "The validation routines detected that the tree is out of balance.";

        public TreeOutOfBalanceException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class InvalidEmptyTreeException : Exception
    {
        static String message = "The validation routines detected that an empty tree is invalid.";

        public InvalidEmptyTreeException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }


    public class InvalidEndItemException : Exception
    {
        static String message = "The validation routines detected that the end item of a tree is invalid.";

        public InvalidEndItemException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class EntryNotFoundException : Exception
    {
        static String message = "The requested entry could not be located in the specified collection.";

        public EntryNotFoundException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class HashEntryNotFoundException : Exception
    {
        static String message = "An entry in a hash table could not be located.";

        public HashEntryNotFoundException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }


    public class AtBeginningOrEndException : Exception
    {
        static String message = "The current position is at the beginning or end of the object.";

        public AtBeginningOrEndException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class AllocationFailedException : Exception
    {
        static String message = "Memory allocation failed.";

        public AllocationFailedException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class AddSubTreeFailedException : Exception
    {
        static String message = "Subtree creation failed.";

        public AddSubTreeFailedException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class EntryNotFromTreeException : Exception
    {
        static String message = "The specified enumerator is not from the given tree.";

        public EntryNotFromTreeException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class ObjectDestroyedException : Exception
    {
        static String message = "The entry has already been destroyed.";

        public ObjectDestroyedException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class SameTreeException : Exception
    {
        static String message = "The items were from an invalid tree.";

        public SameTreeException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class DifferentTreesException : Exception
    {
        static String message = "The items were from different trees.";

        public DifferentTreesException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class InvalidTypeException : Exception
    {
        static String message = "A type mismatch occurred.";

        public InvalidTypeException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class InvalidSetOperationException : Exception
    {
        static String message = "An invalid set operation was specified.";

        public InvalidSetOperationException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class IsEndItemException : Exception
    {
        static String message = "The requested action cannot be performed on an end item.";

        public IsEndItemException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class DifferentKeysException : Exception
    {
        static String message = "The specified items have different keys.";

        public DifferentKeysException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class InvalidTreeHashTableException : Exception
    {
        static String message = "The hash table associated with a tree is invalid or corrupt.";

        public InvalidTreeHashTableException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class IndexOutOfBoundsException : Exception
    {
        static String message = "The specified index is out of allowable bounds.";

        public IndexOutOfBoundsException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class NotComparableException : Exception
    {
        static String message = "The key is not comparable - derive key K from IComparable of T or supply a comparer.";

        public NotComparableException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class NotEquatableException : Exception
    {
        static String message = "The key is not equatable - derive key K from IEquatable of T or supply a comparer.";

        public NotEquatableException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class NotHashComparableException : Exception
    {
        static String message = "The data type is not hash comparable - derive it from the interface IHashComparable or supply a comparer.";

        public NotHashComparableException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }


    public class InvalidParameterException : Exception
    {
        static String message = "An input parameter is invalid.";

        public InvalidParameterException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "Collections.Net.dll";
        }
    }

    public class IsListRootException : Exception
    {
        static String message = "An invalid operation was requested on a list Root Node.";

        public IsListRootException() : base(message) { }
    }

    public class InvalidShiftParametersException : Exception
    {
        static String message = "The shift parameters were invalid.";

        public InvalidShiftParametersException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "System.Net.dll";
        }
    }

    public class InvalidListNodeException : Exception
    {
        static String message = "An invalid list node was specified on input.";

        public InvalidListNodeException()
            : base(message)
        {
            this.HelpLink = "mail@BenedictBede.com";
            this.Source = "System.Net.dll";
        }
    }

}
