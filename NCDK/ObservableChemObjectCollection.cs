﻿// Copyright (C) 2016-2017  Kazuya Ujihara
// This file is under LGPL-2.1 

using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK
{
    [Serializable]
    internal class ObservableChemObjectCollection<T>
        : IList<T>
        where T : IChemObject
    {
        List<T> list;

        protected IChemObjectListener Listener { get; private set; }
        public bool AllowDuplicate { get; set; } = true;

        public ObservableChemObjectCollection(int size, IChemObjectListener listener)
        {
            Listener = listener;
            list = new List<T>(size);
        }

        public ObservableChemObjectCollection(IChemObjectListener listener)
        {
            Listener = listener;
            list = new List<T>();
        }

        public ObservableChemObjectCollection(IChemObjectListener listener, IEnumerable<T> objects)
        {
            Listener = listener;
            list = new List<T>(objects);
        }

        public T this[int index]
        {
            get { return list[index]; }
            set
            {
                if (!AllowDuplicate)
                    if (list.Contains(value))
                        throw new ArgumentException();
                if (Listener != null)
                {
                    list[index]?.Listeners?.Remove(Listener);
                    value?.Listeners?.Add(Listener);
                }
                list[index] = value;
            }
        }

        public int Count => list.Count;
        public bool IsReadOnly => false;

        public virtual void Add(T item)
        {
            Insert(list.Count, item);
        }

        public void Clear()
        {
            if (Listener != null)
                foreach (T item in list)
                    item?.Listeners?.Remove(Listener);
            list.Clear();
            Listener?.OnStateChanged(new ChemObjectChangeEventArgs(this));
        }

        public bool Contains(T item) => list.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
        public int IndexOf(T item) => list.IndexOf(item);

        public void Insert(int index, T item)
        {
            if (!AllowDuplicate)
                if (list.Contains(item))
                    return;
            if (Listener != null)
                item?.Listeners?.Add(Listener);
            list.Insert(index, item);
            Listener?.OnStateChanged(new ChemObjectChangeEventArgs(this));
        }

        public bool Remove(T item)
        {
            var index = list.IndexOf(item);
            if (index != -1)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            var old = list[index];
            list.RemoveAt(index);
            old?.Listeners?.Remove(Listener);
            Listener?.OnStateChanged(new ChemObjectChangeEventArgs(this));
        }

        internal void RemoveOnly(T item)
        {
            list.Remove(item);
        }

        internal void RemoveOnlyAt(int index)
        {
            list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
