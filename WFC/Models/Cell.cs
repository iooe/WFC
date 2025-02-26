namespace WFC.Models;
public class Cell
{
    public HashSet<int> PossibleStates { get; }
    public bool Collapsed => PossibleStates.Count == 1;
    public int? CollapsedState => Collapsed ? PossibleStates.First() : null;

    public Cell(int numberOfStates)
    {
        PossibleStates = new HashSet<int>(Enumerable.Range(0, numberOfStates));
    }

    public void Collapse(int state)
    {
        PossibleStates.Clear();
        PossibleStates.Add(state);
    }

    public bool RemoveState(int state)
    {
        return PossibleStates.Remove(state);
    }
}