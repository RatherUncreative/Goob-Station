// SPDX-FileCopyrightText: 2024 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 SX-7 <92227810+SX-7@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.ServerCurrency;
using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Goobstation.Server.ServerCurrency.Commands
{
    [AnyCommand]
    public sealed class BalanceServerCurrencyCommand : IConsoleCommand
    {
        [Dependency] private readonly ICommonCurrencyManager _currencyMan = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        public string Command => Loc.GetString("server-currency-balance-command");
        public string Description => Loc.GetString("server-currency-balance-command-description");
        public string Help => Loc.GetString("server-currency-balance-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 0)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if(shell.Player is not { } player){
                shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            var balance = Loc.GetString("server-currency-balance-command-return",
                ("balance", _currencyMan.Stringify(_currencyMan.GetBalance(shell.Player.UserId))));

            _chatManager.ChatMessageToOne(ChatChannel.Local, balance, balance, EntityUid.Invalid, false, shell.Player.Channel);
            shell.WriteLine(balance);
        }
    }

    [AnyCommand]
    public sealed class GiftServerCurrencyCommand : IConsoleCommand
    {
        [Dependency] private readonly ICommonCurrencyManager _currencyMan = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        public string Command => Loc.GetString("server-currency-gift-command");
        public string Description => Loc.GetString("server-currency-gift-command-description");
        public string Help => Loc.GetString("server-currency-gift-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if(shell.Player is not { } player){
                shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            var plyMgr = IoCManager.Resolve<IPlayerManager>();
            if (!plyMgr.TryGetUserId(args[0], out var targetPlayer))
            {
                shell.WriteError(Loc.GetString("server-currency-command-error-1"));
                return;
            } else if (targetPlayer == shell.Player.UserId)
            {
                shell.WriteError(Loc.GetString("server-currency-gift-command-error-1"));
                return;
            }

            if (!int.TryParse(args[1], out int amount))
            {
                shell.WriteError(Loc.GetString("server-currency-command-error-2"));
                return;
            }

            amount = Math.Abs(amount);

            if (amount == 0)
                amount = 1; // Trolled

            if (!_currencyMan.CanAfford(shell.Player.UserId, amount, out int balance)){
                shell.WriteError(Loc.GetString("server-currency-gift-command-error-2", ("balance", balance)));
                return;
            }

            _currencyMan.TransferCurrency(shell.Player.UserId, targetPlayer, amount);

            var giver = Loc.GetString("server-currency-gift-command-giver", ("player", args[0]), ("amount", _currencyMan.Stringify(amount)));
            var reciever = Loc.GetString("server-currency-gift-command-reciever", ("player", shell.Player.Name), ("amount", _currencyMan.Stringify(amount)));

            if(plyMgr.TryGetSessionById(targetPlayer, out var targetPlayerSession))
                _chatManager.ChatMessageToOne(ChatChannel.Local, reciever, reciever, EntityUid.Invalid, false, targetPlayerSession.Channel);
            _chatManager.ChatMessageToOne(ChatChannel.Local, giver, giver, EntityUid.Invalid, false, shell.Player.Channel);

            shell.WriteLine(giver);
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return args.Length switch
            {
                1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("server-currency-command-completion-1")),
                2 => CompletionResult.FromHint(Loc.GetString("server-currency-command-completion-2")),
                _ => CompletionResult.Empty
            };
        }
    }

    [AdminCommand(AdminFlags.Host)]
    public sealed class AddServerCurrencyCommand : IConsoleCommand
    {
        [Dependency] private readonly ICommonCurrencyManager _currencyMan = default!;

        public string Command => Loc.GetString("server-currency-add-command");
        public string Description => Loc.GetString("server-currency-add-command-description");
        public string Help => Loc.GetString("server-currency-add-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            var plyMgr = IoCManager.Resolve<IPlayerManager>();
            if (!plyMgr.TryGetUserId(args[0], out var targetPlayer))
            {
                shell.WriteError(Loc.GetString("server-currency-command-error-1"));
                return;
            }

            if (!int.TryParse(args[1], out int currency))
            {
                shell.WriteError(Loc.GetString("server-currency-command-error-2"));
                return;
            }

            var newCurrency = _currencyMan.Stringify(_currencyMan.AddCurrency(targetPlayer, currency));
            shell.WriteLine(Loc.GetString("server-currency-command-return", ("player", args[0]), ("balance", newCurrency)));
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return args.Length switch
            {
                1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("server-currency-command-completion-1")),
                2 => CompletionResult.FromHint(Loc.GetString("server-currency-command-completion-2")),
                _ => CompletionResult.Empty
            };
        }
    }

    [AdminCommand(AdminFlags.Host)]
    public sealed class RemoveServerCurrencyCommand : IConsoleCommand
    {
        [Dependency] private readonly ICommonCurrencyManager _currencyMan = default!;

        public string Command => Loc.GetString("server-currency-remove-command");
        public string Description => Loc.GetString("server-currency-remove-command-description");
        public string Help => Loc.GetString("server-currency-remove-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            var plyMgr = IoCManager.Resolve<IPlayerManager>();
            if (!plyMgr.TryGetUserId(args[0], out var targetPlayer))
            {
                shell.WriteError(Loc.GetString("server-currency-command-error-1"));
                return;
            }

            if (!int.TryParse(args[1], out int currency))
            {
                shell.WriteError(Loc.GetString("server-currency-command-error-2"));
                return;
            }

            var newCurrency = _currencyMan.Stringify(_currencyMan.RemoveCurrency(targetPlayer, currency));
            shell.WriteLine(Loc.GetString("server-currency-command-return", ("player", args[0]), ("balance", newCurrency)));
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return args.Length switch
            {
                1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("server-currency-command-completion-1")),
                2 => CompletionResult.FromHint(Loc.GetString("server-currency-command-completion-2")),
                _ => CompletionResult.Empty
            };
        }
    }

    [AdminCommand(AdminFlags.Host)]
    public sealed class SetServerCurrencyCommand : IConsoleCommand
    {
        [Dependency] private readonly ICommonCurrencyManager _currencyMan = default!;

        public string Command => Loc.GetString("server-currency-set-command");
        public string Description => Loc.GetString("server-currency-set-command-description");
        public string Help => Loc.GetString("server-currency-set-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            var plyMgr = IoCManager.Resolve<IPlayerManager>();
            if (!plyMgr.TryGetUserId(args[0], out var targetPlayer))
            {
                shell.WriteError(Loc.GetString("server-currency-command-error-1"));
                return;
            }

            if (!int.TryParse(args[1], out int currency))
            {
                shell.WriteError(Loc.GetString("server-currency-command-error-2"));
                return;
            }

            _currencyMan.SetBalance(targetPlayer, currency);
            var newCurrency = _currencyMan.Stringify(currency);
            shell.WriteLine(Loc.GetString("server-currency-command-return", ("player", args[0]), ("balance", newCurrency)));
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return args.Length switch
            {
                1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("server-currency-command-completion-1")),
                2 => CompletionResult.FromHint(Loc.GetString("server-currency-command-completion-2")),
                _ => CompletionResult.Empty
            };
        }
    }

    [AdminCommand(AdminFlags.Host)]
    public sealed class GetServerCurrencyCommand : IConsoleCommand
    {
        [Dependency] private readonly ICommonCurrencyManager _currencyMan = default!;

        public string Command => Loc.GetString("server-currency-get-command");
        public string Description => Loc.GetString("server-currency-get-command-description");
        public string Help => Loc.GetString("server-currency-get-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            var plyMgr = IoCManager.Resolve<IPlayerManager>();
            if (!plyMgr.TryGetUserId(args[0], out var targetPlayer))
            {
                shell.WriteError(Loc.GetString("server-currency-command-error-1"));
                return;
            }

            var currency = _currencyMan.Stringify(_currencyMan.GetBalance(targetPlayer));
            shell.WriteLine(Loc.GetString("server-currency-command-return", ("player", args[0]), ("balance", currency)));
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return args.Length switch
            {
                1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("server-currency-command-completion-1")),
                _ => CompletionResult.Empty
            };
        }
    }
}
