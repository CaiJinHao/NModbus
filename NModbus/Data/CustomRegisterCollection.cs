using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;

namespace NModbus.Data
{
    public class CustomRegisterCollection : Collection<short>, IModbusMessageDataCollection
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RegisterCollection" /> class.
        /// </summary>
        public CustomRegisterCollection()
        {
        }

        /// <summary>
        ///     Converts a network order byte array to an array of UInt16 values in host order.
        /// </summary>
        /// <param name="networkBytes">The network order byte array.</param>
        /// <returns>The host order ushort array.</returns>
        protected static short[] NetworkBytesToHostInt16(byte[] networkBytes)
        {
            if (networkBytes == null)
            {
                throw new ArgumentNullException(nameof(networkBytes));
            }

            short[] result = new short[networkBytes.Length];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = networkBytes[i];
            }

            return result;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MyRegisterCollection" /> class.
        /// </summary>
        /// <param name="bytes">Array for register collection.</param>
        public CustomRegisterCollection(byte[] bytes)
            : this((IList<short>)NetworkBytesToHostInt16(bytes))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MyRegisterCollection" /> class.
        /// </summary>
        /// <param name="registers">Array for register collection.</param>
        public CustomRegisterCollection(params short[] registers)
            : this((IList<short>)registers)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MyRegisterCollection" /> class.
        /// </summary>
        /// <param name="registers">List for register collection.</param>
        public CustomRegisterCollection(IList<short> registers)
            : base(registers.IsReadOnly ? new List<short>(registers) : registers)
        {
        }

        public byte[] NetworkBytes
        {
            get
            {
                var bytes = new MemoryStream(ByteCount);

                foreach (ushort register in this)
                {
                    var b = BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)register));
                    bytes.Write(b, 0, b.Length);
                }

                return bytes.ToArray();
            }
        }

        /// <summary>
        ///     Gets the byte count.
        /// </summary>
        public byte ByteCount => (byte)(Count * 2);

        /// <summary>
        ///     Returns a <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
        /// </returns>
        public override string ToString()
        {
            return string.Concat("{", string.Join(", ", this.Select(v => v.ToString()).ToArray()), "}");
        }
    }
}
