using System;
using System.Collections.Generic;
using System.Linq;
using Systematizer.Common;

namespace Systematizer.WPF
{
    class AgendaController : ListBlockController
    {
        public enum AgendaSize { TwoWeeks, Month, All }

        public AgendaVM VM { get; private set; }

        public override BaseBlockVM GenericVM => VM;

        AgendaSize Size = AgendaSize.TwoWeeks;
        long LastFocusedBoxId { get; set; } = -1;
        int _nDaysToOmit;
        int priorContentHash;

        public AgendaController(Action<BlockController> blockGotFocusHandler, Action<BlockController, bool> collapseRequested, int initialOmitDays)
            : base(blockGotFocusHandler, collapseRequested)
        {
            VM = new AgendaVM(VMGotFocus)
            {
                AllowRequestMore = true
            };

            //hook up VM events
            VM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "BoxVisibilityIncluded")
                {
                    RefreshDefinitely(true);
                }
            };
            VM.ItemGotFocus = idx =>
            {
                LastFocusedBoxId = VM.Rows[idx].AgendaEntry.Box.RowId;
            };
            VM.MoreRequested = () =>
            {
                if (Size == AgendaSize.TwoWeeks) Size = AgendaSize.Month;
                else if (Size == AgendaSize.Month) Size = AgendaSize.All;
                VM.AllowRequestMore = Size != AgendaSize.All;
                RefreshDefinitely(true);
            };
            VM.MouseOpenRequested = (eventSource, rowVM) =>
            {
                LastFocusedBoxId = rowVM.AgendaEntry.Box.RowId;
                HandleCommand(Globals.Commands.OPEN);
            };

            NDaysToOmit = initialOmitDays; //refreshes
        }

        /// <summary>
        /// The number of days to omit from the head of the agenda; used to hide today and tomorrow if those other views are open.
        /// Caller must call Refresh after this is set. Ignored if viewing low clutter.
        /// </summary>
        public int NDaysToOmit
        {
            get => _nDaysToOmit;
            set
            {
                if (value == _nDaysToOmit) return;
                _nDaysToOmit = value;
                RefreshDefinitely(true);
            }
        }

        public override bool ChangeMode(Mode mode, bool saveChanges)
        {
            return true;
        }

        public override bool HandleCommand(CommandCenter.Item command)
        {
            if (command == Globals.Commands.OPEN)
            {
                if (LastFocusedBoxId < 0) return false;
                var ebox = Globals.UI.LoadBoxForEditing(LastFocusedBoxId);
                if (ebox == null) { UIGlobals.Do.ShowTimedMessge("Cannot find task"); return true; }
                UIGlobals.Do.AddBoxToEditStack(ebox);
                return true;
            }
            return base.HandleCommand(command);
        }

        public override void Refresh(BoxEditingPool.Item changes)
        {
            if (changes != null && changes.IsAgendaChanged)
                RefreshDefinitely(false);
        }

        void RefreshDefinitely(bool overrideOptimization)
        { 
            //collect items from cache (includes items out of range of displayed dates)
            var agenda = Globals.BoxCache.GetAgenda();
            var agendaItems = agenda.Where(r => r.Box.Visibility >= VM.BoxVisibilityIncluded).ToArray();

            //exit now if no changes 
            int hash = HashContent(agendaItems);
            bool wasChanged = hash != priorContentHash;
            priorContentHash = hash;
            if (!wasChanged && !overrideOptimization) return;

            VM.Rows.Clear();

            //figure days to omit
            int effectiveNDaysToOmit = NDaysToOmit;
            if (VM.BoxVisibilityIncluded <= Constants.VISIBILITY_NORMAL) effectiveNDaysToOmit = 0;

            //figure date range to include
            DateTime startDt = DateTime.Today.AddDays(effectiveNDaysToOmit), endDt = DateTime.Today.AddMonths(1);
            if (Size == AgendaSize.TwoWeeks) endDt = DateTime.Today.AddDays(14);
            else if (Size == AgendaSize.All && agendaItems.Any())
                endDt = DateUtil.ToDateTime(agendaItems.Last().Time).Value.AddMonths(1); //month past last item
            string minBoxTime = DateUtil.ToYMDHM(startDt);
            string maxBoxTime = DateUtil.ToYMDHM(endDt.AddDays(1));

            //create a list of days and a same-typed list of agenda items
            var dayRowData = new List<(string, AgendaEntry)>();
            for (DateTime runDt = startDt; runDt <= endDt; runDt = runDt.AddDays(1))
                dayRowData.Add((DateUtil.ToYMDHM(runDt), null));
            var agendaRowData = agendaItems
                .Where(a => DateUtil.IsBefore(a.Time, maxBoxTime) && DateUtil.IsBefore(minBoxTime, a.Time))
                .Select(a => (a.Time, a));

            //combine day list and item list into one and build VM items
            var rowData = dayRowData.Union(agendaRowData).OrderBy(x => x.Item1).ToArray();
            var rowVMs = new List<AgendaVM.RowVM>();
            foreach (var d in rowData)
            {
                AgendaVM.RowVM row;
                if (d.Item2 != null)
                    row = new AgendaVM.RowVM(d.Item2);
                else
                    row = new AgendaVM.RowVM(d.Item1.Substring(0, 8), DateUtil.ToReadableDate(d.Item1, includeDOW: true));
                rowVMs.Add(row);
            }
            VM.Rows.AddRange(rowVMs);

            //set the highlight color for every row in between the start and end time of a highlighted agenda item;
            //note that we use the full agenda, not just the items selected for the VM, so that highlights starting
            //before the view's start date are included.
            foreach (var ae in agendaItems.Where(a => a.HighlightColor >= 0 && a.HighlightEndTime != null))
            {
                string tMin = ae.Time, tMax = ae.HighlightEndTime;
                foreach (var row in VM.Rows)
                    if (!DateUtil.IsBefore(row.Time, tMin) && DateUtil.IsBefore(row.Time, tMax))
                        row.VertHighlightColor[ae.HighlightColor] = UIGlobals.HIGHLIGHT_COLORS[ae.HighlightColor];
            }
        }

        /// <summary>
        /// loose hashing algorithm to detect changes in a set of displayable data
        /// </summary>
        static int HashContent(AgendaEntry[] agendaItems)
        {
            unchecked
            {
                int hash = 0;
                foreach (var a in agendaItems)
                {
                    hash += (int)a.Box.RowId + a.Box.Title.GetHashCode() + a.Box.BoxTime.GetHashCode()
                        + a.Box.Visibility.GetHashCode() + a.Box.TimeType.GetHashCode();
                    if (a.Box.Duration != null) hash += a.Box.Duration.GetHashCode();
                    if (a.Box.RepeatInfo != null) hash += a.Box.RepeatInfo.GetHashCode();
                }
                return hash;
            }
        }
    }
}
