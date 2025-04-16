namespace WFC.Models;

/// <summary>
/// Represents a cell in the WFC grid
/// </summary>
public class Cell
{
    private int _lastCount;

    /// <summary>
    /// Possible states for this cell
    /// </summary>
    public HashSet<int> PossibleStates { get; }

    /// <summary>
    /// Whether this cell has collapsed to a single state
    /// </summary>
    public bool Collapsed => PossibleStates.Count == 1;

    /// <summary>
    /// The collapsed state of this cell, or null if not collapsed
    /// </summary>
    public int? CollapsedState => Collapsed ? PossibleStates.First() : null;

    /// <summary>
    /// Entropy of this cell (lower entropy = fewer possible states)
    /// </summary>
    public float Entropy { get; private set; }

    /// <summary>
    /// Create a new cell with all possible states
    /// </summary>
    /// <param name="numberOfStates">Total number of possible states</param>
    public Cell(int numberOfStates)
    {
        PossibleStates = new HashSet<int>(Enumerable.Range(0, numberOfStates));
        UpdateEntropy();
    }

    /// <summary>
    /// Create a new cell with specific possible states
    /// </summary>
    /// <param name="possibleStates">Possible states for this cell</param>
    public Cell(IEnumerable<int> possibleStates)
    {
        PossibleStates = new HashSet<int>(possibleStates);
        UpdateEntropy();
    }

    /// <summary>
    /// Collapse this cell to a specific state
    /// </summary>
    /// <param name="state">State to collapse to</param>
    public void Collapse(int state)
    {
        if (!PossibleStates.Contains(state))
        {
            throw new InvalidOperationException($"Cannot collapse to state {state} as it's not in possible states");
        }

        PossibleStates.Clear();
        PossibleStates.Add(state);
        Entropy = 0; // Fully determined state has zero entropy
    }

    /// <summary>
    /// Constrain possible states based on a predicate
    /// </summary>
    /// <param name="predicate">Predicate to filter states</param>
    /// <returns>True if constraints were applied, false if no changes</returns>
    public bool Constrain(Func<int, bool> predicate)
    {
        int beforeCount = PossibleStates.Count;

        // Find states to remove
        var statesToRemove = PossibleStates.Where(s => !predicate(s)).ToList();

        // Remove invalid states
        foreach (var state in statesToRemove)
        {
            PossibleStates.Remove(state);
        }

        // Update entropy if states changed
        if (PossibleStates.Count != beforeCount)
        {
            UpdateEntropy();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Constrain possible states to a specific set
    /// </summary>
    /// <param name="allowedStates">Allowed states</param>
    /// <returns>True if constraints were applied, false if no changes</returns>
    public bool ConstrainToStates(IEnumerable<int> allowedStates)
    {
        var allowedSet = new HashSet<int>(allowedStates);
        return Constrain(s => allowedSet.Contains(s));
    }

    /// <summary>
    /// Update the entropy of this cell
    /// </summary>
    private void UpdateEntropy()
    {
        int count = PossibleStates.Count;
        
        _lastCount = count;
        
        if (_lastCount == count)
            return; // Skip recalculation if count hasn ’ t changed

        if (count <= 1)
        {
            Entropy = 0; // Fully determined state has zero entropy
        }
        else
        {
            // Use standard information theory entropy: -sum(p*log(p))
            // For equiprobable states, this simplifies to log(n)
            Entropy = (float)Math.Log(count);
        }
    }
}