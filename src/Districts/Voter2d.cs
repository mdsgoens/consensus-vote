using System.Drawing;
using System.Drawing.Imaging;
using Consensus.VoterFactory;

namespace Districts;

public sealed class Voter2d
{
    public Voter2d(VoterFactory source)
    {
        if (source.CandidateCount != 2)
            throw new InvalidOperationException("Must be 2d.");
        m_x = source[0];
        m_y = source[1];
    }

    public static void ToImage(IReadOnlyList<Voter2d> source, int maxDimension, string fileName, Func<IEnumerable<Voter2d>, (int R, int G, int B)> getColor)
    {
        var minX = source.Min(s => s.m_x);
        var maxX = source.Max(s => s.m_x);
        var minY = source.Min(s => s.m_y);
        var maxY = source.Max(s => s.m_y);

        var scale = Math.Max(maxX - minX, maxY - minY) / maxDimension;
        var (width, height) = Scale(maxX, maxY);

        var buckets = source.GroupBy(v => Scale(v.m_x, v.m_y))
            .Select(gp => (gp.Key, Color: getColor(gp)))
            .ToList();

        var rScale = Math.Max(buckets.Max(b => b.Color.R) / 255d, .0001);
        var gScale = Math.Max(buckets.Max(b => b.Color.G) / 255d, .0001);
        var bScale = Math.Max(buckets.Max(b => b.Color.B) / 255d, .0001);

#pragma warning disable CA1416
        Bitmap pic = new Bitmap(width + 1, height + 1, PixelFormat.Format32bppArgb);

        foreach (var gp in buckets)
        {
            pic.SetPixel(gp.Key.X, gp.Key.Y, Color.FromArgb(
                255 - (int)(gp.Color.R / rScale),
                255 - (int)(gp.Color.G / gScale),
                255 - (int)(gp.Color.B / bScale)
            ));
        }

        pic.Save(fileName + ".bmp");
#pragma warning restore CA1416

        (int X, int Y) Scale(double x, double y)
        {
            return (
                Math.Min(maxDimension - 1, (int)((x - minX) / scale)),
                Math.Min(maxDimension - 1, (int) ((y - minY) / scale)));
        }
    }


    private readonly double m_x;
    private readonly double m_y;
}