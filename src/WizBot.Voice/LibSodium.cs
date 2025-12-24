using System;
using System.Runtime.InteropServices;

namespace WizBot.Voice
{
    internal static unsafe class Sodium
    {
        private const string SODIUM = "data/lib/libsodium";

        // XChaCha20-Poly1305 AEAD encryption
        // Key: 32 bytes, Nonce: 24 bytes, Tag: 16 bytes
        [DllImport(SODIUM, EntryPoint = "crypto_aead_xchacha20poly1305_ietf_encrypt", CallingConvention = CallingConvention.Cdecl)]
        private static extern int XChaCha20Poly1305Encrypt(
            byte* ciphertext,
            ulong* ciphertextLength,
            byte* message,
            ulong messageLength,
            byte* additionalData,
            ulong additionalDataLength,
            byte* nsec,
            byte* nonce,
            byte* key);

        public const int NONCE_SIZE = 24;
        public const int TAG_SIZE = 16;
        public const int KEY_SIZE = 32;

        public static int Encrypt(
            byte[] message, int messageOffset, int messageLength,
            byte[] ciphertext, int ciphertextOffset,
            byte[] header, int headerLength,
            byte[] nonce,
            byte[] key)
        {
            ulong ciphertextLength = 0;

            fixed (byte* msgPtr = message)
            fixed (byte* ctPtr = ciphertext)
            fixed (byte* headerPtr = header)
            fixed (byte* noncePtr = nonce)
            fixed (byte* keyPtr = key)
            {
                return XChaCha20Poly1305Encrypt(
                    ctPtr + ciphertextOffset,
                    &ciphertextLength,
                    msgPtr + messageOffset,
                    (ulong)messageLength,
                    headerPtr,
                    (ulong)headerLength,
                    null,
                    noncePtr,
                    keyPtr);
            }
        }
    }
}