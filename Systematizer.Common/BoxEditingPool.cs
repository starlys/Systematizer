using System;
using System.Collections.Generic;
using System.Linq;
using Systematizer.Common.PersistentModel;

namespace Systematizer.Common
{
    /// <summary>
    /// Storage for boxes that are being edited, for the purpose of knowing what changed on save, allowing for efficient
    /// screen refreshes
    /// </summary>
    public class BoxEditingPool
    {
        public class Item
        {
            //note the initial values are set here to ensure that a new box will always show changes

            public long BoxId;
            public string OldTitle, NewTitle;
            public string OldDuration, NewDuration;
            public string OldBoxTime, NewBoxTime;
            public string OldRepeatInfo, NewRepeatInfo;
            public short OldTimeType = -1, NewTimeType;
            public short OldVisibility = -1, NewVisibility;
            public string OldDoneDate, NewDoneDate;
            public long? OldParentId, NewParentId;

            public bool IsAgendaChanged => OldBoxTime != NewBoxTime || OldTimeType != NewTimeType || OldVisibility != NewVisibility
                || OldDuration != NewDuration
                || OldDoneDate != NewDoneDate || OldRepeatInfo != NewRepeatInfo || IsTitleChanged;

            bool InvolvesSubjects => OldTimeType == Constants.TIMETYPE_NONE || NewTimeType == Constants.TIMETYPE_NONE; //this is only true if is root
            bool InvolvesRoots => OldParentId == 0 || NewParentId == 0 || OldParentId == null || NewParentId == null;
            bool InvolvesRootSubjects => InvolvesRoots && InvolvesSubjects;
            
            public bool IsTitleChanged => OldTitle != NewTitle;

            /// <summary>
            /// True if box has a different parent
            /// </summary>
            public bool IsParentageChanged => OldParentId != NewParentId;

            /// <summary>
            /// True if the subject tree roots list is changed
            /// </summary>
            public bool IsRootSubjectsChanged => InvolvesRootSubjects && (IsTitleChanged || IsParentageChanged);
        }

        /// <summary>
        /// Boxes currently being edited, with the old values filled in
        /// </summary>
        List<Item> OpenItems = new List<Item>();

        /// <summary>
        /// Create Item that forces all changes, using mock data
        /// </summary>
        /// <returns></returns>
        public static Item CreateUniversalChangeItem()
        {
            return new Item
            {
                OldBoxTime = "x",
                NewBoxTime = "y",
                OldTimeType = Constants.TIMETYPE_DAY,
                NewTimeType = Constants.TIMETYPE_NONE,
                OldTitle = "x",
                NewTitle = "y",
                NewParentId = 1
            };
        }

        /// <summary>
        /// Record the "before" conditions of the box; call this when UI is starting the edits
        /// </summary>
        /// <param name="box"></param>
        internal void CheckOut(Box box)
        {
            Abandon(box.RowId); //ensure no double entries
            OpenItems.Add(new Item
            {
                BoxId = box.RowId,
                OldBoxTime = box.BoxTime,
                OldDuration = box.Duration,
                OldParentId = box.ParentId,
                OldTimeType = box.TimeType,
                OldTitle = box.Title,
                OldVisibility = box.Visibility,
                OldDoneDate = box.DoneDate,
                OldRepeatInfo = box.RepeatInfo
            });
        }

        /// <summary>
        /// Record the "after" conditions of the box, and return information about what changed.
        /// After this call, this instance forgets about the box.
        /// </summary>
        /// <returns>null if not found</returns>
        internal Item CheckIn(Box box)
        {
            var item = OpenItems.FirstOrDefault(b => b.BoxId == box.RowId);
            if (item == null) item = new Item(); //happens when this is a new box
            item.NewBoxTime = box.BoxTime;
            item.NewDuration = box.Duration;
            item.NewParentId = box.ParentId;
            item.NewTimeType = box.TimeType;
            item.NewTitle = box.Title;
            item.NewVisibility = box.Visibility;
            item.NewDoneDate = box.DoneDate;
            item.NewRepeatInfo = box.RepeatInfo;
            return item;
        }

        /// <summary>
        /// Abandon memory of the given box; call this when closing a box without saving
        /// </summary>
        internal void Abandon(long boxId)
        {
            int idx = OpenItems.FindIndex(b => b.BoxId == boxId);
            if (idx >= 0) OpenItems.RemoveAt(idx);
        }
    }
}
