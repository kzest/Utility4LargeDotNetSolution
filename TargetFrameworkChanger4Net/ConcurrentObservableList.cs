using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace TargetFrameworkChanger4Net
{
    public class ConcurrentObservableList<T> : IList<T>, INotifyCollectionChanged, IDisposable
    {
        private delegate void _action();

        private List<T> _list = new List<T>();

        private object syncObject = new object();

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void RaiseCollectionChanged(NotifyCollectionChangedAction changeType, T oldItem, T newItem, int oldIndex, int newIndex)
        {
            if (this.CollectionChanged != null)
            {
                try
                {
                    switch (changeType)
                    {
                        case NotifyCollectionChangedAction.Reset:
                            if (Application.Current != null)
                                Application.Current.Dispatcher.BeginInvoke(new _action(() => CollectionChanged(this, new NotifyCollectionChangedEventArgs(changeType))));
                            else
                                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(changeType));
                            break;

                        case NotifyCollectionChangedAction.Add:
                            if (Application.Current != null)
                                Application.Current.Dispatcher.BeginInvoke(new _action(() => CollectionChanged(this, new NotifyCollectionChangedEventArgs(changeType, newItem))));
                            else
                                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(changeType, newItem));
                            break;

                        case NotifyCollectionChangedAction.Remove:
                            if (Application.Current != null)
                                Application.Current.Dispatcher.BeginInvoke(new _action(() => CollectionChanged(this, new NotifyCollectionChangedEventArgs(changeType, newItem, oldIndex))));
                            else
                                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(changeType, newItem, oldIndex));
                            break;

                        case NotifyCollectionChangedAction.Replace:
                            if (Application.Current != null)
                                Application.Current.Dispatcher.BeginInvoke(new _action(() => CollectionChanged(this, new NotifyCollectionChangedEventArgs(changeType, newItem, oldItem, oldIndex))));
                            else
                                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(changeType, newItem, oldItem, oldIndex));
                            break;

                        case NotifyCollectionChangedAction.Move:
                            if (Application.Current != null)
                                Application.Current.Dispatcher.BeginInvoke(new _action(() => CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, newItem, newIndex, oldIndex))));
                            else
                                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, newItem, newIndex, oldIndex));
                            break;
                    }
                }
                catch (Exception exceptio)
                {
                    throw;
                }
            }
        }

        private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (this.CollectionChanged != null)
            {
                if (Application.Current != null)
                    Application.Current.Dispatcher.BeginInvoke(new _action(() => this.CollectionChanged(this, e)));
                else
                    this.CollectionChanged(this, e);
            }
        }

        #endregion

        #region IList<T> Members

        public int IndexOf(T item)
        {
            int i = -1;

            lock (syncObject)
            {
                i = _list.IndexOf(item);
            }

            return i;
        }

        public void Insert(int index, T item)
        {
            bool changed = false;
            lock (syncObject)
            {
                if (!_isReadOnly)
                {
                    _list.Insert(index, item);
                    changed = true;
                }
            }

            if (changed)
            {
                RaisePropertyChanged("Count");
                RaiseCollectionChanged(NotifyCollectionChangedAction.Add, item, item, 0, 0);
            }
        }

        public void RemoveAt(int index)
        {
            bool changed = false;
            T oldItem = (T)Activator.CreateInstance(typeof(T), new object[] { 0, string.Empty });

            T[] arrT = new T[_list.Count];

            lock (syncObject)
            {
                if (!_isReadOnly)
                {
                    oldItem = _list[index];

                    _list.RemoveAt(index);
                    changed = true;
                }
            }

            if (changed)
            {
                RaisePropertyChanged("Count");
                RaiseCollectionChanged(NotifyCollectionChangedAction.Remove, oldItem, oldItem, index, index);
            }
        }

        public T this[int index]
        {
            get
            {
                T newItem;

                lock (syncObject)
                {
                    newItem = _list.ElementAt<T>(index);
                }

                return newItem;
            }
            set
            {
                bool isChanged = false;
                T oldItem = _list.ElementAt<T>(index);

                lock (syncObject)
                {
                    if (oldItem.Equals(value) == false && _isReadOnly == false)
                    {
                        _list[index] = value;
                        isChanged = true;
                    }
                }

                if (isChanged)
                    RaiseCollectionChanged(NotifyCollectionChangedAction.Replace, oldItem, value, index, 0);
            }
        }

        #endregion

        #region ICollection<T> Members

        public void Add(T item)
        {
            bool isChanged = false;

            lock (syncObject)
            {
                if (!_isReadOnly)
                {
                    _list.Add(item);
                    isChanged = true;
                }
            }

            if (isChanged)
            {
                RaisePropertyChanged("Count");
                RaiseCollectionChanged(NotifyCollectionChangedAction.Add, item, item, 0, 0);
            }
        }

        public void Clear()
        {
            bool isChanged = false;

            lock (syncObject)
            {
                if (!_isReadOnly)
                {
                    _list.Clear();
                    isChanged = true;
                }
            }

            if (isChanged)
            {
                RaisePropertyChanged("Count");
                RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public bool Contains(T item)
        {
            bool contains = false;

            lock (syncObject)
            {
                contains = _list.Contains<T>(item);
            }

            return contains;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            bool isChanged = false;

            lock (syncObject)
            {
                if (!_isReadOnly)
                {
                    _list.CopyTo(array, arrayIndex);
                    isChanged = true;
                }
            }

            T item = (T)Activator.CreateInstance(typeof(T), new object[] { 0, string.Empty });

            if (isChanged)
                RaiseCollectionChanged(NotifyCollectionChangedAction.Reset, item, item, 0, 0);
        }

        public int Count
        {
            get
            {
                int i = 0;

                lock (syncObject)
                {
                    i = _list.Count;
                }

                return i;
            }
        }

        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get { return _isReadOnly; }
        }

        public bool Remove(T item)
        {
            bool changed = false;
            int index = 0;

            lock (syncObject)
            {
                if (!_isReadOnly)
                {
                    index = _list.IndexOf(item);
                    changed = _list.Remove(item);
                }
            }

            if (changed)
            {
                RaisePropertyChanged("Count");
                RaiseCollectionChanged(NotifyCollectionChangedAction.Remove, item, item, index, index);
            }

            return changed;
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Range Operations for better performance

        public List<T> GetRange(int index, int count)
        {
            List<T> range = new List<T>();

            lock (syncObject)
            {
                range = _list.GetRange(index, count);
            }

            return range;
        }

        public void AddRange(IEnumerable<T> range)
        {
            bool isChanged = false;

            lock (syncObject)
            {
                if (!_isReadOnly)
                {
                    _list.AddRange(range);
                    isChanged = true;
                }
            }

            if (isChanged)
            {
                RaisePropertyChanged("Count");
                RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>(range)));
            }
        }

        public void InsertRange(int index, IEnumerable<T> range)
        {
            bool isChanged = false;

            lock (syncObject)
            {
                if (!_isReadOnly)
                {
                    _list.InsertRange(index, range);
                    isChanged = true;
                }
            }

            if (isChanged)
            {
                RaisePropertyChanged("Count");
                RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>(range), index));
            }
        }

        public void RemoveRange(int index, int count)
        {
            bool isChanged = false;
            IEnumerable<T> range = new T[0];

            lock (syncObject)
            {
                if (!_isReadOnly)
                {
                    range = _list.GetRange(index, count);

                    _list.RemoveRange(index, count);
                    isChanged = true;
                }
            }

            if (isChanged)
            {
                RaisePropertyChanged("Count");
                RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<T>(range), index));
            }
        }

        #endregion

        public bool Replace(T oldItem, T newItem)
        {
            bool changed = false;
            bool result = false;
            int index = 0;

            lock (syncObject)
            {
                if (!_isReadOnly)
                {
                    index = _list.IndexOf(oldItem);
                    if (index != -1)
                    {
                        _list[index] = newItem;
                        result = true;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                RaiseCollectionChanged(NotifyCollectionChangedAction.Replace, oldItem, newItem, index, index);
            }

            return result;
        }

        public void Sort(System.Comparison<T> comparison)
        {
            bool isChanged = false;

            lock (syncObject)
            {
                if (!_isReadOnly)
                {
                    _list.Sort(comparison);
                    isChanged = true;
                }
            }

            if (isChanged)
            {
                RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            this.CollectionChanged = null;
            this._list.Clear();
        }

        #endregion
    }
}
