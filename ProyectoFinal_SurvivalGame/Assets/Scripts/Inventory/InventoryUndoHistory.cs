using System.Collections.Generic;

internal sealed class InventoryUndoHistory
{
    private readonly int _maxEntries;
    private readonly Stack<InventoryState> _states = new Stack<InventoryState>();

    public InventoryUndoHistory(int maxEntries)
    {
        _maxEntries = maxEntries > 0 ? maxEntries : 1;
    }

    public int Count => _states.Count;

    public void Push(InventoryState state)
    {
        if (state == null)
        {
            return;
        }

        _states.Push(state);
        TrimOldestEntries();
    }

    public bool TryPop(out InventoryState state)
    {
        if (_states.Count == 0)
        {
            state = null;
            return false;
        }

        state = _states.Pop();
        return true;
    }

    private void TrimOldestEntries()
    {
        if (_states.Count <= _maxEntries)
        {
            return;
        }

        InventoryState[] newestToOldest = _states.ToArray();
        _states.Clear();

        int keptCount = _maxEntries < newestToOldest.Length ? _maxEntries : newestToOldest.Length;
        for (int i = keptCount - 1; i >= 0; i--)
        {
            _states.Push(newestToOldest[i]);
        }
    }
}
