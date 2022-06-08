// Copyright (c) 2022 OTSUKI Takashi
// SPDX-License-Identifier: MIT
using AIWolf.Lib;

namespace MyAgent;

/// <summary>
/// 村人役エージェントクラス
/// </summary>
public class MyVillager : MyBasePlayer
{
    /// <inheritdoc/>
    protected override void ChooseVoteCandidate()
    {
        Werewolves.Clear();
        // 自分や殺されたエージェントを人狼と判定していて，生存している占い師を投票先候補とする
        Werewolves.AddRange(DivinationList
            .Where(j => j.Result == Species.WEREWOLF && (j.Target == Me || IsKilled(j.Target)) && IsAlive(j.Agent))
            .Select(j => j.Agent).Distinct());
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
            // 候補がいない場合はランダム
            if (!AliveOthers.Contains(VoteCandidate))
            {
                VoteCandidate = AliveOthers.Shuffle().FirstOrDefault(Agent.NONE);
            }
        }
    }

    /// <inheritdoc/>
    public override string Whisper()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override Agent Attack()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override Agent Divine()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override Agent Guard()
    {
        throw new NotImplementedException();
    }
}
