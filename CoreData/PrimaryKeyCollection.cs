using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CoreData
{
    /// <summary>
    /// Wraps around <see cref="System.Runtime.Serialization.ObjectIDGenerator"/> to maintain a primary key count of each unique type that
    /// is added.
    /// </summary>
    public class PrimaryKeyCollection
    {
        private readonly Dictionary<Type, ObjectIDGenerator> _store = new Dictionary<Type, ObjectIDGenerator>();

        private readonly ObjectIDGenerator _objectIds = new ObjectIDGenerator();

        public long Add(object item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            Type objectType = item.GetType();

            if (!_store.ContainsKey(objectType))
            {
                _store[objectType] = new ObjectIDGenerator();
            }

            bool firstTime;
            return _store[objectType].GetId(item, out firstTime);
        }

        public long GetKeyFor(object item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            Type itemType = item.GetType();

            if (!_store.ContainsKey(itemType))
            {
                throw new KeyNotFoundException(String.Format("There are no objects of type {0} defined.", itemType.Name));
            }

            bool firstTime;
            long itemId = _store[itemType].HasId(item, out firstTime);

            if (itemId == 0)
            {
                throw new ArgumentException(String.Format("The given {0} does not exist in the collection.",
                                                          itemType.Name), "item");
            }

            return itemId;
        }

        public bool Contains(object item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            Type itemType = item.GetType();
            bool firstTime;
            return _store.ContainsKey(itemType) && _store[itemType].HasId(item, out firstTime) != 0;
        }

        public void Clear()
        {
            _store.Clear();
        }
    }
}
