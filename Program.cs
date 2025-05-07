using System.Net.Sockets;
using System.Net;
using static UdpListener.Library;

namespace UdpListener
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const int port = 5000;


            var defaultMessagePrototype = new FedLabsMessagePart[]
            {
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
            };

            var udpListener = new UdpClient(port);
            var endPoint = new IPEndPoint(IPAddress.Any, port);

            Console.WriteLine($"Listening for UDP messages on port {port}...");

            ulong messageCount = 0;
            
            while (true)
            {
                try
                {
                    byte[] receivedBytes = udpListener.Receive(ref endPoint);
                    Console.WriteLine($"Message #{messageCount++}: {receivedBytes.Length} bytes from {endPoint.Address}:{endPoint.Port} at {DateTime.UtcNow}");

                    //string receivedMessage = Encoding.UTF8.GetString(receivedBytes);
                    DecodeMessage(receivedBytes, defaultMessagePrototype);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        static void DecodeMessage(byte[] receivedBytes, FedLabsMessagePart[] defaultMessagePrototype)
        {
            var messagePrototype = Array.Empty<FedLabsMessagePart>();

            // Check if the received message length matches the expected length
            // If not, try to improvise a message prototype based on the received bytes

            (int quotient, int remainder) = Math.DivRem(receivedBytes.Length, 8);

            
            if (receivedBytes.Length != defaultMessagePrototype.Sum(x => (int)x.ValueType))
            {
                Console.WriteLine($"\tUNEXPECTED MESSAGE LENGTH --- IMPROVISING\n");

                if (remainder != 0)
                {
                    Console.WriteLine("\tMESSAGE LENGTH NOT MULTIPLE OF 8 BYTES --- FUCKING OFF\n");
                    return;
                }

                messagePrototype = Enumerable.Range(0, quotient).Select(x => FedLabsMessagePart.Double($"VAL {x}")).ToArray();
            }
            else
            {
                messagePrototype = defaultMessagePrototype;
            }


            // Decode the message

            int messageByteOffset = 0;

            Console.WriteLine("\tPosition\tType\t\tLength\t\tName\t\t\tContent");

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
