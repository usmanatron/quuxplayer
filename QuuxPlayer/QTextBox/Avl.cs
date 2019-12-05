/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace QuuxControls
{
    internal class Avl<T> /*: IEnumerable<T> */ where T : AvlNode<T>
    {
        // Private State

        private T root;
        private T cache;
        private int cacheIndex = int.MinValue / 2;

#if DEBUG
        private int cacheTries = 0;
        private int cacheMisses = 0;
#endif

        private object threadLock = new object();

        public Avl(T EmptyNode)
        {
            root = EmptyNode;
            cacheIndex = int.MinValue / 2;
        }

        // Properties

        private bool doing = false;

        public T this[int Index]
        {
            get
            {
                lock (threadLock)
                {


#if DEBUG

                    //AvlNode<T> tt = root.LeftMost;
                    //for (int i = 0; i < this.Count; i++)
                    //{
                    //    Debug.Assert(getNodeNum(root, i) == tt);
                    //    tt = tt.Next;
                    //}

                    cacheTries++;
#endif
                    System.Diagnostics.Debug.Assert(!doing);

                    doing = true;
                    
                    if (Index == cacheIndex)
                    {
                        Debug.Assert(cacheIndex == Index);
                        doing = false;
                        return cache;
                    }
                    else if (Index == cacheIndex - 1)
                    {
                        cache = cache.Previous;
                        cacheIndex--;
                        doing = false;
                        return cache;
                    }
                    else if (Index == cacheIndex + 1)
                    {
                        cache = cache.Next;
                        cacheIndex++;
                        doing = false;
                        return cache;
                    }
                    
#if DEBUG
                    cacheMisses++;
#endif
                    cache = getNodeNum(root, Index);
                    cacheIndex = Index;
                    doing = false;
                    return cache;
                }
            }
        }
        public int Count
        {
            get
            {
                lock (threadLock)
                {
                    return (root == null) ? 0 : root.Count;
                }
            }
        }
        public int Length
        {
            get
            {
                lock (threadLock)
                {
                    if (root == null)
                        return 0;
                    else
                        return root.SubTreeLength;
                }
            }
        }
        public int MaxLength
        {
            get
            {
                lock (threadLock)
                {
                    
                    if (root == null)
                    {
                        return 0;
                    }
                    else
                    {
                        return root.MaxLength;
                    }
                }
            }
        }

        // Public Methods

        public void Add(T Item)
        {
            lock (threadLock)
            {
                if (root == null)
                {
                    root = Item;
                    root.Fix();
                    //test(true);
                    
                }
                else
                {
                    root.Append(Item);
                    Item.Fix();
                    Item.Balance(false);
                    fixRoot();
                    //test(true);

                }
                cacheIndex = int.MinValue / 2;
            }
        }
        public void Insert(T Item, int Index)
        {
            lock (threadLock)
            {
                //if (cacheIndex >= Index)
                    //cacheIndex = int.MinValue / 2;

                if (root == null || Index >= root.Count)
                {
                    Add(Item);
                }
                else
                {
                    this[Index].InsertBefore(Item);

                    cacheIndex = int.MinValue / 2;

                    Item.Fix();

                    Item.Balance(false);

                    fixRoot();

                    //test(true);
                }
                System.Diagnostics.Debug.Assert(Item.Index >= this.Count - 1 || Item.Next.Index == Item.Index + 1);
                System.Diagnostics.Debug.Assert(Item.Index == 0 || Item.Previous.Index == Item.Index - 1);
            }
        }
        public void Delete(int Index)
        {
            lock (threadLock)
            {
                //if (cacheIndex >= Index)
                
                T n = this[Index];

                n = delete(n);

                cacheIndex = int.MinValue / 2;

                if (n != null)
                {
                    n.Fix();
                    //test(false);
                    n.Balance(true);
                    fixRoot();
                    //test(true);
                }
                else
                {
                    root = null;
                }
            }
        }
        public void Clear(T EmptyNode)
        {
            lock (threadLock)
            {
                cacheIndex = int.MinValue / 2;
                root = EmptyNode;
            }
        }

        private T delete(T node)
        {
            T retNode;

            if (node.Left == null && node.Right == null)
            {
                if (node.Parent == null)
                {
                    root = null;
                }
                else if (node.IsRight)
                {
                    node.Parent.Right = null;
                }
                else
                {
                    node.Parent.Left = null;
                }
                retNode = node.Parent;
            }
            else if (node.Left == null)
            {
                node.Right.Parent = node.Parent;
                node.Right.IsRight = node.IsRight;

                if (node.Parent == null)
                {
                    root = node.Right;
                }
                else
                {
                    if (node.IsRight)
                        node.Parent.Right = node.Right;
                    else
                        node.Parent.Left = node.Right;
                }
                retNode = node.Right;
            }
            else if (node.Right == null)
            {
                node.Left.Parent = node.Parent;
                node.Left.IsRight = node.IsRight;

                if (node.Parent == null)
                {
                    root = node.Left;
                }
                else
                {
                    if (node.IsRight)
                        node.Parent.Right = node.Left;
                    else
                        node.Parent.Left = node.Left;
                }
                retNode = node.Left;
            }
            else // interior node
            {
                T n = node.Right.LeftMost;

                if (n.IsRight)
                {
                    n.Parent = node.Parent;

                    n.IsRight = node.IsRight;
                    n.Left = node.Left;

                    if (n.Left != null)
                        n.Left.Parent = n;

                    if (node.Parent == null)
                    {
                        root = n;
                        Debug.Assert(root.Parent == null);
                    }
                    else
                    {
                        if (node.IsRight)
                            node.Parent.Right = n;
                        else
                            node.Parent.Left = n;
                    }
                    retNode = n;
                }
                else
                {
                    if (n.Right != null)
                    {
                        retNode = n.Right;

                        n.Right.IsRight = n.IsRight;
                        n.Right.Parent = n.Parent;
                        n.Parent.Left = n.Right;
                        if (n.IsRight)
                            n.Right.Parent.Right = n.Right;
                        else
                            n.Right.Parent.Left = n.Right;
                    }
                    else
                    {
                        retNode = n.Parent;
                        n.Parent.Left = null;
                    }

                    n.Parent = node.Parent;
                    n.IsRight = node.IsRight;

                    n.Right = node.Right;
                    if (n.Right != null)
                        n.Right.Parent = n;

                    n.Left = node.Left;
                    if (n.Left != null)
                        n.Left.Parent = n;

                    if (n.Parent == null)
                    {
                        root = n;
                    }
                    else
                    {
                        if (n.IsRight)
                            n.Parent.Right = n;
                        else
                            n.Parent.Left = n;
                    }
                }
            }
            return retNode;
        }
        private T getNodeNum(T node, int Index)
        {
            if (node.Left == null)
            {
                if (Index == 0)
                {
                    return node;
                }
                else
                {
                    return getNodeNum(node.Right, Index - 1);
                }
            }
            else
            {
                if (node.Left.Count > Index)
                {
                    return getNodeNum(node.Left, Index);
                }
                else if (node.Left.Count == Index)
                {
                    return node;
                }
                else
                {
                    // node.right should not be null
                    return getNodeNum(node.Right, Index - node.Left.Count - 1);
                }
            }
        }
        private void fixRoot()
        {
            while (root.Parent != null)
            {
                root = root.Parent;
            }
        }

        //// Test
        //[Conditional("DEBUG")]
        //private void test(bool TestAvl)
        //{
        //    testIntegrity(root);
        //    testStructure(root);
        //    if (TestAvl)
        //        testAvl(root);
        //    testIndex();
        //}
        //[Conditional("DEBUG")]
        //private void testIntegrity(T node)
        //{
        //    Debug.Assert((node.Parent == null && node.Equals(root)) || (node.Parent != null && !node.Equals(root)));

        //    Debug.Assert(node.Parent == null || (node.IsRight && node.Parent.Right.Equals(node)) ||
        //        (!node.IsRight && node.Parent.Left.Equals(node)));

        //    if (node.Left != null)
        //        testIntegrity(node.Left);

        //    if (node.Right != null)
        //        testIntegrity(node.Right);

        //}

        //[Conditional("DEBUG")]
        //private void testStructure(T node)
        //{


        //    Debug.Assert(((node.Left == null) && (node.Right == null)) ||
        //                 ((node.Left == null) && (node.Right.Count == node.Count - 1)) ||
        //                 ((node.Right == null) && (node.Left.Count == node.Count - 1)) ||
        //                 (node.Count == node.Left.Count + node.Right.Count + 1));

        //    Debug.Assert(((node.Left == null) && (node.Right == null)) ||
        //                 ((node.Left == null) && (node.Right.SubTreeLength == node.SubTreeLength - node.Length - 1)) ||
        //                 ((node.Right == null) && (node.Left.SubTreeLength == node.SubTreeLength - node.Length - 1)) ||
        //                 (node.SubTreeLength == node.Left.SubTreeLength + node.Right.SubTreeLength + node.Length + 1));

        //    if (node.Left != null)
        //    {
        //        Debug.Assert(node.Parent == null || node.Left.IsRight == false);
        //        Debug.Assert(((node.Right == null) && (node.Height == node.Left.Height + 1)) ||
        //                     (node.Height == Math.Max(node.Right.Height + 1, node.Left.Height + 1)));

        //        testStructure(node.Left);
        //    }

        //    if (node.Right != null)
        //    {
        //        Debug.Assert(node.Parent == null || node.Right.IsRight == true);
        //        Debug.Assert(((node.Left == null) && (node.Height == node.Right.Height + 1)) ||
        //                     (node.Height == Math.Max(node.Right.Height + 1, node.Left.Height + 1)));
        //        testStructure(node.Right);
        //    }
        //    if (node.Left == null && node.Right == null)
        //    {
        //        Debug.Assert(node.Height == 0);
        //    }
        //}
        //[Conditional("DEBUG")]
        //private void testIndex()
        //{
        //    for (int i = 0; i < root.Count; i++)
        //    {
        //        Debug.Assert(this[i].Index == i);
        //    }
        //}
        //[Conditional("DEBUG")]
        //private void testAvl(T node)
        //{
        //    if (node.Right != null)
        //    {
        //        testAvl(node.Right);
        //    }
        //    if (node.Left != null)
        //    {
        //        testAvl(node.Left);
        //    }

        //    Debug.Assert(Math.Abs(node.BalanceFactor) < 2);
        //}
    }
}
