namespace RcLibrary.Models;



public class DungeonMetrics
{
    public int Level { get; set; }
    public double Base { get; set; }
    public double Min { get { return Base - 30; } }
    public double Max { get { return Base + 15; } }
}
