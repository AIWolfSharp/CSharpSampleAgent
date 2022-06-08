// Copyright (c) 2022 OTSUKI Takashi
// SPDX-License-Identifier: MIT
using AIWolf.Lib;

namespace MyAgent;

/// <summary>
/// 裏切り者役エージェントクラス
/// </summary>
public class MyPossessed : MyVillager
{
    int numWolves;
    bool isCameout;
    readonly List<Judge> fakeDivinationList = new();
    readonly Queue<Judge> fakeDivinationQueue = new();
    readonly List<Agent> divinedAgents = new();

    /// <inheritdoc/>
    public override void Initialize(IGameInfo gameInfo, IGameSetting gameSetting)
    {
        base.Initialize(gameInfo, gameSetting);
        numWolves = gameSetting.RoleNumMap[Role.WEREWOLF];
        isCameout = false;
        fakeDivinationList.Clear();
        fakeDivinationQueue.Clear();
        divinedAgents.Clear();
    }

    Judge GetFakeDivination()
    {
        Agent target = null;
        var candidates = AliveOthers.Where(a => !divinedAgents.Contains(a) && ComingoutMap.TryGetValue(a, out var r) && r != Role.SEER);
        if (candidates.Any())
        {
            target = candidates.Shuffle().First();
        }
        else
        {
            target = AliveOthers.Shuffle().FirstOrDefault();
        }
        if (target is not null)
        {
            // 偽人狼に余裕があれば，人狼と人間の割合を勘案して，30%の確率で人狼と判定
            var result = Species.HUMAN;
            if (fakeDivinationList.Where(j => j.Result == Species.WEREWOLF).Count() < numWolves && new Random().NextDouble() < 0.3)
            {
                result = Species.WEREWOLF;
            }
            return new Judge(Day, Me, target, result);
        }
        return null;
    }

    /// <inheritdoc/>
    public override void DayStart()
    {
        base.DayStart();
        // 偽の判定
        if (Day > 0)
        {
            var judge = GetFakeDivination();
            if (judge is not null)
            {
                fakeDivinationList.Add(judge);
                fakeDivinationQueue.Enqueue(judge);
                divinedAgents.Add(judge.Target);
            }
        }
    }

    /// <inheritdoc/>
    protected override void ChooseVoteCandidate()
    {
        // 自分や殺されたエージェントを人狼と判定していて，生存している占い師は人狼候補
        Werewolves.Clear();
        Werewolves.AddRange(DivinationList
            .Where(j => j.Result == Species.WEREWOLF && (j.Target == Me || IsKilled(j.Target))).Select(j => j.Agent).Distinct());
        // 対抗カミングアウトのエージェントは投票先候補
        var rivals = AliveOthers.Where(a => !Werewolves.Contains(a) && ComingoutMap.TryGetValue(a, out var r) && r == Role.SEER);
        // 人狼と判定したエージェントは投票先候補
        var fakeHumans = fakeDivinationQueue.Where(j => j.Result == Species.HUMAN).Select(j => j.Target).Distinct();
        var fakeWerewolves = fakeDivinationQueue.Where(j => j.Result == Species.WEREWOLF).Select(j => j.Target).Distinct();
        var candidates = rivals.Concat(fakeWerewolves).Distinct();
        // 候補がいなければ人間と判定していない村人陣営から
        if (!candidates.Any())
        {
            candidates = AliveOthers.Where(a => !Werewolves.Contains(a) && !fakeHumans.Contains(a));
            // それでも候補がいなければ村人陣営から
            if (!candidates.Any())
            {
                candidates = AliveOthers.Where(a => !Werewolves.Contains(a));
            }
        }
        if (candidates.Any())
        {
            if (!candidates.Contains(VoteCandidate))
            {
                VoteCandidate = candidates.Shuffle().First();
                // 以前の投票先から変わる場合，新たに推測発言と占い要請をする
                if (CanTalk)
                {
                    TalkQueue.Enqueue(new Content(new EstimateContentBuilder(Me, VoteCandidate, Role.WEREWOLF)));
                    TalkQueue.Enqueue(new Content(new RequestContentBuilder(Me, Agent.ANY, new Content(new DivinationContentBuilder(Agent.NONE, VoteCandidate)))));
                }
            }
        }
        else
        {
            // 候補がいない場合はランダム
            if (!AliveOthers.Contains(VoteCandidate))
            {
                VoteCandidate = AliveOthers.Shuffle().FirstOrDefault();
            }
        }
    }

    /// <inheritdoc/>
    public override string Talk()
    {
        // 即占い師カミングアウト
        if (!isCameout)
        {
            TalkQueue.Enqueue(new Content(new ComingoutContentBuilder(Me, Me, Role.SEER)));
            isCameout = true;
        }
        // カミングアウトしたらこれまでの偽判定結果をすべて公開
        if (isCameout)
        {
            while (fakeDivinationQueue.Count > 0)
            {
                var judge = fakeDivinationQueue.Dequeue();
                TalkQueue.Enqueue(new Content(new DivinedResultContentBuilder(Me, judge.Target, judge.Result)));
            }
        }
        return base.Talk();
    }
}
