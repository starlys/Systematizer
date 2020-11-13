using System;
using Systematizer.Common;

namespace Systematizer.WPF
{
    class CollapsedBlockController : BlockController
    {
        public Type OriginalControllerType { get; private set; }
        public ICollapseBlockData Data; //can be null
        DateTime ExpiresAtUtc; //after this time, should remove collapsed block from stack
        Action<CollapsedBlockController> ExpansionRequested;

        public override BaseBlockVM GenericVM => VM;

        public CollapsedBlockVM VM { get; private set; }

        /// <summary>
        /// If the given block is not collapsible, return null. Else return the collapsed block controller for it,
        /// which can be used to replace it in the owning stack
        /// </summary>
        public static CollapsedBlockController CreateControllerFor(BaseBlockVM vm, Action<BlockController> blockGotFocusHandler,
            Action<CollapsedBlockController> expansionRequested)
        {
            if (vm is ExtBoxVM vm1)
            {
                var box = vm1.Persistent.Box;
                if (box.IsUnclass != 0) return null; //cant collapse unclassified
                var data = new ExtBoxController.CollapseData { RowId = box.RowId };
                return new CollapsedBlockController(typeof(ExtBoxController), blockGotFocusHandler, expansionRequested, box.Title, data: data, true);
            }
            if (vm is ExtPersonVM vm2)
            {
                var person = vm2.Persistent.Person;
                var data = new ExtPersonController.CollapseData { RowId = person.RowId };
                return new CollapsedBlockController(typeof(ExtPersonController), blockGotFocusHandler, expansionRequested, person.Name, data: data, true);
            }
            if (vm is TodayVM vm3)
            {
                string title = vm3.IsToday ? "Today" : "Tomorrow";
                var data = new TodayController.CollapseData { IsToday = vm3.IsToday };
                return new CollapsedBlockController(typeof(TodayController), blockGotFocusHandler, expansionRequested, title, data, false);
            }
            if (vm is BoxSearchVM vm4)
            {
                var data = new BoxSearchController.CollapseData
                {
                    DoneMode = vm4.DoneMode,
                    DoneSinceCri = vm4.DoneSinceCri.Date,
                    IncludeDetailsCri = vm4.IncludeDetailsCri,
                    TermCri = vm4.TermCri
                };
                return new CollapsedBlockController(typeof(BoxSearchController), blockGotFocusHandler, expansionRequested, vm4.BlockTitle, data, !vm4.DoneMode);
            }
            if (vm is PersonSearchVM vm8)
            {
                var data = new PersonSearchController.CollapseData
                {
                    IncludeDetailsCri = vm8.IncludeDetailsCri,
                    TermCri = vm8.TermCri,
                    CatIdCri = vm8.CatIdCri
                };
                return new CollapsedBlockController(typeof(PersonSearchController), blockGotFocusHandler, expansionRequested, vm8.BlockTitle, data, true);
            }
            if (vm is AgendaVM vm5)
            {
                return new CollapsedBlockController(typeof(AgendaController), blockGotFocusHandler, expansionRequested, "Agenda", null, false);
            }
            if (vm is CalendarVM vm6)
            {
                return new CollapsedBlockController(typeof(CalendarController), blockGotFocusHandler, expansionRequested, "Calendar", null, false);
            }
            if (vm is SubjectVM vm7)
            {
                return new CollapsedBlockController(typeof(SubjectController), blockGotFocusHandler, expansionRequested, "Subjects", null, false);
            }

            return null;
        }

        /// <summary>
        /// Create controller for "Done" block which is initially collapsed
        /// </summary>
        public static CollapsedBlockController CreateControllerForDoneBlock(Action<BlockController> blockGotFocusHandler,
            Action<CollapsedBlockController> expansionRequested)
        {
            var data = new BoxSearchController.CollapseData
            {
                DoneMode = true,
                DoneSinceCri = DateUtil.ToYMD(DateTime.Today.AddDays(-14))
            };
            return new CollapsedBlockController(typeof(BoxSearchController), blockGotFocusHandler, expansionRequested, "Done", data, false);
        }

        CollapsedBlockController(Type origControllerType, Action<BlockController> blockGotFocusHandler,
            Action<CollapsedBlockController> expansionRequested,
            string collapseTitle, ICollapseBlockData data, bool autoRemoveOld)
            : base(blockGotFocusHandler, (c, _) => { })
        {
            OriginalControllerType = origControllerType;
            Data = data;
            ExpansionRequested = expansionRequested;
            VM = new CollapsedBlockVM(VMGotFocus)
            {
                Title = collapseTitle,
                ExpansionRequested = () => ExpansionRequested(this)
            };

            if (autoRemoveOld)
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10);
            else
                ExpiresAtUtc = DateTime.MaxValue;
        }

        public override bool ChangeMode(Mode mode, bool saveChanges)
        {
            //handled at stack level
            return true;
        }

        /// <summary>
        /// True if collapsed block should be removed
        /// </summary>
        public bool IsExpired()
        {
            return DateTime.UtcNow > ExpiresAtUtc;
        }

        /// <summary>
        /// Create the controller for reinflating the collapsed block; or return null if not possible
        /// </summary>
        /// <param name="collapseRequested">The handler that will be used for the re-expanded block</param>
        public BlockController Reinflate(Action<BlockController> blockGotFocusHandler, Action<BlockController, bool> collapseRequested)
        {
            if (OriginalControllerType == typeof(ExtBoxController))
            {
                var inflateData = Data as ExtBoxController.CollapseData;
                var c = new ExtBoxController(inflateData.RowId, blockGotFocusHandler, collapseRequested, true);
                return c;
            }
            if (OriginalControllerType == typeof(ExtPersonController))
            {
                var inflateData = Data as ExtPersonController.CollapseData;
                var c = new ExtPersonController(inflateData.RowId, blockGotFocusHandler, collapseRequested, false);
                return c;
            }
            if (OriginalControllerType == typeof(TodayController))
            {
                var inflateData = Data as TodayController.CollapseData;
                var c = new TodayController(inflateData.IsToday, blockGotFocusHandler, collapseRequested);
                return c;
            }
            if (OriginalControllerType == typeof(BoxSearchController))
            {
                var inflateData = Data as BoxSearchController.CollapseData;
                var c = new BoxSearchController(blockGotFocusHandler, collapseRequested, inflateData.DoneMode);
                c.VM.TermCri = inflateData.TermCri;
                c.VM.DoneSinceCri.Date = inflateData.DoneSinceCri;
                c.VM.IncludeDetailsCri = inflateData.IncludeDetailsCri;
                c.SearchRequested();
                return c;
            }
            if (OriginalControllerType == typeof(PersonSearchController))
            {
                var inflateData = Data as PersonSearchController.CollapseData;
                var c = new PersonSearchController(blockGotFocusHandler, collapseRequested);
                c.VM.TermCri = inflateData.TermCri;
                c.VM.IncludeDetailsCri = inflateData.IncludeDetailsCri;
                c.VM.CatIdCri = inflateData.CatIdCri;
                c.SearchRequested();
                return c;
            }
            if (OriginalControllerType == typeof(AgendaController))
            {
                var c = new AgendaController(blockGotFocusHandler, collapseRequested, 15); //the 15 gets fixed by caller
                return c;
            }
            if (OriginalControllerType == typeof(CalendarController))
            {
                var c = new CalendarController(blockGotFocusHandler, collapseRequested); 
                return c;
            }
            if (OriginalControllerType == typeof(SubjectController))
            {
                var c = new SubjectController(blockGotFocusHandler, collapseRequested);
                return c;
            }

            return null;
        }
    }
}
