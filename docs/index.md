# Consensus Voting

## The Problem

Plurality[![link](/link.png)](https://en.wikipedia.org/wiki/Plurality_voting) voting, the choose-one winner-takes-all method used for many elections in the United States, is terrible.

Plurality voting is sometimes also called "First Past the Post".

### Center Squeeze[![link](/link.png)](https://electology.org/center-squeeze-effect)

It's terrible for centrists.

A centrist candidate that would beat anyone else in the election in a head-to-head contest can be left by the wayside by two extremists with more polarized bases. This leads to worse leaders, beholden only to their hardline supporters, who have no incentive to compromise while in office -- other than the threat of the office flip-flopping back to an extremist from the other party in the next cycle, who will roll back all their policy. Instead of a more-stable government with consistent policy between terms, we have a less-stable one wherein the start of each term is dedicated to undoing whatever the last winner did.

### Spoiler Effect[![link](/link.png)](https://electology.org/spoiler-effect)

It's terrible for third-parties.

Candidates without a good chance of winning better serve their constituents by not running at all under Plurality. Any voter who votes for a third-party is likely wasting their vote, and helping their less-preferred of the frontrunner candidates win; because of this, voters can't even demonstrate support for their most-preferred candidate and third-parties can't point to election results as a barometer of the support they *do* have.

### Lesser of Two Evils

It's *also* terrible for the major parties.

[Partisan Primaries](~/partisan-primaries) result in candidate who has high intra-party appeal, not necessarily broad appeal; they can choose candidates who will lose in the general election. Primaries are also expensive - they *double* the expense of running an election, the time spent campaigning, and the times the electorate has to vote, just for the purpose of reducing the amout of choice you have in the general election.

Plurality makes voting a zero-sum game -- a vote not made for one candidate is just as good as a vote for another. This encourages negative campaigning, discourages multiple candidates from within a party contributing their ideas, and forces parties to enact controls preventing multiple of their candidates from competing with one another.

## Many Solutions

### Approval Voting[![link](/link.png)](https://electology.org/approval-voting)

In Approval Voting, voters may cast a ballot for as many candidates as they like. The candidate with approval from the most voters wins.

This is an improvement over Plurality voting in all cases, since it means it's always safe to vote for your favorite candidate. This means we get an accurate measure of support for third-party candidates, even when they don't win, and in elections where the third-party candidate is unlikely to win the frontrunners' respective vote totals aren't "spoiled" by the presence of the third-party.

However, in situations where there are more than two frontrunners, Approval voting is exactly as vulnerable to strategic voting as Plurality is -- and in the same way. If I like two of the frontrunners but have a strong preference between the two, I still won't vote for both (even though Approval gives me the opportunity to do so) -- voting for my second-choice frontrunner harms the chances my first-choice frontrunner will win, so I should only vote for my first-choice[![link](/link.png)](https://electology.org/approval-voting-tactics).

Determining whether my first-choice has a chance of winning, and using that information to determine whether or not to vote for my second-choice, is the exact same calculation I would have to make under Plurality. A voting system that seeks to be a meaningful improvement over Plurality in practice must avoid this dilemma.

Pros:
* Better than Plurality in all cases
* Clone-Proof, eliminating the need for runoffs or primaries
* Encourages positive campaigning
* Allows expressing full first-preference third-party candidate support

Cons:
* Still requires strategic voting based on frontrunner status
* When there are three or more frontrunners, Approval voting becomes a strategic game of chicken: Is it safe to drop support for my second-favorite frontrunner, or will that cause my least-favorite frontrunner to win?
* [[Open Question]]: How often, in practice, does Approval select the "best" of the three frontrunners in contested elections?

### Instant-Runnoff Voting[![link](/link.png)](https://www.fairvote.org/rcv)

(Also called "Ranked Choice Voting" or the "Alternative Vote")

In Instant-Runoff Voting, voters rank all candidates in preference order. Their votes are tallied in rounds; until one candidate gets a majority of first-preference votes, the candidate with the fewest first-preference votes is eliminated and their vote goes to their highest-ranked non-eliminated candidate.

Pros:
* Better than Plurality in all cases
* Clone-Proof, eliminating the need for runoffs or primaries
* Very resistant to strategic voting
* Encourages positive campaigning
* It is now more often than in Plurality safe to vote for your first-preferred third-party candidate over the frontrunners, as your vote will transfer to your preferred frontrunner once the third-party is eliminated.
* Slightly better than Approval in the "Three Frontrunners" scenario in the face of favorite-optimizing strategic voting, because even when it eliminates the "best" of the three frontrunners it will elect the second-best. Approval may not elect *either* the first or second "best" of the three frontrunners, depending on where each candidates' supporters drew their approval threshold.

Cons[![link](/link.png)](https://d3n8a8pro7vhmx.cloudfront.net/fairvote/pages/2298/attachments/original/1449512865/ApprovalVotingJuly2011.pdf):
* Exhibits "Center Squeeze" -- can eliminate a candidate who would beat each other candidate one-on-one early on in the process for lack of first-choice votes
* The elimination algorithm is a bit complicated, and can produce counterintuitive results
* As a single-winner system, does not reveal the full extent of third-party support (since votes stop accumulating once a candidate is eliminated).

### Something Else?

Ideally, we could discover a voting system which maintains the positive aspects of Approval voting and IRV without introducing new flaws.

An [ideal voting system](~/evaluation-philosophy):
* MUST be clone-proof[![link](/link.png)](https://en.wikipedia.org/wiki/Clone_independence) and not exhibit any "Spoiler Effect" or other vulnerability to strategic nomination.
* MUST NOT require multiple rounds of voting.
* MUST NOT have a ["Dark Horse" strategy](~/dark-horse).
* MUST be resolvable[![link](/link.png)](https://en.wikipedia.org/wiki/Resolvability_criterion), SHOULD be summable.
* MUST NOT include randomness or weigh any voter's ballot over another.
* Within the above constraints, SHOULD minimize [complexity](~/complexity) while achieving the best reasonable [voter satisfaction](~/voter-satifaction).

Our preferred system will not be perfect; no voting system can be. So long as it retains a reasonable [voter satisfaction](~/voter-satifaction) profile, in order to reduce complexity or avoid strategic pitfalls a good system MAY:

* Fail to elect a Condorcet or majority winner.
* Violate later-no-harm[![link](/link.png)](https://en.wikipedia.org/wiki/Later-no-harm_criterion)/help[![link](/link.png)](https://en.wikipedia.org/wiki/Later-no-help_criterion).
* Exhibit favorite betrayal.
* Violate any other voting-system criteria not mentioned.

As failing these criteria is harmful only inasmuch as they introduce strategy or reduce voter satisfaction. 

## Conclusions

Any jurisdiction would be well-served by switching from Plurality to Approval Voting or IRV. The worst accusation one can levy against either system is that, in their worst cases, they elect the same candidate Plurality would have -- but **after** having eliminated an expensive primary or runoff process and discouraged negative campaigning.

Any jurisdiction that uses a method other than Plurality also raises awareness that voting systems shouldn't be taken for granted and are a thing that can be changed, and gathers data about how the systems work in practice that can be used to implement better systems elsewhere.