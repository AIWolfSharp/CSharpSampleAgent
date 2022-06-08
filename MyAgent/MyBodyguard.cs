// Copyright (c) 2022 OTSUKI Takashi
// SPDX-License-Identifier: MIT
using AIWolf.Lib;

namespace MyAgent;

/// <summary>
/// 狩人役エージェントクラス
/// </summary>
public class MyBodyguard : MyVillager
{
    // 護衛したエージェント
    Agent guardedAgent;

    /// <inheritdoc/>
    public override void Initialize(IGameInfo gameInfo, IGameSetting gameSetting)
    {
        base.Initialize(gameInfo, gameSetting);
        guardedAgent = null;
    }

    /// <inheritdoc/>
    public override Agent Guard()
    {
        Agent guardCandidate = null;
        // 前日の護衛が成功しているようなら同じエージェントを護衛
        if (guardedAgent is not null && IsAlive(guardedAgent) && CurrentGameInfo.LastDeadAgentList.Count == 0)
        {
            guardCandidate = guardedAgent;
        }
        // 新しい護衛先の選定
        else
        {
            // 占い師をカミングアウトしていて，かつ人狼候補になっていないエージェントを探す
            var candidates = AliveOthers.Where(a => ComingoutMap.TryGetValue(a, out var r) && r == Role.SEER && !Werewolves.Contains(a));
            // 見つからなければ霊媒師をカミングアウトしていて，かつ人狼候補になっていないエージェントを探す
            if (!candidates.Any())
            {
                candidates = AliveOthers.Where(a => ComingoutMap.TryGetValue(a, out var r) && r == Role.MEDIUM && !Werewolves.Contains(a));
            }
            // それでも見つからなければ自分と人狼候補以外から護衛
            if (!candidates.Any())
            {
                candidates = AliveOthers.Where(a => !Werewolves.Contains(a));
            }
            // それでもいなければ自分以外から護衛
            if (!candidates.Any())
            {
                candidates = AliveOthers;
            }
            // 護衛候補からランダムに護衛
            guardCandidate = candidates.Shuffle().FirstOrDefault();
        }
        guardedAgent = guardCandidate;
        return guardCandidate;
    }
}
