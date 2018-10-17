using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.AdminMessage
{
    public class NewSceneDefinition
    {
        public SceneDefinition Definition { get; set; }

        public ComponentUpdate[] InitialStatus { get; set; }
    }
}
