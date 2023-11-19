using System;
using System.Collections.Generic;
using System.Linq;

namespace TouhouCardEngine
{
    public class RNG
    {
        public RNG(int seed, uint state)
        {
            _seed = seed;
            setState(state);
        }
        public RNG() : this(Environment.TickCount)
        {
        }
        public RNG(int seed) : this(seed, 0)
        {
        }
        public RNG(RNG rng) : this(rng.seed, rng.state)
        {
        }
        public int next()
        {
            _state++;
            return _rng.Next();
        }
        public int next(int max)
        {
            _state++;
            return _rng.Next(max);
        }
        public int next(int min, int max)
        {
            _state++;
            return _rng.Next(min, max);
        }
        public double nextDouble()
        {
            _state++;
            return _rng.NextDouble();
        }
        public void nextBytes(byte[] buffer)
        {
            _state++;
            _rng.NextBytes(buffer);
        }
        public void setState(uint state)
        {
            _rng = new Random(_seed);
            _state = state;
            advanceToState(_state);
        }

        private void advanceToState(uint state)
        {
            for (uint i = 0; i < state; i++)
            {
                _rng.Next();
            }
        }

        private Random _rng;
        public int seed => _seed;
        public uint state => _state;
        private int _seed;
        private uint _state;
    }
    [Serializable]
    public class SerializableRNG
    {
        public SerializableRNG(RNG rng)
        {
            _seed = rng.seed;
            _state = unchecked((int)rng.state);
        }
        public RNG toRNG()
        {
            return new RNG(_seed, unchecked((uint)_state));
        }
        public int _seed;
        public int _state;
    }
    public static class RNGHelper
    {
        public static T random<T>(this IEnumerable<T> e, RNG rng)
        {
            int count = e.Count();
            if (count < 1)
                return default;
            return e.ElementAt(rng.next(0, count));
        }
        public static IEnumerable<T> randomTake<T>(this IEnumerable<T> enumrable, RNG rng, int count)
        {
            List<T> list = enumrable.ToList();
            if (list.Count <= count)
                return list;
            T[] results = new T[count];
            for (int i = 0; i < results.Length; i++)
            {
                int index = rng.next(0, list.Count);
                results[i] = list[index];
                list.RemoveAt(index);
            }
            return results;
        }
    }
}
