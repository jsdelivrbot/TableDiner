﻿using System;
using UnityEngine;
using RimWorld;
using Verse;
using Harmony;
using Verse.AI;

namespace Table_Diner_Configurable
{
	//extraSelectionOverlays applies to blueprints and building defs.
	[HarmonyPatch(typeof(Verse.Thing), "DrawExtraSelectionOverlays")]
	public static class DrawExtraSelectionOverlays
	{
		[HarmonyPostfix]
		public static void _Postfix(Thing __instance)
		{
			bool bp = false;
			//check if a blueprint
			if (__instance.def.entityDefToBuild != null)
			{
				ThingDef td = __instance.def.entityDefToBuild as ThingDef;
				bp = (td != null && td.surfaceType == SurfaceType.Eat);
			}
			//if eat surface / eat surface blueprint, draw circle.
			if (TableDiner.settings.displayRing && (__instance.def.surfaceType == SurfaceType.Eat || bp))
			{
				//we draw a custom circle, because GenDraw.DrawRadiusRing is limited in it's radius.

				float r = TableDinerGlobal.GetTableRadius(__instance.ThingID);
				if (r < 1)
				{
					r = TableDiner.settings.tableDistance;
				}
				//Graphics.DrawMesh(TableDiner.tableCircle, Matrix4x4.TRS(__instance.TrueCenter() + Vector3.up * 10, Quaternion.identity, new Vector3(TableDiner.settings.tableDistance, TableDiner.settings.tableDistance, TableDiner.settings.tableDistance)),bp ? TableDinerGlobal.circleMaterialBP : TableDinerGlobal.circleMaterial, 0);
				Graphics.DrawMesh(TableDiner.tableCircle, Matrix4x4.TRS(__instance.TrueCenter() + Vector3.up * 10, Quaternion.identity, new Vector3(r, r, r)), bp ? TableDinerGlobal.circleMaterialBP : TableDinerGlobal.circleMaterial, 0);
			}
		}
	}

	//SelectedUpdate handles the building widget when placing a blueprint.
	[HarmonyPatch(typeof(RimWorld.Designator_Place), "SelectedUpdate")]
	public static class SelectedUpdate
	{
		[HarmonyPostfix]
		public static void _Postfix(Designator_Place __instance)
		{
			if (!TableDiner.settings.displayRing) return;

			//work our way through the tree to a thingDef, and check if it's an eat surface.
			ThingDef td = __instance.PlacingDef as ThingDef;
			if (td != null && td.surfaceType == SurfaceType.Eat)
			{
				//we draw a custom circle, because GenDraw.DrawRadiusRing is limited in it's radius.
				Graphics.DrawMesh(TableDiner.tableCircle, Matrix4x4.TRS(GenThing.TrueCenter(UI.MouseCell(), TableDiner.modInstance.lastRotation, td.size, 0) + Vector3.up * 10, Quaternion.identity, new Vector3(TableDiner.settings.tableDistance, TableDiner.settings.tableDistance, TableDiner.settings.tableDistance)), TableDinerGlobal.circleMaterialBP, 0);
			}
		}
	}

	//really hacky crap to get around a protected rotation variable.. ugh...
	[HarmonyPatch(typeof(Verse.GenDraw), "DrawInteractionCell")]
	public static class DrawInteractionCell
	{
		[HarmonyPostfix]
		public static void _Postfix(Rot4 placingRot)
		{
			TableDiner.modInstance.lastRotation = placingRot;
		}
	}


	//Patch ExposeData to save individual table radius
	[HarmonyPatch(typeof(Verse.Thing), "ExposeData")]
	public static class ExposeDataThing
	{
		[HarmonyPostfix]
		public static void _Postfix(Thing __instance)
		{
			if (!TableDiner.settings.useExtraFeatures)
			{
				return;
			}
			bool bp = false;
			//check if a blueprint
			if (__instance.def.entityDefToBuild != null)
			{
				ThingDef td = __instance.def.entityDefToBuild as ThingDef;
				bp = (td != null && td.surfaceType == SurfaceType.Eat);
			}
			//if eat surface / eat surface blueprint, draw circle.
			if (TableDiner.settings.displayRing && (__instance.def.surfaceType == SurfaceType.Eat || bp))
			{
				float td = TableDinerGlobal.GetTableRadius(__instance.ThingID);
				Scribe_Values.Look(ref td, "TableDiner_TableDistance", 0);
				TableDinerGlobal.tableRadii[__instance.ThingID] = td;
			}
		}
	}

