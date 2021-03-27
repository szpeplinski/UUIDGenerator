using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace UUIDGenerator
{
    /// <summary>
    /// Simple implementation of UUID Generator, inspired by Snowflake, prototypes of UUID V6, 128bit.
    /// Sortable, no V1 MAC addresses, with sequencing, with node id, with random bytes, with UUID v4 version.
    /// </summary>
    public class UUIDGenerator
    {
        private readonly Stopwatch stopwatch = new();

        // Time between now and 1582-10-15
        private readonly TimeSpan offset = DateTimeOffset.UtcNow - new DateTime(1582, 10, 15, 0, 0, 0, DateTimeKind.Utc);

        // 14 bits for sequencing
        private const ushort MaxSequence = (1 << 14) - 1;

        // Lock for generating the nodes.
        private readonly object genLock = new();

        // Current sequence
        private ushort sequence;

        // Lastly used timestamp
        private ulong lastTimestamp;

        /// <summary> Constructor. </summary>
        /// <param name="node">The current node, you can use up all the 32 bits.</param>
        public UUIDGenerator(uint node)
        {
            this.Node = node;
            this.stopwatch.Start();
        }

        /// <summary> Gets the current node. </summary>
        public uint Node { get; }

        /// <summary> Gets the ticks. </summary>
        private ulong Ticks => (ulong)(this.offset.Ticks + this.stopwatch.Elapsed.Ticks);

        /// <summary> Creates a new Id. </summary>
        public Guid CreateId()
        {
            Guid id = this.CreateUUIDInternal();
            return id;
        }

        /// <summary>
        /// Creates UUID.
        /// </summary>
        /// <returns>The GUID.</returns>
        private Guid CreateUUIDInternal()
        {
            uint tempNode = this.Node;
            uint tempSequence;
            ulong tempTimestamp;
            lock (this.genLock)
            {
                tempTimestamp = this.Ticks;

                // Don't change your time backwards.
                if (tempTimestamp < this.lastTimestamp)
                {
#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
                    throw new InvalidOperationException($"Clock moved backwards or wrapped around. Refusing to generate id for { this.lastTimestamp - tempTimestamp} ticks");
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation
                }

                if (tempTimestamp != this.lastTimestamp)
                {
                    this.sequence = 0;
                    this.lastTimestamp = tempTimestamp;
                }
                else
                {
                    // If limit reached, wait until different timestamp is generated.
                    if (this.sequence >= MaxSequence)
                    {
                        ulong newTicks;
                        do
                        {
                            newTicks = this.Ticks;
                        } while (this.lastTimestamp == newTicks);

                        tempTimestamp = newTicks;
                        this.sequence = 0;
                        this.lastTimestamp = tempTimestamp;
                    }
                    else
                    {
                        this.sequence++;
                    }
                }

                tempSequence = this.sequence;
            }

            // 48 bits for timestamp, 4 bits for version, 12 bits for the rest of timestamp.
            // |__________________TIMESTAMP_________________________|VER_|__TIMESTAMP__|
            // 11111111_11111111_11111111_11111111_11111111_11111111_0100_0000_00000000
            ulong first64Bits = ((tempTimestamp << 4) & 0xFF_FF_FF_FF_FF_FF_00_00) | (tempTimestamp & 0x0F_FF) | 0x40_00;

            // 2 bit variant, 14 bits clock sequence, 32 bits node, other 16 bits are random
            // |VA|___SEQUENCE____|_______________NODE________________|_____RANDOM______|
            //  10_000000_00000000_11111111_11111111_11111111_11111111_00000000_00000000
            ulong second64Bits = ((ulong)0b_10000000_00000000 << 48) | (((ulong)tempSequence & 0b_00111111_11111111) << 48) | ((ulong)tempNode << 16);

            Span<byte> random = stackalloc byte[2];
            RandomNumberGenerator.Fill(random);

            byte a1 = (byte)((first64Bits >> 56) & 0xFF);
            byte a2 = (byte)((first64Bits >> 48) & 0xFF);
            byte a3 = (byte)((first64Bits >> 40) & 0xFF);
            byte a4 = (byte)((first64Bits >> 32) & 0xFF);
            int a = (a1 << 24) | (a2 << 16) | (a3 << 8) | a4;

            byte b1 = (byte)((first64Bits >> 24) & 0xFF);
            byte b2 = (byte)((first64Bits >> 16) & 0xFF);
            short b = (short)((b1 << 8) | b2);

            byte c1 = (byte)((first64Bits >> 8) & 0xFF);
            byte c2 = (byte)(first64Bits & 0xFF);
            short c = (short)((c1 << 8) | c2);

            byte d = (byte)((second64Bits >> 56) & 0xFF);
            byte e = (byte)((second64Bits >> 48) & 0xFF);
            byte f = (byte)((second64Bits >> 40) & 0xFF);
            byte g = (byte)((second64Bits >> 32) & 0xFF);
            byte h = (byte)((second64Bits >> 24) & 0xFF);
            byte i = (byte)((second64Bits >> 16) & 0xFF);
            byte j = random[0]; // 8 bytes for random
            byte k = random[1]; // 8 bytes for random

            Guid result = new(a, b, c, d, e, f, g, h, i, j, k);
            return result;
        }
    }
}
