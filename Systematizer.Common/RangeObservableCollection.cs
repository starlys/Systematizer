using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Systematizer.Common
{
    /// <summary>
    /// ObservableCollection with an efficient AddRange method, which does not fire notifications on each addition.
    /// Note that Clear() also suppresses notifications via override of ClearItems
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        bool _suppressNotification = false;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
                base.OnCollectionChanged(e);
        }

        public void AddRange(IEnumerable<T> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            _suppressNotification = true;

            foreach (T item in list)
            {
                Add(item);
            }
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Override that suppresses notifications during clearing; not sure if this is needed
        /// </summary>
        protected override void ClearItems()
        {
            _suppressNotification = true;
            base.ClearItems();
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