	//Patch Pawn ExposeData to save Pawn table radius
	[HarmonyPatch(typeof(Verse.Pawn), "ExposeData")]
	public static class ExposeDataPawn
	{
		[HarmonyPostfix]
		public static void _Postfix(Pawn __instance)
		{
			if (!TableDiner.settings.useExtraFeatures)
			{
				return;
			}
			if (__instance.IsColonist)
			{
				float td = TableDinerGlobal.GetTableRadius(__instance.ThingID);
				Scribe_Values.Look(ref td, "TableDiner_TableDistance", 0);
				TableDinerGlobal.tableRadii[__instance.ThingID] = td;
			}
		}
	}

	//Patch ITab_Pawn_Needs to add table search distance
	[HarmonyPatch(typeof(RimWorld.ITab_Pawn_Needs), "FillTab")]
	public static class FillTab
	{
		public static bool mOver = false;

		[HarmonyPostfix]
		public static void _Postfix(ITab_Pawn_Needs __instance)
		{
			if (!TableDiner.settings.useExtraFeatures)
			{
				return;
			}
			Pawn SelPawn = Find.Selector.SingleSelectedThing as Pawn;
			if (SelPawn != null && SelPawn.IsColonist)
			{
				Vector2 size = NeedsCardUtility.GetSize(SelPawn);
				Rect tabRect = new Rect(20, size.y - (ITab_Table.WinSize.y) + 10, ITab_Table.WinSize.x - 40, ITab_Table.WinSize.y - 20);
				Rect tabRectBig = new Rect(10, size.y - (ITab_Table.WinSize.y) + 5, ITab_Table.WinSize.x - 20, ITab_Table.WinSize.y - 10);
				float tr = TableDinerGlobal.GetTableRadius(SelPawn.ThingID);
				GUI.color = Color.white;
				if (tr > TableDiner.settings.tableDistance)
				{
					GUI.color = Color.yellow;
				}
				if (Mouse.IsOver(tabRect))
				{
					Widgets.DrawHighlight(tabRectBig);
					mOver = true;
				}
				TableDinerGlobal.tableRadii[SelPawn.ThingID] = Mathf.Pow(Widgets.HorizontalSlider(tabRect, Mathf.Sqrt(tr), 0, 23, true, tr < 1 ? "TDiner.Ignored".Translate() : Mathf.Round(tr).ToString(), "TDiner.TRSlideLabel".Translate()), 2);
				GUI.color = Color.white;
			}
		}
	}

	//circle overlay for pawns
	[HarmonyPatch(typeof(RimWorld.ITab_Pawn_Needs), "TabUpdate")]
	public static class TabUpdate
	{
		[HarmonyPostfix]
		public static void __Postfix(ITab_Pawn_Needs __instance)
		{
			if (!TableDiner.settings.displayRing) return;
			Pawn SelPawn = Find.Selector.SingleSelectedThing as Pawn;
			if (SelPawn != null && SelPawn.IsColonist && FillTab.mOver)
			{
				float r = TableDinerGlobal.GetTableRadius(SelPawn.ThingID);
				if (r < 1)
				{
					r = TableDiner.settings.tableDistance;
				}
				Graphics.DrawMesh(TableDiner.tableCircle, Matrix4x4.TRS(SelPawn.TrueCenter() + Vector3.up * 10, Quaternion.identity, new Vector3(r, r, r)), TableDinerGlobal.circleMaterial, 0);
				FillTab.mOver = false;
			}
		}
	}

