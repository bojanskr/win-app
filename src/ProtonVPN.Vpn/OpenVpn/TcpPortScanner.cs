﻿/*
 * Copyright (c) 2024 Proton AG
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

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ProtonVPN.Common.Extensions;

namespace ProtonVPN.Vpn.OpenVpn
{
    public class TcpPortScanner
    {
        public async Task<bool> IsAliveAsync(string ip, int port, Task timeoutTask)
        {
            IPEndPoint endpoint = new(IPAddress.Parse(ip), port);
            using Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                await SafeSocketAction(socket.ConnectAsync(endpoint)).WithTimeout(timeoutTask);
                return socket.Connected;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                socket.Close();
            }
        }

        private static Task SafeSocketAction(Task task)
        {
            task.ContinueWith(t =>
                {
                    switch (t.Exception?.InnerException)
                    {
                        case null:
                        case SocketException _:
                        case ObjectDisposedException _:
                            return;
                        default:
                            throw t.Exception;
                    }
                },
                TaskContinuationOptions.OnlyOnFaulted);

            return task;
        }
    }
}