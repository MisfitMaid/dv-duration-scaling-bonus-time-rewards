using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using DV.Logic.Job;
using DV.ThingTypes;

namespace ScalingBonusRewards;

[EnableReloading]
public static class Main
{
	public static Settings settings;

	private static bool Load(UnityModManager.ModEntry modEntry)
	{
		settings = Settings.Load<Settings>(modEntry);
		modEntry.OnGUI = OnGUI;
		modEntry.OnSaveGUI = OnSaveGUI;


		Harmony? harmony = null;
		try
		{
			harmony = new Harmony(modEntry.Info.Id);
			harmony.PatchAll(Assembly.GetExecutingAssembly());

			// Other plugin startup logic
		}
		catch (Exception ex)
		{
			modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
			harmony?.UnpatchAll(modEntry.Info.Id);
			return false;
		}

		return true;
	}

	static void OnGUI(UnityModManager.ModEntry modEntry)
	{
		settings.Draw(modEntry);
	}

	static void OnSaveGUI(UnityModManager.ModEntry modEntry)
	{
		settings.Save(modEntry);
	}

	static bool Unload(UnityModManager.ModEntry modEntry)
	{
		Harmony harmony = new Harmony(modEntry.Info.Id);
		harmony.UnpatchAll();

		return true;
	}
}

public class Settings : UnityModManager.ModSettings, IDrawable
{
	[Draw("Maximum Possible Bonus Multiplier")] public float maxBonusCoef = 1.0f;

	public override void Save(UnityModManager.ModEntry modEntry)
	{
		Save(this, modEntry);
	}

	public void OnChange()
	{
	}
}

[HarmonyPatch(typeof(Job), nameof(Job.GetBonusPaymentForTheJob))]
static class Patch_GetBonusPaymentForTheJob
{
	static void Postfix(ref float __result, Job __instance)
	{
		if (__instance.State == JobState.Completed)
		{
			if (__instance.GetJobCompletionTime() <= __instance.TimeLimit + 60f)
			{
				float maxBonus = __instance.GetBasePaymentForTheJob() * Main.settings.maxBonusCoef;
				if (__instance.TimeLimit > 0)
					__result = maxBonus * (1 - (__instance.GetJobCompletionTime() / __instance.TimeLimit));
				return;
			}
		}
		else if (__instance.State == JobState.InProgress && __instance.GetTimeOnJob() <= __instance.TimeLimit + 60f)
		{
			__result = __instance.GetPotentialBonusPaymentForTheJob();
			return;
		}
		__result = 0f;
		return;
	}
}
