// SPDX-FileCopyrightText: 2022 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Alex Evgrashin <aevgrashin@yandex.ru>
// SPDX-FileCopyrightText: 2023 HerCoyote23 <131214189+HerCoyote23@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Interrobang01 <113810873+Interrobang01@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Kara <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Vordenburg <114301317+Vordenburg@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 gus <august.eymann@gmail.ccom>
// SPDX-FileCopyrightText: 2023 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2023 router <messagebus@vk.com>
// SPDX-FileCopyrightText: 2024 Kot <1192090+koteq@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2024 SlamBamActionman <83650252+SlamBamActionman@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Thomas <87614336+Aeshus@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Vasilis <vasilis@pikachu.systems>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 CerberusWolfie <wb.johnb.willis@gmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Ilya246 <57039557+Ilya246@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 John Willis <143434770+CerberusWolfie@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 Rinary <72972221+Rinary1@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Timfa <timfalken@hotmail.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Frozen;
using Content.Shared._Starlight.CollectiveMind; // Goobstation - Starlight collective mind port
using System.Text.RegularExpressions;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Speech;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization; // Einstein Engines - Language
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

public abstract class SharedChatSystem : EntitySystem
{
    public const char RadioCommonPrefix = ';';
    public const char RadioChannelPrefix = ':';
    public const char RadioChannelAltPrefix = '.';
    public const char LocalPrefix = '>';
    public const char ConsolePrefix = '/';
    public const char DeadPrefix = '\\';
    public const char LOOCPrefix = '(';
    public const char OOCPrefix = '[';
    public const char EmotesPrefix = '@';
    public const char EmotesAltPrefix = '*';
    public const char AdminPrefix = ']';
    public const char WhisperPrefix = ',';
    public const char TelepathicPrefix = '='; //Nyano - Summary: Adds the telepathic channel's prefix.
    public const char CollectiveMindPrefix = '+'; // Goobstation - Starlight collective mind port
    public const char DefaultChannelKey = 'h';

    [ValidatePrototypeId<RadioChannelPrototype>]
    public const string CommonChannel = "Common";

    public static string DefaultChannelPrefix = $"{RadioChannelPrefix}{DefaultChannelKey}";

    [ValidatePrototypeId<SpeechVerbPrototype>]
    public const string DefaultSpeechVerb = "Default";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <summary>
    /// Cache of the keycodes for faster lookup.
    /// </summary>
    private FrozenDictionary<char, RadioChannelPrototype> _keyCodes = default!;

    // Goobstation - Starlight collective mind port
    private FrozenDictionary<char, CollectiveMindPrototype> _mindKeyCodes = default!;

