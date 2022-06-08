// Copyright (c) 2022 OTSUKI Takashi
// SPDX-License-Identifier: MIT
using AIWolf.Lib;

namespace MyAgent;

/// <summary>
/// 人狼役エージェントクラス
/// </summary>
public class MyWerewolf : MyBasePlayer
{
    // 規定人狼数
    int numWolves;
    // 騙る役職
    Role fakeRole;
    // カミングアウトする日
    int comingoutDay;
    // カミングアウトするターン
    int comingoutTurn;
    // カミングアウト済みか
    bool isCameout;

    // 偽判定マップ
    readonly Dictionary<Agent, Species> fakeJudgeMap = new();

    // 未公表偽判定の待ち行列
    readonly Queue<Judge> fakeJudgeQueue = new();
    // 裏切り者リスト
    readonly List<Agent> possessedList = new();

    // 村人リスト
    readonly List<Agent> villagers = new();
    // talk()のターン
    int talkTurn;

    /// <inheritdoc/>
    public override void Initialize(IGameInfo gameInfo, IGameSetting gameSetting)
    {
        base.Initialize(gameInfo, gameSetting);
        numWolves = gameSetting.RoleNumMap[Role.WEREWOLF];
        Werewolves.Clear();
        Werewolves.AddRange(gameInfo.RoleMap.Keys);
        Humans.Clear();
        Humans.AddRange(AliveOthers.Where(a => !Werewolves.Contains(a)));
        // ランダムに騙る役職を決める
        fakeRole = new Role[] { Role.VILLAGER, Role.SEER, Role.MEDIUM }.
            Where(r => gameInfo.ExistingRoleList.Contains(r)).Shuffle().First();
        // 1～3日目からランダムにカミングアウトする
        comingoutDay = new int[] { 1, 2, 3 }.Shuffle().First();
        // 第0～4ターンからランダムにカミングアウトする
        comingoutTurn = new int[] { 0, 1, 2, 3, 4 }.Shuffle().First();
        isCameout = false;
        fakeJudgeMap.Clear();
        fakeJudgeQueue.Clear();
        possessedList.Clear();
    }

    /// <inheritdoc/>
    public override void Update(IGameInfo gameInfo)
    {
        base.Update(gameInfo);
        // 占い/霊媒結果が嘘の場合，裏切り者候補
        possessedList.Clear();
        possessedList.AddRange(DivinationList.Concat(IdentList)
            .Where(j => !Werewolves.Contains(j.Agent)
            && ((Humans.Contains(j.Target) && j.Result == Species.WEREWOLF)
            || (Werewolves.Contains(j.Target) && j.Result == Species.HUMAN)))
            .Select(j => j.Agent).Distinct());
        foreach (var a in possessedList)
        {
            WhisperQueue.Enqueue(new Content(new EstimateContentBuilder(Me, a, Role.POSSESSED)));
        }
        villagers.Clear();
        villagers.AddRange(AliveOthers.Where(a => !Werewolves.Contains(a) && !possessedList.Contains(a)));
    }

    Judge GetFakeJudge(Role fakeRole)
    {
        Agent target = null;
        // 占い師騙りの場合
        if (fakeRole == Role.SEER)
        {
            target = AliveOthers.Where(a => !fakeJudgeMap.ContainsKey(a) && ComingoutMap.TryGetValue(a, out var r) && r != Role.SEER)
                .Shuffle().FirstOrDefault();
            if (target == null)
            {
                target = AliveOthers.Shuffle().FirstOrDefault();
            }
        }
        // 霊媒師騙りの場合
        else if (fakeRole == Role.MEDIUM)
        {
            target = CurrentGameInfo.ExecutedAgent;
        }
        if (target is not null)
        {
            var result = Species.HUMAN;
            // 人間が偽占い対象の場合
            if (Humans.Contains(target))
            {
                // 偽人狼に余裕があれば
                if (fakeJudgeMap.Where(p => p.Value == Species.WEREWOLF).Count() < numWolves)
                {
                    // 裏切り者，あるいはまだカミングアウトしていないエージェントの場合，判定は五分五分
                    if (possessedList.Contains(target) || !IsCo(target))
                    {
                        if (new Random().NextDouble() < 0.5)
                        {
                            result = Species.WEREWOLF;
                        }
                    }
                    // それ以外は人狼判定
                    else
                    {
                        result = Species.WEREWOLF;
                    }
                }
            }
            return new Judge(Day, Me, target, result);
        }
        return null;
    }

