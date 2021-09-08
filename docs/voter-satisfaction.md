# Satisfaction

"Voter Satisfaction" is a measure of how well a voting system picks the hypothetical "best winner" in simulated elections. It assumes we can simulate elections reasonably representative of real ones, that voters have perfect knowledge of each candidate and their own numeric "utility" values for those candidates, and that we can predict what voting strategies those voters might use. Because of these drawbacks we cannot compare these numbers in absolute terms, and will find a range of values for each voting method depending on initial assumptions. Still, it can provide useful context when comparing the relative ranges of different voting methods.

100 represents an algorithm which magically selects the best candidate every time; 0 represents an algorithm which chooses a candidate at random, effectively the "average" utility amongst all candidates. Between 0 and 100, scores represent the utility for all voters of the candidate chosen as the winner normalized between those two points.

For each voting method, we consider how it responds to voters casting "Honest" ballots, voters casting strategic "Favorite" ballots which attempt to maximize the chances of their favorite candidate winning, and voters casting strategic "Utility" ballots which attempt to maximize the overall utility of the winner (whether it's their favorite or not). For each voting method and strategy, there are a range of values depending on what assumptions one makes about the statistical model for voters and candidates: Whether it's a simple 2d model, whether there are Condorcet cycles, or whether some candidates are clones of each other. We will also compare the "Average" election in our simulation and the 5th percentile of outcomes for that strategy.

## Different Consensus Algorithms

(over 53008 trials)

The "Simple" Consensus algorithm is noticably better than either "Condorcet" variant. It is a bit more resistant to strategy than any other variant, and no variant seems sufficiently better as to justify their extra complexity. Therefore, The "Simple" variant is the best of the Consensus algorithms I have explored here.

### Average Satisfaction
|                    |  Honest     | Favorite    | Utility     |
|--------------------|-------------|-------------|-------------|
| Simple             | 93.3 - 95.9 | 91.7 - 95.1 | 86.6 - 94.8 |
| Saviour            | 93.5 - 96.0 | 88.8 - 94.5 | 82.7 - 93.7 |
| Saviour Difference | 93.6 - 96.1 | 89.6 - 94.9 | 83.8 - 93.9 |
| Beats              | 92.7 - 95.9 | 90.2 - 94.8 | 85.5 - 94.6 |
| Condorcet Simple   | 83.7 - 93.7 | 78.9 - 91.5 | 82.9 - 92.9 |
| Condorcet          | 83.8 - 93.8 | 78.8 - 91.6 | 82.4 - 92.9 |

### 5th Percentile Satisfaction
|                    |  Honest     | Favorite     | Utility      |
|--------------------|-------------|--------------|--------------|
| Simple             | 61.3 - 70.5 |  46.3 - 63.1 |  11.7 - 61.4 |
| Saviour            | 61.9 - 70.1 |  28.6 - 58.9 | -12.0 - 54.0 |
| Saviour Difference | 62.4 - 70.2 |  31.7 - 61.8 |  -6.0 - 55.9 |
| Beats              | 57.0 - 69.4 |  34.7 - 61.3 |   5.4 - 60.1 |
| Condorcet Simple   | 10.4 - 54.3 | -14.8 - 39.6 |  -2.3 - 48.9 |
| Condorcet          | 10.8 - 54.9 | -15.3 - 40.2 |  -4.7 - 49.2 |

## Consensus vs Popular Alternatives

(over 108893 trials)

In this example, we see that Consensus, Approval, Star, and Score voting are all pretty good at selecting optimal candidates -- but even IRV is much better than Plurality.

When comparing the Consensus vote to other popular alternatives, we see that it greatly out-performs Plurality and IRV in almost all cases -- the 5th percentile worst-case of IRV in the face of Utility voting is a bit better than Consensus.

It is a bit more suceptible to strategic voting than Approval voting in the worst cases, but noticably outperforms Approval voting in the "Honest" voting case (even in the 5th percentile of outcomes) and is comparable vs strategic voting on average.

When comparing STAR and Score voting, we notice that STAR's extra anti-strategic-voting step over Store gives it a wider range of worst-case outcomes vs Score -- sometimes it helps, and sometimes it hurts! And on average, STAR performs worse vs honest voting than Score does, while not noticably improving the average result in the face of strategy. From this, I conclude that STAR's extra complexity (which comes with the extra downside of not being clone-independent) is not justified over Score voting; STAR is a theoritical solution to a theoretical problem.

Comparing Consensus to Score voting, we see that Score looks better from a "Satisfation" perspective -- more resistant to strategy **and** slightly better outcomes in the honest case. But the ranges of outcomes between Score and Consensus at least overlap, which they do not between Score and Plurality, Approval, or IRV; and there is reasonable reason to doubt that the theoretical performance of Score voting in this experiment reflects how it would perform in real life. Real-life voters do not know their exact mathematical utility for each candidate, and are probably more likely to vote strategically as a simplification mechanism given the increased complexity of rating candidates into 5 buckets over 3 [citation needed].

Pending further testing, preferably with real-world examples, I would like to say that Consensus voting is superiour to Score (and STAR) voting due to its decreased ballot complexity, equivalent strategic complexity, and similarity of outcome.

### Average

|                |  Honest     | Favorite    | Utility     |
|----------------|-------------|-------------|-------------|
| Plurality      | 56.5 - 89.7 | 56.5 - 89.7 | 78.2 - 91.3 |
| Approval       | 89.2 - 92.4 | 91.9 - 95.7 | 93.2 - 95.4 |
| Consensus      | 93.3 - 95.9 | 91.6 - 95.0 | 86.8 - 94.6 |
| Instant Runoff | 84.6 - 91.5 | 85.4 - 91.6 | 89.6 - 90.4 |
| Score          | 95.6 - 98.0 | 94.1 - 95.9 | 94.2 - 95.4 |
| Star           | 95.6 - 96.5 | 94.5 - 96.7 | 94.6 - 96.5 |

### 5th Percentile

|                |  Honest       | Favorite      | Utility      |
|----------------|---------------|---------------|--------------|
| Plurality      | -122.6 - 30.0 | -122.6 - 30.0 | -21.5 - 38.9 |
| Approval       |   46.2 - 52.5 |   52.8 - 68.7 |  58.7 - 66.3 |
| Consensus      |   61.3 - 71.1 |   44.5 - 63.1 |  13.4 - 60.4 |
| Instant Runoff |   13.2 - 40.2 |   16.5 - 40.3 |  33.7 - 38.1 |
| Score          |   72.6 - 86.6 |   65.8 - 69.1 |  64.9 - 67.9 |
| Star           |   69.3 - 80.6 |   58.3 - 81.9 |  59.0 - 80.3 |
