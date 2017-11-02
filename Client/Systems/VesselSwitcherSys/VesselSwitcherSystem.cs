﻿using System;
using System.Collections;
using UnityEngine;

namespace LunaClient.Systems.VesselSwitcherSys
{
    /// <summary>
    /// This system handles the vessel loading into the game and sending our vessel structure to other players.
    /// We only load vesels that are in our subspace
    /// </summary>
    public class VesselSwitcherSystem : Base.System
    {
        #region Fields & properties

        private static Vessel VesselToSwitchTo { get; set; }

        #endregion

        #region Base overrides
        
        protected override void OnDisabled()
        {
            base.OnDisabled();
            VesselToSwitchTo = null;
        }

        #endregion

        #region Public

        /// <summary>
        /// Specifies the vessel that we should switch to
        /// </summary>
        public void SwitchToVessel(Guid vesselId)
        {
            VesselToSwitchTo = FlightGlobals.FindVessel(vesselId);
            if (VesselToSwitchTo != null)
            {
                LunaLog.Log($"[LMP]: Switching to vessel {vesselId}");
                Client.Singleton.StartCoroutine(SwitchToVessel());
            }
        }

        #endregion

        #region Private

        /// <summary>
        /// Switch to the vessel
        /// </summary>
        private static IEnumerator SwitchToVessel()
        {
            if (VesselToSwitchTo != null)
            {
                var tries = 0;
                var zoom = FlightCamera.fetch.Distance;

                OrbitPhysicsManager.HoldVesselUnpack();
                while (!VesselToSwitchTo.loaded && tries < 100)
                {
                    tries++;
                    yield return new WaitForFixedUpdate();
                }

                LunaLog.Log($"Tries: {tries} Loaded: {VesselToSwitchTo.loaded}");
                FlightGlobals.ForceSetActiveVessel(VesselToSwitchTo);
                FlightCamera.fetch.SetDistance(zoom);
                VesselToSwitchTo = null;
            }
        }

        #endregion
    }
}