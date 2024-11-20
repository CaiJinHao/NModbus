using NModbus.Logging;
using NModbus.Message;
using NModbus.Utility;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NModbus.IO
{
    internal class CustomModbusRtuTransport : ModbusSerialTransport, IModbusRtuTransport
    {
        public const int RequestFrameStartLength = 0;

        public const int ResponseFrameStartLength = 0;
        /// <summary>
        /// 结束 帧尾
        /// </summary>
        protected byte[] EndOfFrame { get; set; } = { 0x0D, 0x0A };
        /// <summary>
        /// 上位机发送标识
        /// </summary>
        protected byte SendTag { get; set; } = 0xCC;
        protected byte[] HeadOfFrame { get; set; } = { 0x5A, 0x5B };//帧头
        /// <summary>
        /// 已知的需要读取的字节数
        /// </summary>
        protected int? Count { get; set; }

        internal CustomModbusRtuTransport(IStreamResource streamResource, IModbusFactory modbusFactory, IModbusLogger logger, byte[] headOfFrame, byte sendTag, byte[] endOfFrame) : base(streamResource, modbusFactory, logger)
        {
            HeadOfFrame = headOfFrame;
            EndOfFrame = endOfFrame;
            SendTag = sendTag;
        }

        internal CustomModbusRtuTransport(IStreamResource streamResource, IModbusFactory modbusFactory, IModbusLogger logger, byte[] headOfFrame, byte sendTag, int count) : base(streamResource, modbusFactory, logger)
        {
            HeadOfFrame = headOfFrame;
            Count = count;
            SendTag = sendTag;
        }

        public override byte[] BuildMessageFrame(IModbusMessage message)
        {
            var messageFrame = message.MessageFrame;//包含地址位
            var crc = ModbusUtility.XORRange(messageFrame);
            var crcLength = 1;
            var messageBody = new MemoryStream(HeadOfFrame.Length + messageFrame.Length + crcLength + EndOfFrame.Length);
            messageBody.Write(HeadOfFrame, 0, HeadOfFrame.Length);//帧头
            messageBody.Write(new byte[] { SendTag }, 0, 1);
            messageBody.Write(messageFrame, 0, messageFrame.Length);
            messageBody.Write(new byte[] { crc }, 0, crcLength);
            messageBody.Write(EndOfFrame, 0, EndOfFrame.Length);


            return messageBody.ToArray();
        }

        public override bool ChecksumsMatch(IModbusMessage message, byte[] messageFrame)
        {
            return true;
        }

        public override IModbusMessage ReadResponse<T>()
        {
            byte[] frame = ReadResponse();

            Logger.LogFrameRx(frame);
            var headIndex = IndexOf(frame, HeadOfFrame);//查找帧头
            if (headIndex <= 0)
            {
                //当第一个就是帧头时，不需要截取，当没有找到帧头时，返回整个帧
                return CreateResponse<T>(frame);
            }

            var newSize = frame.Length - headIndex;
            var newArray = new byte[newSize];
            Array.Copy(frame, newArray, newSize);
            return CreateResponse<T>(newArray);
        }

        private byte[] ReadResponse()
        {
            return RunWithTimeout(() =>
            {
                // 读取直到帧尾结束
                if (Count.HasValue)
                {
                    return Read(Count.Value);
                }
                else
                {
                    return Read();
                }
            }, TimeSpan.FromMilliseconds(StreamResource.WriteTimeout));//120秒不返回数据为超时
        }

        /// <summary>
        /// 一次最多读取2048个字节，如果需要更多字节，请修改count
        /// </summary>
        /// <returns></returns>
        public virtual byte[] Read()
        {
            var step = 64;
            byte[] frameBytes = new byte[step];
            int numBytesRead = 0;

            //一直不包含EndOfFrame可能会导致内存溢出
            while (!ByteArrayContainsPattern(frameBytes, EndOfFrame))
            {
                numBytesRead += StreamResource.Read(frameBytes, numBytesRead, step - numBytesRead);
                if (numBytesRead == step)
                {
                    frameBytes = ExtendByteArray(frameBytes, step);
                }
            }

            var newArray = new byte[numBytesRead];
            Array.Copy(frameBytes, newArray, numBytesRead);
            return newArray;
        }

        protected virtual T RunWithTimeout<T>(Func<T> operation, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource();
            var task = Task.Run(() =>
            {
                try
                {
                    return operation();
                }
                catch (OperationCanceledException)
                {
                    throw; // Re-throw the exception to handle timeout outside
                }
            }, cts.Token);

            if (!task.Wait(timeout))
            {
                cts.Cancel(); // Request cancellation
                throw new SocketException((int)SocketError.TimedOut);
            }

            return task.Result; // Get the result of the task
        }

        /// <summary>
        /// 判断数组中是否包含指定的字节数组
        /// </summary>
        /// <param name="data"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        protected virtual bool ByteArrayContainsPattern(byte[] data, byte[] pattern)
        {
            if (data == null || pattern == null || pattern.Length == 0 || data.Length < pattern.Length)
            {
                return false;
            }

            string dataString = BitConverter.ToString(data);
            string patternString = BitConverter.ToString(pattern);

            return dataString.Contains(patternString);
        }

        /// <summary>
        /// 字符串转换为字节数组
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        protected virtual byte[] HexStringToByteArray(string hexString)
        {
            // Remove any hyphens or other delimiters
            hexString = hexString.Replace("-", string.Empty);

            // Check if the length of the string is valid
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException("Invalid length of hex string.");
            }

            // Convert the hex string to a byte array
            byte[] bytes = new byte[hexString.Length / 2];
            for (int i = 0; i < hexString.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return bytes;
        }

        /// <summary>
        /// 查找指定数组的位置
        /// </summary>
        /// <param name="array"></param>
        /// <param name="subArray"></param>
        /// <returns></returns>
        protected virtual int IndexOf(byte[] array, byte[] subArray)
        {
            if (array == null || subArray == null || subArray.Length > array.Length)
            {
                return -1;
            }

            for (int i = 0; i <= array.Length - subArray.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < subArray.Length; j++)
                {
                    if (array[i + j] != subArray[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 为数组扩展空间
        /// </summary>
        protected virtual byte[] ExtendByteArray(byte[] original, int addSize)
        {
            byte[] newArray = new byte[original.Length + addSize];
            Array.Copy(original, newArray, original.Length);
            return newArray;
        }

        public virtual byte[] Read(int count)
        {
            byte[] frameBytes = new byte[count];
            int numBytesRead = 0;

            while (numBytesRead != count)
            {
                numBytesRead += StreamResource.Read(frameBytes, numBytesRead, count - numBytesRead);
            }

            return frameBytes;
        }

        public override void IgnoreResponse()
        {
        }

        public override byte[] ReadRequest()
        {
            byte[] frame = ReadResponse();

            Logger.LogFrameRx(frame);
            var headIndex = IndexOf(frame, HeadOfFrame);//查找帧头
            if (headIndex <= 0)
            {
                return frame;
            }

            var newSize = frame.Length - headIndex;
            var newArray = new byte[newSize];
            Array.Copy(frame, newArray, newSize);
            return newArray;
        }

        /// <summary>
        /// 判断是否需要重试
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public override bool OnShouldRetryResponse(IModbusMessage request, IModbusMessage response)
        {
            return false;//不需要重试，Read 中已经读取到了帧尾
        }

        public override void ValidateResponse(IModbusMessage request, IModbusMessage response)
        {
            // message specific validation
            var req = request as IModbusRequest;

            if (req != null)
            {
                req.ValidateResponse(response);
            }

            OnValidateResponse(request, response);
        }

        public override bool ShouldRetryResponse(IModbusMessage request, IModbusMessage response)
        {
            return OnShouldRetryResponse(request, response);
        }
    }
}
