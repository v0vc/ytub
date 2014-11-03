﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YTub.Common
{
    public sealed class TrulyObservableCollection<T> : ObservableCollection<T> where T : INotifyPropertyChanged
    {
        public TrulyObservableCollection()
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
