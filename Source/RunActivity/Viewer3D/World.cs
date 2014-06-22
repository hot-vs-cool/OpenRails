﻿// COPYRIGHT 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

using Microsoft.Xna.Framework;
using ORTS.Common;
using System.Collections.Generic;

namespace ORTS.Viewer3D
{
    public class World
    {
        readonly Viewer Viewer;
        public readonly WeatherControl WeatherControl;
        public readonly SkyViewer Sky;
        public readonly MSTSSkyDrawer MSTSSky;
        public readonly PrecipitationViewer Precipitation;
        public readonly TerrainViewer Terrain;
        public readonly SceneryDrawer Scenery;
        public readonly TrainDrawer Trains;
        public readonly RoadCarViewer RoadCars;
        public readonly SoundSource GameSounds;
        public readonly WorldSounds Sounds;

        int TileX;
        int TileZ;
        int VisibleTileX;
        int VisibleTileZ;
        bool PerformanceTune;

        [CallOnThread("Render")]
        public World(Viewer viewer)
        {
            Viewer = viewer;
            // Control stuff first.
            WeatherControl = new WeatherControl(viewer);
            // Then drawers.
            if (viewer.Settings.UseMSTSEnv)
                MSTSSky = new MSTSSkyDrawer(viewer);
            else
                Sky = new SkyViewer(viewer);
            Precipitation = new PrecipitationViewer(viewer, WeatherControl);
            Terrain = new TerrainViewer(viewer);
            Scenery = new SceneryDrawer(viewer);
            Trains = new TrainDrawer(viewer);
            RoadCars = new RoadCarViewer(viewer);
            // Then sound.
            if (viewer.Settings.SoundDetailLevel > 0)
            {
                // Keep it silent while loading.
                ALSoundSource.MuteAll();
                // TODO: This looks kinda evil; do something about it.
                GameSounds = new SoundSource(viewer, Events.Source.MSTSInGame, viewer.Simulator.RoutePath + "\\Sound\\ingame.sms");
                Viewer.SoundProcess.AddSoundSource(GameSounds.SMSFolder + "\\" + GameSounds.SMSFileName, new List<SoundSourceBase>() { GameSounds });
                Sounds = new WorldSounds(viewer);
            }
        }

        [CallOnThread("Loader")]
        public void Load()
        {
            Terrain.Load();
            Scenery.Load();
            Trains.Load();
            RoadCars.Load();
            if (TileX != VisibleTileX || TileZ != VisibleTileZ)
            {
                TileX = VisibleTileX;
                TileZ = VisibleTileZ;
                Viewer.ShapeManager.Mark();
                Viewer.MaterialManager.Mark();
                Viewer.TextureManager.Mark();
                if (Viewer.Settings.UseMSTSEnv)
                    MSTSSky.Mark();
                else
                    Sky.Mark();
                Precipitation.Mark();
                Terrain.Mark();
                Scenery.Mark();
                Trains.Mark();
                RoadCars.Mark();
                Viewer.Mark();
                Viewer.ShapeManager.Sweep();
                Viewer.MaterialManager.Sweep();
                Viewer.TextureManager.Sweep();
            }
        }

        [CallOnThread("Updater")]
        public void Update(ElapsedTime elapsedTime)
        {
            if (PerformanceTune && Viewer.RenderProcess.IsActive)
            {
                // Work out how far we need to change the actual FPS to get to the target.
                //   +ve = under-performing/too much detail
                //   -ve = over-performing/not enough detail
                var fpsTarget = Viewer.Settings.PerformanceTunerTarget - Viewer.RenderProcess.FrameRate.SmoothedValue;

                // If vertical sync is on, we're capped to 60 FPS. This means we need to shift a target of 60FPS down to 57FPS.
                if (Viewer.Settings.VerticalSync && Viewer.Settings.PerformanceTunerTarget > 55)
                    fpsTarget -= 3;

                // Summarise the FPS adjustment to: +1 (add detail), 0 (keep), -1 (remove detail).
                var fpsChange = fpsTarget < -2.5 ? +1 : fpsTarget > 2.5 ? -1 : 0;

                // If we're not vertical sync-limited, there's no point calculating the CPU change, just assume adding detail is okay.
                var cpuTarget = 0f;
                var cpuChange = 1;
                if (Viewer.Settings.VerticalSync)
                {
                    // Work out how much spare CPU we have; the target is 90%.
                    //   +ve = under-performing/too much detail
                    //   -ve = over-performing/not enough detail
                    var cpuTargetRender = Viewer.RenderProcess.Profiler.Wall.SmoothedValue - 90;
                    var cpuTargetUpdater = Viewer.UpdaterProcess.Profiler.Wall.SmoothedValue - 90;
                    cpuTarget = cpuTargetRender > cpuTargetUpdater ? cpuTargetRender : cpuTargetUpdater;

                    // Summarise the CPS adjustment to: +1 (add detail), 0 (keep), -1 (remove detail).
                    cpuChange = cpuTarget < -2.5 ? +1 : cpuTarget > 2.5 ? -1 : 0;
                }

                // Now we adjust the viewing distance to try and balance out the FPS.
                var oldViewingDistance = Viewer.Settings.ViewingDistance;
                if (fpsChange < 0)
                    Viewer.Settings.ViewingDistance -= (int)(fpsTarget - 1.5);
                else if (cpuChange < 0)
                    Viewer.Settings.ViewingDistance -= (int)(cpuTarget - 1.5);
                else if (fpsChange > 0 && cpuChange > 0)
                    Viewer.Settings.ViewingDistance += (int)(-fpsTarget - 1.5);
                Viewer.Settings.ViewingDistance = (int)MathHelper.Clamp(Viewer.Settings.ViewingDistance, 500, 10000);

                // If we've changed the viewing distance, we need to update the camera matricies.
                if (oldViewingDistance != Viewer.Settings.ViewingDistance)
                    Viewer.Camera.ScreenChanged();

                // Flag as done, so the next load prep (every 250ms) can trigger us again.
                PerformanceTune = false;
            }
            WeatherControl.Update(elapsedTime);
            Scenery.Update(elapsedTime);
        }

        [CallOnThread("Updater")]
        public void LoadPrep()
        {
            Terrain.LoadPrep();
            Scenery.LoadPrep();
            Trains.LoadPrep();
            RoadCars.LoadPrep();
            VisibleTileX = Viewer.Camera.TileX;
            VisibleTileZ = Viewer.Camera.TileZ;
            PerformanceTune = Viewer.Settings.PerformanceTuner;
        }

        [CallOnThread("Updater")]
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (Viewer.Settings.UseMSTSEnv)
                MSTSSky.PrepareFrame(frame, elapsedTime);
            else
                Sky.PrepareFrame(frame, elapsedTime);
            Precipitation.PrepareFrame(frame, elapsedTime);
            Terrain.PrepareFrame(frame, elapsedTime);
            Scenery.PrepareFrame(frame, elapsedTime);
            Trains.PrepareFrame(frame, elapsedTime);
            RoadCars.PrepareFrame(frame, elapsedTime);
        }
    }
}
