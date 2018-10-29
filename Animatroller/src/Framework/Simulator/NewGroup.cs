namespace Animatroller.Framework.Simulator
{
    public class NewGroup
    {
        public string Name { get; private set; }

        public NewGroup([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            Name = name;
        }
    }
}