    public override void Initialize()
    {
        base.Initialize();
        DebugTools.Assert(_prototypeManager.HasIndex<RadioChannelPrototype>(CommonChannel));
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);
        CacheRadios();
        CacheCollectiveMinds(); // Goobstation - Starlight collective mind port
    }

    protected virtual void OnPrototypeReload(PrototypesReloadedEventArgs obj)
    {
        if (obj.WasModified<RadioChannelPrototype>())
            CacheRadios();

        // Goobstation - Starlight collective mind port
        if (obj.WasModified<CollectiveMindPrototype>())
            CacheCollectiveMinds();
    }

    private void CacheRadios()
    {
        _keyCodes = _prototypeManager.EnumeratePrototypes<RadioChannelPrototype>()
            .ToFrozenDictionary(x => x.KeyCode);
    }

    // Goobstation - Starlight collective mind port
    private void CacheCollectiveMinds()
    {
        _prototypeManager.PrototypesReloaded -= OnPrototypeReload;
        _mindKeyCodes = _prototypeManager.EnumeratePrototypes<CollectiveMindPrototype>()
            .ToFrozenDictionary(x => x.KeyCode);
    }

    /// <summary>
    ///     Attempts to find an applicable <see cref="SpeechVerbPrototype"/> for a speaking entity's message.
    ///     If one is not found, returns <see cref="DefaultSpeechVerb"/>.
    /// </summary>
    public SpeechVerbPrototype GetSpeechVerb(EntityUid source, string message, SpeechComponent? speech = null)
    {
        if (!Resolve(source, ref speech, false))
            return _prototypeManager.Index<SpeechVerbPrototype>(DefaultSpeechVerb);

        // check for a suffix-applicable speech verb
        SpeechVerbPrototype? current = null;
        foreach (var (str, id) in speech.SuffixSpeechVerbs)
        {
            var proto = _prototypeManager.Index<SpeechVerbPrototype>(id);
            if (message.EndsWith(Loc.GetString(str)) && proto.Priority >= (current?.Priority ?? 0))
            {
                current = proto;
            }
        }

        // if no applicable suffix verb return the normal one used by the entity
        return current ?? _prototypeManager.Index<SpeechVerbPrototype>(speech.SpeechVerb);
    }

    /// <summary>
    /// Splits the input message into a radio prefix part and the rest to preserve it during sanitization.
    /// </summary>
    /// <remarks>
    /// This is primarily for the chat emote sanitizer, which can match against ":b" as an emote, which is a valid radio keycode.
    /// </remarks>
    public void GetRadioKeycodePrefix(EntityUid source,
        string input,
        out string output,
        out string prefix)
    {
        prefix = string.Empty;
        output = input;

        // If the string is less than 2, then it's probably supposed to be an emote.
        // No one is sending empty radio messages!
        if (input.Length <= 2)
            return;

        if (!(input.StartsWith(RadioChannelPrefix) || input.StartsWith(RadioChannelAltPrefix)))
            return;

        if (!_keyCodes.TryGetValue(char.ToLower(input[1]), out _))
            return;

        prefix = input[..2];
        output = input[2..];
    }

    /// <summary>
    ///     Attempts to resolve radio prefixes in chat messages (e.g., remove a leading ":e" and resolve the requested
    ///     channel. Returns true if a radio message was attempted, even if the channel is invalid.
    /// </summary>
    /// <param name="source">Source of the message</param>
    /// <param name="input">The message to be modified</param>
    /// <param name="output">The modified message</param>
    /// <param name="channel">The channel that was requested, if any</param>
    /// <param name="quiet">Whether or not to generate an informative pop-up message.</param>
    /// <returns></returns>
    public bool TryProccessRadioMessage(
        EntityUid source,
        string input,
        out string output,
        out RadioChannelPrototype? channel,
        bool quiet = false)
    {
        output = input.Trim();
        channel = null;

        if (input.Length == 0)
            return false;

        if (input.StartsWith(RadioCommonPrefix))
        {
            output = SanitizeMessageCapital(input[1..].TrimStart());
            channel = _prototypeManager.Index<RadioChannelPrototype>(CommonChannel);
            return true;
        }

        if (!(input.StartsWith(RadioChannelPrefix) || input.StartsWith(RadioChannelAltPrefix)))
            return false;

        if (input.Length < 2 || char.IsWhiteSpace(input[1]))
        {
            output = SanitizeMessageCapital(input[1..].TrimStart());
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("chat-manager-no-radio-key"), source, source);
            return true;
        }

        var channelKey = input[1];
        channelKey = char.ToLower(channelKey);
        output = SanitizeMessageCapital(input[2..].TrimStart());

        if (channelKey == DefaultChannelKey)
        {
            var ev = new GetDefaultRadioChannelEvent();
            RaiseLocalEvent(source, ev);

            if (ev.Channel != null)
                _prototypeManager.TryIndex(ev.Channel, out channel);
            return true;
        }

        if (!_keyCodes.TryGetValue(channelKey, out channel) && !quiet)
        {
            var msg = Loc.GetString("chat-manager-no-such-channel", ("key", channelKey));
            _popup.PopupEntity(msg, source, source);
        }

        return true;
    }

    // Goobstation - Starlight collective mind port
    public bool TryProccessCollectiveMindMessage(
        EntityUid source,
        string input,
        out string output,
        out CollectiveMindPrototype? channel,
        bool quiet = false)
    {
        output = input.Trim();
        channel = null;

        if (input.Length == 0)
            return false;

        if (!input.StartsWith(CollectiveMindPrefix))
            return false;

        ProtoId<CollectiveMindPrototype>? defaultChannel = null;
        if (TryComp<CollectiveMindComponent>(source, out var mind))
            defaultChannel = mind.DefaultChannel;

        if (input.Length < 2 || (char.IsWhiteSpace(input[1]) && defaultChannel == null))
        {
            output = SanitizeMessageCapital(input[1..].TrimStart());
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("chat-manager-no-radio-key"), source, source);
            return true;
        }

        var channelKey = input[1];
        channelKey = char.ToLower(channelKey);

        if (_mindKeyCodes.TryGetValue(channelKey, out channel))
        {
            output = SanitizeMessageCapital(input[2..].TrimStart());
            return true;
        }
        else if (defaultChannel != null)
        {
            output = SanitizeMessageCapital(input[1..].TrimStart());
            channel = _prototypeManager.Index<CollectiveMindPrototype>(defaultChannel.Value);
            return true;
        }

        if (quiet)
            return false;

        var msg = Loc.GetString("chat-manager-no-such-channel", ("key", channelKey));
        _popup.PopupEntity(msg, source, source);

        return false;
    }

    public virtual void TrySendInGameICMessage(
        EntityUid source,
        string message,
        InGameICChatType desiredType,
        bool hideChat, bool hideLog = false,
        IConsoleShell? shell = null,
        ICommonSession? player = null, string? nameOverride = null,
        bool checkRadioPrefix = true,
        bool ignoreActionBlocker = false,
        string wrappedMessagePostfix = "" // Goobstation
    ) { }

    public string SanitizeMessageCapital(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;
        // Capitalize first letter
        message = OopsConcat(char.ToUpper(message[0]).ToString(), message.Remove(0, 1));
        return message;
    }

    private static string OopsConcat(string a, string b)
    {
        // This exists to prevent Roslyn being clever and compiling something that fails sandbox checks.
        return a + b;
    }

    public string SanitizeMessageCapitalizeTheWordI(string message, string theWordI = "i")
    {
        if (string.IsNullOrEmpty(message))
            return message;

        for
        (
            var index = message.IndexOf(theWordI);
            index != -1;
            index = message.IndexOf(theWordI, index + 1)
        )
        {
            // Stops the code If It's tryIng to capItalIze the letter I In the mIddle of words
            // Repeating the code twice is the simplest option
            if (index + 1 < message.Length && char.IsLetter(message[index + 1]))
                continue;
            if (index - 1 >= 0 && char.IsLetter(message[index - 1]))
                continue;

            var beforeTarget = message.Substring(0, index);
            var target = message.Substring(index, theWordI.Length);
            var afterTarget = message.Substring(index + theWordI.Length);

            message = beforeTarget + target.ToUpper() + afterTarget;
        }

        return message;
    }

    public static string SanitizeAnnouncement(string message, int maxLength = 0, int maxNewlines = 2)
    {
        var trimmed = message.Trim();
        if (maxLength > 0 && trimmed.Length > maxLength)
        {
            trimmed = $"{message[..maxLength]}...";
        }

        // No more than max newlines, other replaced to spaces
        if (maxNewlines > 0)
        {
            var chars = trimmed.ToCharArray();
            var newlines = 0;
            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] != '\n')
                    continue;

                if (newlines >= maxNewlines)
                    chars[i] = ' ';

                newlines++;
            }

            return new string(chars);
        }

        return trimmed;
    }

    public static string InjectTagInsideTag(ChatMessage message, string outerTag, string innerTag, string? tagParameter)
    {
        var rawmsg = message.WrappedMessage;
        var tagStart = rawmsg.IndexOf($"[{outerTag}]");
        var tagEnd = rawmsg.IndexOf($"[/{outerTag}]");
        if (tagStart < 0 || tagEnd < 0) //If the outer tag is not found, the injection is not performed
            return rawmsg;
        tagStart += outerTag.Length + 2;

        string innerTagProcessed = tagParameter != null ? $"[{innerTag}={tagParameter}]" : $"[{innerTag}]";

        rawmsg = rawmsg.Insert(tagEnd, $"[/{innerTag}]");
        rawmsg = rawmsg.Insert(tagStart, innerTagProcessed);

        return rawmsg;
    }

    /// <summary>
    /// Injects a tag around all found instances of a specific string in a ChatMessage.
    /// Excludes strings inside other tags and brackets.
    /// </summary>
    public static string InjectTagAroundString(ChatMessage message, string targetString, string tag, string? tagParameter)
    {
        var rawmsg = message.WrappedMessage;
        rawmsg = Regex.Replace(rawmsg, "(?i)(" + targetString + ")(?-i)(?![^[]*])", $"[{tag}={tagParameter}]$1[/{tag}]");
        return rawmsg;
    }

    public static string GetStringInsideTag(ChatMessage message, string tag)
    {
        var rawmsg = message.WrappedMessage;
        var tagStart = rawmsg.IndexOf($"[{tag}]");
        var tagEnd = rawmsg.IndexOf($"[/{tag}]");
        if (tagStart < 0 || tagEnd < 0)
            return "";
        tagStart += tag.Length + 2;
        return rawmsg.Substring(tagStart, tagEnd - tagStart);
    }
}

