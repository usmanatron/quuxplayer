/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using System.Windows.Automation.Provider;
using System.Windows.Automation;

namespace QuuxPlayer.Automation
{
    public class TrackListTrackProvider : IRawElementProviderFragment, ISelectionItemProvider
    {
        private TrackListProvider containingProvider;
        private Track trackData;
        internal TrackListTrackProvider(TrackListProvider ContainingProvider, Track Track)
        {
            this.containingProvider = ContainingProvider;
            trackData = Track;
        }
        public IRawElementProviderSimple[] GetEmbeddedFragmentRoots()
        {
            return null;
        }
        public int Index
        {
            get
            {
                if (!containingProvider.TrackList.Queue.Contains(trackData))
                    return -1;

                return containingProvider.TrackList.Queue.IndexOf(trackData);
            }
        }
        public int[] GetRuntimeId()
        {
            return new int[] { AutomationInteropProvider.AppendRuntimeId, trackData.ID };
        }
        public IRawElementProviderFragment Navigate(NavigateDirection direction)
        {
            if (direction == NavigateDirection.Parent)
            {
                return containingProvider;
            }
            else if (direction == NavigateDirection.NextSibling)
            {
                if (containingProvider.TrackList.Queue.Contains(trackData))
                    return containingProvider.GetProviderForIndex(containingProvider.TrackList.Queue.IndexOf(trackData) + 1);
            }
            else if (direction == NavigateDirection.PreviousSibling)
            {
                if (containingProvider.TrackList.Queue.Contains(trackData))
                    return containingProvider.GetProviderForIndex(containingProvider.TrackList.Queue.IndexOf(trackData) - 1);
            }
            return null;
        }
        public void SetFocus()
        {
            Select();
            //containingProvider.TrackList.s.Find(trackData);
        }
        public IRawElementProviderFragmentRoot FragmentRoot
        {
            get { return containingProvider; }
        }
        public object GetPatternProvider(int PatternId)
        {
            if (PatternId == SelectionItemPatternIdentifiers.Pattern.Id)
            {
                return this;
            }
            return null;
        }
        public object GetPropertyValue(int PropertyID)
        {
            if (trackData.Deleted)
            {
                throw new ElementNotAvailableException();
            }
            if (PropertyID == AutomationElementIdentifiers.NameProperty.Id)
            {
                return trackData.Title + " " + trackData.Artist;
            }
            else if (PropertyID == AutomationElementIdentifiers.ControlTypeProperty.Id)
            {
                return ControlType.ListItem.Id;
            }
            else if (PropertyID == AutomationElementIdentifiers.AutomationIdProperty.Id)
            {
                return trackData.ID.ToString();
            }
            else if (PropertyID == AutomationElementIdentifiers.HasKeyboardFocusProperty.Id)
            {
                return trackData.Selected;
            }
            else if (PropertyID == AutomationElementIdentifiers.ItemStatusProperty.Id)
            {
                if (trackData.ConfirmExists)
                {
                    return "Available";
                }
                else
                {
                    return "Not Available";
                }
            }
            else if (PropertyID == AutomationElementIdentifiers.IsEnabledProperty.Id)
            {
                return true;
            }
            else if (PropertyID == AutomationElementIdentifiers.IsKeyboardFocusableProperty.Id)
            {
                return true;
            }
            else if (PropertyID == AutomationElementIdentifiers.FrameworkIdProperty.Id)
            {
                return "Custom";
            }
            return null;
        }
        public IRawElementProviderSimple HostRawElementProvider
        {
            get
            {
                // Because the element is not directly hosted in a window, null is returned.
                return null;
            }
        }
        public ProviderOptions ProviderOptions
        {
            get { return ProviderOptions.ServerSideProvider; }
        }
        public IRawElementProviderSimple SelectionContainer
        {
            get { return containingProvider; }
        }
        public System.Windows.Rect BoundingRectangle
        {
            get
            {
                Rectangle r = containingProvider.TrackList.GetBoundingRectangle(trackData);
                return new System.Windows.Rect(r.X,
                                               r.Y,
                                               r.Width,
                                               r.Height);
            }
        }
        public void AddToSelection()
        {
            trackData.Selected = true;
            containingProvider.TrackList.Invalidate();
        }
        public void Select()
        {
            if (containingProvider.TrackList.Queue.Contains(trackData))
                containingProvider.TrackList.SelectTrack(containingProvider.TrackList.Queue.IndexOf(trackData),
                                                         true,
                                                         true,
                                                         true);
        }
        public bool IsSelected
        {
            get { return trackData.Selected; }
        }
        public void RemoveFromSelection()
        {
            trackData.Selected = false;
            containingProvider.TrackList.Invalidate();
        }
    }
}
