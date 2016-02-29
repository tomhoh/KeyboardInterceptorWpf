using System;
using System.Collections.Generic;
using System.Linq;

namespace KeyboardInterceptor.Resolvers
{
    public abstract class AbstractSequenceKeyResolver : IKeyResolver
    {
        private int _delayTolerance;
        public int DelayTolerance
        {
            get { return _delayTolerance >= 400 ? _delayTolerance : 400; }
            set { _delayTolerance = value; }
        }

        protected AbstractSequenceKeyResolver(IEnumerable<Key> sequence)
        {            
            _sequence = (sequence ?? new List<Key>()).ToList();
            _buffer = new List<Key>();            
            _lastUpdate = DateTime.MinValue;;
        }

        protected abstract void OnSequenceEqual();

        public void Resolve(Key key)
        {            
            if (!_sequence.Any()) throw new ArgumentException("The sequence is empty.");            
            _buffer.Add(key);
            if (IsSequence)
            {
                if (_buffer.Count >= _sequence.Count)
                {
                    var slice = _buffer
                        .Skip(_buffer.Count - _sequence.Count)
                        .Take(_sequence.Count);

                    if (_sequence.SequenceEqual(slice))
                    {
                        OnSequenceEqual();
                        _buffer.Clear();
                    }
                }
            }
            else
            {
                _buffer.Clear();
            }                
            _lastUpdate = DateTime.Now;
        }

        private readonly List<Key> _sequence;
        private readonly List<Key> _buffer;
        private DateTime _lastUpdate;

        private bool IsSequence
        {
            get
            {
                if (_lastUpdate > DateTime.MinValue)
                {                    
                    var delta = _lastUpdate.AddMilliseconds(DelayTolerance);
                    return DateTime.Now <= delta;
                }
                return true;
            }
        }       
    }
}
