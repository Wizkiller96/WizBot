using System;
using System.Buffers;

namespace WizBot.Voice
{
    public sealed class VoiceClient : IDisposable
    {
        delegate int EncodeDelegate(Span<byte> input, byte[] output);

        private readonly int sampleRate;
        private readonly int bitRate;
        private readonly int channels;
        private readonly int frameDelay;
        private readonly int bitDepth;

        public LibOpusEncoder Encoder { get; }
        private readonly ArrayPool<byte> _arrayPool;

        public int BitDepth => bitDepth * 8;
        public int Delay => frameDelay;

        private int FrameSizePerChannel => Encoder.FrameSizePerChannel;
        public int InputLength => FrameSizePerChannel * channels * bitDepth;

        EncodeDelegate Encode;

        public VoiceClient(SampleRate sampleRate = SampleRate._48k,
            Bitrate bitRate = Bitrate._192k,
            Channels channels = Channels.Two,
            FrameDelay frameDelay = FrameDelay.Delay20,
            BitDepthEnum bitDepthEnum = BitDepthEnum.Float32)
        {
            this.frameDelay = (int) frameDelay;
            this.sampleRate = (int) sampleRate;
            this.bitRate = (int) bitRate;
            this.channels = (int) channels;
            this.bitDepth = (int) bitDepthEnum;

            this.Encoder = new(this.sampleRate, this.channels, this.bitRate, this.frameDelay);

            Encode = bitDepthEnum switch
            {
                BitDepthEnum.Float32 => Encoder.EncodeFloat,
                BitDepthEnum.UInt16 => Encoder.Encode,
                _ => throw new NotSupportedException(nameof(BitDepth))
            };

            _arrayPool = ArrayPool<byte>.Shared;
        }

        public int SendPcmFrame(VoiceGateway gw, Span<byte> data, int offset, int count)
        {
            var secretKey = gw.SecretKey;
            if (secretKey.Length == 0)
                return (int) SendPcmError.SecretKeyUnavailable;

            var encodeOutput = _arrayPool.Rent(LibOpusEncoder.MaxData);
            try
            {
                var encodeOutputLength = Encode(data, encodeOutput);
                return SendOpusFrame(gw, encodeOutput, 0, encodeOutputLength);
            }
            finally
            {
                 _arrayPool.Return(encodeOutput);
            }
        }

        public int SendOpusFrame(VoiceGateway gw, byte[] data, int offset, int count)
        {
            var secretKey = gw.SecretKey;
            if (secretKey is null || secretKey.Length != Sodium.KEY_SIZE)
                return (int) SendPcmError.SecretKeyUnavailable;

            // RTP header: 12 bytes
            const int RtpHeaderLength = 12;
            var header = new byte[RtpHeaderLength];

            header[0] = 0x80; // version + flags
            header[1] = 0x78; // payload type

            // Sequence (big-endian)
            var seqBytes = BitConverter.GetBytes(gw.Sequence);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(seqBytes);
            Buffer.BlockCopy(seqBytes, 0, header, 2, 2);

            // Timestamp (big-endian)
            var timestampBytes = BitConverter.GetBytes(gw.Timestamp);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(timestampBytes);
            Buffer.BlockCopy(timestampBytes, 0, header, 4, 4);

            // SSRC (big-endian)
            var ssrcBytes = BitConverter.GetBytes(gw.Ssrc);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(ssrcBytes);
            Buffer.BlockCopy(ssrcBytes, 0, header, 8, 4);

            // Increment counters for next packet
            gw.Timestamp += (uint) FrameSizePerChannel;
            gw.Sequence++;

            // Build 24-byte nonce: 4-byte counter (big-endian) + 20 zero bytes
            var nonce = new byte[Sodium.NONCE_SIZE];
            var nonceCounterBytes = BitConverter.GetBytes(gw.NonceSequence);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(nonceCounterBytes);
            Buffer.BlockCopy(nonceCounterBytes, 0, nonce, 0, 4);
            gw.NonceSequence++;

            // Encrypt: ciphertext = encrypted audio + 16-byte tag
            var encryptedLength = count + Sodium.TAG_SIZE;
            var encryptedBytes = new byte[encryptedLength];

            Sodium.Encrypt(
                data, offset, count,
                encryptedBytes, 0,
                header, RtpHeaderLength,
                nonce,
                secretKey);

            // Final packet: RTP header + encrypted data + tag + 4-byte nonce suffix
            const int NonceSuffixSize = 4;
            var rtpDataLength = RtpHeaderLength + encryptedLength + NonceSuffixSize;
            var rtpData = _arrayPool.Rent(rtpDataLength);
            try
            {
                // Copy RTP header
                Buffer.BlockCopy(header, 0, rtpData, 0, RtpHeaderLength);
                // Copy encrypted audio + tag
                Buffer.BlockCopy(encryptedBytes, 0, rtpData, RtpHeaderLength, encryptedLength);
                // Copy 4-byte nonce suffix (big-endian)
                Buffer.BlockCopy(nonceCounterBytes, 0, rtpData, RtpHeaderLength + encryptedLength, NonceSuffixSize);

                gw.SendRtpData(rtpData, rtpDataLength);

                return rtpDataLength;
            }
            finally
            {
                _arrayPool.Return(rtpData);
            }
        }

        public void Dispose()
            => Encoder.Dispose();
    }

    public enum SendPcmError
    {
        SecretKeyUnavailable = -1,
    }

    public enum FrameDelay
    {
        Delay5 = 5,
        Delay10 = 10,
        Delay20 = 20,
        Delay40 = 40,
        Delay60 = 60,
    }

    public enum BitDepthEnum
    {
        UInt16 = sizeof(UInt16),
        Float32 = sizeof(float),
    }

    public enum SampleRate
    {
        _48k = 48_000,
    }

    public enum Bitrate
    {
        _64k = 64 * 1024,
        _96k = 96 * 1024,
        _128k = 128 * 1024,
        _192k = 192 * 1024,
    }

    public enum Channels
    {
        One = 1,
        Two = 2,
    }
}