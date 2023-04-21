using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public interface IDefineNode<TDefine>
    {
        TDefine define { get; set; }
        void Define();
    }
    public interface INodeDefiner
    {
        void Define(Node node);
    }
}
