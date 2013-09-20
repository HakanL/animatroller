using Expander = Animatroller.Framework.Expander;

namespace Animatroller.SceneRunner
{
    public interface ISceneSupportsRaspExpander
    {
        void WireUp(Expander.Raspberry port);
    }

    public interface ISceneSupportsSimulator
    {
        void WireUp(Animatroller.Simulator.SimulatorForm sim);
    }

    public interface ISceneSupportsIOExpander
    {
        void WireUp(Expander.IOExpander port);
    }

    public interface ISceneSupportsDMXPro
    {
        void WireUp(Expander.DMXPro port);
    }

    public interface ISceneSupportsAcnStream
    {
        void WireUp(Expander.AcnStream port);
    }
}
