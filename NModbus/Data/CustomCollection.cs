using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NModbus.Data
{
    /// <summary>
    /// 字节
    /// </summary>
    public class CustomCollection : Collection<byte>, IModbusMessageDataCollection
    {
        public byte[] NetworkBytes
        {
            get
            {
                return this.Items.ToArray();
            }
        }

        /// <summary>
        ///     Gets the byte count.
        /// </summary>
        public byte ByteCount => (byte)(Count);

        public CustomCollection()
        {
        }

        public CustomCollection(params byte[] registers)
            : this((IList<byte>)registers)
        {
        }

        public CustomCollection(IList<byte> registers)
            : base(registers.IsReadOnly ? new List<byte>(registers) : registers)
        {
        }

        public override string ToString()
        {
            return string.Concat("{", string.Join(", ", this.Select(v => v.ToString()).ToArray()), "}");
        }
    }
}
