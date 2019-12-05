/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal enum PodcastDownloadSchedule { FifteenMinutes, HalfHour, Hourly, EveryTime, Manual, Count };
    internal enum PodcastDownloadDisposition { DownloadAll, DownloadLatest, DownloadNone, Count };

    internal class PodcastManager : Control, IMainView, IActionHandler
    {
        private const int MARGIN = 7;
        private const int SPACING = 7;

        private QListView<PodcastSubscription> lvwSubscriptions;
        private QListView<PodcastEpisode> lvwEpisodes;
        private QTextBoxFocusOnClick txtURL;
        private QButton btnGo;
        private QButton btnDone;
        private QButton btnStopDownloads;
        private QButton btnSetDownloadFolder;
        private QButton btnAutoManage;
        private QButton btnRefreshAll;
        private QLabel lblSubscriptions;
        private QLabel lblEpisodes;
        private QPodcastDetails pnlSubscriptionDetails;
        private QPodcastAutoManageOptions pnlAutoManageOptions;
        private bool locked = false;
        private Controller controller = null;
        private static PodcastManager instance;
        private PodcastSubscription pendingPodcastSubscription = null;
        private ulong updateTimer = Clock.NULL_ALARM;
        private PodcastDownloadSchedule lastDownloadschedule;

        public PodcastManager()
        {
            this.BackColor = Color.Black;
            instance = this;

            List<string> subHeadings = new List<string>() { "Podcast Name", "Genre", "Last Download", "Count", "Status", "Reload", "Get All", "Edit", "Remove" };
            List<QListView<PodcastSubscription>.ClickDelegate> subActions = new List<QListView<PodcastSubscription>.ClickDelegate>() { checkForNewEpisodes,
                                                                                                                                       downloadSubscription,
                                                                                                                                       showSubscriptionEditPanel,
                                                                                                                                       removeSubscription };
            lvwSubscriptions = new QListView<PodcastSubscription>(subHeadings,
                                                                  subActions,
                                                                  new string[] { String.Empty, "GenreXXXXXX", "Last DownloadXX", "XXXX / XXXX", "XXXX Available", "Reload", "Get All", "Edit", "Remove" },
                                                                  PodcastSubscription.Compare);

            lvwSubscriptions.ContextMenuHook += new QListView<PodcastSubscription>.ContextMenuHookDelegate(lvwSubscriptions_ContextMenuHook);
            lvwSubscriptions.Sort((int)(PodcastSubscription.Columns.Name), true);
            this.Controls.Add(lvwSubscriptions);

            List<string> epHeadings = new List<string>() { "Title", "Description", "Episode Date", "Status", "Duration", "Download", "Play", "Remove" };
            lvwEpisodes = new QListView<PodcastEpisode>(epHeadings,
                                                        new List<QListView<PodcastEpisode>.ClickDelegate>() { downloadEpisode, playEpisode, removeEpisode },
                                                        new string[] { string.Empty, String.Empty,"Episode DateXX", "Downloading XXX%", "DurationXX", "Download", "Play", "Remove" },
                                                        PodcastEpisode.Compare);

            lvwEpisodes.ContextMenuHook += new QListView<PodcastEpisode>.ContextMenuHookDelegate(lvwEpisodes_ContextMenuHook);
            lvwEpisodes.Sort((int)(PodcastEpisode.Columns.Date), false);
            this.Controls.Add(lvwEpisodes);

            btnDone = new QButton("Done", false, false);
            btnDone.BackColor = this.BackColor;
            this.Controls.Add(btnDone);
            btnDone.ButtonPressed += (s) => { controller.RequestAction(QActionType.AdvanceScreen); };

            btnStopDownloads = new QButton("Stop Downloads", false, false);
            btnStopDownloads.BackColor = this.BackColor;
            this.Controls.Add(btnStopDownloads);
            btnStopDownloads.Enabled = false;
            btnStopDownloads.ButtonPressed += (s) => { if (!Locked) stopDownloads(); };

            btnSetDownloadFolder = new QButton("Set Download Folder...", false, false);
            btnSetDownloadFolder.BackColor = this.BackColor;
            this.Controls.Add(btnSetDownloadFolder);
            btnSetDownloadFolder.ButtonPressed += (s) => { if (!Locked) setDownloadFolder(); };

            btnRefreshAll = new QButton("Refresh All Subscriptions", false, false);
            btnRefreshAll.BackColor = this.BackColor;
            this.Controls.Add(btnRefreshAll);
            btnRefreshAll.ButtonPressed += (s) => { if (!Locked) Clock.DoOnNewThread(autoRefreshSubscriptions); };

            btnAutoManage = new QButton("Auto Manage Options...", false, false);
            btnAutoManage.BackColor = this.BackColor;
            this.Controls.Add(btnAutoManage);
            btnAutoManage.ButtonPressed += (s) => { if (!Locked) showAutoManageOptions(); };

            txtURL = new QTextBoxFocusOnClick();
            this.Controls.Add(txtURL);
            txtURL.MaxLength = 2048;
            txtURL.Enter += (s, e) => { keyPreviewChange(); };
            txtURL.Leave += (s, e) => { keyPreviewChange(); };
            txtURL.KeyPress += (s, e) =>
            {
                if (!Locked)
                {
                    switch (e.KeyChar)
                    {
                        case '\r':
                            if (txtURL.Text.Length > 0)
                                btnGetInfo_ButtonPressed(btnGo);
                            e.Handled = true;
                            break;
                    }
                }
            };

            txtURL.EnableWatermark(this, "[Type or drag a podcast feed here.]", String.Empty);

            btnGo = new QButton("Add Podcast", false, true);
            btnGo.ButtonPressed += new QButton.ButtonDelegate(btnGetInfo_ButtonPressed);
            btnGo.BackColor = this.BackColor;
            this.Controls.Add(btnGo);

            lblSubscriptions = new QLabel("Subscriptions", Styles.FontBold);
            this.Controls.Add(lblSubscriptions);

            lblEpisodes = new QLabel("Episodes", Styles.FontBold);
            this.Controls.Add(lblEpisodes);

            txtURL.DragEnter += (s, e) => { onDragEnter(e); };
            txtURL.DragDrop += (s, e) => { onDragDrop(e); };
            txtURL.AllowDrop = true;
            txtURL.Watermark.DragEnter += (s, e) => { onDragEnter(e); };
            txtURL.Watermark.DragDrop += (s, e) => { onDragDrop(e); };
            txtURL.Watermark.AllowDrop = true;

            lvwSubscriptions.ClickCallback += new QListView<PodcastSubscription>.ClickDelegate(populateEpisodes);
            lvwSubscriptions.DoubleClickCallback += new QListView<PodcastSubscription>.ClickDelegate(lvwSubscriptions_DoubleClickCallback);
            lvwSubscriptions.DragEnter += (s, e) => { onDragEnter(e); };
            lvwSubscriptions.DragDrop += (s, e) => { onDragDrop(e); };
            lvwSubscriptions.AllowDrop = true;

            lvwEpisodes.DoubleClickCallback += new QListView<PodcastEpisode>.ClickDelegate(lvwEpisodes_DoubleClickCallback);
            lvwEpisodes.ClickCallback += new QListView<PodcastEpisode>.ClickDelegate(lvwEpisodes_ClickCallback);
        }

        private void lvwSubscriptions_DoubleClickCallback(PodcastSubscription SelectedItem)
        {
            checkForNewEpisodes(SelectedItem);
        }

        public static List<PodcastSubscription> Subscriptions
        {
            get
            {
                return instance.lvwSubscriptions.Items;
            }
            set
            {
                instance.lvwSubscriptions.Items = value;
                foreach (PodcastSubscription PS in value)
                    setupSubscriptionDataChangedCallback(PS);
                if (value.Count > 0)
                {
                    instance.lvwSubscriptions.SelectFirstItem();
                    instance.populateEpisodes(instance.lvwSubscriptions.SelectedItem);
                    instance.lvwEpisodes.SelectFirstItem();
                }
            }
        }

        public bool DownloadInProgress
        {
            get { return lvwSubscriptions.HasItem(s => s.DownloadInProgress); }
        }
        public bool AllowTagEditing
        {
            get { return lvwSubscriptions.SelectedItem != null; }
        }
        public ActionHandlerType Type
        { get { return ActionHandlerType.Podcast; } }
        public ViewType ViewType
        { get { return ViewType.Podcast; } }
        public bool Locked
        {
            get { return locked; }
            set
            {
                if (this.locked != value)
                {
                    this.locked = value;
                    lvwEpisodes.Enabled = !this.locked;
                    lvwSubscriptions.Enabled = !this.locked;
                    btnAutoManage.Enabled = !this.locked;
                    btnRefreshAll.Enabled = !this.locked;
                    btnGo.Enabled = !this.locked;
                    btnStopDownloads.Enabled = !this.locked;
                    btnSetDownloadFolder.Enabled = !this.locked;
                    btnDone.Enabled = !this.locked;
                }
            }
        }
        public Controller Controller
        {
            set { controller = value; }
        }
        public bool KeyPreview
        {
            get { return this.Visible && pnlSubscriptionDetails != null; }
        }

        public static void InvalidateAll()
        {
            Clock.DoOnMainThread(instance.invalidateAll);
        }

        public void RequestAction(QActionType Type)
        {
            switch (Type)
            {
                case QActionType.Play:
                case QActionType.PlaySelectedTracks:
                case QActionType.PlayThisAlbum:
                    if (lvwEpisodes.SelectedItem != null && lvwEpisodes.SelectedItem.Playable)
                        controller.Play(lvwEpisodes.SelectedItem);
                    break;
                case QActionType.MoveTracksDown:
                case QActionType.PageDown:
                    pageDown();
                    break;
                case QActionType.MoveDown:
                case QActionType.SelectNextItemGamePadRight:
                    moveDown();
                    break;
                case QActionType.MoveTracksUp:
                case QActionType.PageUp:
                    pageUp();
                    break;
                case QActionType.MoveUp:
                case QActionType.SelectPreviousItemGamePadRight:
                    moveUp();
                    break;
                case QActionType.Home:
                    home();
                    break;
                case QActionType.End:
                    end();
                    break;
                case QActionType.MoveLeft:
                    break;
                case QActionType.MoveRight:
                    break;
                case QActionType.FocusSearchBox:
                    break;
                case QActionType.ExportPlaylist:
                    break;
                case QActionType.FindPlayingTrack:
                    findPlayingTrack();
                    break;
                case QActionType.EditTags:
                    showSubscriptionEditPanel(lvwSubscriptions.SelectedItem);
                    break;
                default:
                    controller.RequestActionNoRedirect(Type);
                    break;
            }
        }
        private void findPlayingTrack()
        {
            List<PodcastSubscription> pps = lvwSubscriptions.CopyOfItems;
            foreach (PodcastSubscription ps in pps)
            {
                List<PodcastEpisode> ppe = ps.Episodes.ToList();
                foreach (PodcastEpisode pe in ppe)
                {
                    if (pe.Track != null && pe.Track == controller.PlayingTrack)
                    {
                        lvwSubscriptions.SelectedItem = ps;
                        lvwSubscriptions.EnsureSelectedItemVisible();
                        populateEpisodes(ps);
                        lvwEpisodes.SelectedItem = pe;
                        lvwEpisodes.EnsureSelectedItemVisible();
                        return;
                    }
                }
            }
        }
        public void RequestAction(QAction Action)
        {
            RequestAction(Action.Type);
        }
        public void StartRefreshSubscriptionInfo()
        {
            if (Setting.PodcastDownloadSchedule != PodcastDownloadSchedule.Manual)
                autoRefreshSubscriptions();
            else if (updateTimer != Clock.NULL_ALARM)
                Clock.RemoveAlarm(ref updateTimer);
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.DrawImageUnscaled(Styles.BitmapRSS, MARGIN, MARGIN);
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            arrangeControls();
        }

        private static void setupSubscriptionDataChangedCallback(PodcastSubscription PS)
        {
            PS.DataChanged += (s) =>
            {
                instance.lvwSubscriptions.InvalidateThreadSafe();
                if (PS == instance.lvwSubscriptions.SelectedItem)
                    instance.lvwEpisodes.InvalidateThreadSafe();
                Clock.DoOnMainThread(instance.updateStopDownloadButtonEnable);
            };
        }

        private void showAutoManageOptions()
        {
            if (!this.Locked)
            {
                lastDownloadschedule = Setting.PodcastDownloadSchedule;
                pnlAutoManageOptions = new QPodcastAutoManageOptions(removePanel);
                this.Controls.Add(pnlAutoManageOptions);
                Clock.DoOnMainThread(arrangeControls);
                keyPreviewChange();
                pnlAutoManageOptions.Focus();
            }
        }
        private void checkForNewEpisodes(PodcastSubscription PS)
        {
            Controller.ShowMessage("Checking for podcast updates...");
            Clock.DoOnNewThread(PS.UpdateEpisodeInfo);
        }
        private void downloadSubscription(PodcastSubscription PS)
        {
            if (!this.Locked)
            {
                Clock.DoOnNewThread(PS.DownloadThisSubscription);
            }
        }
        private void autoRefreshSubscriptions()
        {
            List<PodcastSubscription> pss = lvwSubscriptions.Items.ToList();
            foreach (PodcastSubscription ps in pss)
            {
                ps.UpdateEpisodeInfo();
                switch (Setting.PodcastDownloadDisposition)
                {
                    case PodcastDownloadDisposition.DownloadAll:
                        ps.DownloadNewEpisodes();
                        break;
                    case PodcastDownloadDisposition.DownloadLatest:
                        ps.DownloadLatestEpisode();
                        break;
                    case PodcastDownloadDisposition.DownloadNone:
                        break;
                }
            }
            populateEpisodes(lvwSubscriptions.SelectedItem);
            updateNextRefreshCallback();
        }
        private void updateNextRefreshCallback()
        {
            switch (Setting.PodcastDownloadSchedule)
            {
                case PodcastDownloadSchedule.Manual:
                case PodcastDownloadSchedule.EveryTime:
                    if (updateTimer != Clock.NULL_ALARM)
                        Clock.RemoveAlarm(ref updateTimer);
                    break;
                case PodcastDownloadSchedule.FifteenMinutes:
                    Clock.Update(ref updateTimer, autoRefreshSubscriptions, 1000 * 60 * 15, true);
                    break;
                case PodcastDownloadSchedule.HalfHour:
                    Clock.Update(ref updateTimer, autoRefreshSubscriptions, 1000 * 60 * 30, true);
                    break;
                case PodcastDownloadSchedule.Hourly:
                    Clock.Update(ref updateTimer, autoRefreshSubscriptions, 1000 * 60 * 60, true);
                    break;
            }
        }
        private void playEpisode(PodcastEpisode PE)
        {
            controller.Play(PE);
        }
        private void downloadEpisode(PodcastEpisode PE)
        {
            if (PE.IsQueuedOrDownloading)
                PodcastSubscription.CancelDownload(PE);
            else
                Clock.DoOnNewThread(PE.QueueForDownload);
        }

        private void removeSubscription(PodcastSubscription PS)
        {
            if (!Locked)
            {
                List<frmTaskDialog.Option> options = new List<frmTaskDialog.Option>();

                options.Add(new frmTaskDialog.Option("Remove Entire Podcast", "Remove the subscription and delete all downloaded tracks from my library.", 1));
                options.Add(new frmTaskDialog.Option("Unsubscribe Only", "Remove the subscription but don't delete any downloaded tracks.", 2));
                options.Add(new frmTaskDialog.Option("Cancel", "Leave the podcast subscription in place.", 0));

                frmTaskDialog od = new frmTaskDialog("Remove Podcast Subscription",
                                                     "Choose options for removing a podcast:",
                                                     options);
                od.ShowDialog(this);

                switch (od.ResultIndex)
                {
                    case 1:
                        removeSubscriptionFromListView(PS);
                        PS.DeleteSubscriptionFiles(InvalidateAll);
                        break;
                    case 2:
                        removeSubscriptionFromListView(PS);
                        PS.Close(InvalidateAll);
                        break;
                    default:
                        break;
                }
            }
        }
        private void invalidateAll()
        {
            Invalidate(true);
        }
        private void lvwEpisodes_DoubleClickCallback(PodcastEpisode SelectedItem)
        {
            if (!Locked)
            {
                if (SelectedItem.Playable)
                {
                    Controller.GetInstance().Play(SelectedItem);
                }
                else
                {
                    PodcastSubscription.Download(SelectedItem);
                    lvwEpisodes.Invalidate();
                }
            }
        }
        private void btnGetInfo_ButtonPressed(object sender)
        {
            if (!Locked)
                Clock.DoOnNewThread(go);
        }
        private void updateStopDownloadButtonEnable()
        {
            List<PodcastSubscription> ps = lvwSubscriptions.Items.ToList();
            btnStopDownloads.Enabled = ps.Exists(p => p.HasQueuedOrDownloadingEpisodes);
        }
        private void onDragEnter(DragEventArgs drgevent)
        {
            base.OnDragEnter(drgevent);

            if (!Locked &&
                drgevent.Data.GetDataPresent(DataFormats.Text) &&
                drgevent.Data.GetData(DataFormats.Text).ToString().StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                drgevent.Effect = DragDropEffects.Copy | DragDropEffects.Link;
            }
            else
            {
                drgevent.Effect = DragDropEffects.None;
            }
        }
        private void onDragDrop(DragEventArgs drgevent)
        {
            base.OnDragDrop(drgevent);
            string url;
            if (
            (drgevent.Data.GetDataPresent(DataFormats.Text) &&
             (url = drgevent.Data.GetData(DataFormats.Text).ToString()).StartsWith("http", StringComparison.OrdinalIgnoreCase)))
            {
                txtURL.Text = url;
                Clock.DoOnNewThread(go);
            }
        }
        private void go()
        {
            if (lvwSubscriptions.HasItem(s => String.Compare(s.URL, txtURL.Text.Trim(), StringComparison.OrdinalIgnoreCase) == 0))
            {
                Controller.ShowPriorityMessage("Podcast subscription already loaded.");
            }
            else
            {
                Controller.ShowPriorityMessage("Downloading podcast information...");

                PodcastSubscription p = new PodcastSubscription(txtURL.Text.Trim());

                if (p.Loaded)
                {
                    pendingPodcastSubscription = p;
                    Clock.DoOnMainThread(addPendingSubscription);
                    Controller.ShowPriorityMessage("Podcast information downloaded.");
                }
                else
                {
                    Controller.ShowPriorityMessage("Podcast audio information not found.");
                }
            }
        }
        private void lvwEpisodes_ClickCallback(PodcastEpisode SelectedItem)
        {
            System.Diagnostics.Debug.WriteLine(SelectedItem.ToString());
        }
        private void lvwEpisodes_ContextMenuHook(ContextMenuStrip ContextMenu, PodcastEpisode Item)
        {
            ToolStripMenuItem tsi = new ToolStripMenuItem("Play");
            tsi.Click += (s, ee) => { playEpisode(Item); };
            tsi.Enabled = Item.Playable;
            ContextMenu.Items.Add(tsi);

            tsi = new ToolStripMenuItem("&Find in Library");
            tsi.Click += (s, ee) =>
            {
                controller.ShowTrack(Item.Track, true);
            };
            tsi.Enabled = Item.Track != null;
            ContextMenu.Items.Add(tsi);

            ContextMenu.Items.Add(new ToolStripSeparator());

            tsi = new ToolStripMenuItem("Mark As");

            ToolStripMenuItem tsi2 = new ToolStripMenuItem("Unplayed");
            tsi2.Click += (s, ee) => { Item.SetDownloadStatus(PodcastDownloadStatus.Unplayed); };
            tsi2.Enabled = (Item.DownloadStatus == PodcastDownloadStatus.Played);
            tsi.DropDownItems.Add(tsi2);

            tsi2 = new ToolStripMenuItem("Played");
            tsi2.Click += (s, ee) => { Item.SetDownloadStatus(PodcastDownloadStatus.Played); };
            tsi2.Enabled = (Item.DownloadStatus == PodcastDownloadStatus.Unplayed);
            tsi.DropDownItems.Add(tsi2);

            ContextMenu.Items.Add(tsi);
        }
        private void lvwSubscriptions_ContextMenuHook(ContextMenuStrip ContextMenu, PodcastSubscription PS)
        {
            ToolStripMenuItem tsi = new ToolStripMenuItem("&Restore Deleted Episode Entries");
            tsi.Click += (s, ee) =>
            {
                List<PodcastEpisode> ppe = PS.Episodes.ToList();

                foreach (PodcastEpisode pe in ppe)
                {
                    if (pe.IsDeleted)
                        if (pe.Track == null)
                            pe.SetDownloadStatus(PodcastDownloadStatus.NotDownloaded);
                        else
                            pe.SetDownloadStatus(PodcastDownloadStatus.Unplayed);

                }
                if (lvwSubscriptions.SelectedItem == PS)
                    populateEpisodes(PS);
            };
            tsi.Enabled = PS.HasDeletedEpisodes;
            ContextMenu.Items.Add(tsi);

            tsi = new ToolStripMenuItem("Check for New Episodes");
            tsi.Click += (s, ee) =>
            {
                checkForNewEpisodes(PS);
                //PS.UpdateEpisodeInfo();
                //if (lvwSubscriptions.SelectedItem == PS)
                  //  populateEpisodes(PS);
            };
            ContextMenu.Items.Add(tsi);

            ContextMenu.Items.Add(new ToolStripSeparator());

            tsi = new ToolStripMenuItem("Visit Podcast Web Site...");
            tsi.Click += (s, ee) =>
            {
                Net.BrowseTo(PS.ReferenceURL);
            };
            tsi.Enabled = PS.ReferenceURL.Length > 0;
            ContextMenu.Items.Add(tsi);
        }
        private void pageDown()
        {
            if (lvwEpisodes.Focused)
                lvwEpisodes.PageDown();
            else
                lvwSubscriptions.PageDown();
        }
        private void pageUp()
        {
            if (lvwEpisodes.Focused)
                lvwEpisodes.PageUp();
            else
                lvwSubscriptions.PageUp();
        }
        private void moveDown()
        {
            if (lvwEpisodes.Focused)
                lvwEpisodes.MoveDown();
            else
                lvwSubscriptions.MoveDown();
        }
        private void moveUp()
        {
            if (lvwEpisodes.Focused)
                lvwEpisodes.MoveUp();
            else
                lvwSubscriptions.MoveUp();
        }
        private void home()
        {
            if (lvwEpisodes.Focused)
                lvwEpisodes.Home();
            else
                lvwSubscriptions.Home();
        }
        private void end()
        {
            if (lvwEpisodes.Focused)
                lvwEpisodes.End();
            else
                lvwSubscriptions.End();
        }
        private void addPendingSubscription()
        {
            PodcastSubscription p = pendingPodcastSubscription;
            pendingPodcastSubscription = null;
            lvwSubscriptions.AddItem(p, true, true);
            lvwSubscriptions.SelectedItem = p;
            lvwSubscriptions.EnsureSelectedItemVisible();
            setupSubscriptionDataChangedCallback(p);
            populateEpisodes(p);
        }
        private void showSubscriptionEditPanel(PodcastSubscription PS)
        {
            if (!this.Locked)
            {
                if (PS != null)
                {
                    this.Locked = true;

                    pnlSubscriptionDetails = new QPodcastDetails(PS, removePanel);
                    this.Controls.Add(pnlSubscriptionDetails);
                    Clock.DoOnMainThread(arrangeControls);
                    keyPreviewChange();
                    pnlSubscriptionDetails.Focus();
                }
            }
        }
        private void populateEpisodes(PodcastSubscription p)
        {
            if (p != null)
            {
                txtURL.Text = p.URL;

                p.PopulateAndSortWithNewEpisodes(lvwEpisodes);
                InvalidateAll();
            }
        }
        private void removeEpisode(PodcastEpisode PE)
        {
            List<frmTaskDialog.Option> options = new List<frmTaskDialog.Option>();

            if (PE.Playable || PE.IsDownloading) // only ask if there's already some data
            {
                options.Add(new frmTaskDialog.Option("Remove Podcast Entry", "Remove this episode from the episode list but leave the audio file in your library.", 1));
                options.Add(new frmTaskDialog.Option("Remove All", "Remove this episode from the list and delete the audio file.", 2));
                options.Add(new frmTaskDialog.Option("Cancel", "Don't remove this episode.", 0));

                frmTaskDialog od = new frmTaskDialog("Remove Podcast Episode",
                                                           "Choose an option for removing a podcast episode:",
                                                           options);

                od.ShowDialog(this);

                switch (od.ResultIndex)
                {
                    case 0:
                        return;
                    case 1:
                        break;
                    case 2:
                        if (PE.Track != null)
                        {
                            Database.RemoveFromLibrary(new List<Track>() { PE.Track });
                            TrackWriter.AddToDeleteList(PE.Track.FilePath);
                            TrackWriter.DeleteItems();
                            PE.Track = null;
                        }
                        break;
                }
            }
            PE.SetDownloadStatus(PodcastDownloadStatus.Deleted);
            lvwEpisodes.RemoveItem(PE);
        }
        private void removeSubscriptionFromListView(PodcastSubscription PS)
        {
            if (PS == lvwSubscriptions.SelectedItem)
            {
                lvwEpisodes.Clear();
            }
            lvwSubscriptions.RemoveItem(PS);
        }
        private void removePanel()
        {
            if (pnlAutoManageOptions != null)
            {
                if (Setting.PodcastDownloadSchedule != lastDownloadschedule)
                {
                    updateNextRefreshCallback();
                }
            }

            QPanel panel = pnlSubscriptionDetails;
            pnlSubscriptionDetails = null;

            if (panel == null)
            {
                panel = pnlAutoManageOptions;
                pnlAutoManageOptions = null;
            }

            if (panel != null)
            {
                this.Controls.Remove(panel);
                panel.Dispose();
            }
            arrangeControls();
            this.Locked = false;
            keyPreviewChange();
        }
        private void keyPreviewChange()
        {
            controller.RequestAction(QActionType.KeyPreviewChange);
        }
        private void arrangeControls()
        {
            Rectangle r;

            QPanel panel = pnlSubscriptionDetails;
            if (panel == null)
                panel = pnlAutoManageOptions;
            if (panel != null)
            {
                panel.Bounds = new Rectangle(0,
                                             this.ClientRectangle.Height - panel.Height,
                                             this.ClientRectangle.Width,
                                             panel.Height);

                r = new Rectangle(Point.Empty, new Size(panel.Width, panel.Top));
            }
            else
            {
                r = this.ClientRectangle;
            }

            btnGo.Location = new Point(r.Width - btnGo.Width - MARGIN,
                                       MARGIN);

            txtURL.Bounds = new Rectangle(MARGIN + Styles.BitmapRSS.Width + SPACING, MARGIN, btnGo.Left - MARGIN - MARGIN - Styles.BitmapRSS.Width - SPACING, txtURL.Height);

            lblSubscriptions.Location = new Point(MARGIN, txtURL.Bottom + SPACING);

            lvwSubscriptions.Bounds = new Rectangle(MARGIN,
                                                    lblSubscriptions.Bottom + SPACING,
                                                    r.Width - MARGIN - MARGIN,
                                                    r.Height / 3);

            lblEpisodes.Location = new Point(MARGIN, lvwSubscriptions.Bottom + SPACING);

            btnDone.Location = new Point(r.Width - btnDone.Width - MARGIN,
                                         r.Height - btnDone.Height - MARGIN);

            btnStopDownloads.Location = new Point(btnDone.Left - btnStopDownloads.Width - SPACING, btnDone.Top);

            btnSetDownloadFolder.Location = new Point(btnStopDownloads.Left - btnSetDownloadFolder.Width - SPACING, btnDone.Top);

            btnRefreshAll.Location = new Point(btnSetDownloadFolder.Left - btnRefreshAll.Width - SPACING, btnDone.Top);

            btnAutoManage.Location = new Point(btnRefreshAll.Left - btnAutoManage.Width - SPACING, btnDone.Top);

            lvwEpisodes.Bounds = new Rectangle(MARGIN,
                                               lblEpisodes.Bottom + SPACING,
                                               lvwSubscriptions.Width,
                                               btnDone.Top - lblEpisodes.Bottom - SPACING - SPACING);
        }
        private void setDownloadFolder()
        {
            string dir = Lib.GetUserSelectedFolder("Choose a folder into which QuuxPlayer will download new podcasts:", Setting.PodcastDownloadDirectory, true);

            if (!String.IsNullOrEmpty(dir))
            {
                Setting.PodcastDownloadDirectory = dir;
            }
        }
        private void stopDownloads()
        {
            PodcastSubscription.StopDownloads();
            InvalidateAll();
            Controller.ShowMessage("Podcast downloads stopped.");
        }

        private class QPodcastAutoManageOptions : QPanel
        {
            private QLabel lblHeading;
            private QLabel lblCheckFreq;
            private QComboBox cboRefreshSchedule;
            private QLabel lblDownloadDisposition;
            private QComboBox cboDownloadDisposition;
            private Callback doneCallback;

            public QPodcastAutoManageOptions(Callback DoneCallback) : base()
            {
                doneCallback = DoneCallback;

                lblHeading = new QLabel("Podcast Automanagement Options", Styles.FontBold);
                this.Controls.Add(lblHeading);

                lblCheckFreq = new QLabel("Check for new Episodes");
                this.Controls.Add(lblCheckFreq);

                cboRefreshSchedule = new QComboBox(false);
                cboRefreshSchedule.Items.AddRange(new string[] { "Every 15 Minutes", "Every Half Hour", "Every Hour", "Every Time QuuxPlayer Starts", "Manual Only" });
                cboRefreshSchedule.AutoSetWidth();
                cboRefreshSchedule.SelectedIndex = (int)Setting.PodcastDownloadSchedule;
                this.Controls.Add(cboRefreshSchedule);

                lblDownloadDisposition = new QLabel("When new episodes are found");
                this.Controls.Add(lblDownloadDisposition);

                cboDownloadDisposition = new QComboBox(false);
                cboDownloadDisposition.Items.AddRange(new string[] { "Download All New Episodes", "Download Latest Episode", "Don't Download" });
                cboDownloadDisposition.AutoSetWidth();
                cboDownloadDisposition.SelectedIndex = (int)Setting.PodcastDownloadDisposition;
                this.Controls.Add(cboDownloadDisposition);

                System.Diagnostics.Debug.Assert(cboRefreshSchedule.Items.Count == (int)PodcastDownloadSchedule.Count);
                System.Diagnostics.Debug.Assert(cboDownloadDisposition.Items.Count == (int)PodcastDownloadDisposition.Count);

                /*
                spnMaxConcurrentDownloads = new QSpin(false, false, "Maximum Simultaneous Downloads:", String.Empty, 1, 10, 1, 1, this.BackColor);
                this.Controls.Add(spnMaxConcurrentDownloads);
                */

                btnOK = new QButton(Localization.OK, false, false);
                AddButton(btnOK, ok);

                btnCancel = new QButton(Localization.CANCEL, false, false);
                AddButton(btnCancel, cancel);

                //btnOK.Width = btnCancel.Width = (Math.Max(btnOK.Width, btnCancel.Width));

                int tabIndex = 0;
                lblCheckFreq.TabIndex = tabIndex++;
                cboRefreshSchedule.TabIndex = tabIndex++;
                lblDownloadDisposition.TabIndex = tabIndex++;
                cboDownloadDisposition.TabIndex = tabIndex++;
                btnOK.TabIndex = tabIndex++;
                btnCancel.TabIndex = tabIndex++;

                setWrapAroundTabControl(tabIndex, cboRefreshSchedule, null);

                arrangeControls();

                //this.Height = spnMaxConcurrentDownloads.Bottom + MARGIN;
                this.Height = cboDownloadDisposition.Bottom + MARGIN;
            }
            
            protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
            {
                switch (keyData)
                {
                    case Keys.Enter:
                        ok();
                        return true;
                    case Keys.Escape:
                        cancel();
                        return true;
                    default:
                        return base.ProcessCmdKey(ref msg, keyData);
                }
            }
            protected override void OnGotFocus(EventArgs e)
            {
                base.OnGotFocus(e);
                cboRefreshSchedule.Focus();
            }
            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                arrangeControls();
            }
            protected override void ok()
            {
                Setting.PodcastDownloadSchedule = (PodcastDownloadSchedule)cboRefreshSchedule.SelectedIndex;
                Setting.PodcastDownloadDisposition = (PodcastDownloadDisposition)cboDownloadDisposition.SelectedIndex;
                doneCallback();
            }
            protected override void cancel()
            {
                doneCallback();
            }

            private void arrangeControls()
            {
                lblHeading.Location = new Point(MARGIN, MARGIN);

                int left = Math.Max(lblCheckFreq.Right, lblDownloadDisposition.Right) + SPACING;

                lblCheckFreq.Location = new Point(MARGIN, lblHeading.Bottom + SPACING);
                cboRefreshSchedule.Location = new Point(left, lblCheckFreq.Top + (lblCheckFreq.Height - cboRefreshSchedule.Height) / 2);

                lblDownloadDisposition.Location = new Point(MARGIN, cboRefreshSchedule.Bottom + SPACING);
                cboDownloadDisposition.Location = new Point(left, lblDownloadDisposition.Top + (lblDownloadDisposition.Height - cboDownloadDisposition.Height) / 2);

                btnCancel.Location = new Point(this.ClientRectangle.Width - btnCancel.Width - MARGIN, this.ClientRectangle.Height - btnCancel.Height - MARGIN);
                btnOK.Location = new Point(btnCancel.Left - btnOK.Width - SPACING, btnCancel.Top);
            }
        }
        private class QPodcastDetails : QPanel
        {
            private QTextBox txtTitle;
            private QTextBox txtURL;
            private QLabel lblTitle;
            private QLabel lblURL;
            private QLabel lblGenre;
            private QComboBox cboGenre;
            private PodcastSubscription ps;

            private int buttonWidth;
            private Callback doneCallback;

            public QPodcastDetails(PodcastSubscription Subscription, Callback Done)
                : base()
            {
                this.ps = Subscription;
                this.doneCallback = Done;

                lblTitle = new QLabel("Title");
                lblURL = new QLabel("URL");
                lblGenre = new QLabel("Mark New Episodes with Genre");
                txtTitle = new QTextBox();
                txtTitle.Width = 1000; // prevent scrolling when text set
                txtTitle.Text = ps.Name;
                txtURL = new QTextBox();
                txtURL.Text = ps.URL;

                btnOK = new QButton("Save", false, false);
                btnCancel = new QButton("Cancel", false, false);

                AddButton(btnOK, ok);
                AddButton(btnCancel, cancel);

                cboGenre = new QComboBox(true);
                List<string> genres = Database.GetGenres();
                if (!genres.Contains(ps.DefaultGenre, StringComparer.OrdinalIgnoreCase))
                    genres.Add(ps.DefaultGenre);
                genres.Sort();
                cboGenre.Items.AddRange(genres.ToArray());
                cboGenre.SelectedIndex = genres.FindIndex(g => String.Compare(g, ps.DefaultGenre, StringComparison.OrdinalIgnoreCase) == 0);

                this.Controls.Add(lblTitle);
                this.Controls.Add(lblURL);
                this.Controls.Add(txtTitle);
                this.Controls.Add(txtURL);
                this.Controls.Add(lblGenre);
                this.Controls.Add(cboGenre);
                
                buttonWidth = Math.Max(btnOK.Width, btnCancel.Width);

                btnOK.Width = buttonWidth;
                btnCancel.Width = buttonWidth;

                this.Height = calcHeight();

                int tabIndex = 0;
                lblTitle.TabIndex = tabIndex++;
                txtTitle.TabIndex = tabIndex++;
                lblURL.TabIndex = tabIndex++;
                txtURL.TabIndex = tabIndex++;
                lblGenre.TabIndex = tabIndex++;
                cboGenre.TabIndex = tabIndex++;
                btnOK.TabIndex = tabIndex++;
                btnCancel.TabIndex = tabIndex++;

                setWrapAroundTabControl(tabIndex, txtTitle, null);
            }
            protected override void ok()
            {
                ps.Name = txtTitle.Text.Trim();
                ps.URL = txtURL.Text.Trim();
                ps.DefaultGenre = cboGenre.Text.Trim();
                
                doneCallback();
            }
            protected override void cancel()
            {
                doneCallback();
            }
            protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
            {
                switch (keyData)
                {
                    case Keys.Enter:
                        ok();
                        return true;
                    case Keys.Escape:
                        cancel();
                        return true;
                    default:
                        return base.ProcessCmdKey(ref msg, keyData);
                }
            }
            protected override void OnGotFocus(EventArgs e)
            {
                base.OnGotFocus(e);
                txtTitle.Focus();
            }
            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                arrangeControls();
                
                // weird bug where i can't prevent cboGenre from text being selected
                if (!cboGenre.Focused)
                    cboGenre.SelectionLength = 0;
            }
            private void arrangeControls()
            {
                btnOK.Location = new Point(this.ClientRectangle.Width - buttonWidth - MARGIN, MARGIN);
                btnCancel.Location = new Point(btnOK.Left, btnOK.Bottom + SPACING);

                lblTitle.Location = new Point(MARGIN, MARGIN);
                int left = Math.Max(lblGenre.Right, Math.Max(lblTitle.Right, lblURL.Right)) + SPACING;
                int width = btnOK.Left - left - SPACING;

                txtTitle.Bounds = new Rectangle(left, lblTitle.Top + (lblTitle.Height - txtTitle.Height) / 2, width, txtTitle.Height);
                lblURL.Location = new Point(MARGIN, txtTitle.Bottom + SPACING);
                txtURL.Bounds = new Rectangle(txtTitle.Left, lblURL.Top + (lblURL.Height - txtURL.Height) / 2, width, txtURL.Height);

                lblGenre.Location = new Point(MARGIN, lblURL.Bottom + SPACING);
                cboGenre.Location = new Point(txtURL.Left, lblGenre.Top + (lblGenre.Height - cboGenre.Height) / 2);
            }

            private int calcHeight()
            {
                return MARGIN + txtTitle.Height + SPACING + txtURL.Height + SPACING + cboGenre.Height + MARGIN;
            }
        }
    }
}
