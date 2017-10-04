using Animatroller.Framework.Interface;
using Animatroller.Framework.LogicalDevice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Effect = Animatroller.Framework.Effect;
using Expander = Animatroller.Framework.Expander;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.Scenes.Modules
{
    public class HalloweenGrumpyCat : BaseModule
    {
        Dimmer3 catLights = new Dimmer3();
        DigitalOutput2 catAir = new DigitalOutput2(initial: true);

        Effect.Pulsating pulsatingCatLow = new Effect.Pulsating(S(4), 0.2, 0.5, false);
        Effect.Pulsating pulsatingCatHigh = new Effect.Pulsating(S(2), 0.5, 1.0, false);

        public HalloweenGrumpyCat(
            Expander.AcnStream acnOutput,
            (int Universe, int DmxChannel) airAddress,
            (int Universe, int DmxChannel) lightAddress
            )
        {
//FIXME            acnOutput.Connect(new Physical.GenericDimmer(catAir, airAddress.DmxChannel), airAddress.Universe);
//FIXME            acnOutput.Connect(new Physical.GenericDimmer(catLights, lightAddress.DmxChannel), lightAddress.Universe);

            pulsatingCatLow.ConnectTo(catLights);
            pulsatingCatHigh.ConnectTo(catLights);
        }
    }
}
