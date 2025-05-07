using Microsoft.Extensions.Hosting;
using System.Net.Sockets;
using System.Net;
using static UdpListener.Library;
using Microsoft.Extensions.Logging;

namespace UdpListener
{
    internal class Worker(ILogger<Worker> logger): BackgroundService
    {
        private readonly ILogger<Worker> logger = logger;

        private readonly FedLabsMessagePart[] defaultMessagePrototype = 
        [
            FedLabsMessagePart.Double("Clock"),

            FedLabsMessagePart.Double("Rx [ECI]"),
            FedLabsMessagePart.Double("Ry [ECI]"),
            FedLabsMessagePart.Double("Rz [ECI]"),

            FedLabsMessagePart.Double("Vx [ECI]"),
            FedLabsMessagePart.Double("Vy [ECI]"),
            FedLabsMessagePart.Double("Vz [ECI]"),

            FedLabsMessagePart.Double("qr"),
            FedLabsMessagePart.Double("qx"),
            FedLabsMessagePart.Double("qy"),
            FedLabsMessagePart.Double("qz"),

            FedLabsMessagePart.Double("Wx [body]"),
            FedLabsMessagePart.Double("Wy [body]"),
            FedLabsMessagePart.Double("Wz [body]"),
        ];

        private UdpClient? udpListener;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int port = 5000;

            udpListener = new UdpClient(port);
            var endPoint = new IPEndPoint(IPAddress.Any, port);

            logger.LogInformation("Started UDP listener on port {portNumber}", port);

            ulong messageCount = 0;


            while (stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    var result = await udpListener.ReceiveAsync(stoppingToken);

                    var receivedBytes = result.Buffer;

                    logger.LogInformation(
                        "Message #{messageCount}: {messageLength} bytes from {senderAddress}:{senderPort} at {messageTimestamp}",
                        messageCount++, receivedBytes.Length, endPoint.Address, endPoint.Port, DateTime.UtcNow);

                    //string receivedMessage = Encoding.UTF8.GetString(receivedBytes);
                    DecodeMessage(receivedBytes);

                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("UDP listener stopping due to cancellation.");

                    udpListener?.Close();
                    udpListener?.Dispose();

                    logger.LogInformation("UDP listener stopped");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error in UDP listener.");
                }
                finally
                {
                    
                }
            }
        }

        private void DecodeMessage(byte[] receivedBytes)
        {
            var messagePrototype = Array.Empty<FedLabsMessagePart>();

            // Check if the received message length matches the expected length
            // If not, try to improvise a message prototype based on the received bytes

            (int quotient, int remainder) = Math.DivRem(receivedBytes.Length, 8);


            if (receivedBytes.Length != defaultMessagePrototype.Sum(x => (int)x.ValueType))
            {
                logger.LogWarning($"\tUNEXPECTED MESSAGE LENGTH --- IMPROVISING\n");

                if (remainder != 0)
                {
                    logger.LogError("\tMESSAGE LENGTH NOT MULTIPLE OF 8 BYTES --- NOPE.\n");
                    return;
                }

                messagePrototype = [.. Enumerable.Range(0, quotient).Select(x => FedLabsMessagePart.Double($"VAL #{x}"))];
            }
            else
            {
                messagePrototype = defaultMessagePrototype;
            }


            // Decode the message

            int messageByteOffset = 0;

            Console.WriteLine("\tPosition\tType\t\tLength\t\tName\t\t\t\tContent");

            for (int i = 0; i < messagePrototype.Length; i++)
            {
                var part = messagePrototype[i];

                int valueTypeBytes = (int)part.ValueType;
                string valueTypeName = part.ValueType.ToString();

                object value;

                // Decode the value based on its type
                switch (part.ValueType)
                {
                    case FedLabsMessageValueTypes.Double:
                        value = BitConverter.ToDouble(receivedBytes, messageByteOffset);
                        messageByteOffset += (int)FedLabsMessageValueTypes.Double;
                        break;

                    case FedLabsMessageValueTypes.Int32:
                        value = BitConverter.ToInt32(receivedBytes, messageByteOffset);
                        messageByteOffset += (int)FedLabsMessageValueTypes.Int32;
                        break;

                    default:
                        throw new Exception($"No converter registered for {part.ValueType.GetType().FullName}");
                }

                Console.WriteLine($"\t{i}\t\t{valueTypeName}\t\t{valueTypeBytes}\t\t{part.Name,-12}\t\t\t{value,+12:+0.000000;-0.000000; 0.000000}");
            }

            Console.WriteLine();
        }
    }
}
