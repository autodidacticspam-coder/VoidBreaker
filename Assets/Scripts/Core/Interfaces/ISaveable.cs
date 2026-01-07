namespace VoidBreaker.Core
{
    /// <summary>
    /// Interface for objects that can be saved/loaded
    /// </summary>
    public interface ISaveable
    {
        string SaveKey { get; }
        object GetSaveData();
        void LoadSaveData(object data);
    }
}
