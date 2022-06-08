// Copyright (c) 2022 OTSUKI Takashi
// SPDX-License-Identifier: MIT
using AIWolf.Lib;

namespace MyAgent;

/// <summary>
/// すべての役職のベースとなるクラス
/// </summary>
public class MyBasePlayer : IPlayer
{
    /// <summary>
    /// このエージェント
    /// </summary>
    protected Agent Me { get; private set; }

    /// <summary>
    /// 日付
    /// </summary>
    protected int Day { get; private set; }

    /// <summary>
    /// Talk()できる時間帯か
    /// </summary>
    protected bool CanTalk { get; private set; }

    /// <summary>
    /// Whisper()できる時間帯か
    /// </summary>
    protected bool CanWhisper { get; private set; }

    /// <summary>
    /// 最新のゲーム情報
    /// </summary>
    protected IGameInfo CurrentGameInfo { get; private set; }

    /// <summary>
    /// 自分以外の生存エージェント
    /// </summary>
    protected List<Agent> AliveOthers { get; private set; }

    /// <summary>
    /// 追放されたエージェント
    /// </summary>
    protected List<Agent> ExecutedAgents { get; } = new();

    /// <summary>
    /// 殺されたエージェント
    /// </summary>
    protected List<Agent> KilledAgents { get; } = new();

    /// <summary>
    /// 発言された占い結果報告のリスト
    /// </summary>
    protected List<Judge> DivinationList { get; } = new();

    /// <summary>
    /// 発言された霊媒結果報告のリスト
    /// </summary>
    protected List<Judge> IdentList { get; } = new();

    /// <summary>
    /// 発言用待ち行列
    /// </summary>
    protected Queue<Content> TalkQueue { get; } = new();

    /// <summary>
    /// 囁き用待ち行列
    /// </summary>
    protected Queue<Content> WhisperQueue { get; } = new();

    /// <summary>
    /// 投票先候補
    /// </summary>
    protected Agent VoteCandidate { get; set; }

    /// <summary>
    /// 宣言済み投票先候補
    /// </summary>
    protected Agent DeclaredVoteCandidate { get; set; }

    /// <summary>
    /// 襲撃投票先候補
    /// </summary>
    protected Agent AttackVoteCandidate { get; set; }

    /// <summary>
    /// 宣言済み襲撃投票先候補
    /// </summary>
    protected Agent DeclaredAttackVoteCandidate { get; set; }

    // <summary>
    // カミングアウト状況
    // </summary>
    protected Dictionary<Agent, Role> ComingoutMap { get; } = new();

    /// <summary>
    /// GameInfo.TalkList読み込みのヘッド
    /// </summary>
    int talkListHead;

    /// <summary>
    /// 人間リスト
    /// </summary>
    protected List<Agent> Humans { get; } = new();

    /// <summary>
    /// 人狼リスト
    /// </summary>
    protected List<Agent> Werewolves { get; } = new();

    /// <summary>
    /// エージェントが生きているかどうかを返す
    /// </summary>
    /// <param name="agent">判定対象エージェント</param>
    /// <returns>agentが生きていればTrue</returns>
    protected bool IsAlive(Agent agent) => CurrentGameInfo.StatusMap[agent] == Status.ALIVE;

    /// <summary>
    /// エージェントが殺されたかどうかを返す
    /// </summary>
    /// <param name="agent">判定対象エージェント</param>
    /// <returns>agentが殺されていればTrue</returns>
    protected bool IsKilled(Agent agent) => KilledAgents.Contains(agent);

    /// <summary>
    /// エージェントがカミングアウトしたかどうかを返す
    /// </summary>
    /// <param name="agent">判定対象エージェント</param>
    /// <returns>agentがカミングアウトしていればTrue</returns>
    protected bool IsCo(Agent agent) => ComingoutMap.ContainsKey(agent);

    /// <summary>
    /// 役職がカミングアウトされているかどうかを返す
    /// </summary>
    /// <param name="role">判定対象役職</param>
    /// <returns>roleがカミングアウトされていればTrue</returns>
    protected bool IsCo(Role role) => ComingoutMap.ContainsValue(role);

    /// <summary>
    /// エージェントが人間かどうかを返す
    /// </summary>
    /// <param name="agent">判定対象エージェント</param>
    /// <returns>agentが人間ならばTrue</returns>
    protected bool IsHuman(Agent agent) => Humans.Contains(agent);

    /// <summary>
    /// エージェントが人狼かどうかを返す
    /// </summary>
    /// <param name="agent">判定対象エージェント</param>
    /// <returns>agentが人狼ならばTrue</returns>
    protected bool IsWerewolf(Agent agent) => Werewolves.Contains(agent);

    /// <inheritdoc/>
    public string Name => "MyBasePlayer";

    /// <inheritdoc/>
    public virtual void Initialize(IGameInfo gameInfo, IGameSetting gameSetting)
    {
        Day = -1;
        Me = gameInfo.Agent;
        AliveOthers = new(gameInfo.AliveAgentList.Where(a => a != Me));
        ExecutedAgents.Clear();
        KilledAgents.Clear();
        DivinationList.Clear();
        IdentList.Clear();
        ComingoutMap.Clear();
        Humans.Clear();
        Werewolves.Clear();
    }

