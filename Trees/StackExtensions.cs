using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Trees
{
    internal static class StackExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T TryPop<T>(this Stack<T> stack)
        {
            return stack.Count == 0 ? default(T) : stack.Pop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T TryPeek<T>(this Stack<T> stack)
        {
            return stack.Count == 0 ? default(T) : stack.Peek();
        }
    }
}