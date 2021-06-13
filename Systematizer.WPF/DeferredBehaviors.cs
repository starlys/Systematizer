using System;
using System.Collections.Generic;
using System.Linq;
using Systematizer.Common.PersistentModel;

namespace Systematizer.WPF
{
    /// <summary>
    /// Container for deferred behaviors that require multiple parts of the code to collaborate
    /// </summary>
    class DeferredBehaviors
    {
        public class TaskAssignBehavior
        {
            public Box Box;
            public string ChunkTitle; //if nonnull, this is the chunk that the box should be saved under
        }

        public class NewBoxBehavior
        {
            /// <summary>
            /// Flag set when creating new box to link it to this parent; cleared when new command completed
            /// </summary>
            public long? ParentId;

            /// <summary>
            /// Flag set when creating a new box from a person; cleared by the box controller when it opens (so this is only set for a moment
            /// and isn't written to the database until after the box is actually saved)
            /// </summary>
            public long? LinkedPersonId;
        }

        public class OpenBoxBehavior
        {
            /// <summary>
            /// Flag set prior to a box opening; when the box gets opened, and this flag is set, it should mark the task not-done
            /// </summary>
            public bool MakeUndone;
        }

        public class NewPersonBehavior
        {
            /// <summary>
            /// Flag set prior to person creating, identifying the box to link it to after saving
            /// </summary>
            public long? LinkedBoxId;
        }

        /// <summary>
        /// List of deferred chunk assignments: these are added when the today view creates a task, and then used after the task is saved
        /// and when the today view is rebuilding with that task in it
        /// </summary>
        public List<TaskAssignBehavior> TaskAssigns = new();

        /// <summary>
        /// Caller can create or clear this with the instruction for the box controller. The command handler that allows cancelation must clear this
        /// so that it is not accidentally used for the next new box.
        /// </summary>
        public NewBoxBehavior OnNewBox;

        /// <summary>
        /// Caller can create or clear this with the instruction for the box controller. 
        /// </summary>
        public OpenBoxBehavior OnOpenBox;

        /// <summary>
        /// Caller can create or clear this with the instruction for the person controller.
        /// </summary>
        public NewPersonBehavior OnNewPerson;

        public TaskAssignBehavior GetAndRemoveTaskAssign(long rowId)
        {
            var beh = TaskAssigns.FirstOrDefault(b => b.Box.RowId == rowId);
            if (beh == null) return null;
            TaskAssigns.Remove(beh);
            return beh;
        }
    }
}
