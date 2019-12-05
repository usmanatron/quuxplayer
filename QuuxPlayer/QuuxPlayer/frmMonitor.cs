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
    internal sealed partial class frmMonitor : QFixedDialog
    {
        private QLabel lblInstructions;
        private QButton btnAdd;
        private QButton btnRemove;
        private QListBox lstDirectories;
        private bool initialized;

        public frmMonitor() : base(Localization.Get(UI_Key.Auto_Monitor_Title), ButtonCreateType.OKAndCancel)
        {
            initialized = false;

            this.ClientSize = new System.Drawing.Size(590, 300);

            lblInstructions = new QLabel(Localization.Get(UI_Key.Auto_Monitor_Instructions, Application.ProductName));
            lblInstructions.Visible = true;
            
            this.Controls.Add(lblInstructions);

            lstDirectories = new QListBox(true);
            this.Controls.Add(lstDirectories);

            btnAdd = new QButton(Localization.Get(UI_Key.Auto_Monitor_Add_Folder), false, false);
            AddButton(btnAdd, add);

            btnRemove = new QButton(Localization.Get(UI_Key.Auto_Monitor_Remove_Folder), false, false);
            AddButton(btnRemove, remove);

            initialized = true;

            lstDirectories.TabStop = false;
            int tabIndex = 0;
            btnAdd.TabIndex = tabIndex++;
            btnRemove.TabIndex = tabIndex++;
            btnOK.TabIndex = tabIndex++;
            btnCancel.TabIndex = tabIndex++;

            arrangeControls();
        }

        public List<string> Directories
        {
            get
            {
                List<string> res = new List<string>();
                
                for (int i = 0; i < lstDirectories.Count; i++)
                    res.Add(lstDirectories[i]);
                
                return res;
            }
            set
            {
                value.Sort();
                lstDirectories.Clear();
                foreach (string s in value)
                {
                    lstDirectories.Add(s);
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            arrangeControls();
        }

        private void add()
        {
            string s = Lib.GetUserSelectedFolder(Localization.Get(UI_Key.Auto_Monitor_Select_Folder, Application.ProductName), String.Empty, false);

            if (!String.IsNullOrEmpty(s))
            {
                lstDirectories.Add(s);
            }
        }
        private void remove()
        {
            lstDirectories.RemoveSelected();
        }
        private void arrangeControls()
        {
            if (initialized)
            {
                lblInstructions.Location = new Point(MARGIN, MARGIN);
                lblInstructions.SetWidth(this.ClientRectangle.Width - MARGIN - MARGIN);

                //btnCancel.Location = new Point(this.ClientRectangle.Width - MARGIN - btnCancel.Width,
                  //                           this.ClientRectangle.Height - MARGIN - btnCancel.Height);

                //btnOK.Location = new Point(btnCancel.Left - MARGIN - btnOK.Width, btnCancel.Top);
                //btnRemove.Location = new Point(btnOK.Left - MARGIN - btnRemove.Width, btnCancel.Top);
                //btnAdd.Location = new Point(btnRemove.Left - MARGIN - btnAdd.Width, btnCancel.Top);

                PlaceButtons(this.ClientRectangle.Width,
                             this.ClientRectangle.Height - MARGIN - btnCancel.Height,
                             btnCancel,
                             btnOK,
                             btnRemove,
                             btnAdd);

                lstDirectories.Bounds = new Rectangle(MARGIN, lblInstructions.Bottom + MARGIN, this.ClientRectangle.Width - MARGIN - MARGIN, btnCancel.Top - MARGIN - lblInstructions.Bottom - MARGIN);
            }
        }
    }
}