    /// <inheritdoc/>
    public override void DayStart()
    {
        base.DayStart();
        talkTurn = -1;
        if (Day == 0)
        {
            WhisperQueue.Enqueue(new Content(new ComingoutContentBuilder(Me, Me, fakeRole)));
        }
        // 偽の判定
        else
        {
            var judge = GetFakeJudge(fakeRole);
            if (judge is not null)
            {
                fakeJudgeQueue.Enqueue(judge);
                fakeJudgeMap[judge.Target] = judge.Result;
            }
        }
    }

    /// <inheritdoc/>
    protected override void ChooseVoteCandidate()
    {
        List<Agent> candidates = new();
        // 占い師/霊媒師騙りの場合
        if (fakeRole != Role.VILLAGER)
        {
            // 対抗カミングアウトした，あるいは人狼と判定した村人は投票先候補
            candidates.AddRange(villagers.Where(a => ComingoutMap.TryGetValue(a, out var r) && r == fakeRole
            || fakeJudgeMap.TryGetValue(a, out var s) && s == Species.WEREWOLF));
            // 候補がいなければ人間と判定していない村人陣営から
            if (candidates.Count == 0)
            {
                candidates.AddRange(villagers.Where(a => fakeJudgeMap.TryGetValue(a, out var s) && s != Species.HUMAN));
            }
        }
        // 村人騙り，あるいは候補がいない場合ば村人陣営から選ぶ
        if (candidates.Count == 0)
        {
            candidates.AddRange(villagers);
        }
        // それでも候補がいない場合は裏切り者に投票
        if (candidates.Count == 0)
        {
            candidates.AddRange(possessedList);
        }
        if (candidates.Count > 0)
        {
            if (!candidates.Contains(VoteCandidate))
            {
                VoteCandidate = candidates.Shuffle().First();
                if (CanTalk)
                {
                    TalkQueue.Enqueue(new Content(new EstimateContentBuilder(Me, VoteCandidate, Role.WEREWOLF)));
                }
            }
        }
        else
        {
            VoteCandidate = null;
        }
    }

    /// <inheritdoc/>
    public override string Talk()
    {
        talkTurn++;
        if (fakeRole != Role.VILLAGER)
        {
            if (!isCameout)
            {
                // 他の人狼のカミングアウト状況を調べて騙る役職が重複しないようにする
                var hasFakeSeerCO = Werewolves.Where(a => a != Me && ComingoutMap.TryGetValue(a, out var r) && r == Role.SEER).Any();
                var hasFakeMediumCO = Werewolves.Where(a => a != Me && ComingoutMap.TryGetValue(a, out var r) && r == Role.MEDIUM).Any();
                if ((fakeRole == Role.SEER && hasFakeSeerCO) || (fakeRole == Role.MEDIUM && hasFakeMediumCO))
                {
                    fakeRole = Role.VILLAGER; // 潜伏人狼
                    WhisperQueue.Enqueue(new Content(new ComingoutContentBuilder(Me, Me, Role.VILLAGER)));
                }
                else
                {
                    // 対抗カミングアウトがある場合，今日カミングアウトする
                    if (Humans.Where(a => ComingoutMap.TryGetValue(a, out var r) && r == fakeRole).Any())
                    {
                        comingoutDay = Day;
                    }
                    // カミングアウトするタイミングになったらカミングアウト
                    if (Day >= comingoutDay && talkTurn >= comingoutTurn)
                    {
                        isCameout = true;
                        TalkQueue.Enqueue(new Content(new ComingoutContentBuilder(Me, Me, fakeRole)));
                    }
                }
            }
            // カミングアウトしたらこれまでの偽判定結果をすべて公開
            else
            {
                while (fakeJudgeQueue.Count > 0)
                {
                    var judge = fakeJudgeQueue.Dequeue();
                    if (fakeRole == Role.SEER)
                    {
                        TalkQueue.Enqueue(new Content(new DivinedResultContentBuilder(Me, judge.Target, judge.Result)));
                    }
                    else if (fakeRole == Role.MEDIUM)
                    {
                        TalkQueue.Enqueue(new Content(new IdentContentBuilder(Me, judge.Target, judge.Result)));
                    }
                }
            }
        }
        return base.Talk();
    }

    /// <inheritdoc/>
    protected override void ChooseAttackVoteCandidate()
    {
        // カミングアウトした村人陣営は襲撃先候補
        var candidates = villagers.Where(a => IsCo(a)).ToList();
        // 候補がいなければ村人陣営から
        if (candidates.Count == 0)
        {
            candidates = new List<Agent>(villagers);
        }
        // 村人陣営がいない場合は裏切り者を襲う
        if (candidates.Count == 0)
        {
            candidates = new List<Agent>(possessedList);
        }
        if (candidates.Count > 0)
        {
            AttackVoteCandidate = candidates.Shuffle().First();
        }
        else
        {
            AttackVoteCandidate = null;
        }
    }
}
