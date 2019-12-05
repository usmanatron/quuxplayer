/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxControls
{
    internal class Word
    {
        private const int TAB_SPACES = 8;
        public int Start { get; private set; }
        public int End { get; private set; }
        public string Text { get; private set; }
        public bool WhiteSpace { get; private set; }
        
        public bool SingleLineComment { get; private set; }

        private bool quote;

        private static List<char> breakChars = new List<char> { '.', '"', '[', ']', '(', ')', '{', '}', '*', '%', ':', ';', '\\', '!', '`', '~', '|', '=', '+', '-', ',' };
        private static List<char> breakLeftChars = new List<char> { '<' };
        private static List<char> breakRightChars = new List<char> { '>' };

        private static string commentSymbol = "//";

        public Word(int Start, int End, string Text)
        {
            this.Start = Start;
            this.End = End;
            this.Text = this.ToString(Text);
            this.WhiteSpace = false;
            this.Quote = false;
        }
        public Word(int Start, int End, string Text, bool WhiteSpace) : this(Start, End, Text)
        {
            this.WhiteSpace = WhiteSpace;
        }
        public bool Quote
        {
            get { return quote; }
            set
            {
                quote = value;
            }
        }
        public static List<Word> GetWords(string Input, ref bool Quoting, ref bool SingleLineCommenting, ref bool Escaping)
        {
            List<Word> words = new List<Word>();
            if (Input.Length > 0)
            {
                int cursor = 0;
                int start = 0;

                bool wasSpace = Input[0] == ' ';
                bool breakAfter = false;

                if (SingleLineCommenting)
                {
                    words.Add(new Word(0, Input.Length, Input));
                    words[0].SingleLineComment = true;
                    return words;
                }
                while (cursor < Input.Length)
                {   
                    if (Quoting)
                    {
                        if (((cursor > 0 && Input[cursor - 1] != '\\') || (cursor == 0 && !Escaping)) && (Quoting && Input[cursor] == '"'))
                        {
                            Quoting = false;
                            cursor++;
                            Word w = new Word(start, cursor, Input);
                            w.Quote = true;
                            words.Add(w);
                            Quoting = false;
                            start = cursor;
                        }
                        else
                        {
                            cursor++;
                        }
                    }
                    else if (Input[cursor] == '"')
                    {
                        Quoting = true;
                        if (cursor > start)
                            words.Add(new Word(start, cursor, Input, wasSpace));
                        wasSpace = false;
                        breakAfter = false;
                        start = cursor++;
                    }
                    else if (isSymbol(Input, commentSymbol, cursor))
                    {
                        if (cursor > start)
                            words.Add(new Word(start, cursor, Input, wasSpace));
                        Word w = new Word(cursor, Input.Length, Input);
                        w.SingleLineComment = true;
                        words.Add(w);
                        SingleLineCommenting = true;
                        return words;
                    }
                    else if (breakAfter)
                    {
                        wasSpace = Input[cursor] == ' ';
                        words.Add(new Word(start, cursor, Input));
                        start = cursor++;
                        breakAfter = false;
                    }
                    else if (wasSpace && Input[cursor] == ' ')
                    {
                        cursor++;
                    }
                    else if (breakChars.IndexOf(Input[cursor]) >= 0)
                    {
                        if (cursor > start)
                            words.Add(new Word(start, cursor, Input, wasSpace));
                        wasSpace = false;
                        breakAfter = true;
                        start = cursor++;
                    }
                    else if (breakLeftChars.IndexOf(Input[cursor]) >= 0)
                    {
                        if (cursor > start)
                            words.Add(new Word(start, cursor, Input, wasSpace));
                        wasSpace = false;
                        breakAfter = false;
                        start = cursor++;
                    }
                    else if (cursor > 0 && breakRightChars.IndexOf(Input[cursor - 1]) >= 0)
                    {
                        words.Add(new Word(start, cursor, Input, wasSpace));
                        wasSpace = false;
                        breakAfter = false;
                        start = cursor++;
                    }
                    else if (wasSpace)
                    {
                        if (cursor > start)
                            words.Add(new Word(start, cursor, Input, true));
                        wasSpace = false;
                        start = cursor++;
                    }
                    else if (Input[cursor] == ' ')
                    {
                        if (cursor > start)
                            words.Add(new Word(start, cursor, Input));
                        wasSpace = true;
                        start = cursor++;
                    }
                    else
                    {
                        cursor++;
                    }
                }
                if (start < Input.Length)
                {
                    Word w = new Word(start, Input.Length, Input);
                    w.Quote =Quoting;
                    w.SingleLineComment = SingleLineCommenting;
                    words.Add(w);
                }
                Escaping = Input[Input.Length - 1] == '\\';
            }
            return words;
        }
        private static bool isSymbol(string Input, string Symbol, int Cursor)
        {
            if (Input[Cursor] == Symbol[0] && Input.Length > Cursor + Symbol.Length - 1)
            {
                bool sym = true;
                for (int i = 1; i < Symbol.Length; i++)
                    sym &= Input[i + Cursor] == Symbol[i];
                return sym;
            }
            return false;
        }
        public override string ToString()
        {
            return "Start: " + Start.ToString() + " End: " + End.ToString() + " Word: " + Text;
        }
        public string ToString(string Input)
        {
            return Input.Substring(Start, End - Start);
        }
    }
}
