using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace PepperMod
{
    public class PepperSpiceSystem : ModSystem
    {
        public const string AttributeKey = "peppermodSpiceState";
        private ICoreServerAPI server;
        private ICoreClientAPI client;
        private long serverTick;
        private long clientTick;
        private SpiceHud hud;

        public override void StartServerSide(ICoreServerAPI api)
        {
            server = api;
            serverTick = api.Event.RegisterGameTickListener(OnServerTick, 250);
            api.Event.PlayerDeath += OnPlayerDeath;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            client = api;
            api.Event.LevelFinalize += OnLevelReady;
            api.Event.LeaveWorld += OnLeaveWorld;
        }

        private void OnLevelReady()
        {
            OnLeaveWorld();
            hud = new SpiceHud(client);
            clientTick = client.Event.RegisterGameTickListener(OnClientTick, 100);
        }

        private void OnClientTick(float dt)
        {
            EntityPlayer player = client.World.Player?.Entity;
            hud?.Update(player?.Alive == true ? ReadState(player) : default);
        }

        private void OnServerTick(float dt)
        {
            foreach (IPlayer player in server.World.AllOnlinePlayers) TickPlayer(player.Entity, dt);
        }

        public static SpiceState ReadState(Entity player)
        {
            ITreeAttribute tree = player?.WatchedAttributes?.GetTreeAttribute(AttributeKey);
            return tree == null ? default : new SpiceState(tree.GetFloat("heat"), tree.GetFloat("coolingDelay"));
        }

        public static void AddSpice(EntityPlayer player, float amount)
        {
            if (player?.World?.Side != EnumAppSide.Server || !player.Alive) return;
            SpiceState before = ReadState(player);
            SpiceState after = before.Add(amount);
            if (after.Heat != before.Heat || after.CoolingDelay != before.CoolingDelay) WriteState(player, after);
        }

        public static void TickPlayer(EntityPlayer player, float dt)
        {
            if (player?.World?.Side != EnumAppSide.Server) return;
            SpiceState state = ReadState(player);
            if (!player.Alive) { ClearState(player); return; }
            if (state.Heat <= 0 || !float.IsFinite(dt) || dt <= 0) return;
            SpiceState after = state.Cool(dt);
            // Only the part of this tick spent at Hot or Extreme supplies warmth.
            float warmSeconds = state.WarmSeconds(dt);
            var temperature = player.GetBehavior<EntityBehaviorBodyTemperature>();
            if (temperature != null && state.Level >= SpiceLevel.Hot)
            {
                float current = temperature.CurBodyTemperature;
                float warmed = state.WarmBody(current, temperature.NormalBodyTemperature, warmSeconds);
                if (warmed != current) temperature.CurBodyTemperature = warmed;
            }
            float hungerDrain = state.HungerDrain(dt);
            if (hungerDrain > 0)
            {
                EnumGameMode mode = player.Player?.WorldData?.CurrentGameMode ?? EnumGameMode.Survival;
                var hunger = player.GetBehavior<EntityBehaviorHunger>();
                if (hunger != null && mode != EnumGameMode.Creative && mode != EnumGameMode.Spectator)
                {
                    float current = hunger.Saturation;
                    if (float.IsFinite(current) && current > 0) hunger.Saturation = Math.Max(0, current - hungerDrain);
                }
            }
            HydrateOrDiedrateCompatibility.Drain(player, state.HydrationDrain(dt));
            if (after.Heat != state.Heat || after.CoolingDelay != state.CoolingDelay) WriteState(player, after);
        }

        private static void WriteState(Entity player, SpiceState state)
        {
            if (state.Heat <= 0) { ClearState(player); return; }
            var tree = new TreeAttribute();
            tree.SetFloat("heat", state.Heat);
            tree.SetFloat("coolingDelay", state.CoolingDelay);
            player.WatchedAttributes.SetAttribute(AttributeKey, tree);
            player.WatchedAttributes.MarkPathDirty(AttributeKey);
        }

        public static void ClearState(Entity player)
        {
            if (player?.WatchedAttributes?.HasAttribute(AttributeKey) != true) return;
            player.WatchedAttributes.RemoveAttribute(AttributeKey);
            player.WatchedAttributes.MarkPathDirty(AttributeKey);
        }

        private void OnPlayerDeath(IServerPlayer player, DamageSource source) => ClearState(player.Entity);

        private void OnLeaveWorld()
        {
            if (clientTick != 0) client.Event.UnregisterGameTickListener(clientTick);
            clientTick = 0;
            hud?.Dispose();
            hud = null;
        }

        public override void Dispose()
        {
            if (server != null)
            {
                server.Event.UnregisterGameTickListener(serverTick);
                server.Event.PlayerDeath -= OnPlayerDeath;
            }
            if (client != null)
            {
                client.Event.LevelFinalize -= OnLevelReady;
                client.Event.LeaveWorld -= OnLeaveWorld;
                OnLeaveWorld();
            }
            base.Dispose();
        }
    }
}
