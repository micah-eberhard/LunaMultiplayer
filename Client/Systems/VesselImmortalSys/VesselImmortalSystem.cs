﻿using System;
using LunaClient.Base;
using LunaClient.Systems.Lock;
using UniLinq;
using UnityEngine;

namespace LunaClient.Systems.VesselImmortalSys
{
    /// <summary>
    /// This class makes the other vessels immortal, this way if we crash against them they are not destroyed but we do.
    /// In the other player screens they will be destroyed and they will send their new vessel definition.
    /// </summary>
    public class VesselImmortalSystem : System<VesselImmortalSystem>
    {
        #region Fields & properties

        private bool VesselImmortalSystemReady => Enabled && HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && Time.timeSinceLevelLoad > 1f;

        #endregion

        #region Base overrides

        public override void OnEnabled()
        {
            base.OnEnabled();
            SetupRoutine(new RoutineDefinition(2000, RoutineExecution.Update, MakeOtherPlayerVesselsImmortal));
        }

        #endregion

        #region Update methods

        /// <summary>
        /// Make the other player vessels inmortal
        /// </summary>
        private void MakeOtherPlayerVesselsImmortal()
        {
            if (Enabled && VesselImmortalSystemReady)
            {
                var ownedVessels = LockSystem.Singleton.GetOwnedLocksPrefix("control-").Select(LockSystem.TrimLock)
                                        .Union(LockSystem.Singleton.GetLocksWithPrefix("update-").Select(LockSystem.TrimLock))
                                        .Select(i => FlightGlobals.FindVessel(new Guid(i)))
                                        .Where(v => v != null)
                                        .ToArray();

                var othersPeopleVessels = LockSystem.Singleton.GetLocksWithPrefix("control-").Select(LockSystem.TrimLock)
                    .Union(LockSystem.Singleton.GetLocksWithPrefix("update-").Select(LockSystem.TrimLock))
                    .Except(ownedVessels.Select(v => v.id.ToString()))
                    .Select(i => FlightGlobals.FindVessel(new Guid(i)))
                    //Select the vessels and filter out the nulls
                    .Where(v => v != null).ToArray();

                foreach (var vessel in ownedVessels)
                {
                    SetVesselImmortalState(vessel, false);
                }

                foreach (var vessel in othersPeopleVessels)
                {
                    SetVesselImmortalState(vessel, true);
                }
            }
        }

        #endregion

        #region Private methods
        
        /// <summary>
        /// Set all vessel parts to unbreakable or not (makes the vessel immortal or not)
        /// </summary>
        private static void SetVesselImmortalState(Vessel vessel, bool immortal)
        {
            vessel.Parts.Where(p => p.attachJoint != null).ToList()
                .ForEach(p => p.attachJoint.SetUnbreakable(immortal, immortal));
        }

        #endregion
    }
}