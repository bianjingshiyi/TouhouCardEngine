using System.IO;

namespace TouhouCardEngine
{
    public interface IGameEnvironment
    {
        T runFunc<T>(string script, EffectGlobals globals);
        void runAction(string script, EffectGlobals globals);
        string[] getFiles(string path, string searchPattern);
        TextReader getFileReader(string path);
    }
}