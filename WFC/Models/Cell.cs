namespace WFC.Models;

public class Cell
{
    public HashSet<int> PossibleStates { get; }
    public bool Collapsed => PossibleStates.Count == 1;
    public int? CollapsedState => Collapsed ? PossibleStates.First() : null;

    // Добавляем свойство энтропии для более точного выбора ячеек
    public float Entropy { get; private set; }

    public Cell(int numberOfStates)
    {
        PossibleStates = new HashSet<int>(Enumerable.Range(0, numberOfStates));
        UpdateEntropy();
    }

    public void Collapse(int state)
    {
        if (!PossibleStates.Contains(state))
        {
            throw new InvalidOperationException($"Cannot collapse to state {state} as it's not in possible states");
        }

        PossibleStates.Clear();
        PossibleStates.Add(state);
        Entropy = 0; // Полностью определенное состояние имеет нулевую энтропию
    }

    private void UpdateEntropy()
    {
        int count = PossibleStates.Count;
        if (count <= 1)
        {
            Entropy = 0; // Полностью определенное состояние имеет нулевую энтропию
        }
        else
        {
            // Используем нормальную энтропию из теории информации: -sum(p*log(p))
            // Для равновероятных состояний это становится log(n)
            Entropy = (float)Math.Log(count);
        }
    }
}