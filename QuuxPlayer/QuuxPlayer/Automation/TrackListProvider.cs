/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Windows.Automation.Provider;
using System.Windows.Automation;

namespace QuuxPlayer.Automation
{
    public class TrackListProvider : IRawElementProviderFragmentRoot, ISelectionProvider
    {

        private TrackList trackList;
        private IntPtr trackListHandle;
        private Dictionary<Track, IRawElementProviderFragment> trackProviders;
        public TrackListProvider(TrackList TrackList)
        {
            trackList = TrackList;
            trackListHandle = trackList.Handle;
            trackProviders = new Dictionary<Track, IRawElementProviderFragment>();
        }
        internal TrackList TrackList { get { return trackList; } }
        object IRawElementProviderSimple.GetPatternProvider(int patternId)
        {
            if (patternId == SelectionPatternIdentifiers.Pattern.Id)
            {
                return this;
            }
            return null;
        }
        object IRawElementProviderSimple.GetPropertyValue(int PropertyId)
        {
            if (PropertyId == AutomationElementIdentifiers.ControlTypeProperty.Id)
                return ControlType.List.Id;
            else if (PropertyId == AutomationElementIdentifiers.IsKeyboardFocusableProperty.Id)
                return true;
            else if (PropertyId == AutomationElementIdentifiers.FrameworkIdProperty.Id)
                return "Custom";
            else
                return null;
        }
        System.Windows.Rect IRawElementProviderFragment.BoundingRectangle
        {
            get { return System.Windows.Rect.Empty; }
        }
        IRawElementProviderFragmentRoot IRawElementProviderFragment.FragmentRoot
        {
            get { return this; }
        }
        IRawElementProviderSimple[] IRawElementProviderFragment.GetEmbeddedFragmentRoots()
        {
            return null;
        }
        int[] IRawElementProviderFragment.GetRuntimeId()
        {
            return null;
        }
        public IRawElementProviderFragment GetProviderForIndex(int index)
        {
            if (index < 0 || index >= trackList.Count)
                return null;

            return GetProvider(trackList[index]);
        }
        private IRawElementProviderFragment GetProvider(Track Track)
        {
            if (!trackProviders.ContainsKey(Track))
                trackProviders.Add(Track, new TrackListTrackProvider(this, Track));

            return trackProviders[Track];
        }
        IRawElementProviderFragment IRawElementProviderFragmentRoot.ElementProviderFromPoint(double x, double y)
        {
            int index = -1;
            System.Drawing.Point clientPoint = new System.Drawing.Point((int)x, (int)y);

            // Invoke control method on separate thread to avoid clashing with UI.
            // Use anonymous method for simplicity.
            this.trackList.Invoke(new MethodInvoker(delegate()
            {
                clientPoint = this.trackList.PointToClient(clientPoint);
            }));

            index = trackList.ItemIndexFromPoint(clientPoint);

            if (index == -1)
            {
                return null;
            }
            return GetProviderForIndex(index);
        }
        IRawElementProviderFragment IRawElementProviderFragmentRoot.GetFocus()
        {
            return GetProviderForIndex(trackList.FirstSelectedIndex);
        }
        IRawElementProviderFragment IRawElementProviderFragment.Navigate(NavigateDirection direction)
        {
            if (direction == NavigateDirection.FirstChild)
            {
                return GetProviderForIndex(0);
            }
            else if (direction == NavigateDirection.LastChild)
            {
                return GetProviderForIndex(trackList.Count - 1);
            }
            return null;
        }
        ProviderOptions IRawElementProviderSimple.ProviderOptions
        {
            get { return ProviderOptions.ServerSideProvider; }
        }
        IRawElementProviderSimple IRawElementProviderSimple.HostRawElementProvider
        {
            get { return AutomationInteropProvider.HostProviderFromHandle(trackListHandle); }
        }
        bool ISelectionProvider.CanSelectMultiple
        {
            get { return true; }
        }
        IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        {
            List<IRawElementProviderSimple> items = new List<IRawElementProviderSimple>();
            foreach (Track t in trackList.SelectedTracks)
                items.Add(GetProvider(t));

            return items.ToArray();
        }
        bool ISelectionProvider.IsSelectionRequired
        {
            get { return false; }
        }
        void IRawElementProviderFragment.SetFocus()
        {
            throw new Exception("The method is not implemented.");
        }
    }
}
