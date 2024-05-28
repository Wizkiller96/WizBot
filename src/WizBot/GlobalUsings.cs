// global using System.Collections.Concurrent;
global using NonBlocking;

// packages
global using Serilog;

// wizbot
global using WizBot;
global using WizBot.Db;
global using WizBot.Services;
global using Wiz.Common; // new project
global using WizBot.Common; // old + wizbot specific things
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