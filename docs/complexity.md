# Complexity

At least three kinds of complexity:
* Ballot complexity
  * Number of possible ballots for `n` candidates
    * Plurality: `O(n)`
    * Approval: `O(2^n)`
    * Candinal systems with `x` categories: `O(x^n)`
    * Ranked: `O(n!)`
  * More options means more chances for confusion or mistakes
  * More options means we are asking the voter to make un-necessary decicions our algorithm may not consider
  * More options means we are increasing the amount of channels voters can use to "lie" and vote strategically, without necessarily increasing the number of channels from which we can extract useful signal.
* Strategic complexity
  * How do I, as a voter, know what ballot to cast?
  * Uncertainty => anxiety => Lack of trust in the outcome
  * Can cause systems to devolve into a simpler one under strategic incentives, meaning we should just have done the simpler one to start with
  * Cardinal [![link](/link.png)](https://en.wikipedia.org/wiki/Cardinal_voting) systems which treat the values as "Scores" are particularly hard, as knowing the relative utility of each candidate more accurately than "yes" or "no" is very hard in practice
* Tallying complexity
  * I MUST be able to explain the tallying algorithm to every citizen so that they trust it
    * Including those who don't like math
    * Including those looking for any excuse why their favorite candidate didn't win
    * Including those with no background in election-science wondering why things are one way, but not another
  * Ideally, can be performed "on the back of a napkin" or "in your head"
    * To be able to clearly demonstrate to skeptics why the outcome was correct
    * To be able to easily coordinate the official tally between different precincts
    * So that voters can have some confidence what effect each of the decicisions they make when casting the ballot will have
  * Ideally, results in a tally with "concrete meaning" which can also be compared between elections
    * e.g., "number of supporters" is concrete
    * e.g., "average rating" is not concrete
  * Ideally, can be explained in a sentence or two at the top of each ballot