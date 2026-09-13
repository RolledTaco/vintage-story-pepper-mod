using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace PepperMod
{
    internal static class HydrateOrDiedrateCompatibility
    {
        private const string ModId = "hydrateordiedrate";
        private const string ThirstType = "HydrateOrDiedrate.EntityBehaviorThirst";
        private static readonly ConditionalWeakTable<Type, Accessors> Contracts = new();

        public static void Drain(EntityPlayer player, float amount)
        {
            if (!float.IsFinite(amount) || amount <= 0 || player?.World?.Side != EnumAppSide.Server
                || !player.Alive || player.Player?.WorldData?.CurrentGameMode != EnumGameMode.Survival
                || player.Api?.ModLoader?.IsModEnabled(ModId) != true) return;

            var behavior = player.GetBehavior("thirst");
            if (behavior?.GetType().FullName != ThirstType
                || player.WatchedAttributes?.GetTreeAttribute("thirst") == null) return;

            var access = Contracts.GetValue(behavior.GetType(), type => new Accessors(type));
            if (access.Failed) return;
            try
            {
                if (!access.Supported) throw new MissingMemberException("The public thirst API has changed.");
                object config = access.ConfigInstance.GetValue(null);
                object thirstConfig = config == null ? null : access.ThirstConfig.GetValue(config);
                if (thirstConfig == null || !(bool)access.Enabled.GetValue(thirstConfig)) return;
                float current = (float)access.CurrentThirst.GetValue(behavior);
                if (!float.IsFinite(current) || current <= 0) return;

                // HoD handles clamping, sync, and movement penalties. A zero delay
                // leaves drinking's existing hydration-loss delay untouched.
                access.ModifyThirst.Invoke(behavior, new object[] { -Math.Min(current, amount), 0f });
            }
            catch (Exception error)
            {
                access.Failed = true;
                player.Api.Logger?.Warning("[PepperMod] Hydrate or Diedrate spice drain disabled for this session: {0}",
                    (error.InnerException ?? error).Message);
            }
        }

        // Bind only the public contract at runtime; HoD remains optional and its
        // assembly is never referenced by or bundled with Pepper Mod.
        private sealed class Accessors
        {
            public readonly MethodInfo ModifyThirst;
            public readonly PropertyInfo CurrentThirst, ConfigInstance, ThirstConfig, Enabled;
            public readonly bool Supported;
            public bool Failed;

            public Accessors(Type type)
            {
                try
                {
                    ModifyThirst = type.GetMethod("ModifyThirst", new[] { typeof(float), typeof(float) });
                    CurrentThirst = type.GetProperty("CurrentThirst", BindingFlags.Public | BindingFlags.Instance);
                    var config = type.Assembly.GetType("HydrateOrDiedrate.Config.ModConfig");
                    ConfigInstance = config?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    ThirstConfig = config?.GetProperty("Thirst", BindingFlags.Public | BindingFlags.Instance);
                    Enabled = ThirstConfig?.PropertyType.GetProperty("Enabled", BindingFlags.Public | BindingFlags.Instance);
                    Supported = ModifyThirst?.ReturnType == typeof(void) && !ModifyThirst.IsStatic
                        && CurrentThirst?.PropertyType == typeof(float) && CurrentThirst.GetMethod != null
                        && ConfigInstance?.GetMethod != null && ThirstConfig?.GetMethod != null
                        && Enabled?.PropertyType == typeof(bool) && Enabled.GetMethod != null;
                }
                catch (Exception)
                {
                    Supported = false;
                }
            }
        }
    }
}
