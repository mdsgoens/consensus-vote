---
title: FAQ - Consensus Voting
---

FAQ
=====

## 1. Isn't Consensus Voting just Bucklin Voting[1]?

In Bucklin voting, *all* second-choice votes are added to the tally if the first-choices are insufficient to elect a majority winner. Consensus voting differs in three ways:

1. It only adds votes to the tally when necessary to prevent the election of someone the voter likes less.
2. It is more resistant to "burying" because it is independent of clones one could use to "pad" the choices between one's first and last preferences.
3. It allows voters to rank multiple first-choice preferences, choosing the "best" majority winner when multiple candidates have majority support.

## 2. How about Majority Approval Voting[2]?

Majority Approval Voting still adds *all* second-choice candidates to the tally at the same time until a majority is achieved. It is still vulnerable to burying because it has more than three levels -- strategic voters are incentivised to *use* only three levels (the top level, the two bottom levels), and all voters who use more than that are disadvantaged.

The Consensus Vote solves the strategic problem by only *allowing* three levels, then compensates for the reduction in signal with a more sophisticated method of determining when the middle level counts as an approval.

[1]:https://electology.org/bucklin-voting
[2]:https://electowiki.org/wiki/Majority_Approval_Voting