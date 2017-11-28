using System.Collections.Generic;
using RedistHeat;
using RimWorld;
using Verse;

namespace FrontierDevelopments.General.Comps
{
    public class Comp_RedistHeatSink : Comp_HeatSink
    {
        private const double AirSpecificHeat = 0.718;
        private const double AirMassCubicFoot = 36.61;
        private const double AirVolumeCuFt = 25;

        private bool _useAirNet;
        private CompAirTrader _airTrader;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Common.WipeExistingPipe(parent.Map, parent.Position);
            _airTrader = parent.GetComp<CompAirTrader>();
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            OverlayDrawHandler_AirNet.DrawAitNetOverlayThisFrame();
        }

        private bool AirConnectedConnected()
        {
            return _useAirNet && _airTrader.connectedNet?.nodes?.Count > 1;
        }

        protected override double AmbientTemp()
        {
            return AirConnectedConnected() ? _airTrader.connectedNet.NetTemperature : base.AmbientTemp();
        }

        protected override void DisipateHeat(double kilojoules)
        {
            if (!AirConnectedConnected())
            {
                base.DisipateHeat(kilojoules);
                return;
            }
            
            var intakeJoules = 
                (_airTrader.connectedNet.NetTemperature + KELVIN_ZERO_CELCIUS) 
                * AirSpecificHeat 
                * AirMassCubicFoot * AirVolumeCuFt;
            var exhaustTemperature =
                (intakeJoules + kilojoules * 1000) 
                / AirSpecificHeat 
                / (AirMassCubicFoot * AirVolumeCuFt)
                - KELVIN_ZERO_CELCIUS;
            _airTrader.SetNetTemperatureDirect((float)(exhaustTemperature - AmbientTemp()));
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var current in base.CompGetGizmosExtra())
                yield return current;

            if (parent.Faction == Faction.OfPlayer)
            {
                yield return new Command_Action
                {
                    defaultLabel = _useAirNet ? "Network" : "Room",
                    defaultDesc = "Cycle between the duct network and the room",
                    icon = _useAirNet ? ResourceBank.UINetwork : ResourceBank.UIRoom,
                    activateSound = SoundDef.Named("DesignateMine"),
                    hotKey = KeyBindingDefOf.CommandColonistDraft,
                    action = () => _useAirNet = !_useAirNet
                };
            }
        }
        
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref _useAirNet, "useAirNet", true);
        }
    }
}