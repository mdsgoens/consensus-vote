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

### Instant-Runnoff Voting[![link](/link.png)](https://www.fairvote.org/rcv)

(Also called "Ranked Choice Voting" or the "Alternative Vote")

Pros:
* Better than Plurality in all cases
* Resistant to strategic voting

Cons[![link](/link.png)](https://d3n8a8pro7vhmx.cloudfront.net/fairvote/pages/2298/attachments/original/1449512865/ApprovalVotingJuly2011.pdf):
* Still squeezes the middle
* Elimination algorithm is arcane and can produce counterintuitive results
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

"Consensus Voting" is a system designed to allow voters to cast ballots as honestly as possible, in a way that Approval voting does not, while also avoiding the traps that come from eliminating candidates in the manner that IRV does.

In Consensus Voting, each voter casts a ranked-choice ballot. Every voter's ballot is then compared, and used to calculate that voter's "most strategic" approval vote possible. The approval votes are tallied, and determine the winner.

The "most strategic" approval vote for each ranked-choice ballot is determined in rounds, much like in IRV. First, each ballot approves of its first choices. Then we repeat these steps:

1. For each other candidate `c` than the winner `w`, consider each ballot which:
  a. prefers the candidate `c` over the winner `w`, and
  b. does not already approve of `c`.
2. If there exists a non-empty set of candidates `C` who, with the approval of each ballot identified above, now beats the winner `w`, then:
  a. Choose the candidate `c` from that set `C` who requires the *smallest* amount of new approvals in order to beat the winner `w`.
  b. Add each of those approvals to `c`'s official tally.
  c. `c` is now the new winner; repeat from step `1`.
3. If there is no candidate who can beat the current winner `w`, even when every ballot approves of all candidates the ballot ranks higher than the winner, then:
  a. `w` is the final winner, and
  b. We add to the tally for each losing candidate `c` each ballot which prefers `c` over the final winner `w` and does not already approve of `c`. This will never change the outcome -- it is just a show of support.

### Pros
* No spoiler effect, eliminating the need for [partisan primaries])(/partisan-primaries).
* Strategy does not degenerate to Plurality when the frontrunner is unclear.
* Always expresses maximum support possible for all candidates who do not win.
* Often chooses "Consensus" candidates when IRV would not.
* Strong chance of a higher [voter satisfaction](/satisfaction) than either Approval or IRV.

### Cons
* Still has strategic voting (of course[![link](link.png "Gibbard-Satterthwaite theorem")](https://en.wikipedia.org/wiki/Gibbard%E2%80%93Satterthwaite_theorem)); aims to minimize it.
* Suceptible to the [DH3](https://www.rangevoting.org/DH3.html) strategy, but less so than most Condorcet methods.
* More complicated to explain than Approval
* Rounds are harder to visualize than IRV

## Conclusions

Any jurisdiction would be well-served by switching from Plurality to Approval Voting or RCV; the worst accusation one can levy against either system is that, in their worst cases, they behave exactly like Plurality does. Any jurisdiction that uses a method other than Plurality also raises awareness that voting systems shouldn't be taken for granted and are a thing that can be changed, and gathers data about how the systems work in practice that can be used to implement better systems elsewhere as well.

Consensus Voting improves on both without adding undue complexity to the voting process. I'm certain with input from the community we can refine the algorithm; if you have any feedback, please [open an issue](https://github.com/mdsgoens/Consensus/issues) in this site's Github project.