	//add distance checks to CarryIngestible toil chairValidator
	[HarmonyPatch(typeof(RimWorld.Toils_Ingest), "CarryIngestibleToChewSpot")]
	public static class CarryIngestibleToChewSpot
	{
		[HarmonyPrefix]
		public static bool __Prefix(Pawn pawn, TargetIndex ingestibleInd, ref Toil __result)
		{
			if (!TableDiner.settings.useExtraFeatures)
			{
				return true;
			}
			Toil toil = new Verse.AI.Toil();
			toil.initAction = delegate
			{
				Pawn actor = toil.actor;
				IntVec3 intVec = IntVec3.Invalid;
				Thing thing = null;
				Thing thing2 = actor.CurJob.GetTarget(ingestibleInd).Thing;
				Predicate<Thing> baseChairValidator = delegate (Thing t)
				{
					bool result;
					if (t.def.building == null || !t.def.building.isSittable)
					{
						result = false;
					}
					else if (t.IsForbidden(pawn))
					{
						result = false;
					}
					else if (!actor.CanReserve(t, 1, -1, null, false))
					{
						result = false;
					}
					else if (!t.IsSociallyProper(actor))
					{
						result = false;
					}
					else if (t.IsBurning())
					{
						result = false;
					}
					else if (t.HostileTo(pawn))
					{
						result = false;
					}
					else
					{
						bool flag = false;
						for (int i = 0; i < 4; i++)
						{
							IntVec3 c = t.Position + GenAdj.CardinalDirections[i];
							Building edifice = c.GetEdifice(t.Map);
							if (edifice != null && edifice.def.surfaceType == SurfaceType.Eat)
							{
								float tr = TableDinerGlobal.GetTableRadius(edifice.ThingID);
								float pr = TableDinerGlobal.GetTableRadius(actor.ThingID);

								if (tr >= 1 || pr >= 1)
								{
									float r2 = (edifice.TrueCenter() - actor.TrueCenter()).sqrMagnitude;
									if (tr < 1)
									{
										if (r2 <= Mathf.Pow(pr, 2))
										{
											flag = true;
											break;
										}
									}
									else if (pr < 1)
									{
										if (r2 <= Mathf.Pow(tr, 2))
										{
											flag = true;
											break;
										}
									}
									else
									{
										if (r2 <= Mathf.Pow(Mathf.Min(tr, pr), 2))
										{
											flag = true;
											break;
										}
									}
								}
								else
								{
									flag = true;
									break;
								}
							}
						}
						result = flag;
					}
					return result;
				};
				if (thing2.def.ingestible.chairSearchRadius > 0f)
				{
					thing = GenClosest.ClosestThingReachable(actor.Position, actor.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.OnCell, TraverseParms.For(actor, Danger.Deadly, TraverseMode.ByPawn, false), thing2.def.ingestible.chairSearchRadius, (Thing t) => baseChairValidator(t) && t.Position.GetDangerFor(pawn, t.Map) == Danger.None, null, 0, -1, false, RegionType.Set_Passable, false);
				}
				if (thing == null)
				{
					intVec = RCellFinder.SpotToChewStandingNear(actor, actor.CurJob.GetTarget(ingestibleInd).Thing);
					Danger chewSpotDanger = intVec.GetDangerFor(pawn, actor.Map);
					if (chewSpotDanger != Danger.None)
					{
						thing = GenClosest.ClosestThingReachable(actor.Position, actor.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.OnCell, TraverseParms.For(actor, Danger.Deadly, TraverseMode.ByPawn, false), thing2.def.ingestible.chairSearchRadius, (Thing t) => baseChairValidator(t) && t.Position.GetDangerFor(pawn, t.Map) <= chewSpotDanger, null, 0, -1, false, RegionType.Set_Passable, false);
					}
				}
				if (thing != null)
				{
					intVec = thing.Position;
					actor.Reserve(thing, actor.CurJob, 1, -1, null);
				}
				actor.Map.pawnDestinationReservationManager.Reserve(actor, actor.CurJob, intVec);
				actor.pather.StartPath(intVec, PathEndMode.OnCell);
			};
			toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
			__result = toil;
			return false;
		}
	}
}
