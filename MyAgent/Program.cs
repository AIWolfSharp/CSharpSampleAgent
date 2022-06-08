// Copyright (c) 2022 OTSUKI Takashi
// SPDX-License-Identifier: MIT
using AIWolf.Client;

namespace MyAgent;

class MyAgentApp
{
    static void Main(string[] args)
    {
        new ClientStarter(new MyRoleAssignPlayer(), args).Start();
    }
}
