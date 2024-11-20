using NModbus.Data;
using System;

namespace NModbus.Message
{
    public class ReadCustomMessageRequest : AbstractModbusMessageWithData<CustomCollection>, IModbusRequest
    {
        public ReadCustomMessageRequest()
        {
        }

        public ReadCustomMessageRequest(CustomCollection data)
        {
            Data = data;
        }

        public ReadCustomMessageRequest(CustomCollection data, byte slaveAddress)
        {
            Data = data;
            SlaveAddress = slaveAddress;
        }

        public override byte[] MessageFrame => Data.NetworkBytes;

        public byte ByteCount
        {
            get => MessageImpl.ByteCount.Value;
            set => MessageImpl.ByteCount = value;
        }

        public ushort NumberOfPoints
        {
            get => MessageImpl.NumberOfPoints.Value;

            set
            {
                if (value > Modbus.MaximumRegisterRequestResponseSize)
                {
                    string msg = $"Maximum amount of data {Modbus.MaximumRegisterRequestResponseSize} registers.";
                    throw new ArgumentOutOfRangeException(nameof(NumberOfPoints), msg);
                }

                MessageImpl.NumberOfPoints = value;
            }
        }

        public override int MinimumFrameSize => 7;

        public override string ToString()
        {
            string msg = $"Write {NumberOfPoints} holding registers starting at slaveAddress {SlaveAddress}.";
            return msg;
        }

        public void ValidateResponse(IModbusMessage response)
        {
        }

        protected override void InitializeUnique(byte[] frame)
        {
            Data = new CustomCollection(frame);
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
