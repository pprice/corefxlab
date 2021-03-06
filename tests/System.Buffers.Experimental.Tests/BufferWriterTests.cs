// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Binary.Base64Experimental;
using System.Buffers.Text;
using System.Text;
using Xunit;
using static System.Buffers.Binary.BinaryPrimitives;

namespace System.Buffers.Tests
{
    public partial class BufferWriterTests
    {
        TransformationFormat s_base64 = new TransformationFormat(Base64Experimental.BytesToUtf8Encoder);

        [Fact]
        public void Basics()
        {
            Span<byte> buffer = stackalloc byte[256];
            var writer = Text.BufferWriter.Create(buffer);

            writer.WrittenCount = 0;
            writer.Write("AaBc", new TransformationFormat(Encodings.Ascii.ToLowercase, Encodings.Ascii.ToUppercase));
            Assert.Equal("AABC", Encodings.Utf8.ToString(writer.Written));

            writer.WrittenCount = 0;
            writer.Write("AaBc", new TransformationFormat(Encodings.Ascii.ToLowercase));
            Assert.Equal("aabc", Encodings.Utf8.ToString(writer.Written));

            writer.WrittenCount = 0;
            writer.Write("AaBc", new TransformationFormat(Encodings.Ascii.ToUppercase));
            Assert.Equal("AABC", Encodings.Utf8.ToString(writer.Written));

            writer.WrittenCount = 0;
            writer.Write("AaBc", new TransformationFormat(
                Encodings.Ascii.ToUppercase,
                Base64Experimental.Utf8ToBytesDecoder,
                Base64Experimental.BytesToUtf8Encoder)
            );
            Assert.Equal("AABC", Encodings.Utf8.ToString(writer.Written));
        }

        [Fact]
        public void Writable()
        {
            Span<byte> buffer = stackalloc byte[256];
            var writer = Text.BufferWriter.Create(buffer);

            var ulonger = new UInt128();
            ulonger.Lower = ulong.MaxValue;
            ulonger.Upper = 1;

            writer.WriteBytes(ulonger, s_base64);
            var result = Encodings.Utf8.ToString(writer.Written);
            Assert.Equal("//////////8BAAAAAAAAAA==", result);

            var ulongerSpan = new Span<UInt128>(new UInt128[1]);
            Assert.Equal(OperationStatus.Done, Base64.DecodeFromUtf8(writer.Written, ulongerSpan.AsBytes(), out int consumed, out int written));
            Assert.Equal(ulongerSpan[0].Lower, ulonger.Lower);
            Assert.Equal(ulongerSpan[0].Upper, ulonger.Upper);
        }

        [Fact]
        public void WriteDateTime()
        {
            var now = DateTime.UtcNow;
            Span<byte> buffer = stackalloc byte[256];
            var writer = Text.BufferWriter.Create(buffer);
            writer.WriteLine(now, 'R');
            var result = Encodings.Utf8.ToString(writer.Written);
            Assert.Equal(string.Format("{0:R}\n", now), result);
        }
    }

    public struct UInt128 : IWritable
    {
        public ulong Lower;
        public ulong Upper;

        const int size = sizeof(ulong) * 2;

        public bool TryWrite(Span<byte> buffer, out int written, StandardFormat format = default)
        {
            if (format == default)
            {
                if (buffer.Length < size)
                {
                    written = 0;
                    return false;
                }

                if (BitConverter.IsLittleEndian)
                {
                    WriteMachineEndian(buffer, ref Lower);
                    WriteMachineEndian(buffer.Slice(sizeof(ulong)), ref Upper);
                }
                else
                {
                    WriteMachineEndian(buffer, ref Upper);
                    WriteMachineEndian(buffer.Slice(sizeof(ulong)), ref Lower);
                }
                written = size;
                return true;
            }
            if (format.Symbol == 't')
            {
                var utf8 = Encoding.UTF8.GetBytes("hello").AsReadOnlySpan();
                if (utf8.TryCopyTo(buffer))
                {
                    written = utf8.Length;
                    return true;
                }
                written = 0;
                return false;
            }

            throw new Exception("invalid format");
        }
    }
}
