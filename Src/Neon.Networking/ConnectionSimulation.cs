using System;
namespace Neon.Networking
{
    /// <summary>
    /// Connection issues simulation rules
    /// </summary>
    public class ConnectionSimulation
    {
        /// <summary>
        /// Available only for UDP, percentage of incoming/outgoing packet loss where 0 - 0%, 1 - 100%
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If a value is out of range 0-1</exception>
        public float PacketLoss
        {
            get
            {
                return packetLoss;
            }
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException("PacketLoss should be in range 0.0f-1.0f, where 0.0f is no packet loss");
                packetLoss = value;
            }
        }

        /// <summary>
        /// Adds a static delay in milliseconds for every incoming/outgoing messages
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If a value less than 0</exception>
        public int AdditionalLatencyStatic
        {
            get
            {
                return latencyStatic;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("Latency should be more than 0");
                latencyStatic = value;
            }
        }

        /// <summary>
        /// Adds a random delay from zero to value in milliseconds for every incoming/outgoing messages
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If a value less than 0</exception>
        public int AdditionalLatencyRandom
        {
            get
            {
                return latencyRandom;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("LatencyVariation should be more than 0");
                latencyRandom = value;
            }
        }

        float packetLoss;
        int latencyStatic;
        int latencyRandom;
        Random random;

        public ConnectionSimulation()
        {
            random = new Random();
        }

        public ConnectionSimulation(int latencyStatic, int latencyRandom) : this()
        {
            this.AdditionalLatencyStatic = latencyStatic;
            this.AdditionalLatencyRandom = latencyRandom;
        }

        public ConnectionSimulation(int latencyStatic, int latencyRandom, float packetLoss) : this()
        {
            this.AdditionalLatencyStatic = latencyStatic;
            this.AdditionalLatencyRandom = latencyRandom;
            this.PacketLoss = packetLoss;
        }

        public ConnectionSimulation(float packetLoss) : this()
        {
            this.PacketLoss = packetLoss;
        }

        public static ConnectionSimulation Normal => new ConnectionSimulation();
        public static ConnectionSimulation Poor => new ConnectionSimulation(200, 50, 0.1f);
        public static ConnectionSimulation Terrible => new ConnectionSimulation(700, 300, 0.2f);

        internal int GetHalfDelay()
        {
            return Math.Max(0, (latencyStatic + (int)(random.NextDouble() *
                    latencyRandom)) / 2);
        }
    }
}
