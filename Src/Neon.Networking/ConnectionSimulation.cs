using System;

namespace Neon.Networking
{
    /// <summary>
    ///     Connection issues simulation rules
    /// </summary>
    public class ConnectionSimulation
    {
        /// <summary>
        ///     Available only for UDP, percentage of incoming/outgoing packet loss where 0 - 0%, 1 - 100%
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If a value is out of range 0-1</exception>
        public float PacketLoss
        {
            get => _packetLoss;
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException(
                        "PacketLoss should be in range 0.0f-1.0f, where 0.0f is no packet loss");
                _packetLoss = value;
            }
        }

        /// <summary>
        ///     Adds a static delay in milliseconds for every incoming/outgoing messages
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If a value less than 0</exception>
        public int AdditionalLatencyStatic
        {
            get => _latencyStatic;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("Latency should be more than 0");
                _latencyStatic = value;
            }
        }

        /// <summary>
        ///     Adds a random delay from zero to value in milliseconds for every incoming/outgoing messages
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If a value less than 0</exception>
        public int AdditionalLatencyRandom
        {
            get => _latencyRandom;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("LatencyVariation should be more than 0");
                _latencyRandom = value;
            }
        }

        public static ConnectionSimulation Normal => new ConnectionSimulation();
        public static ConnectionSimulation Poor => new ConnectionSimulation(200, 50, 0.1f);
        public static ConnectionSimulation Terrible => new ConnectionSimulation(700, 300, 0.2f);
        readonly Random _random;
        int _latencyRandom;
        int _latencyStatic;

        float _packetLoss;

        public ConnectionSimulation()
        {
            _random = new Random();
        }

        public ConnectionSimulation(int latencyStatic, int latencyRandom) : this()
        {
            AdditionalLatencyStatic = latencyStatic;
            AdditionalLatencyRandom = latencyRandom;
        }

        public ConnectionSimulation(int latencyStatic, int latencyRandom, float packetLoss) : this()
        {
            AdditionalLatencyStatic = latencyStatic;
            AdditionalLatencyRandom = latencyRandom;
            PacketLoss = packetLoss;
        }

        public ConnectionSimulation(float packetLoss) : this()
        {
            PacketLoss = packetLoss;
        }

        internal int GetHalfDelay()
        {
            return Math.Max(0, (_latencyStatic + (int) (_random.NextDouble() *
                                                        _latencyRandom)) / 2);
        }
    }
}