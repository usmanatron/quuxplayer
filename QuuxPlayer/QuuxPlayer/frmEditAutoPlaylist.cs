/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed partial class frmEditAutoPlaylist : QFixedDialog
    {
        private const string BLANK_KEY = "Blank";

        private static readonly string instructions = Localization.Get(UI_Key.Edit_Auto_Playlist_Instructions);
        private static QuuxControls.QTextBox textbox;
        private static frmEditAutoPlaylist currentInstance;

        private static List<string> stringComparitors;
        private static List<string> comparitorsComplete;
        private static List<string> numericComparitors;
        private static List<string> orderableFields;
        private static List<string> orderableFieldsComplete;
        private static List<string> stringFields;
        private static List<string> numericFields;
        private static List<string> binaryFields;
        private static List<string> allFieldsAndModifierStarts;
        private static List<string> allStarts;
        private static List<string> literals;
        private static List<string> operators;
        private static List<string> operatorsComplete;
        private static List<string> modifierStarts;
        private static List<string> modifierEnds;
        private static List<string> operatorsAndModifiers;
        private static List<string> initialModifiers;
        private static List<string> limitTypes;
        private static List<string> trueOrFalse;

        private ExpressionTree.ValidationError valid = ExpressionTree.ValidationError.Valid;
        
        private bool convert = false;

        private QButton btnSaveAndConvert;
        private QButton btnTest;
        private QButton btnHelp;

        private Label lblOK;
        private QLabel lblMessage;

        public frmEditAutoPlaylist() : base(Localization.Get(UI_Key.Edit_Auto_Playlist_Title), ButtonCreateType.OKAndCancel)
        {
            btnSaveAndConvert = new QButton(Localization.Get(UI_Key.Edit_Auto_Playlist_Save_As_Standard), false, true);
            btnSaveAndConvert.ButtonPressed += (s) => { btnSaveAndConvert_Click(); };
            this.Controls.Add(btnSaveAndConvert);

            btnOK.Text = Localization.Get(UI_Key.Edit_Auto_Playlist_Save);

            btnTest = new QButton(Localization.Get(UI_Key.Edit_Auto_Playlist_Test), false, false);
            AddButton(btnTest, btnTest_Click);

            btnHelp = new QButton(Localization.Get(UI_Key.Edit_Auto_Playlist_Help), false, false);
            AddButton(btnHelp, btnHelp_Click);

            lblOK = new Label();
            lblOK.BorderStyle = BorderStyle.FixedSingle;
            lblOK.BackColor = Color.Green;
            lblOK.ForeColor = Styles.LightText;
            lblOK.Size = new Size(12, 12);
            this.Controls.Add(lblOK);

            lblMessage = new QLabel(String.Empty);
            this.Controls.Add(lblMessage);

            textbox.CaretLocationChanged += (dl) => { };
            textbox.SelectionChanged += (s, e) => { };
            textbox.DocumentChangedChanged += (s, e) => { };
            textbox.BeforeShowIntellisense += new QuuxControls.BeforeShowIntellisense(textbox_BeforeShowIntellisense);
            textbox.NeedWordsBeforeCursorRefresh += (s, e) => { textbox.WordsBeforeCursor = getWordsBeforeCursor(); };
            textbox.Location = new Point(MARGIN, MARGIN + Styles.TextHeight + MARGIN);

            this.Controls.Add(textbox);

            currentInstance = this;

            this.ClientSize = new System.Drawing.Size(580, 360);
        }

        private void textbox_BeforeShowIntellisense(object sender, QuuxControls.BeforeShowIntellisenseEventArgs e)
        {
            if (textbox.IsCommentedOrQuoted(textbox.CaretLocation))
            {
                e.Cancel = true;
                return;
            }
            string w = textbox.CaretIsAfterWhiteSpaceOrParen ? textbox.LastWord : textbox.WordBeforeCurrent;

            List<string> ww = textbox.WordsBeforeCursor;

            bool modifierSection = ww.Contains("SortBy", StringComparer.InvariantCultureIgnoreCase) ||
                                   ww.Contains("SelectBy", StringComparer.InvariantCultureIgnoreCase) ||
                                   ww.Contains("LimitTo", StringComparer.InvariantCultureIgnoreCase);

            if (modifierSection)
            {
                switch (w.ToLower())
                {
                    case "sortby":
                    case "selectby":
                    case "thenby":
                        e.Values = orderableFieldsComplete;
                        break;
                    case "tracks":
                    case "kilobytes":
                    case "megabytes":
                    case "gigabytes":
                    case "days":
                    case "hours":
                    case "minutes":
                    case "seconds":
                    case "ascending":
                    case "descending":
                        e.Values = modifierStarts.ToList();
                        adjustModifierSection(e, ww);
                        break;
                    case "limitto":
                        e.Cancel = true;
                        break;
                    default:
                        if (w == "ascending" || w == "descending" || orderableFieldsComplete.Contains(w, StringComparer.InvariantCultureIgnoreCase))
                        {
                            e.Values = modifierStarts.ToList(); // copy since the list will be changed

                            adjustModifierSection(e, ww);
                            
                            string wbwbc = textbox.WordBeforeWordBeforeCurrent.ToLower();
                            if (wbwbc == "selectby" || wbwbc == "sortby" || wbwbc == "thenby")
                            {
                                e.Values = e.Values.Union(modifierEnds).ToList();
                            }
                        }
                        else
                        {
                            string wbwbc = textbox.WordBeforeWordBeforeCurrent.ToLower();
                            if (wbwbc == "limitto")
                            {
                                e.Values = limitTypes;
                            }
                            else
                            {
                                e.Cancel = true;
                            }
                        }
                        break;
                }

            }
            else
            {
                if (w.Length > 0)
                {
                    if (numericFields.Contains(w, StringComparer.InvariantCultureIgnoreCase))
                    {
                        e.Values = numericComparitors;
                    }
                    else if (stringFields.Contains(w, StringComparer.InvariantCultureIgnoreCase))
                    {
                        switch (w.ToLower())
                        {
                            case "track":
                                e.Values = new List<string>() { "ContainedIn", "NotContainedIn" };
                                break;
                            default:
                                e.Values = stringComparitors;
                                break;
                        }
                    }
                    else if (numericFields.Contains(w, StringComparer.InvariantCultureIgnoreCase))
                    {
                        e.Values = numericComparitors;
                    }
                    else if (binaryFields.Contains(w, StringComparer.InvariantCultureIgnoreCase))
                    {
                        e.Values = new List<string>() { "Is" };
                    }
                    else if (operatorsComplete.Contains(w, StringComparer.InvariantCultureIgnoreCase))
                    {
                        e.Values = allFieldsAndModifierStarts;
                    }
                    else if (comparitorsComplete.Contains(w, StringComparer.InvariantCultureIgnoreCase))
                    {
                        e.ValueNeedsQuoting = true;
                        switch (textbox.WordBeforeWordBeforeCurrent.ToLower())
                        {
                            case "artist":
                                e.Values = Database.GetArtists();
                                e.Values.Insert(0, BLANK_KEY);
                                e.QuoteException = BLANK_KEY;
                                break;
                            case "albumartist":
                                e.Values = Database.GetAlbumArtists();
                                e.Values.Insert(0, BLANK_KEY);
                                e.QuoteException = BLANK_KEY;
                                break;
                            case "album":
                                e.Values = Database.GetAlbums();
                                e.Values.Insert(0, BLANK_KEY);
                                e.QuoteException = BLANK_KEY;
                                break;
                            case "genre":
                                e.Values = Database.GetGenres();
                                e.Values.Insert(0, BLANK_KEY);
                                e.QuoteException = BLANK_KEY;
                                break;
                            case "grouping":
                                e.Values = Database.GetGroupings();
                                e.Values.Insert(0, BLANK_KEY);
                                e.QuoteException = BLANK_KEY;
                                break;
                            case "composer":
                                e.Values = Database.GetComposers();
                                e.Values.Insert(0, BLANK_KEY);
                                e.QuoteException = BLANK_KEY;
                                break;
                            case "track":
                                e.Values = Database.GetPlaylists();
                                break;
                            case "compilation":
                                e.Values = trueOrFalse;
                                e.ValueNeedsQuoting = false;
                                break;
                            case "mono":
                                e.Values = trueOrFalse;
                                e.ValueNeedsQuoting = false;
                                break;
                            case "encoder":
                                e.Values = Database.GetEncoders();
                                e.Values.Insert(0, BLANK_KEY);
                                e.QuoteException = BLANK_KEY;
                                break;
                            case "equalizer":
                                e.Values = EqualizerSetting.GetAllSettingStrings(Equalizer.GetEqualizerSettings());
                                break;
                            case "filetype":
                                e.Values = Database.GetFileTypes();
                                break;
                            case "title":
                                e.Values = Database.GetTitles();
                                e.Values.Insert(0, BLANK_KEY);
                                e.QuoteException = BLANK_KEY;
                                break;
                            case "year":
                                e.Values = Database.GetYears();
                                break;
                            case "rating":
                                e.Values = new List<string>() { "0", "1", "2", "3", "4", "5" };
                                break;
                            default:
                                e.Cancel = true;
                                break;
                        }
                    }
                    else if (w == "byalbum" || w == "bytrack")
                    {
                        e.Values = allFieldsAndModifierStarts;
                    }
                    else if (w.Length > 0)
                    {
                        e.Values = operatorsAndModifiers;
                    }
                    else
                    {
                        e.Values = allFieldsAndModifierStarts;
                    }
                }
                else
                {
                    e.Values = allStarts;
                }
            }
        }

        private static void adjustModifierSection(QuuxControls.BeforeShowIntellisenseEventArgs e, List<string> ww)
        {
            bool hasSelect = ww.Contains("selectby", StringComparer.InvariantCultureIgnoreCase);
            bool hasSort = ww.Contains("sortby", StringComparer.InvariantCultureIgnoreCase);
            bool hasLimit = ww.Contains("limitto", StringComparer.InvariantCultureIgnoreCase);

            if (hasSelect || !hasLimit)
            {
                e.Values.Remove("SelectBy");
            }
            if (hasSort)
            {
                e.Values.Remove("SortBy");
            }
            if (!(hasSelect || hasSort))
            {
                e.Values.Remove("ThenBy");
            }
            if (hasSelect || hasSort)
            {
                e.Values.Remove("LimitTo");
            }
        }
        private void btnSaveAndConvert_Click()
        {
            save(true);    
        }

        public string PlaylistName
        {
            set { this.Text = Localization.Get(UI_Key.Edit_Auto_Playlist_Title) + ": " + value; }
        }
        public bool Valid
        {
            get { return valid == ExpressionTree.ValidationError.Valid; }
        }

        static frmEditAutoPlaylist()
        {
            setupTextBox();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            textbox.DocumentChanged += new EventHandler(textbox_ContentsChanged);
            this.ActiveControl = textbox;
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            this.Controls.Remove(textbox);
            textbox.DocumentChanged -= textbox_ContentsChanged;
            base.OnFormClosing(e);
        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            //base.OnKeyUp(e);
        }

        public string Expression
        {
            get
            {
                return textbox.ToString();
            }
            set
            {
                textbox.Clear();
                textbox.Insert(QuuxControls.DocumentLocation.BOF, value);
                textbox_ContentsChanged(this, EventArgs.Empty);
                textbox.ClearUndo();
                if (textbox.ToString().Trim().Length == 0)
                    textbox.ShowIntellisense();
            }
        }
        public bool Convert
        {
            get { return convert; }
        }
        private static void setupTextBox()
        {
            stringComparitors = new List<string> { "Is",
                                                   "IsNot",
                                                   "ComesAfter",
                                                   "ComesBefore",
                                                   "Contains",
                                                   "StartsWith",
                                                   "EndsWith",
                                                   "DoesNotStartWith",
                                                   "DoesNotEndWith",
                                                   "ContainedIn",
                                                   "DoesNotContain",
                                                   "NotContainedIn" };

            numericComparitors = new List<string>() { "Is",
                                                      "IsNot",
                                                      "LessThan",
                                                      "MoreThan",
                                                      "AtMost",
                                                      "AtLeast" };

            orderableFields = new List<string> { "FileSize",
                                                 "Length",
                                                 "Random",
                                                 "TrackNum",
                                                 "DiskNum",
                                                 "Year",
                                                 "PlayCount",
                                                 "Rating",
                                                 "BitRate",
                                                 "SampleRate",
                                                 "DaysSinceLastPlayed",
                                                 "FileAgeInDays",
                                                 "DaysSinceFileAdded" };

            stringFields = new List<string> { "Title",
                                              "Artist",
                                              "Album",
                                              "AlbumArtist",
                                              "Composer",
                                              "Genre",
                                              "FileType",
                                              "Grouping",
                                              "PlayCount",
                                              "FilePath",
                                              "Encoder",
                                              "Equalizer",
                                              "Track" };

            numericFields = new List<string> { "TrackNum",
                                               "DiskNum",
                                               "LengthInSeconds",
                                               "LengthInMinutes",
                                               "FileSizeInKB",
                                               "FileSizeInMB",
                                               "Year",
                                               "PlayCount",
                                               "Rating",
                                               "BitRate",
                                               "SampleRate",
                                               "DaysSinceLastPlayed",
                                               "FileAgeInDays",
                                               "DaysSinceFileAdded" };

            binaryFields = new List<string> { "Compilation", "Mono" };

            literals = new List<string> { "True",
                                          "False",
                                          "Blank" };

            operators = new List<string> { "And",
                                           "Or" };

            trueOrFalse = new List<string> { "True",
                                             "False" };

            modifierStarts = new List<string> { "SortBy",
                                                "LimitTo",
                                                "SelectBy",
                                                "ThenBy" };

            modifierEnds = new List<string> { "Ascending",
                                              "Descending" };

            initialModifiers = new List<string> { "ByAlbum",
                                                  "ByTrack" };

            limitTypes = new List<string> { "Tracks",
                                            "Kilobytes",
                                            "Megabytes",
                                            "Gigabytes",
                                            "Days",
                                            "Hours",
                                            "Minutes",
                                            "Seconds" };

            orderableFieldsComplete = orderableFields.Union(stringFields).ToList();
            orderableFieldsComplete.RemoveAll(w => w == "LengthInSeconds" ||
                                                   w == "LengthInMinutes" ||
                                                   w == "FileSizeInKB" ||
                                                   w == "FileSizeInMB" ||
                                                   w == "Track" ||
                                                   w == "Compilation" ||
                                                   w == "Mono");

            allFieldsAndModifierStarts = stringFields.Union(numericFields.Union(binaryFields)).ToList();
            allFieldsAndModifierStarts.AddRange(new List<string>() { "LimitTo", "SortBy" });
            
            allStarts = allFieldsAndModifierStarts.Union(initialModifiers).ToList();

            List<string> addedComparitors = new List<string>() { "=", "!=", ">", "<", "<=", ">=" };
            comparitorsComplete = numericComparitors.Union(stringComparitors.Union(addedComparitors)).ToList();

            operatorsAndModifiers = new List<string>() { "And", "Or", "LimitTo", "SortBy" };

            operatorsComplete = new List<string>() { "And", "Or", "||", "&&" };

            allStarts.Sort();
            orderableFieldsComplete.Sort();
            stringComparitors.Sort();
            stringFields.Sort();
            numericFields.Sort();
            numericComparitors.Sort();
            orderableFields.Sort();
            literals.Sort();
            operators.Sort();
            modifierStarts.Sort();
            modifierEnds.Sort();
            limitTypes.Sort();
            numericComparitors.Sort();
            allFieldsAndModifierStarts.Sort();

            textbox = new QuuxControls.QTextBox(Styles.FontMono, StringComparer.OrdinalIgnoreCase);

            textbox.AddKeywords(0, stringComparitors.ToArray());
            textbox.AddKeywords(0, numericComparitors.ToArray());
            textbox.AddKeywords(1, stringFields.ToArray());
            textbox.AddKeywords(1, numericFields.ToArray());
            textbox.AddKeywords(2, literals.ToArray());
            textbox.AddKeywords(3, operators.ToArray());
            textbox.AddKeywords(3, new string[] { "Nor" });
            textbox.AddKeywords(4, modifierStarts.ToArray());
            textbox.AddKeywords(4, modifierEnds.ToArray());
            textbox.AddKeywords(4, initialModifiers.ToArray());
            textbox.AddKeywords(5, limitTypes.ToArray());
            textbox.AddKeywords(6, orderableFields.ToArray());

            textbox.AddKeywords(0, new string[] { "=", "!=", "<", "<=", ">=", ">" });
            textbox.AddKeywords(3, new string[] { "&&", "||" });

            textbox.LineNumbering = true;
            textbox.WordWrap = true;

            textbox.SetKeywordGroupStyle(0, FontStyle.Regular, Color.DarkGreen);
            textbox.SetKeywordGroupStyle(1, FontStyle.Regular, Color.Blue);
            textbox.SetKeywordGroupStyle(2, FontStyle.Regular, Color.DarkOrange);
            textbox.SetKeywordGroupStyle(3, FontStyle.Regular, Color.Purple);
            textbox.SetKeywordGroupStyle(4, FontStyle.Regular, Color.Maroon);
            textbox.SetKeywordGroupStyle(5, FontStyle.Regular, Color.Brown);
            textbox.SetKeywordGroupStyle(6, FontStyle.Regular, Color.DodgerBlue);
        }

        private static void textbox_ContentsChanged(object sender, EventArgs e)
        {
            currentInstance.valid = new ExpressionTree(ExpressionTree.CleanExpression(textbox.ToString())).Compile();
            currentInstance.lblOK.BackColor = (currentInstance.valid == ExpressionTree.ValidationError.Valid) ? Color.Green : Color.Red;
            currentInstance.updateMessage();

            List<string> ww = getWordsBeforeCursor();
            ww.RemoveAll(w => (w == "(" || w == ")" || w == "\""));
            textbox.WordsBeforeCursor = ww;
        }

        private static List<string> getWordsBeforeCursor()
        {
            List<string> words;
            new ExpressionTree(ExpressionTree.CleanExpression(textbox.ToString(textbox.CaretLocation))).Compile(out words);
            return words;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            TextRenderer.DrawText(e.Graphics, instructions, Styles.Font, new Point(MARGIN, MARGIN), Styles.LightText, TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }
        private void updateMessage()
        {
            lblMessage.Text = getValidationErrorText(valid);
            /*
            string s = getValidationErrorText(valid);

            TextRenderer.DrawText(e.Graphics,
                                  s,
                                  Styles.Font,
                                  new Point(lblOK.Right + 1, lblOK.Top - 2),
                                  (valid == ExpressionTree.ValidationError.Valid) ? Styles.LightText : Styles.WarningText);
        
             */
        }

        private string getValidationErrorText(ExpressionTree.ValidationError ValError)
        {
            string s;
            switch (ValError)
            {
                case ExpressionTree.ValidationError.Valid:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_Valid);
                    break;
                case ExpressionTree.ValidationError.BadComparitor:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_Bad_Comparitor);
                    break;
                case ExpressionTree.ValidationError.BadField:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_Bad_Field);
                    break;
                case ExpressionTree.ValidationError.PlaylistError:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_Playlist_Error);
                    break;
                case ExpressionTree.ValidationError.BadExpression:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_Bad_Expression);
                    break;
                case ExpressionTree.ValidationError.UnmatchedParens:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_Unmatched_Parens);
                    break;
                case ExpressionTree.ValidationError.ShortExpression:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_Short_Expression);
                    break;
                case ExpressionTree.ValidationError.UnknownKeyword:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_Unknown_Keyword);
                    break;
                case ExpressionTree.ValidationError.BooleanValueError:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_Boolean_Value_Error);
                    break;
                case ExpressionTree.ValidationError.NumericValueError:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_Numeric_Value_Error);
                    break;
                case ExpressionTree.ValidationError.NumericComparatorError:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_Numeric_Comparitor_Error);
                    break;
                case ExpressionTree.ValidationError.BadSelectByModifier:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_Bad_SelectBy);
                    break;
                case ExpressionTree.ValidationError.BadLimitToModifier:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_Bad_LimitTo);
                    break;
                case ExpressionTree.ValidationError.BadSortByModifier:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_Bad_SortBy);
                    break;
                case ExpressionTree.ValidationError.BadThenByModifier:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_Bad_ThenBy);
                    break;
                case ExpressionTree.ValidationError.ThenByWithoutSortByOrSelectBy:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_ThenBy_Without_SortBy_Or_SelectBy);
                    break;
                case ExpressionTree.ValidationError.BadModifier:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_Bad_Modifier);
                    break;
                default:
                    s = Localization.Get(UI_Key.Edit_Auto_Playlist_Invalid);
                    break;
            }
            return s;
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            /*
            btnCancel.Top = this.ClientRectangle.Height - MARGIN - btnOK.Height;
            btnCancel.Left = this.ClientRectangle.Width - btnCancel.Width - MARGIN;
            btnOK.Location = new Point(btnCancel.Left - btnOK.Width - MARGIN, btnCancel.Top);
            btnSaveAndConvert.Location = new Point(btnOK.Left - btnSaveAndConvert.Width - 5, btnCancel.Top);
            btnTest.Location = new Point(btnSaveAndConvert.Left - btnTest.Width - MARGIN, btnCancel.Top);
            btnHelp.Location = new Point(btnTest.Left - btnHelp.Width - MARGIN, btnCancel.Top);
            */

            PlaceButtons(this.ClientRectangle.Width,
                         this.ClientRectangle.Height - MARGIN - btnOK.Height,
                         btnCancel,
                         btnOK,
                         btnSaveAndConvert,
                         btnTest,
                         btnHelp);

            textbox.Size = new Size(this.ClientRectangle.Width - MARGIN - MARGIN, btnOK.Top - MARGIN - textbox.Top - lblOK.Height - MARGIN);

            lblOK.Location = new Point(MARGIN, textbox.Bottom + 5);
            lblMessage.Location = new Point(lblOK.Right + 4,
                                            lblOK.Top + (lblOK.Height - lblMessage.Height) / 2);
            lblMessage.Width = this.ClientRectangle.Width;
        }
        private void btnHelp_Click()
        {
            Net.BrowseTo(Lib.PRODUCT_URL + "/doc_auto_playlists.php");
        }
        protected override void ok()
        {
            save(false);
        }
        private void save(bool Convert)
        {
            textbox.CloseIntellisense();

            convert = Convert;

            if (!Valid)
            {
                switch (QMessageBox.Show(this,
                                     Localization.Get(UI_Key.Edit_Auto_Playlist_Invalid_Dialog),
                                     Localization.Get(UI_Key.Edit_Auto_Playlist_Invalid_Dialog_Title),
                                     QMessageBoxButtons.OKCancel,
                                     QMessageBoxIcon.Warning,
                                     QMessageBoxButton.NoCancel))
                {
                    case DialogResult.OK:
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                        break;
                    case DialogResult.Cancel:
                        break;
                }
            }
            else
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
        private void btnTest_Click()
        {
            textbox.CloseIntellisense();

            ExpressionTree et = new ExpressionTree(ExpressionTree.CleanExpression(textbox.ToString()));
            ExpressionTree.ValidationError ve = et.Compile();

            if (ve == ExpressionTree.ValidationError.Valid)
            {
                bool sorted;
                var v = et.Filter(Database.LibrarySnapshot, out sorted);
                
                string message;
                
                if (v.Count == 1)
                    message = String.Format("Playlist returns {0} track.", v.Count.ToString());
                else
                    message = String.Format("Playlist returns {0} tracks.", v.Count.ToString());

                QMessageBox.Show(this, message, "Auto Playlist Test", QMessageBoxIcon.Information);
            }
            else
            {
                QMessageBox.Show(this, getValidationErrorText(ve), "Auto Playlist Test Failed", QMessageBoxIcon.Information);
            }
        }
        protected override void cancel()
        {
            this.DialogResult = DialogResult.Cancel;
            textbox.CloseIntellisense();
            this.Close();
        }
    }
}