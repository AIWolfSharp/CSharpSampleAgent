// Copyright (c) 2022 OTSUKI Takashi
// SPDX-License-Identifier: MIT
using AIWolf.Lib;

namespace MyAgent;

/// <summary>
/// 占い師役エージェントクラス
/// </summary>
public class MySeer : MyVillager
{
    int comingoutDay;
    bool isCameout;
    readonly Queue<Judge> divinationQueue = new();
    readonly Dictionary<Agent, Species> myDivinationMap = new();
    readonly List<Agent> whiteList = new();
    readonly List<Agent> blackList = new();
    List<Agent> grayList;
    readonly List<Agent> semiWolves = new();
    readonly List<Agent> possessedList = new();

    /// <inheritdoc/>
    public override void Initialize(IGameInfo gameInfo, IGameSetting gameSetting)
    {
        base.Initialize(gameInfo, gameSetting);
        comingoutDay = new int[] { 1, 2, 3 }.Shuffle().First();
        isCameout = false;
        divinationQueue.Clear();
        myDivinationMap.Clear();
        whiteList.Clear();
        blackList.Clear();
        grayList = new List<Agent>(AliveOthers);
        semiWolves.Clear();
        possessedList.Clear();
    }

    /// <inheritdoc/>
    public override void DayStart()
    {
        base.DayStart();
        // 占い結果を待ち行列に入れる
        var divination = CurrentGameInfo.DivineResult;
        if (divination is not null)
        {
            divinationQueue.Enqueue(divination);
            grayList.Remove(divination.Target);
            if (divination.Result == Species.HUMAN)
            {
                whiteList.Add(divination.Target);
            }
            else
            {
                blackList.Add(divination.Target);
            }
            myDivinationMap[divination.Target] = divination.Result;
        }
    }

    /// <inheritdoc/>
    protected override void ChooseVoteCandidate()
    {
        // 生存人狼がいれば当然投票
        var aliveWolves = blackList.Where(a => IsAlive(a));
        if (aliveWolves.Any())
        {
            // 既定の投票先が生存人狼でない場合投票先を変える
            if (!aliveWolves.Contains(VoteCandidate))
            {
                VoteCandidate = aliveWolves.Shuffle().First();
                if (CanTalk)
                {
                    TalkQueue.Enqueue(new Content(new RequestContentBuilder(Me, Agent.ANY, new Content(new VoteContentBuilder(Agent.NONE, VoteCandidate)))));
                }
            }
            return;
        }
        // 確定人狼がいない場合は推測する
        // 偽占い師
        var fakeSeers = AliveOthers.Where(a => ComingoutMap.TryGetValue(a, out var r) && r == Role.SEER);
        // 偽霊媒師
        var fakeMediums = IdentList.Where(j => myDivinationMap.TryGetValue(j.Target, out var s) && j.Result != s).Select(j => j.Agent);
        Werewolves.Clear();
        Werewolves.AddRange(fakeSeers.Concat(fakeMediums).Where(a => IsAlive(a)).Distinct());
        possessedList.Clear();
        // 人狼候補なのに人間⇒裏切り者
        foreach (Agent possessed in Werewolves.Where(a => whiteList.Contains(a)))
        {
            if (!possessedList.Contains(possessed))
            {
                TalkQueue.Enqueue(new Content(new EstimateContentBuilder(Me, possessed, Role.POSSESSED)));
                possessedList.Add(possessed);
            }
        }
        semiWolves.Clear();
        semiWolves.AddRange(Werewolves.Where(a => !whiteList.Contains(a)));
        if (semiWolves.Count > 0)
        {
            if (!semiWolves.Contains(VoteCandidate))
            {
                VoteCandidate = semiWolves.Shuffle().First();
                // 以前の投票先から変わる場合，新たに推測発言をする
                if (CanTalk)
                {
                    TalkQueue.Enqueue(new Content(new EstimateContentBuilder(Me, VoteCandidate, Role.WEREWOLF)));
                }
            }
        }
        // 人狼候補がいない場合はグレイからランダム
        else
        {
            if (grayList.Count > 0)
            {
                if (!grayList.Contains(VoteCandidate))
                {
                    VoteCandidate = grayList.Shuffle().First();
                }
            }
            // グレイもいない場合ランダム
            else
            {
                if (!AliveOthers.Contains(VoteCandidate))
                {
                    VoteCandidate = AliveOthers.Shuffle().FirstOrDefault();
                }
            }
        }
    }

    /// <inheritdoc/>
    public override string Talk()
    {
        // カミングアウトする日になったら，あるいは占い結果が人狼だったら
        // あるいは占い師カミングアウトが出たらカミングアウト
        if (!isCameout && (Day >= comingoutDay
                || (divinationQueue.Count > 0 && divinationQueue.Peek().Result == Species.WEREWOLF)
                || IsCo(Role.SEER)))
        {
            TalkQueue.Enqueue(new Content(new ComingoutContentBuilder(Me, Me, Role.SEER)));
            isCameout = true;
        }
        // カミングアウトしたらこれまでの占い結果をすべて公開
        if (isCameout)
        {
            while (divinationQueue.Count > 0)
            {
                var divination = divinationQueue.Dequeue();
                TalkQueue.Enqueue(new Content(new DivinedResultContentBuilder(Me, divination.Target, divination.Result)));
            }
        }
        return base.Talk();
    }

    /// <inheritdoc/>
    public override Agent Divine()
    {
        // 人狼候補がいればそれらからランダムに占う
        if (semiWolves.Count > 0)
        {
            return semiWolves.Shuffle().First();
        }
        // 人狼候補がいない場合，まだ占っていない生存者からランダムに占う
        return AliveOthers.Where(a => !myDivinationMap.ContainsKey(a)).Shuffle().FirstOrDefault();
    }
}
