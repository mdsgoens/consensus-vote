# Consensus Voting

## The Problem

Plurality voting, the choose-one winner-takes-all method used for many elections in the United States, is terrible.

Plurality voting is sometimes also called "First past the Post".

### Center Squeeze[![link](/link.png)](https://electology.org/center-squeeze-effect)
It's terrible for centrists.

A centrist candidate that would beat anyone else in the election in a head-to-head contest can be left by the wayside by two extremists with more polarized bases. This leads to worse leaders, beholden only to their hardline supporters, who have no incentive to compromise while in office -- other than the threat of the office flip-flopping back to an extremist from the other party in the next cycle, who will roll back all their policy. 

### Spoiler Effect[![link](/link.png)](https://electology.org/spoiler-effect)
It's terrible for third-parties.

Candidates without a good chance of winning are better off not running at all under Plurality. Any voter who votes for a third-party is likely wasting their vote, and helping their less-preferred of the frontrunner candidates win; because of this, voters can't even demonstrate support for their most-preferred candidate and third-parties can't point to election results as a barometer of the support they *do* have.

### Lesser of Two Evils
It's *also* terrible for the major parties.

Primaries result in candidate who has high intra-party appeal, not necessarily broad appeal; they can choose candidates who will lose in the general election. Primaries are also expensive - they *double* the expense of running an election, the time spent campaigning, and the times the electorate has to vote, just for the purpose of reducing the amout of choice you have in the general election.

Plurality makes voting a zero-sum game -- a vote not made for one candidate is just as good as a vote for another. This encourages negative campaigning, discourages multiple candidates from within a party contributing their ideas, and forces parties to enact rules that result in only one of their candidates being allowed to run.

## Many Solutions

### Approval Voting[![link](/link.png)](https://electology.org/approval-voting)

In Approval Voting, voters may cast a ballot for as many candidates as they like. The candidate with approval from the most voters wins.

This is an improvement over Plurality voting in all cases, since it means it's always safe to vote for your favorite candidate. This means we get an accurate measure of support for third-party candidates, even when they don't win, and in elections where the third-party candidate is unlikely to win the frontrunners' respective vote totals aren't "spoiled" by the presence of the third-party.

However, in situations where there are more than two frontrunners, Approval voting is exactly as vulnerable to strategic voting as Plurality is -- and in the same way. If I like two of the frontrunners but have a strong preference between the two, I still won't vote for both (even though Approval gives me the opportunity to do so) -- voting for my second-choice frontrunner harms the chances my first-choice frontrunner will win, so I should only vote for my first-choice[![link](/link.png)](https://electology.org/approval-voting-tactics).

Determining whether my first-choice has a chance of winning, and using that information to determine whether or not to vote for my second-choice, is the exact same calculation I would have to make under Plurality. A voting system that seeks to be a meaningful improvement over Plurality in practice must avoid this dilemma.

### Ranked-Choice Voting[![link](/link.png)](https://www.fairvote.org/rcv)
Pros:
* Better than Plurality in all cases
* Less strategic voting

Cons[![link](/link.png)](https://d3n8a8pro7vhmx.cloudfront.net/fairvote/pages/2298/attachments/original/1449512865/ApprovalVotingJuly2011.pdf):
* Still squeezes the middle (the elimination algorithm is terrible)
* Elimination algorithm is arcane and produces counterintuitive results
* Unless paired with porpotional representation, still doesn't encourage third-parties in practice.

## How to evaluate solutions
* Metric to optimize: Voter participation & voter honesty.
* Must be simple enough that voters understand:
  * How to vote.
  * What strategy they should use to optimize their voice (Ideally, "by voting honestly")
  * How to interpret results
  * That the results are "fair"
* How would I personally want to cast my vote?
* Resistance to strategic voting is a primary goal
  * Encourage as much honest voting as possible
  * Minimize regret at casting an honest vote, or regret of casting a suboptimal strategic vote
* Any voting method with academic support is going to select a "reasonable" winner; instead of optimizing for how the winner is selected, optimize for what behaviours the voting system encourages from both candidates and voters.

## The Solution

"Consensus Voting" is a system designed to allow voters to cast ballots as honestly as possible, in a way that Approval voting does not, while also avoiding the traps that come from eliminating candidates in the manner that RCV does.

In Consensus Voting, each voter casts a ranked-choice ballot. Every voter's ballot is then compared, and used to calculate that voter's "most strategic" approval vote possible, given every vote's ranking. The approval votes are tallied, and determine the winner.

### Pros of both!
* Same resistance to strategic voting as RCV
* No spoiler effect

### Cons of neither!
* Still has strategic voting (of course[![link](link.png "Gibbard-Satterthwaite theorem")](https://en.wikipedia.org/wiki/Gibbard%E2%80%93Satterthwaite_theorem)); aims to minimize it.

### The Algorithm

```
sealed class Vote
{
	public Vote(decimal weight, IReadOnlyDictionary<string, int> ranking)
	{
		Weight = weight;
		Ranking = ranking;
	}
  
	public decimal Weight { get; }
	public IReadOnlyDictionary<string, int> Ranking { get; }
	public List<string> Approval { get; } = new List<string>();
}

IEnumerable<string> ConsensusVote(IEnumerable<Vote> votes)
{
	bool madeChanges = true;
	var tally = votes.SelectMany(v => v.Ranking)
		.Select(p => p.Key)
		.Distinct()
		.ToDictionary(c => c, c => 0);

	while (madeChanges)
	{
		madeChanges = false;
    
		// TODO: Does order matter somehow?
		foreach (var candidate in votes.Candidates)
		{
			// Approve of all candidates whose total would be less than that of anyone you like more
			// who isn't themseves being beaten by someone you like less
			foreach(var group in votes
				.Where(v => !v.Approval.Contains(candidate) && v.Ranking.ContainsKey(candidate))
				.GroupBy(v => {
					var highestApprovalForCandidatesLikedLess = v.Ranking
						.Where(p => p.Value > v.Ranking[candidate])
						.Select(p => (decimal?) tally[p.Key])
						.Max();

					return v.Ranking
						.Where(p => p.Value < v.Ranking[candidate])
						.Select(p => tally[p.Key])
						.Where(tally => !highestApprovalForCandidatesLikedLess.HasValue || tally >= highestApprovalForCandidatesLikedLess)
						.Cast<decimal?>()
						.Min();
				})
				.OrderByDescending(gp => gp.Key, new NullIsHighestComparer()))
			{
				var weight = gp.Sum(v => v.Weight);
				if (gp.Key == null || tally[candidate] + weight < gp.Key)
				{
					madeChanges = true;
					tally[candidate] += weight;
					foreach(var vote in gp)
						vote.Approval.Add(candidate);
				}
			}
		}
	}

	int winningTally = tally.Max(p => p.Value);
	return tally
		.Where(p => r.Value == winningTally)
		.Select(p => p.Key);
}
```

## Conclusions

Any jurisdiction would be well-served by switching from Plurality to Approval Voting or RCV; the worst accusation one can levy against either system is that, in their worst cases, they behave exactly like Plurality does. Any jurisdiction that uses a method other than Plurality also raises awareness that voting systems shouldn't be taken for granted and are a thing that can be changed, and gathers data about how the systems work in practice that can be used to implement better systems elsewhere as well.

Consensus Voting improves on both without adding undue complexity to the voting process. I'm certain with input from the community we can refine the algorithm; if you have any feedback, please [open an issue](https://github.com/mdsgoens/Consensus/issues) in this site's Github project.