    /// <inheritdoc/>
    public virtual void Update(IGameInfo gameInfo)
    {
        CurrentGameInfo = gameInfo;
        // 1日の最初の呼び出しはDayStart()の前なので何もしない
        if (CurrentGameInfo.Day == Day + 1)
        {
            Day = gameInfo.Day;
            return;
        }
        // 2回目の呼び出し以降
        // （夜限定）追放されたエージェントを登録
        AddExecutedAgent(CurrentGameInfo.LatestExecutedAgent);
        // GameInfo.TalkListからカミングアウト，占い報告，霊媒報告を抽出
        for (var i = talkListHead; i < CurrentGameInfo.TalkList.Count; i++)
        {
            var talk = CurrentGameInfo.TalkList[i];
            var talker = talk.Agent;
            if (talker == Me)
            {
                continue;
            }
            var content = new Content(talk.Text);
            switch (content.Topic)
            {
                case Topic.COMINGOUT:
                    ComingoutMap[talker] = content.Role;
                    break;
                case Topic.DIVINED:
                    DivinationList.Add(new Judge(Day, talker, content.Target, content.Result));
                    break;
                case Topic.IDENTIFIED:
                    IdentList.Add(new Judge(Day, talker, content.Target, content.Result));
                    break;
                default:
                    break;
            }
        }
        talkListHead = CurrentGameInfo.TalkList.Count;
    }

    /// <inheritdoc/>
    public virtual void DayStart()
    {
        CanTalk = true;
        CanWhisper = false;
        if (CurrentGameInfo.Role == Role.WEREWOLF)
        {
            CanWhisper = true;
        }
        TalkQueue.Clear();
        WhisperQueue.Clear();
        DeclaredVoteCandidate = null;
        VoteCandidate = null;
        DeclaredAttackVoteCandidate = null;
        AttackVoteCandidate = null;
        talkListHead = 0;
        // 前日に追放されたエージェントを登録
        AddExecutedAgent(CurrentGameInfo.ExecutedAgent);
        // 昨夜死亡した（襲撃された）エージェントを登録
        if (CurrentGameInfo.LastDeadAgentList.Count > 0)
        {
            AddKilledAgent(CurrentGameInfo.LastDeadAgentList[0]);
        }
    }

    /// <summary>
    /// エージェントを追放されたエージェントのリストに追加する
    /// </summary>
    /// <param name="executedAgent">追放されたエージェント</param>
    void AddExecutedAgent(Agent executedAgent)
    {
        if (executedAgent is not null)
        {
            AliveOthers.Remove(executedAgent);
            if (!ExecutedAgents.Contains(executedAgent))
            {
                ExecutedAgents.Add(executedAgent);
            }
        }
    }

    /// <summary>
    /// エージェントを殺されたエージェントのリストに追加する
    /// </summary>
    /// <param name="killedAgent">殺されたエージェント</param>
    void AddKilledAgent(Agent killedAgent)
    {
        if (killedAgent is not null)
        {
            AliveOthers.Remove(killedAgent);
            if (!KilledAgents.Contains(killedAgent))
            {
                KilledAgents.Add(killedAgent);
            }
        }
    }

    /// <summary>
    /// 投票先候補を選びvoteCandidateにセットする
    /// </summary>
    protected virtual void ChooseVoteCandidate()
    {
    }

    /// <inheritdoc/>
    public virtual string Talk()
    {
        ChooseVoteCandidate();
        if (VoteCandidate is not null && VoteCandidate != DeclaredVoteCandidate)
        {
            TalkQueue.Enqueue(new Content(new VoteContentBuilder(Me, VoteCandidate)));
            DeclaredVoteCandidate = VoteCandidate;
        }
        return TalkQueue.Count > 0 ? TalkQueue.Dequeue().Text : AIWolf.Lib.Talk.SKIP;
    }

    /// <summary>
    /// 襲撃先候補を選びattackVoteCandidateにセットする
    /// </summary>
    protected virtual void ChooseAttackVoteCandidate()
    {
    }

    /// <inheritdoc/>
    public virtual string Whisper()
    {
        ChooseAttackVoteCandidate();
        if (AttackVoteCandidate is not null && AttackVoteCandidate != DeclaredAttackVoteCandidate)
        {
            WhisperQueue.Enqueue(new Content(new AttackContentBuilder(Me, AttackVoteCandidate)));
            DeclaredAttackVoteCandidate = AttackVoteCandidate;
        }
        return WhisperQueue.Count > 0 ? WhisperQueue.Dequeue().Text : AIWolf.Lib.Talk.SKIP;
    }

    /// <inheritdoc/>
    public virtual Agent Vote()
    {
        CanTalk = false;
        ChooseVoteCandidate();
        return VoteCandidate;
    }

    /// <inheritdoc/>
    public virtual Agent Attack()
    {
        CanWhisper = false;
        ChooseAttackVoteCandidate();
        CanWhisper = true;
        return AttackVoteCandidate;
    }

    /// <inheritdoc/>
    public virtual Agent Divine() => null;

    /// <inheritdoc/>
    public virtual Agent Guard() => null;

    /// <inheritdoc/>
    public virtual void Finish()
    {
    }
}