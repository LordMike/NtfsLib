namespace NTFSLib.Objects
{
    public interface ISaveableObject
    {
        int GetSaveLength();
        void Save(byte[] buffer, int offset);
    }
}
