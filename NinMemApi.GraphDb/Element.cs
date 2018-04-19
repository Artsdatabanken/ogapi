using System;
using System.Collections.Generic;

namespace NinMemApi.GraphDb
{
    public class Element
    {
        protected readonly G _g;

        public string Id { get; set; }
        public int Label { get; set; }
        public IDictionary<int, object> Properties { get; set; }

        public Element(G g, int label, string id)
        {
            _g = g;
            Id = id;
            Label = label;
            Properties = new Dictionary<int, object>();
        }

        public bool HasValue<T>(int id, T value)
        {
            if (!Properties.ContainsKey(id))
            {
                return false;
            }

            return EqualityComparer<T>.Default.Equals(Cast<T>(Properties[id]), value);
        }

        public bool HasValue(int id)
        {
            return Properties.ContainsKey(id);
        }

        public T Value<T>(int id)
        {
            if (!Properties.ContainsKey(id))
            {
                ThrowKeyNotFoundException(id);
            }

            return Cast<T>(Properties[id]);
        }

        protected void AddProperty(int id, object value)
        {
            Properties.Add(id, value);
        }

        protected void ThrowKeyNotFoundException(int id)
        {
            throw new KeyNotFoundException(CreateErrorMessage($"Id {id} not found in"));
        }

        protected string CreateErrorMessage(string customPart)
        {
            return $"{customPart} {GetElementAndIdString()}";
        }

        protected string GetElementAndIdString()
        {
            return $"element {Label} with id {Id}";
        }

        protected T Cast<T>(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentException(CreateErrorMessage("Object obj cannot be null"));
            }

            T t = default(T);

            try
            {
                t = (T)obj;
            }
            catch (Exception ex)
            {
                throw new InvalidCastException(CreateErrorMessage($"The object {obj} could not be cast to {typeof(T).GetType().FullName} for"), ex);
            }

            return t;
        }
    }
}