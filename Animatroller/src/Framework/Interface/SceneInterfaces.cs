using Expander = Animatroller.Framework.Expander;

namespace Animatroller.Framework
{
    public interface ISceneRequiresRaspExpander1
    {
        void WireUp1(Expander.Raspberry port);
    }

    public interface ISceneRequiresRaspExpander2
    {
        void WireUp2(Expander.Raspberry port);
    }

    public interface ISceneRequiresRaspExpander3
    {
        void WireUp3(Expander.Raspberry port);
    }

    public interface ISceneRequiresRaspExpander4
    {
        void WireUp4(Expander.Raspberry port);
    }

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
