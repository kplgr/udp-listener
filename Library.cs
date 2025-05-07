using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpListener
{
    internal class Library
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
    }
}
