using NModbus.Data;
using System;

namespace NModbus.Message
{
    public class ReadCustomMessageReponse : AbstractModbusMessageWithData<CustomRegisterCollection>, IModbusMessage
    {
        public ReadCustomMessageReponse()
        {
        }

        public override byte[] MessageFrame => Data.NetworkBytes;
        public override byte[] ProtocolDataUnit => Data.NetworkBytes;

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
            Data = new CustomRegisterCollection(frame);
        }

        public override void Initialize(byte[] frame)
        {
            if (frame.Length < MinimumFrameSize)
            {
                string msg = $"Message frame must contain at least {MinimumFrameSize} bytes of data.";
                throw new FormatException(msg);
            }

            ByteCount = (byte)frame.Length;

            //没有地址和功能码
            SlaveAddress = 0;
            FunctionCode = 0;

            InitializeUnique(frame);
        }
    }
}
