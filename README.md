# Consensus Voting

Github Pages: https://mdsgoens.github.io/consensus-vote/

## Applications

Voting Method implementations: https://github.com/mdsgoens/consensus-vote/tree/master/src/Consensus/Methods

Compare [Voter-Satisfaction](https://mdsgoens.github.io/consensus-vote/voter-satifaction) between various methods:
```
dotnet run --project ./src/Satisfaction -- [[list of voting method names]]
```

Determine presence of voting strategies:
```
dotnet run --project ./src/Compare -- FindStrategies [[list of voting method names]]
```

Tally votes according to a particular method
```
dotnet run --project ./src/Tally -- [[method name]] "ballots as string"
```

Tally honest votes according to a particular method
```
dotnet run --project ./src/Tally -- [[method name]] voters "voters as string"
```
