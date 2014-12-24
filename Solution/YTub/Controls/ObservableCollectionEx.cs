using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace YTub.Controls
{
    public sealed class ObservableCollectionEx<T> : ObservableCollection<T> where T : INotifyPropertyChanged
    {
        public ObservableCollectionEx()
        {
            CollectionChanged += TrulyObservableCollection_CollectionChanged;
        }

        void TrulyObservableCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Object item in e.NewItems)
                {
                    var notifyPropertyChanged = item as INotifyPropertyChanged;
                    if (notifyPropertyChanged != null)
                        notifyPropertyChanged.PropertyChanged += item_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (Object item in e.OldItems)
                {
                    var notifyPropertyChanged = item as INotifyPropertyChanged;
                    if (notifyPropertyChanged != null)
                        notifyPropertyChanged.PropertyChanged -= item_PropertyChanged;
                }
            }
        }

        void item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var a = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            OnCollectionChanged(a);
        }
    }
}
