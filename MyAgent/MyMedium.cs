// Copyright (c) 2022 OTSUKI Takashi
// SPDX-License-Identifier: MIT
using AIWolf.Lib;

namespace MyAgent;

/// <summary>
/// 霊媒師役エージェントクラス
/// </summary>
public class MyMedium : MyVillager
{
    int comingoutDay;
    bool isCameout;
    readonly Queue<Judge> identQueue = new();
    readonly Dictionary<Agent, Species> myIdentMap = new();

    /// <inheritdoc/>
    public override void Initialize(IGameInfo gameInfo, IGameSetting gameSetting)
    {
        base.Initialize(gameInfo, gameSetting);
        comingoutDay = new int[] { 1, 2, 3 }.Shuffle().First();
        isCameout = false;
        identQueue.Clear();
        myIdentMap.Clear();
    }

    /// <inheritdoc/>
    public override void DayStart()
    {
        base.DayStart();
        // 霊媒結果を待ち行列に入れる
        if (CurrentGameInfo.MediumResult is not null)
        {
            identQueue.Enqueue(CurrentGameInfo.MediumResult);
            myIdentMap[CurrentGameInfo.MediumResult.Target] = CurrentGameInfo.MediumResult.Result;
        }
    }

    /// <inheritdoc/>
    protected override void ChooseVoteCandidate()
    {
        // 霊媒師をカミングアウトしている他のエージェントは人狼候補
        var fakeMediums = AliveOthers.Where(a => ComingoutMap.TryGetValue(a, out var r) && r == Role.MEDIUM);
        // 自分や殺されたエージェントを人狼と判定，あるいは自分と異なる判定の占い師は人狼候補
        var fakeSeers = DivinationList
            .Where(j => (j.Result == Species.WEREWOLF && (j.Target == Me || IsKilled(j.Target)))
           || (myIdentMap.TryGetValue(j.Target, out var s) && j.Result != s)).Select(j => j.Agent);
        Werewolves.Clear();
        Werewolves.AddRange(fakeMediums.Concat(fakeSeers).Where(a => IsAlive(a)).Distinct());
        if (Werewolves.Count > 0)
        {
            if (!Werewolves.Contains(VoteCandidate))
            {
                VoteCandidate = Werewolves.Shuffle().First();
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
            // 人狼候補がいない場合はランダム
            if (!AliveOthers.Contains(VoteCandidate))
            {
                VoteCandidate = AliveOthers.Shuffle().FirstOrDefault();
            }
        }
    }

    /// <inheritdoc/>
    public override string Talk()
    {
        // カミングアウトする日になったら，あるいは霊媒結果が人狼だったら
        // あるいは霊媒師カミングアウトが出たらカミングアウト
        if (!isCameout && (Day >= comingoutDay
                || (identQueue.Count > 0 && identQueue.Peek().Result == Species.WEREWOLF)
                || IsCo(Role.MEDIUM)))
        {
            TalkQueue.Enqueue(new Content(new ComingoutContentBuilder(Me, Me, Role.MEDIUM)));
            isCameout = true;
        }
        // カミングアウトしたらこれまでの霊媒結果をすべて公開
        if (isCameout)
        {
            while (identQueue.Count > 0)
            {
                var ident = identQueue.Dequeue();
                TalkQueue.Enqueue(new Content(new IdentContentBuilder(Me, ident.Target, ident.Result)));
            }
        }
        return base.Talk();
    }
}
