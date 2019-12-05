/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace QuuxControls
{
    internal abstract class AvlNode<T> where T : AvlNode<T>
    {
        // State

        private T left = null;
        private T right = null;
        private T parent = null;

        private bool isRight;
        private int height = 0;
        private int count = 1;
        private int subTreeLength = 0;
        private int maxLength = 0;

        public T Left
        {
            get { return left; }
            set { left = value; }
        }
        public T Right
        {
            get { return right; }
            set { right = value; }
        }
        public T Parent
        {
            get { return parent; }
            set { parent = value; }
        }
        public bool IsRight
        {
            get { return isRight; }
            set { isRight = value; }
        }
        public int Height
        {
            get { return height; }
            set { height = value; }
        }
        public int Count
        {
            get { return count; }
            set { count = value; }
        }
        public int Index
        {
            get
            {
                if (Parent == null)
                {
                    if (Left == null)
                        return 0;
                    else
                        return Left.Count;
                }
                else if (isRight)
                {
                    if (Left == null)
                        return Parent.Index + 1;
                    else
                        return Parent.Index + Left.Count + 1;
                }
                else
                {
                    if (Right == null)
                        return Parent.Index - 1;
                    else
                        return Parent.Index - Right.Count - 1;
                }
            }
        }
        public int SubTreeLength
        {
            get
            {
                return subTreeLength;
            }
            set
            {
                subTreeLength = value;
            }
        }
        public int MaxLength
        {
            get { return maxLength; }
            set { maxLength = value; }
        }
        public abstract int Length { get; }

        public AvlNode()
        {
        }

        public T Previous
        {
            get
            {
                T node = previousInSubTree;
                if (node != null)
                    return node;
                else if (isRight && Parent != null)
                    return Parent;
                else if (!isRight && Parent != null)
                    return Parent.previousUp;
                else
                    return null;
            }
        }
        public T Next
        {
            get
            {
                T node = nextInSubTree;
                if (node != null)
                    return node;
                else if (!isRight && Parent != null)
                    return Parent;
                else if (isRight && Parent != null)
                    return Parent.nextUp;
                else
                    return null;
            }
        }
        public T RightMost
        {
            get
            {
                if (this.Right == null)
                    return (T)this;
                else
                    return this.Right.RightMost;
            }
        }
        public T LeftMost
        {
            get
            {
                if (this.Left == null)
                    return (T)this;
                else
                    return this.Left.LeftMost;
            }
        }
        public int BalanceFactor
        {
            get
            {
                int balance = 0;

                if (Left != null)
                    balance -= (Left.height + 1);
                if (Right != null)
                    balance += (Right.height + 1);

                return balance;
            }
        }
        public int StartIndex
        {
            get
            {
                int si = 0;

                if (Left != null)
                    si = Left.SubTreeLength;

                AvlNode<T> n = this;

                do
                {
                    AvlNode<T> parent = n.Parent;
                    if (n.isRight && parent != null)
                    {
                        si += parent.Length + 1;
                        if (parent.Left != null)
                            si += parent.Left.SubTreeLength;
                    }
                    n = parent;
                }
                while (n != null);

                return si;
            }
        }

        public void Append(T Item)
        {
            this.RightMost.addRight(Item);
        }
        public void InsertBefore(T Item)
        {
            if (Left == null)
            {
                addLeft(Item);
            }
            else
            {
                Left.RightMost.addRight(Item);
            }
        }
        public void Balance(bool Recursive)
        {
            int balance = this.BalanceFactor;
            bool rotated = false;

            if (balance < -1)
            {
                if (this.Left.BalanceFactor > 0)
                {
                    this.Left.rotateLeft();
                }

                this.rotateRight();

                rotated = true;
            }
            else if (balance > 1)
            {
                if (this.Right.BalanceFactor < 0)
                {
                    this.Right.rotateRight();
                }

                this.rotateLeft();

                rotated = true;
            }

            this.Fix();

            if ((!rotated || Recursive) && (this.Parent != null))
                this.Parent.Balance(Recursive);
        }
        public void Fix()
        {
            this.Count = 1;
            this.Height = 0;
            this.SubTreeLength = this.Length + 1;
            
            this.MaxLength = this.Length;
            
            if (this.Left != null)
            {
                this.Count += this.Left.Count;
                this.Height = this.Left.Height + 1;
                this.SubTreeLength += this.Left.SubTreeLength;
                this.MaxLength = Math.Max(this.Left.MaxLength, this.MaxLength);
            }
            if (this.Right != null)
            {
                this.Count += this.Right.Count;
                this.Height = Math.Max(this.Right.Height + 1, this.Height);
                this.SubTreeLength += this.Right.SubTreeLength;
                this.MaxLength = Math.Max(this.Right.MaxLength, this.MaxLength);
            }
            if (this.Parent != null)
            {
                this.Parent.Fix();
            }
        }

        private T previousUp
        {
            get
            {
                if (Parent == null)
                    return null;
                else if (isRight)
                    return Parent;
                else
                    return Parent.previousUp;
            }
        }
        private T previousInSubTree
        {
            get
            {
                if (isRight)
                {
                    if (Left == null)
                    {
                        return Parent;
                    }
                    else
                    {
                        return Left.RightMost;
                    }
                }
                else
                {
                    if (Left == null)
                    {
                        return null;
                    }
                    else
                    {
                        return Left.RightMost;
                    }
                }
            }
        }
        private T nextUp
        {
            get
            {
                if (Parent == null)
                    return null;
                else if (isRight)
                    return Parent.nextUp;
                else
                    return Parent;
            }
        }
        private T nextInSubTree
        {
            get
            {
                if (isRight)
                {
                    if (Right == null)
                    {
                        return null;
                    }
                    else
                    {
                        return Right.LeftMost;
                    }
                }
                else
                {
                    if (Right == null)
                    {
                        return Parent;
                    }
                    else
                    {
                        return Right.LeftMost;
                    }
                }
            }
        }
        private void rotateRight()
        {
            T left = this.Left;

            left.Parent = this.Parent;
            this.Left = left.Right;
            if (this.Left != null)
            {
                this.Left.Parent = (T)this;
                this.Left.IsRight = false;
            }
            left.Right = (T)this;
            this.Parent = left;
            if (left.Parent != null)
            {
                if (this.IsRight)
                    left.Parent.Right = left;
                else
                    left.Parent.Left = left;
            }
            left.IsRight = this.IsRight;

            this.IsRight = true;

            this.Fix();
        }
        private void rotateLeft()
        {
            T right = this.Right;

            right.Parent = this.Parent;
            this.Right = right.Left;

            if (this.Right != null)
            {
                this.Right.Parent = (T)this;
                this.Right.IsRight = true;
            }

            right.Left = (T)this;
            this.Parent = right;
            if (right.Parent != null)
            {
                if (this.IsRight)
                    right.Parent.Right = right;
                else
                    right.Parent.Left = right;
            }
            right.IsRight = this.IsRight;

            this.IsRight = false;

            this.Fix();
        }
        private void addRight(T Item)
        {
            Debug.Assert(Right == null);

            Right = Item;
            Right.IsRight = true;
            Right.Parent = (T)this;
        }
        private void addLeft(T Item)
        {
            Debug.Assert(Left == null);

            Left = Item;
            Left.IsRight = false;
            Left.Parent = (T)this;
        }
    }
}
