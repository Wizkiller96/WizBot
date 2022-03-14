global using System.Collections.Concurrent;

// packages
global using Serilog;
global using Humanizer;

// wizbot
global using WizBot;
global using WizBot.Services;
global using WizBot.Common;
global using WizBot.Common.Attributes;
global using WizBot.Extensions;

// discord
global using Discord;
global using Discord.Commands;
global using Discord.Net;
global using Discord.WebSocket;

// aliases
global using GuildPerm = Discord.GuildPermission;
global using ChannelPerm = Discord.ChannelPermission;
global using BotPermAttribute = Discord.Commands.RequireBotPermissionAttribute;
global using LeftoverAttribute = Discord.Commands.RemainderAttribute;
global using TypeReaderResult = WizBot.Common.TypeReaders.TypeReaderResult;

// non-essential
global using JetBrains.Annotations;