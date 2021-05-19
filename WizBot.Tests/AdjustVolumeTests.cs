using System;
using System.Runtime.InteropServices;

namespace WizBot.Tests
{
    public class AdjustVolumeTests
    {
        private static void AdjustVolume(byte[] audioSamples, float volume)
        {
            if (Math.Abs(volume - 1f) < 0.0001f) return;
            
            var samples = MemoryMarshal.Cast<byte, short>(audioSamples);

            for (var i = 0; i < samples.Length; i++)
            {
                ref var sample = ref samples[i];
                sample = (short) (sample * volume);
            }
        }
    }
}