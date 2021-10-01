using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace UndergroundPolice
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class UndergroundPolice : MySessionComponentBase
    {
        readonly Updater _updater;
        readonly ConcurrentQueue<IMyCubeGrid> _addedGrids;
        readonly ConcurrentQueue<IMySlimBlock> _addedBlocks;
        readonly List<IMyCubeGrid> _tmpGrids;
        readonly List<IMySlimBlock> _tmpBlocks;
        readonly List<MyVoxelBase> _tmpVoxels;
        readonly List<IMySlimBlock> _tmpSlimBlocks;

        public UndergroundPolice()
        {
            _addedGrids = new ConcurrentQueue<IMyCubeGrid>();
            _addedBlocks = new ConcurrentQueue<IMySlimBlock>();
            _tmpGrids = new List<IMyCubeGrid>();
            _tmpBlocks = new List<IMySlimBlock>();
            _tmpSlimBlocks = new List<IMySlimBlock>();
            _tmpVoxels = new List<MyVoxelBase>();
            _updater = new Updater(TimeSpan.FromSeconds(5), Update);
        }

        public override void LoadData()
        {
            base.LoadData();

            if (!MyAPIGateway.Session.IsServer)
            {
                UpdateOrder = MyUpdateOrder.NoUpdate;
                return;
            }

            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdded;
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemoved;
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdded;
            MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemoved;
        }

        void OnEntityAdded(IMyEntity entity)
        {
            var grid = entity as IMyCubeGrid;
            if (grid == null) return;

            _addedGrids.Enqueue(grid);
            grid.OnBlockAdded += OnBlockAddedOnExistingGrid;
        }

        void OnEntityRemoved(IMyEntity entity)
        {
            var grid = entity as IMyCubeGrid;
            if (grid == null) return;

            grid.OnBlockAdded -= OnBlockAddedOnExistingGrid;
        }

        void OnBlockAddedOnExistingGrid(IMySlimBlock block)
        {
            _addedBlocks.Enqueue(block);
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            _updater.Update();
        }

        void Update()
        {
            _addedGrids.DequeueAll(_tmpGrids);
            foreach (var grid in _tmpGrids)
            {
                ProcessGrid(grid);
            }

            _tmpGrids.Clear();

            _addedBlocks.DequeueAll(_tmpBlocks);
            foreach (var block in _tmpBlocks)
            {
                ProcessBlock(block);
            }

            _tmpBlocks.Clear();
        }

        void ProcessGrid(IMyCubeGrid grid)
        {
            if (grid.Physics == null) return; // projector
            if (!IsInVoxel(grid)) return;

            grid.GetBlocks(_tmpSlimBlocks);
            foreach (var block in _tmpSlimBlocks)
            {
                ProcessBlock(block);
            }

            _tmpSlimBlocks.Clear();
        }

        void ProcessBlock(IMySlimBlock block)
        {
            var fatBlock = block.FatBlock;
            if (!IsProhibitedUnderground(fatBlock)) return;

            MyLog.Default.Info($"woo woo {fatBlock.GetType()}");

            if (!IsInVoxel(block)) return;

            block.CubeGrid.RemoveBlock(block);
        }

        static bool IsProhibitedUnderground(IMyEntity block)
        {
            if (block is IMyBeacon) return true;
            if (block is IMyLaserAntenna) return true;
            if (block is IMyRadioAntenna) return true;
            if (block is IMyUserControllableGun) return true;
            if (block is IMyDecoy) return true;
            return false;
        }

        bool IsInVoxel(IMyEntity entity)
        {
            try
            {
                var aabb = entity.WorldAABB;
                MyGamePruningStructure.GetAllVoxelMapsInBox(ref aabb, _tmpVoxels);
                return _tmpVoxels.Count > 0;
            }
            finally
            {
                _tmpVoxels.Clear();
            }
        }

        bool IsInVoxel(IMySlimBlock block)
        {
            try
            {
                BoundingBoxD aabb;
                block.GetWorldBoundingBox(out aabb, false);
                MyGamePruningStructure.GetAllVoxelMapsInBox(ref aabb, _tmpVoxels);
                if (_tmpVoxels.Count == 0) return false;

                var gridSize = block.CubeGrid.GridSize;
                var boundingBoxD = new BoundingBoxD(gridSize * ((Vector3D) block.Min - 0.5), gridSize * ((Vector3D) block.Max + 0.5));
                var worldMatrix = block.CubeGrid.WorldMatrix;
                foreach (var voxel in _tmpVoxels)
                {
                    if (voxel.IsAnyAabbCornerInside(ref worldMatrix, boundingBoxD))
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                _tmpVoxels.Clear();
            }
        }
    }
}