using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace PepperMod
{
    public class BlockEntityPerennialPepperPlant : BlockEntity
    {
        private const int DefaultMatureLeafyStage = 6;
        private const int DefaultFloweringStage = 5;
        private const int DefaultPostHarvestStage = 10;
        private const int DefaultMatureRegrowthStage = 11;
        private const int DefaultHarvestStage = 8;
        private const int DefaultDormantStage = 9;

        private const double DefaultInitialHoursPerStage = 31.0;
        private const double DefaultFruitingHoursPerStage = 24.0;
        private const double MinimumCheckHours = 1.0;

        private const float DefaultGrowTemperature = 8.0f;
        private const float DefaultHeatStopTemperature = 38.0f;

        private double lastGrowthHour;
        private int stageBeforeDormancy;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if (lastGrowthHour <= 0 && api.World.Calendar != null)
            {
                lastGrowthHour = api.World.Calendar.TotalHours;
            }

            RegisterGameTickListener(OnGameTick, 2000);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            lastGrowthHour = tree.GetDouble("lastGrowthHour");
            stageBeforeDormancy = tree.GetInt("stageBeforeDormancy");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("lastGrowthHour", lastGrowthHour);
            tree.SetInt("stageBeforeDormancy", stageBeforeDormancy);
        }

        public bool TryHarvest(IPlayer byPlayer)
        {
            if (Api == null || Api.Side != EnumAppSide.Server)
            {
                return false;
            }

            if (GetStage() != ConfigInt("harvestStage", DefaultHarvestStage) || !IsGrowingSeason())
            {
                return false;
            }

            string pepperType = GetPepperType();
            int minYield = ConfigInt("harvestPepperMin", 6);
            int maxYield = ConfigInt("harvestPepperMax", 8);
            int quantity = minYield + Api.World.Rand.Next(Math.Max(1, maxYield - minYield + 1));

            SpawnItemDrops("vegetable-" + pepperType, quantity);

            if (Api.World.Rand.NextDouble() < ConfigDouble("seedChanceOnHarvest", 0.15))
            {
                SpawnItemDrops("seeds-" + pepperType, 1);
            }

            SetStage(ConfigInt("postHarvestStage", DefaultPostHarvestStage));
            lastGrowthHour = Api.World.Calendar.TotalHours;
            MarkDirty(true, byPlayer);

            return true;
        }

        private void OnGameTick(float dt)
        {
            if (Api == null || Api.Side != EnumAppSide.Server || Api.World.Calendar == null)
            {
                return;
            }

            double now = Api.World.Calendar.TotalHours;

            if (now - lastGrowthHour < MinimumCheckHours)
            {
                return;
            }

            UpdateSeasonalState(now);
        }

        private void UpdateSeasonalState(double now)
        {
            int stage = GetStage();
            int dormantStage = ConfigInt("dormantStage", DefaultDormantStage);

            if (!IsGrowingSeason())
            {
                if (stage != dormantStage)
                {
                    stageBeforeDormancy = GetWakeStage(stage);
                    SetStage(dormantStage);
                }

                lastGrowthHour = now;
                MarkDirty(true);
                return;
            }

            if (stage == dormantStage)
            {
                SetStage(stageBeforeDormancy <= 0 ? ConfigInt("postHarvestStage", DefaultPostHarvestStage) : stageBeforeDormancy);
                lastGrowthHour = now;
                MarkDirty(true);
                return;
            }

            int harvestStage = ConfigInt("harvestStage", DefaultHarvestStage);
            int postHarvestStage = ConfigInt("postHarvestStage", DefaultPostHarvestStage);
            int matureRegrowthStage = ConfigInt("matureRegrowthStage", DefaultMatureRegrowthStage);

            if (stage == harvestStage)
            {
                return;
            }

            if (stage == postHarvestStage || stage == matureRegrowthStage)
            {
                AdvanceMatureRegrowth(stage, postHarvestStage, matureRegrowthStage, harvestStage, now);
                return;
            }

            if (stage > harvestStage)
            {
                return;
            }

            double hoursNeeded = stage < ConfigInt("floweringStage", DefaultFloweringStage)
                ? ConfigDouble("initialHoursPerStage", DefaultInitialHoursPerStage)
                : ConfigDouble("fruitingHoursPerStage", DefaultFruitingHoursPerStage);
            if (now - lastGrowthHour < hoursNeeded)
            {
                return;
            }

            SetStage(stage + 1);
            lastGrowthHour = now;
            MarkDirty(true);
        }

        private void AdvanceMatureRegrowth(
            int stage,
            int postHarvestStage,
            int matureRegrowthStage,
            int harvestStage,
            double now)
        {
            double hoursNeeded = ConfigDouble("fruitingHoursPerStage", DefaultFruitingHoursPerStage);
            if (now - lastGrowthHour < hoursNeeded)
            {
                return;
            }

            SetStage(stage == postHarvestStage ? matureRegrowthStage : harvestStage);
            lastGrowthHour = now;
            MarkDirty(true);
        }

        private bool IsGrowingSeason()
        {
            ClimateCondition climate = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.NowValues);
            if (climate == null)
            {
                return true;
            }

            return climate.Temperature >= ConfigFloat("growTemperature", DefaultGrowTemperature) &&
                   climate.Temperature <= ConfigFloat("heatStopTemperature", DefaultHeatStopTemperature);
        }

        private int GetStage()
        {
            string path = Block.Code.Path;
            int lastDash = path.LastIndexOf('-');

            if (lastDash < 0)
            {
                return 1;
            }

            int stage;
            if (int.TryParse(path.Substring(lastDash + 1), out stage))
            {
                return stage;
            }

            return 1;
        }

        private string GetPepperType()
        {
            string path = Block.Code.Path;
            const string prefix = "crop-";

            if (path.StartsWith(prefix))
            {
                path = path.Substring(prefix.Length);
            }

            int lastDash = path.LastIndexOf('-');
            if (lastDash > 0)
            {
                path = path.Substring(0, lastDash);
            }

            return path;
        }

        private void SetStage(int stage)
        {
            string pepperType = GetPepperType();
            Block newBlock = Api.World.GetBlock(new AssetLocation("peppermod", "crop-" + pepperType + "-" + stage));

            if (newBlock == null || newBlock.Id == 0)
            {
                Api.Logger.Warning("Pepper Mod: Could not find block crop-{0}-{1}", pepperType, stage);
                return;
            }

            Api.World.BlockAccessor.ExchangeBlock(newBlock.BlockId, Pos);
        }

        private void SpawnItemDrops(string code, int quantity)
        {
            Item item = Api.World.GetItem(new AssetLocation("peppermod", code));
            if (item == null)
            {
                Api.Logger.Warning("Pepper Mod: Could not find item {0}", code);
                return;
            }

            int remaining = quantity;
            int maxStackSize = Math.Max(1, ConfigInt("harvestDropStackSize", 4));

            while (remaining > 0)
            {
                int stackSize = Math.Min(maxStackSize, remaining);
                remaining -= stackSize;

                double angle = Api.World.Rand.NextDouble() * Math.PI * 2;
                double radius = 0.1 + Api.World.Rand.NextDouble() * 0.2;

                Vec3d spawnPos = new Vec3d(
                    Pos.X + 0.5 + Math.Cos(angle) * radius,
                    Pos.Y + 0.65 + Api.World.Rand.NextDouble() * 0.15,
                    Pos.Z + 0.5 + Math.Sin(angle) * radius
                );

                Vec3d velocity = new Vec3d(0, -0.005, 0);

                Api.World.SpawnItemEntity(new ItemStack(item, stackSize), spawnPos, velocity);
            }
        }

        private int GetWakeStage(int currentStage)
        {
            int matureLeafyStage = ConfigInt("matureLeafyStage", DefaultMatureLeafyStage);
            // Mature plants lose their fruit in dormancy, not their established size.
            return currentStage >= matureLeafyStage
                ? ConfigInt("postHarvestStage", DefaultPostHarvestStage)
                : Math.Max(1, currentStage);
        }

        private JsonObject Config()
        {
            if (Block == null || Block.Attributes == null)
            {
                return null;
            }

            JsonObject config = Block.Attributes["perennialPepperPlant"];
            return config != null && config.Exists ? config : null;
        }

        private int ConfigInt(string key, int defaultValue)
        {
            JsonObject config = Config();
            return config == null ? defaultValue : config[key].AsInt(defaultValue);
        }

        private float ConfigFloat(string key, float defaultValue)
        {
            JsonObject config = Config();
            return config == null ? defaultValue : config[key].AsFloat(defaultValue);
        }

        private double ConfigDouble(string key, double defaultValue)
        {
            JsonObject config = Config();
            return config == null ? defaultValue : config[key].AsDouble(defaultValue);
        }
    }
}
