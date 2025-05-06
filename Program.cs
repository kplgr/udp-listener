using System.Net.Sockets;
using System.Net;
using System.ComponentModel;

namespace UdpListener
{
    internal class Program
    {
        public enum FedLabsMessageValueTypes
        {
            Double = 8,
            Single = 4,
            Int32 = 4,
            Int8 = 1 
        }

        public record FedLabsMessagePart(FedLabsMessageValueTypes ValueType, string Name)
        {
            private FedLabsMessagePart() : this(default, string.Empty) { }

            public static FedLabsMessagePart Double(string name) 
                => new(FedLabsMessageValueTypes.Double, name);

            public static FedLabsMessagePart Single(string name)
                => new(FedLabsMessageValueTypes.Single, name);

            public static FedLabsMessagePart Int32(string name)
                => new(FedLabsMessageValueTypes.Int32, name);

            public static FedLabsMessagePart Int8(string name)
                => new(FedLabsMessageValueTypes.Int8, name);
        };

        static void Main(string[] args)
        {
            const int port = 5802;


            var defaultMessagePrototype = new FedLabsMessagePart[]
            {
                FedLabsMessagePart.Double("Clock"),

                FedLabsMessagePart.Double("Rx ECI"),
                FedLabsMessagePart.Double("Ry ECI"),
                FedLabsMessagePart.Double("Rz ECI"),

                FedLabsMessagePart.Double("Vx ECI"),
                FedLabsMessagePart.Double("Vy ECI"),
                FedLabsMessagePart.Double("Vz ECI"),

                FedLabsMessagePart.Double("Ux ECI"),
                FedLabsMessagePart.Double("Uy ECI"),
                FedLabsMessagePart.Double("Uz ECI"),


                FedLabsMessagePart.Double("Wx ECI"),
                FedLabsMessagePart.Double("Wy ECI"),
                FedLabsMessagePart.Double("Wz ECI"),

            };

            var messagePrototype = Array.Empty<FedLabsMessagePart>();







            // Create a new UdpClient for listening on the given port
            UdpClient udpListener = new UdpClient(port);

            // Define the endpoint (localhost and port)
            var endPoint = new IPEndPoint(IPAddress.Any, port);

            Console.WriteLine($"Listening for UDP messages on port {port}...");

            ulong messageCount = 0;

            // Continuously listen for incoming UDP packets
            while (true)
            {
                try
                {
                    // Receive the incoming UDP packet
                    byte[] receivedBytes = udpListener.Receive(ref endPoint);

                    // Convert the received bytes to a string
                    //string receivedMessage = Encoding.UTF8.GetString(receivedBytes);


                    Console.WriteLine($"Received message {messageCount++}: {receivedBytes.Length} bytes");

                    if (receivedBytes.Length != messagePrototype.Sum(x => (int)x.ValueType))
                    {
                        Console.WriteLine("\tUNEXPECTED MESSAGE LENGTH --- IMPROVISING\n");

                        (int quotient, int remainder) = Math.DivRem(receivedBytes.Length, 8);

                        if (remainder != 0)
                        {
                            Console.WriteLine("\tMESSAGE LENGTH NOT MULTIPLE OF 8 BYTES --- FUCKING OFF\n");
                            continue;
                        }

                        messagePrototype = Enumerable.Range(0, quotient).Select(x => FedLabsMessagePart.Double($"VAL {x}")).ToArray();

                    }
                    else
                    {
                        messagePrototype = defaultMessagePrototype;
                    }
                    

                    int messageByteOffset = 0;

                    Console.WriteLine("\tPosition\tType\t\tLength\t\tName\t\tContent");

                    for (int i = 0; i < messagePrototype.Length; i++)
                    {
                        var part = messagePrototype[i];

                        int valueTypeBytes = (int)part.ValueType;
                        string valueTypeName = part.ValueType.ToString();

                        switch (part.ValueType)
                        {
                            case FedLabsMessageValueTypes.Double:
                                var valueDouble = BitConverter.ToDouble(receivedBytes, messageByteOffset);

                               Console.WriteLine($"\t{i}\t\t{valueTypeName}\t\t{valueTypeBytes}\t\t{part.Name}\t\t{valueDouble,+12:+0.000000;-0.000000; 0.000000}");
                                messageByteOffset += (int)FedLabsMessageValueTypes.Double;
                                break;

                            case FedLabsMessageValueTypes.Int32:
                                var valueInt32 = BitConverter.ToInt32(receivedBytes, messageByteOffset);

                                Console.WriteLine($"\t{i}\t\t{valueTypeName}\t\t{valueTypeBytes}\t\t{part.Name}\t\t{valueInt32}");
                                messageByteOffset += (int)FedLabsMessageValueTypes.Int32;
                                break;

                            default:
                                throw new Exception("Unsupported type");
                        }


                    }

                    Console.WriteLine();

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}
