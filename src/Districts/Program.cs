using Consensus.VoterFactory;
using Consensus;
using Districts;

var seed = new Random().Next();
Console.WriteLine($"Seed: {seed}");
var random = new Random(seed);

var districts = Electorate.Normal(2, random)
    .PolyaModel(random, 2)
    .Skip(100)
    .GetDistricts(10)
    .SelectToArray(d => {
        var (candidates, voters) = d.Take(5, 10000);
        return (
            candidates,
            voters: voters.SelectToArray(v => new Voter2d(v)),
            Votes: voters.SelectToArray(v => v.ProximityTo(candidates)));
    });

int i = 0;
foreach (var d in districts)
{
    i++;
    Voter2d.ToImage(d.voters, 16, "img" + i, v => (v.Count(), v.Count(), v.Count()));
}