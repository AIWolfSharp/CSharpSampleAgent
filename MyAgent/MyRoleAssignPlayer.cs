// Copyright (c) 2022 OTSUKI Takashi
// SPDX-License-Identifier: MIT
using AIWolf.Lib;

namespace MyAgent;

/// <summary>
/// 役職に実際のプレイヤークラスを割り当てるプレイヤークラス
/// </summary>
public class MyRoleAssignPlayer : AbstractRoleAssignPlayer
{
    public override string Name => "MyAgent";

    protected override IPlayer VillagerPlayer => new MyVillager();

    protected override IPlayer BodyguardPlayer => new MyBodyguard();

    protected override IPlayer SeerPlayer => new MySeer();

    protected override IPlayer MediumPlayer => new MyMedium();

    protected override IPlayer PossessedPlayer => new MyPossessed();

    protected override IPlayer WerewolfPlayer => new MyWerewolf();
}
