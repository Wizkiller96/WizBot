﻿#nullable disable
using System.Runtime.InteropServices;

namespace WizBot.Modules.Permissions;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct CleverBotResponseStr
{
    public const string CLEVERBOT_RESPONSE = "CLEVERBOT:RESPONSE";
}