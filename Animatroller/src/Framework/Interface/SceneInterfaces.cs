using Expander = Animatroller.Framework.Expander;

namespace Animatroller.Framework
{
    public interface ISceneRequiresIOExpander
    {
        void WireUp(Expander.IOExpander port);
    }

    public interface ISceneRequiresMidiInput
    {
        void WireUp(Expander.MidiInput port);
    }

    public interface ISceneRequiresDMXPro
    {
        void WireUp(Expander.DMXPro port);
    }

    public interface ISceneRequiresAcnStream
    {
        void WireUp(Expander.AcnStream port);
    }

    public interface ISceneRequiresRenard
    {
        void WireUp(Expander.Renard port);
    }
}
