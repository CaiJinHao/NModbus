using NModbus.Data;
using NModbus.Unme.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NModbus.Message
{
    internal class MyReadHoldingInputRegistersResponse : AbstractModbusMessageWithData<MyRegisterCollection>, IModbusMessage
    {
        public MyReadHoldingInputRegistersResponse()
        {
        }

        public MyReadHoldingInputRegistersResponse(byte functionCode, byte slaveAddress, MyRegisterCollection data)
            : base(slaveAddress, functionCode)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            ByteCount = data.ByteCount;
            Data = data;
        }

        public byte ByteCount
        {
            get => MessageImpl.ByteCount.Value;
            set => MessageImpl.ByteCount = value;
        }

        public override int MinimumFrameSize => 3;

        public override string ToString()
        {
            string msg = $"Read {Data.Count} {(FunctionCode == ModbusFunctionCodes.ReadHoldingRegisters ? "holding" : "input")} registers.";
            return msg;
        }

        protected override void InitializeUnique(byte[] frame)
        {
            if (frame.Length < MinimumFrameSize + frame[2])
            {
                throw new FormatException("Message frame does not contain enough bytes.");
            }

            ByteCount = frame[2];
            Data = new MyRegisterCollection(frame.Slice(3, ByteCount).ToArray());
        }
    }
}
