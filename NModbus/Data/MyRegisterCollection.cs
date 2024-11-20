using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;

namespace NModbus.Data
{
    public class MyRegisterCollection : Collection<short>, IModbusMessageDataCollection
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RegisterCollection" /> class.
        /// </summary>
        public MyRegisterCollection()
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

            if (networkBytes.Length % 2 != 0)
            {
                throw new FormatException("Error");
            }

            short[] result = new short[networkBytes.Length / 2];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (short)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(networkBytes, i * 2));
            }

            return result;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MyRegisterCollection" /> class.
        /// </summary>
        /// <param name="bytes">Array for register collection.</param>
        public MyRegisterCollection(byte[] bytes)
            : this((IList<short>)NetworkBytesToHostInt16(bytes))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MyRegisterCollection" /> class.
        /// </summary>
        /// <param name="registers">Array for register collection.</param>
        public MyRegisterCollection(params short[] registers)
            : this((IList<short>)registers)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MyRegisterCollection" /> class.
        /// </summary>
        /// <param name="registers">List for register collection.</param>
        public MyRegisterCollection(IList<short> registers)
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
