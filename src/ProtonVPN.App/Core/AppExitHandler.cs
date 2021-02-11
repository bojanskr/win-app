﻿/*
 * Copyright (c) 2020 Proton Technologies AG
 *
 * This file is part of ProtonVPN.
 *
 * ProtonVPN is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ProtonVPN is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ProtonVPN.  If not, see <https://www.gnu.org/licenses/>.
 */

using ProtonVPN.Common.Vpn;
using ProtonVPN.Core.Modals;
using ProtonVPN.Core.Vpn;
using ProtonVPN.Translations;
using System.Threading.Tasks;
using System.Windows;
using ProtonVPN.Core.Service.Vpn;

namespace ProtonVPN.Core
{
    internal class AppExitHandler : IVpnStateAware
    {
        private readonly IDialogs _dialogs;
        private VpnStatus _vpnStatus;
        private readonly VpnService _vpnService;

        public bool PendingExit { get; private set; }

        public AppExitHandler(IDialogs dialogs, VpnService vpnService)
        {
            _vpnService = vpnService;
            _dialogs = dialogs;
        }

        public void RequestExit()
        {
            if (_vpnStatus != VpnStatus.Disconnected &&
                _vpnStatus != VpnStatus.Disconnecting)
            {
                var result = _dialogs.ShowQuestion(Translation.Get("App_msg_ExitConnectedConfirm"));
                if (result == false)
                    return;
            }

            PendingExit = true;

            _vpnService.UnRegisterCallback();
            Application.Current.Shutdown();
        }

        public Task OnVpnStateChanged(VpnStateChangedEventArgs e)
        {
            _vpnStatus = e.State.Status;
            return Task.CompletedTask;
        }
    }
}
