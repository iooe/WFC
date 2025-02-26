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
    
    // Конструктор для создания копии ячейки
    public Cell(Cell other)
    {
        PossibleStates = new HashSet<int>(other.PossibleStates);
        Entropy = other.Entropy;
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

    public bool RemoveState(int state)
    {
        if (Collapsed) return false; // Не удаляем состояния из схлопнутых ячеек
        
        bool removed = PossibleStates.Remove(state);
        if (removed)
        {
            UpdateEntropy();
        }
        return removed;
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
    
    // Метод для восстановления ячейки в несхлопнутое состояние
    public void Reset(int numberOfStates)
    {
        PossibleStates.Clear();
        for (int i = 0; i < numberOfStates; i++)
        {
            PossibleStates.Add(i);
        }
        UpdateEntropy();
    }
}