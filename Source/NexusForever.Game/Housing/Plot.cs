using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Static.Housing;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Housing
{
    public class Plot : IPlot
    {
        /// <summary>
        /// Determines which fields need saving for <see cref="IPlot"/> when being saved to the database.
        /// </summary>
        [Flags]
        public enum PlotSaveMask
        {
            None           = 0x0000,
            Create         = 0x0001,
            PlugItemId     = 0x0002,
            PlugFacing     = 0x0004,
            BuildState     = 0x0008,
            PlotInfoId     = 0x0010,
            WarplotUpgrade = 0x0020,
            Upkeep         = 0x0040,
            Contribution   = 0x0080
        }

        public ulong Id { get; }
        public byte Index { get; }

        public HousingPlotInfoEntry PlotInfoEntry
        {
            get => plotInfoEntry;
            set
            {
                plotInfoEntry = value;
                saveMask |= PlotSaveMask.PlotInfoId;
            }
        }

        private HousingPlotInfoEntry plotInfoEntry;

        public HousingPlugItemEntry PlugItemEntry
        {
            get => plugItemEntry;
            set
            {
                plugItemEntry = value;
                saveMask |= PlotSaveMask.PlugItemId;
            }
        }

        private HousingPlugItemEntry plugItemEntry;

        public HousingPlugFacing PlugFacing
        {
            get => plugFacing;
            set
            {
                plugFacing = value;
                saveMask |= PlotSaveMask.PlugFacing;
            }
        }

        private HousingPlugFacing plugFacing;

        public byte BuildState
        {
            get => buildState;
            set
            {
                buildState = value;
                saveMask |= PlotSaveMask.BuildState;
            }
        }

        private byte buildState;

        /// <summary>
        /// Number of upkeep charges remaining for this plot.
        /// </summary>
        public uint UpkeepCharges
        {
            get => upkeepCharges;
            set
            {
                upkeepCharges = value;
                saveMask |= PlotSaveMask.Upkeep;
            }
        }
        private uint upkeepCharges;

        /// <summary>
        /// Time until next upkeep charge is consumed.
        /// </summary>
        public float UpkeepTime
        {
            get => upkeepTime;
            set
            {
                upkeepTime = value;
                saveMask |= PlotSaveMask.Upkeep;
            }
        }
        private float upkeepTime;

        /// <summary>
        /// Contribution totals for this plot. Use <see cref="SetContribution"/> to mutate.
        /// </summary>
        public uint[] ContributionTotals { get; } = new uint[5];

        /// <summary>
        /// Set a contribution total by index and mark the plot dirty for save.
        /// </summary>
        public void SetContribution(int index, uint value)
        {
            ContributionTotals[index] = value;
            saveMask |= PlotSaveMask.Contribution;
        }

        /// <summary>
        /// Warplot plug info for this plot (if it's a warplot plot).
        /// </summary>
        public HousingWarplotPlugInfoEntry WarplotPlugInfo
        {
            get => warplotPlugInfo;
            set
            {
                warplotPlugInfo = value;
                saveMask |= PlotSaveMask.WarplotUpgrade;
            }
        }
        private HousingWarplotPlugInfoEntry warplotPlugInfo;

        /// <summary>
        /// Upgrade level for warplot plot.
        /// </summary>
        public uint UpgradeLevel
        {
            get => upgradeLevel;
            set
            {
                upgradeLevel = value;
                saveMask |= PlotSaveMask.WarplotUpgrade;
            }
        }
        private uint upgradeLevel;

        private PlotSaveMask saveMask;

        public IPlugEntity PlugEntity { get; set; }

        /// <summary>
        /// Create a new <see cref="IPlot"/> from an existing database model.
        /// </summary>
        public Plot(ResidencePlotModel model)
        {
            Id            = model.Id;
            Index         = model.Index;
            plotInfoEntry = GameTableManager.Instance.HousingPlotInfo.GetEntry(model.PlotInfoId);
            if (plotInfoEntry == null)
                throw new DatabaseDataException($"Plot {model.Id} references invalid HousingPlotInfo id {model.PlotInfoId}!");

            plugItemEntry = GameTableManager.Instance.HousingPlugItem.GetEntry(model.PlugItemId);
            plugFacing    = (HousingPlugFacing)model.PlugFacing;
            buildState    = model.BuildState;

            // Load warplot data
            UpgradeLevel = model.UpgradeLevel;
            if (model.WarplotPlugItemId != 0)
                WarplotPlugInfo = GameTableManager.Instance.HousingWarplotPlugInfo.GetEntry(model.WarplotPlugItemId);

            // Load upkeep data
            UpkeepCharges = model.UpkeepCharges;
            UpkeepTime = model.UpkeepTime;
            ContributionTotals[0] = model.ContributionTotal0;
            ContributionTotals[1] = model.ContributionTotal1;
            ContributionTotals[2] = model.ContributionTotal2;
            ContributionTotals[3] = model.ContributionTotal3;
            ContributionTotals[4] = model.ContributionTotal4;

            saveMask = PlotSaveMask.None;
        }

        /// <summary>
        /// Create a new <see cref="IPlot"/> from a <see cref="HousingPlotInfoEntry"/>.
        /// </summary>
        public Plot(ulong id, HousingPlotInfoEntry entry)
        {
            Id            = id;
            Index         = (byte)entry.HousingPropertyPlotIndex;
            plotInfoEntry = entry;
            plugFacing    = HousingPlugFacing.East;

            if (entry.HousingPlugItemIdDefault != 0u)
            {
                // TODO
                // plugItemId = entry.HousingPlugItemIdDefault;
            }

            saveMask = PlotSaveMask.Create;
        }

        public void Save(CharacterContext context)
        {
            if (saveMask == PlotSaveMask.None)
                return;

            if ((saveMask & PlotSaveMask.Create) != 0)
            {
                // plot doesn't exist in database, all infomation must be saved
                context.Add(new ResidencePlotModel
                {
                    Id         = Id,
                    Index      = Index,
                    PlotInfoId = (ushort)PlotInfoEntry.Id,
                    PlugItemId = (ushort)(PlugItemEntry?.Id ?? 0u),
                    PlugFacing = (byte)PlugFacing,
                    BuildState = BuildState,
                    UpgradeLevel = UpgradeLevel,
                    WarplotPlugItemId = (ushort)(WarplotPlugInfo?.Id ?? 0u),
                    UpkeepCharges = UpkeepCharges,
                    UpkeepTime = UpkeepTime,
                    ContributionTotal0 = ContributionTotals[0],
                    ContributionTotal1 = ContributionTotals[1],
                    ContributionTotal2 = ContributionTotals[2],
                    ContributionTotal3 = ContributionTotals[3],
                    ContributionTotal4 = ContributionTotals[4]
                });
            }
            else
            {
                // plot already exists in database, save only data that has been modified
                var model = new ResidencePlotModel
                {
                    Id    = Id,
                    Index = Index,
                };

                EntityEntry<ResidencePlotModel> entity = context.Attach(model);
                if ((saveMask & PlotSaveMask.PlotInfoId) != 0)
                {
                    model.PlotInfoId = (ushort)PlotInfoEntry.Id;
                    entity.Property(p => p.PlotInfoId).IsModified = true;
                }

                if ((saveMask & PlotSaveMask.PlugItemId) != 0)
                {
                    model.PlugItemId = (ushort)(PlugItemEntry?.Id ?? 0u);
                    entity.Property(p => p.PlugItemId).IsModified = true;
                }

                if ((saveMask & PlotSaveMask.PlugFacing) != 0)
                {
                    model.PlugFacing = (byte)PlugFacing;
                    entity.Property(p => p.PlugFacing).IsModified = true;
                }

                if ((saveMask & PlotSaveMask.BuildState) != 0)
                {
                    model.BuildState = BuildState;
                    entity.Property(p => p.BuildState).IsModified = true;
                }

                if ((saveMask & PlotSaveMask.WarplotUpgrade) != 0)
                {
                    model.UpgradeLevel = UpgradeLevel;
                    model.WarplotPlugItemId = (ushort)(WarplotPlugInfo?.Id ?? 0u);
                    entity.Property(p => p.UpgradeLevel).IsModified = true;
                    entity.Property(p => p.WarplotPlugItemId).IsModified = true;
                }

                if ((saveMask & PlotSaveMask.Upkeep) != 0)
                {
                    model.UpkeepCharges = UpkeepCharges;
                    model.UpkeepTime = UpkeepTime;
                    entity.Property(p => p.UpkeepCharges).IsModified = true;
                    entity.Property(p => p.UpkeepTime).IsModified = true;
                }

                if ((saveMask & PlotSaveMask.Contribution) != 0)
                {
                    model.ContributionTotal0 = ContributionTotals[0];
                    model.ContributionTotal1 = ContributionTotals[1];
                    model.ContributionTotal2 = ContributionTotals[2];
                    model.ContributionTotal3 = ContributionTotals[3];
                    model.ContributionTotal4 = ContributionTotals[4];
                    entity.Property(p => p.ContributionTotal0).IsModified = true;
                    entity.Property(p => p.ContributionTotal1).IsModified = true;
                    entity.Property(p => p.ContributionTotal2).IsModified = true;
                    entity.Property(p => p.ContributionTotal3).IsModified = true;
                    entity.Property(p => p.ContributionTotal4).IsModified = true;
                }
            }

            saveMask = PlotSaveMask.None;
        }

        public void SetPlug(ushort plugItemId)
        {
            PlugItemEntry  = GameTableManager.Instance.HousingPlugItem.GetEntry(plugItemId);
            if (PlugItemEntry == null)
            {
                BuildState    = 0;
                UpkeepCharges = 0;
                UpkeepTime    = 0f;
                return;
            }

            BuildState     = 4;
            UpkeepCharges  = PlugItemEntry.UpkeepCharges;
            UpkeepTime     = PlugItemEntry.UpkeepTime;
        }

        /// <summary>
        /// Update the upkeep timer and consume a charge if the interval has elapsed.
        /// </summary>
        /// <returns>True if a charge was consumed.</returns>
        public bool UpdateUpkeep(double deltaTime)
        {
            if (PlugItemEntry == null || PlugItemEntry.UpkeepTime <= 0 || UpkeepCharges == 0)
                return false;

            UpkeepTime -= (float)deltaTime;
            if (UpkeepTime > 0)
                return false;

            UpkeepCharges--;
            UpkeepTime = PlugItemEntry.UpkeepTime;
            return true;
        }
    }
}
