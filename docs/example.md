# An Example

Ranked Consensus Rounds Simple (here called "Consensus Voting") is a system designed to allow voters to cast ballots as honestly as possible, in a way that Approval voting does not, while also avoiding the traps that come from eliminating candidates in the manner that IRV does.

It is unfortunately susceptible to a ["Dark Horse](/dark-horse) strategy, and so I cannot recommend it over IRV until this is proven to not be a problem or we tweak the algorithm so it either (a) no longer rewards lying about "consensus" candidates, or (b) penalizes one's preferred candidates in reasonable porportion, to make the strategy unprofitable.

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
  
## The Situation

![Tennessee map for voting example](https://upload.wikimedia.org/wikipedia/commons/thumb/8/88/Tennessee_map_for_voting_example.svg/750px-Tennessee_map_for_voting_example.svg.png)

Imagine that Tennessee is having an election on the location of its capital. The population of Tennessee is concentrated around its four major cities, which are spread throughout the state. For this example, suppose that the entire electorate lives in these four cities and that everyone wants to live as near to the capital as possible.

The candidates for the capital are:

* Memphis, the state's largest city, with 42% of the voters, but located far from the other cities
* Nashville, with 26% of the voters, near the center of the state
* Knoxville, with 17% of the voters
* Chattanooga, with 15% of the voters

The preferences of the voters would be divided like this:

| | 42% of voters (close to Memphis) | 26% of voters (close to Nashville) | 15% of voters (close to Chattanooga) | 17% of voters (close to Knoxville) |
|---|---|---|---|---|
| 1 | Memphis     | Nashville   | Chattanooga | Knoxville   |
| 2 | Nashville   | Chattanooga | Knoxville   | Chattanooga |
| 3 | Chattanooga | Knoxville   | Nashville   | Nashville   |
| 4 | Knoxville   | Memphis     | Memphis     | Memphis     |

## Tally

First, sum the first-place votes for each candidate:

| City        | Votes |
| Memphis     |   42% |
| Nashville   |   26% |
| Knoxville   |   17% |
| Chattanooga |   15% |

(Note that absolute counts of votes can be used, or percentages of the total number of votes; it makes no difference since it is the ratio of votes between two candidates that matters.)

This leaves Memphis in the lead. For each other city, we calculate (a) how many voters prefer that city over Memphis but have not listed that city first, and (b) how many votes that city would have if it receieved those voters' support:

| City        | New Votes | Total Votes |
| Memphis     |        0% |         42% |
| Nashville   |       32% |         58% |
| Knoxville   |       41% |         58% |
| Chattanooga |       43% |         58% |

Since all the other cities rank Memphis last, they each would be able to have more votes than Memphis. Therefore, we choose the city with the smallest amount of new votes and include those votes in its tally. In this case, Nashville requires only 32% of voters and wins the second round.

| City        | Votes |
| Nashville   |   58% |
| Memphis     |   42% |
| Knoxville   |   17% |
| Chattanooga |   15% |

In the third round, we now calculate for each city how many voters prefer that city to Nashville but do not already support it.

| City        | New Votes | Total Votes |
| Nashville   |        0% |         58% |
| Memphis     |        0% |         42% |
| Knoxville   |       15% |         32% |
| Chattanooga |       17% |         32% |

No city can get more support than Nashville's 58%, and so Nashville wins. Knoxville and Chattanooga each retain their 32% in the final tally.

## Summary

In the example election, the winner is Nashville.

Using the First-past-the-post voting and some other systems, Memphis would have won the election by having the most people, even though Nashville won every simulated pairwise election outright. Using Instant-Runoff voting in this example would result in Knoxville winning even though more people preferred Nashville over Knoxville.

Note that the two "third party" candidates -- Chattanooga and Knoxville -- ended with a respectable tally of votes, even though they had the lowest first-place counts. This feature allows third-parties to demonstrate their level of support even when they do not win.