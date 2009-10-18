using System;
using System.Collections.Generic;

namespace NHibernate.Search.Util {
    using Type = System.Type;

    public class TypeDictionary<TValue> : IDictionary<Type, TValue>
    {
        private readonly IDictionary<Type, TValue> inner = new Dictionary<Type, TValue>();

        public TypeDictionary() : this(true)
        {
            
        }

        public TypeDictionary(bool matchInterfaces)
        {
            MatchesInterfaces = matchInterfaces;
        }

        public void Add(KeyValuePair<Type, TValue> item)
        {
            inner.Add(item);
        }

        public void Add(Type key, TValue value)
        {
            inner.Add(key, value);
        }
        
        public bool Remove(Type key)
        {
            return inner.Remove(key);
        }

        public bool Remove(KeyValuePair<Type, TValue> item)
        {
            return inner.Remove(item);
        }

        public void Clear()
        {
            inner.Clear();
        }

        public bool Contains(KeyValuePair<Type, TValue> item)
        {
            return this.ContainsKey(item.Key);
        }

        public bool ContainsKey(Type key)
        {
            foreach (var type in GetAllMatchingTypes(key)) {
                if (inner.ContainsKey(type))
                    return true;
            }

            return false;
        }

        public void CopyTo(KeyValuePair<Type, TValue>[] array, int arrayIndex)
        {
            inner.CopyTo(array, arrayIndex);
        }

        public bool TryGetValue(Type key, out TValue value)
        {
            foreach (var type in GetAllMatchingTypes(key)) {
                if (inner.TryGetValue(type, out value))
                    return true;
            }

            value = default(TValue);
            return false;
        }

        public TValue this[Type key]
        {
            get {
                TValue value;
                var found = this.TryGetValue(key, out value);
                if (!found)
                    throw new KeyNotFoundException();

                return value;
            }
            set { inner[key] = value; }
        }

        public int Count
        {
            get { return inner.Count; }
        }

        public ICollection<System.Type> Keys
        {
            get { return inner.Keys; }
        }

        public ICollection<TValue> Values
        {
            get { return inner.Values; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public IEnumerator<KeyValuePair<System.Type, TValue>> GetEnumerator()
        {
            return inner.GetEnumerator();
        }

        private IEnumerable<Type> GetAllMatchingTypes(Type type)
        {
            var current = type;
            var previous = (Type)null;

            while (current != previous && current != null)
            {
                yield return current;
                previous = current;
                current = current.BaseType;
            }

            if (!this.MatchesInterfaces)
                yield break;

            var interfaces = type.GetInterfaces();
            foreach (var @interface in interfaces)
            {
                yield return @interface;
            }
        }

        public bool MatchesInterfaces { get; private set; }
        
        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
