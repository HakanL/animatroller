using System;

namespace Animatroller.Framework.LogicalDevice
{
    public abstract class SingleOwnerDevice : BaseDevice, IOwner
    {
        protected IOwner owner;

        public SingleOwnerDevice(string name)
            : base(name)
        {
        }

        public void ReleaseOwner()
        {
            this.owner = null;
        }

        public virtual int Priority
        {
            //FIXME: Do we need this one?
            get { return 0; }
        }
    }
}