// Einstein Engines - Language begin (moves chat types to shared)
/// <summary>
///     InGame IC chat is for chat that is specifically ingame (not lobby) but is also in character, i.e. speaking.
/// </summary>
// ReSharper disable once InconsistentNaming
[Serializable, NetSerializable]
public enum InGameICChatType : byte // Einstein Engines - Make InGameIIChatType available in Shared
{
    Speak,
    Emote,
    Whisper,
    Telepathic, // Goobstation Change
    CollectiveMind // Goobstation - Starlight collective mind port
}

/// <summary>
///     InGame OOC chat is for chat that is specifically ingame (not lobby) but is OOC, like deadchat or LOOC.
/// </summary>
[Serializable, NetSerializable]
public enum InGameOOCChatType : byte
{
    Looc,
    Dead
}

/// <summary>
///     Controls transmission of chat.
/// </summary>
[Serializable, NetSerializable]
public enum ChatTransmitRange : byte
{
    /// Acts normal, ghosts can hear across the map, etc.
    Normal,
    /// Normal but ghosts are still range-limited.
    GhostRangeLimit,
    /// Hidden from the chat window.
    HideChat,
    /// Ghosts can't hear or see it at all. Regular players can if in-range.
    NoGhosts
}
// Einstein Engines - Language end
