﻿using System;
using System.Text;
using System.Collections.Generic;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;


namespace FreeWill
{
    public class Priority : IComparable
    {
        static private readonly int disabledCutoff = 100 / (Pawn_WorkSettings.LowestPriority + 1); // 20 if LowestPriority is 4
        static private readonly int disabledCutoffActiveWorkArea = 100 - disabledCutoff; // 80 if LowestPriority is 4
        static private readonly float onePriorityWidth = (float)disabledCutoffActiveWorkArea / (float)Pawn_WorkSettings.LowestPriority; // ~20 if LowestPriority is 4
        static private FreeWill_WorldComponent worldComp;

        private Pawn pawn;
        private FreeWill_MapComponent mapComp;
        private WorkTypeDef workTypeDef;
        private float value;
        private List<string> adjustmentStrings;
        private bool enabled;
        private bool disabled;

        // work types
        const string FIREFIGHTER = "Firefighter";
        const string PATIENT = "Patient";
        const string DOCTOR = "Doctor";
        const string PATIENT_BED_REST = "PatientBedRest";
        const string BASIC_WORKER = "BasicWorker";
        const string WARDEN = "Warden";
        const string HANDLING = "Handling";
        const string COOKING = "Cooking";
        const string HUNTING = "Hunting";
        const string CONSTRUCTION = "Construction";
        const string GROWING = "Growing";
        const string MINING = "Mining";
        const string PLANT_CUTTING = "PlantCutting";
        const string SMITHING = "Smithing";
        const string TAILORING = "Tailoring";
        const string ART = "Art";
        const string CRAFTING = "Crafting";
        const string HAULING = "Hauling";
        const string CLEANING = "Cleaning";
        const string RESEARCHING = "Research";

        // supported modded work types
        const string HAULING_URGENT = "HaulingUrgent";

        public Priority(Pawn pawn, WorkTypeDef workTypeDef)
        {
            try
            {
                this.pawn = pawn;
                this.workTypeDef = workTypeDef;
                this.adjustmentStrings = new List<string> { };

                // find the map component for this pawn
                mapComp = pawn.Map.GetComponent<FreeWill_MapComponent>();

                // find the world component if it is missing
                if (worldComp == null)
                {
                    worldComp = Find.World.GetComponent<FreeWill_WorldComponent>();
                }

                // pawn has no free will, so use the player set priority
                if (!worldComp.HasFreeWill(pawn))
                {
                    var p = pawn.workSettings.GetPriority(workTypeDef);
                    if (p == 0)
                    {
                        this.set(0.0f, "FreeWillPriorityNoFreeWill".TranslateSimple());
                        return;
                    }
                    this.set((100f - onePriorityWidth * (p - 1)) / 100f, "FreeWillPriorityNoFreeWill".TranslateSimple());
                    return;
                }

                // start priority at the global default and compute the priority
                // using the AI in this file
                this.set(0.2f, "FreeWillPriorityGlobalDefault".TranslateSimple()).compute();
                return;
            }
            catch (System.Exception err)
            {
                Log.ErrorOnce("could not set " + workTypeDef.defName + " priority for pawn: " + pawn.Name + ": " + err.Message, 15448413);
                this.set(0.2f, "FreeWillPriorityGlobalDefault".TranslateSimple()).compute();
            }
        }

        int IComparable.CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }
            Priority p = obj as Priority;
            if (p == null)
            {
                return 1;
            }
            return this.value.CompareTo(p.value);
        }

        private Priority compute()
        {
            this.enabled = false;
            this.disabled = false;
            if (this.pawn.GetDisabledWorkTypes(true).Contains(this.workTypeDef))
            {
                return this.neverDo("FreeWillPriorityPermanentlyDisabled".TranslateSimple());
            }
            switch (this.workTypeDef.defName)
            {
                case FIREFIGHTER:
                    return this
                        .set(0.0f, "FreeWillPriorityFirefightingDefault".TranslateSimple())
                        .alwaysDo("FreeWillPriorityPatientDefault".TranslateSimple())
                        .neverDoIf(this.pawn.Downed, "FreeWillPriorityPawnDowned".TranslateSimple())
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case PATIENT:
                    return this
                        .set(0.0f, "FreeWillPriorityPatientDefault".TranslateSimple())
                        .alwaysDo("FreeWillPriorityPatientDefault".TranslateSimple())
                        .considerHealth()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case DOCTOR:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case PATIENT_BED_REST:
                    return this
                        .set(0.0f, "FreeWillPriorityBedrestDefault".TranslateSimple())
                        .alwaysDo("FreeWillPriorityBedrestDefault".TranslateSimple())
                        .considerHealth()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerBored()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case BASIC_WORKER:
                    return this
                        .set(0.5f, "FreeWillPriorityBasicWorkDefault".TranslateSimple())
                        .considerThoughts()
                        .considerNeedingWarmClothes()
                        .considerHealth()
                        .considerBored()
                        .neverDoIf(this.pawn.Downed, "FreeWillPriorityPawnDowned".TranslateSimple())
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case WARDEN:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case HANDLING:
                    return this
                        .considerRelevantSkills()
                        .considerMovementSpeed()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case COOKING:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerFoodPoisoning()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case HUNTING:
                    return this
                        .considerRelevantSkills()
                        .considerMovementSpeed()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerHasHuntingWeapon()
                        .considerBrawlersNotHunting()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case CONSTRUCTION:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case GROWING:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case MINING:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case PLANT_CUTTING:
                    return this
                        .considerRelevantSkills()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerGauranlenPruning()
                        .considerLowFood()
                        .considerHealth()
                        .considerPlantsBlighted()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case SMITHING:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case TAILORING:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case ART:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case CRAFTING:
                    return this
                        .considerRelevantSkills()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case HAULING:
                    return this
                        .considerBeautyExpectations()
                        .considerMovementSpeed()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case CLEANING:
                    return this
                        .considerBeautyExpectations()
                        .considerIsAnyoneElseDoing()
                        .considerThoughts()
                        .considerOwnRoom()
                        .considerFoodPoisoning()
                        .considerHealth()
                        .considerBored()
                        .neverDoIf(notInHomeArea(this.pawn), "FreeWillPriorityNotInHomeArea".TranslateSimple())
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case RESEARCHING:
                    return this
                        .considerRelevantSkills()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                case HAULING_URGENT:
                    return this
                        .considerBeautyExpectations()
                        .considerMovementSpeed()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;

                default:
                    return this
                        .considerRelevantSkills()
                        .considerMovementSpeed()
                        .considerCarryingCapacity()
                        .considerIsAnyoneElseDoing()
                        .considerPassion()
                        .considerThoughts()
                        .considerInspiration()
                        .considerRefueling()
                        .considerInjuredPets()
                        .considerLowFood()
                        .considerNeedingWarmClothes()
                        .considerColonistLeftUnburied()
                        .considerHealth()
                        .considerAteRawFood()
                        .considerThingsDeteriorating()
                        .considerBored()
                        .considerFire()
                        .considerBuildingImmunity()
                        .considerCompletingTask()
                        .considerColonistsNeedingTreatment()
                        .considerDownedColonists()
                        .considerColonyPolicy()
                        ;
            }
        }

        public void ApplyPriorityToGame()
        {
            if (!Current.Game.playSettings.useWorkPriorities)
            {
                Current.Game.playSettings.useWorkPriorities = true;
            }
            pawn.workSettings.SetPriority(workTypeDef, this.ToGamePriority());
        }

        public string GetTip()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(workTypeDef.description);
            if (!this.disabled)
            {
                int p = this.ToGamePriority();
                string str = string.Format("Priority{0}", p).TranslateSimple();
                string text = str.Colorize(WidgetsWork.ColorOfPriority(p));
                stringBuilder.AppendLine(text);
                stringBuilder.AppendLine("------------------------------");
            }
            foreach (string adj in this.adjustmentStrings)
            {
                stringBuilder.AppendLine(adj);
            }
            return stringBuilder.ToString();
        }

        public int ToGamePriority()
        {
            int valueInt = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(this.value * 100), 0, 100);
            if (valueInt <= disabledCutoff)
            {
                if (this.enabled)
                {
                    return Pawn_WorkSettings.LowestPriority;
                }
                return 0;
            }
            if (this.disabled)
            {
                return 0;
            }
            int invertedValueRange = disabledCutoffActiveWorkArea - (valueInt - disabledCutoff); // 0-80 if LowestPriority is 4
            int gamePriorityValue = UnityEngine.Mathf.FloorToInt((float)invertedValueRange / onePriorityWidth) + 1;
            if (gamePriorityValue > Pawn_WorkSettings.LowestPriority || gamePriorityValue < 1)
            {
                Log.Error("calculated an invalid game priority value of " + gamePriorityValue.ToString());
                gamePriorityValue = UnityEngine.Mathf.Clamp(gamePriorityValue, 1, Pawn_WorkSettings.LowestPriority);
            }

            return gamePriorityValue;
        }

        private Priority set(float x, string s)
        {
            this.value = UnityEngine.Mathf.Clamp01(x);
            if (Prefs.DevMode)
            {
                this.adjustmentStrings.Add("-- reset --");
                this.adjustmentStrings.Add(string.Format("{0} ({1})", this.value.ToStringPercent(), s));
            }
            else
            {
                this.adjustmentStrings = new List<string> { string.Format("{0} ({1})", this.value.ToStringPercent(), s) };
            }
            return this;
        }

        private Priority add(float x, string s)
        {
            if (disabled)
            {
                return this;
            }
            float newValue = UnityEngine.Mathf.Clamp01(value + x);
            if (newValue > value)
            {
                adjustmentStrings.Add(string.Format("+{0} ({1})", (newValue - value).ToStringPercent(), s));
                value = newValue;
            }
            else if (newValue < value)
            {
                adjustmentStrings.Add(string.Format("{0} ({1})", (newValue - value).ToStringPercent(), s));
                value = newValue;
            }
            else if (newValue == value && Prefs.DevMode)
            {
                adjustmentStrings.Add(string.Format("+{0} ({1})", (newValue - value).ToStringPercent(), s));
                value = newValue;
            }
            return this;
        }

        private Priority multiply(float x, string s)
        {
            if (disabled)
            {
                return this;
            }
            float newValue = UnityEngine.Mathf.Clamp01(value * x);
            return add(newValue - value, s);
        }

        private bool isDisabled()
        {
            return this.disabled;
        }

        private Priority alwaysDoIf(bool cond, string s)
        {
            if (!cond || this.enabled)
            {
                return this;
            }
            if (Prefs.DevMode || this.disabled || this.ToGamePriority() == 0)
            {
                string text = string.Format("{0} ({1})", "FreeWillPriorityEnabled".TranslateSimple(), s);
                this.adjustmentStrings.Add(text);
            }
            this.enabled = true;
            this.disabled = false;
            return this;
        }

        private Priority alwaysDo(string s)
        {
            return this.alwaysDoIf(true, s);
        }

        private Priority neverDoIf(bool cond, string s)
        {
            if (!cond || this.disabled)
            {
                return this;
            }
            if (Prefs.DevMode || this.enabled || this.ToGamePriority() >= 0)
            {
                string text = string.Format("{0} ({1})", "FreeWillPriorityDisabled".TranslateSimple(), s);
                this.adjustmentStrings.Add(text);
            }
            this.disabled = true;
            this.enabled = false;
            return this;
        }

        private Priority neverDo(string s)
        {
            return this.neverDoIf(true, s);
        }

        private Priority considerInspiration()
        {
            if (!this.pawn.mindState.inspirationHandler.Inspired)
                return this;
            Inspiration i = this.pawn.mindState.inspirationHandler.CurState;
            foreach (WorkTypeDef workTypeDefB in i?.def?.requiredNonDisabledWorkTypes ?? new List<WorkTypeDef>())
            {
                if (this.workTypeDef.defName == workTypeDefB.defName)
                {
                    return add(0.4f, "FreeWillPriorityInspired".TranslateSimple());
                }
            }
            foreach (WorkTypeDef workTypeDefB in i?.def?.requiredAnyNonDisabledWorkType ?? new List<WorkTypeDef>())
            {
                if (this.workTypeDef.defName == workTypeDefB.defName)
                {
                    return add(0.4f, "FreeWillPriorityInspired".TranslateSimple());
                }
            }
            return this;
        }

        private Priority considerThoughts()
        {
            List<Thought> thoughts = new List<Thought>();
            pawn.needs.mood.thoughts.GetAllMoodThoughts(thoughts);
            foreach (Thought thought in thoughts)
            {
                if (thought.def.defName == "NeedFood")
                {
                    if (workTypeDef.defName == COOKING)
                    {
                        return add(-0.01f * thought.CurStage.baseMoodEffect, "FreeWillPriorityHungerLevel".TranslateSimple());
                    }
                    if (workTypeDef.defName == HUNTING || workTypeDef.defName == PLANT_CUTTING)
                    {
                        return add(-0.005f * thought.CurStage.baseMoodEffect, "FreeWillPriorityHungerLevel".TranslateSimple());
                    }
                    return add(0.005f * thought.CurStage.baseMoodEffect, "FreeWillPriorityHungerLevel".TranslateSimple());
                }
            }
            return this;
        }

        private Priority considerNeedingWarmClothes()
        {
            if (this.workTypeDef.defName == TAILORING && this.mapComp.NeedWarmClothes)
            {
                return add(0.2f, "FreeWillPriorityNeedWarmClothes".TranslateSimple());
            }
            return this;
        }

        private Priority considerColonistLeftUnburied()
        {
            if (this.mapComp.AlertColonistLeftUnburied && (this.workTypeDef.defName == HAULING || this.workTypeDef.defName == HAULING_URGENT))
            {
                return add(0.4f, "AlertColonistLeftUnburied".TranslateSimple());
            }
            return this;
        }

        private Priority considerBored()
        {
            return this.alwaysDoIf(pawn.mindState.IsIdle, "FreeWillPriorityBored".TranslateSimple());
        }

        private Priority considerHasHuntingWeapon()
        {
            if (!Priority.worldComp.settings.ConsiderHasHuntingWeapon)
            {
                return this;
            }
            try
            {
                if (this.workTypeDef.defName != HUNTING)
                {
                    return this;
                }
                return neverDoIf(!WorkGiver_HunterHunt.HasHuntingWeapon(pawn), "FreeWillPriorityNoHuntingWeapon".TranslateSimple());
            }
            catch (System.Exception err)
            {
                Log.Error(pawn.Name + " could not consider has hunting weapon to adjust " + workTypeDef.defName);
                Log.Message(err.ToString());
                Log.Message("this consideration will be disabled in the mod settings to avoid future errors");
                worldComp.settings.ConsiderHasHuntingWeapon = false;
                return this;
            }
        }

        private Priority considerBrawlersNotHunting()
        {
            if (!worldComp.settings.ConsiderBrawlersNotHunting)
            {
                return this;
            }
            try
            {
                if (this.workTypeDef.defName != HUNTING)
                {
                    return this;
                }
                return neverDoIf(this.pawn.story.traits.HasTrait(DefDatabase<TraitDef>.GetNamed("Brawler")), "FreeWillPriorityBrawler".TranslateSimple());
            }
            catch (System.Exception err)
            {
                Log.Error(pawn.Name + " could not consider brawlers can hunt to adjust " + workTypeDef.defName);
                Log.Message(err.ToString());
                Log.Message("this consideration will be disabled in the mod settings to avoid future errors");
                worldComp.settings.ConsiderBrawlersNotHunting = false;
                return this;
            }
        }

        private Priority considerCompletingTask()
        {
            if (pawn.CurJob != null && pawn.CurJob.workGiverDef != null && pawn.CurJob.workGiverDef.workType == workTypeDef)
            {
                return this

                    // pawns should not stop doing the work they are currently
                    // doing
                    .alwaysDo("FreeWillPriorityCurrentlyDoing".TranslateSimple())

                    // pawns prefer the work they are current doing
                    .multiply(1.8f, "FreeWillPriorityCurrentlyDoing".TranslateSimple())

                    ;
            }
            return this;
        }

        private Priority considerMovementSpeed()
        {
            try
            {
                if (worldComp.settings.ConsiderMovementSpeed == 0.0f)
                {
                    return this;
                }
                return this.multiply(
                    (
                        worldComp.settings.ConsiderMovementSpeed
                            * 0.25f
                            * this.pawn.GetStatValue(StatDefOf.MoveSpeed, true)
                    ),
                    "FreeWillPriorityMovementSpeed".TranslateSimple()
                );
            }
            catch (System.Exception err)
            {
                Log.Message(pawn.Name + " could not consider movement speed to adjust " + workTypeDef.defName);
                Log.Message(err.ToString());
                Log.Message("this consideration will be disabled in the mod settings to avoid future errors");
                worldComp.settings.ConsiderMovementSpeed = 0.0f;
                return this;
            }
        }

        private Priority considerCarryingCapacity()
        {
            var _baseCarryingCapacity = 75.0f;
            if (workTypeDef.defName != HAULING && workTypeDef.defName != HAULING_URGENT)
            {
                return this;
            }
            float _carryingCapacity = this.pawn.GetStatValue(StatDefOf.CarryingCapacity, true);
            if (_carryingCapacity >= _baseCarryingCapacity)
            {
                return this;
            }
            return this.multiply(_carryingCapacity / _baseCarryingCapacity, "FreeWillPriorityCarryingCapacity".TranslateSimple());
        }

        private Priority considerPassion()
        {
            var relevantSkills = workTypeDef.relevantSkills;

            for (int i = 0; i < relevantSkills.Count; i++)
            {
                const Passion Apathy = (Passion)3;
                const Passion Natural = (Passion)4;
                const Passion Critical = (Passion)5;
                float x;
                switch (pawn.skills.GetSkill(relevantSkills[i]).passion)
                {
                    case Passion.None:
                        continue;
                    case Passion.Major:
                        x = pawn.needs.mood.CurLevel * 0.5f / relevantSkills.Count;
                        add(x, "FreeWillPriorityMajorPassionFor".TranslateSimple() + " " + relevantSkills[i].skillLabel);
                        continue;
                    case Passion.Minor:
                        x = pawn.needs.mood.CurLevel * 0.25f / relevantSkills.Count;
                        add(x, "FreeWillPriorityMinorPassionFor".TranslateSimple() + " " + relevantSkills[i].skillLabel);
                        continue;
                    case Apathy:
                        x = pawn.needs.mood.CurLevel * 0.15f / relevantSkills.Count;
                        add(x, "FreeWillPriorityApathyPassionFor".TranslateSimple() + " " + relevantSkills[i].skillLabel);
                        continue;
                    case Natural:
                        x = pawn.needs.mood.CurLevel * 0.4f / relevantSkills.Count;
                        add(x, "FreeWillPriorityNaturalPassionFor".TranslateSimple() + " " + relevantSkills[i].skillLabel);
                        continue;
                    case Critical:
                        x = pawn.needs.mood.CurLevel * 0.75f / relevantSkills.Count;
                        add(x, "FreeWillPriorityCriticalPassionFor".TranslateSimple() + " " + relevantSkills[i].skillLabel);
                        continue;
                    default:
                        considerInterest(pawn, relevantSkills[i], relevantSkills.Count, workTypeDef);
                        continue;
                }
            }
            return this;
        }

        private Priority considerInterest(Pawn pawn, SkillDef skillDef, int skillCount, WorkTypeDef workTypeDef)
        {
            if (!Priority.worldComp.HasInterestsFramework())
            {
                return this;
            }
            SkillRecord skillRecord = pawn.skills.GetSkill(skillDef);
            float x;
            string interest;
            try
            {
                interest = Priority.worldComp.interestsStrings[(int)skillRecord.passion];
            }
            catch (System.Exception)
            {
                Log.Message("could not find interest for index " + ((int)skillRecord.passion).ToString());
                return this;
            }
            switch (interest)
            {
                case "DMinorAversion":
                    x = (1.0f - pawn.needs.mood.CurLevel) * -0.25f / skillCount;
                    return add(x, "FreeWillPriorityMinorAversionTo".TranslateSimple() + " " + skillDef.skillLabel);
                case "DMajorAversion":
                    x = (1.0f - pawn.needs.mood.CurLevel) * -0.5f / skillCount;
                    return add(x, "FreeWillPriorityMajorAversionTo".TranslateSimple() + " " + skillDef.skillLabel);
                case "DCompulsion":
                    List<Thought> allThoughts = new List<Thought>();
                    pawn.needs.mood.thoughts.GetAllMoodThoughts(allThoughts);
                    foreach (var thought in allThoughts)
                    {
                        if (thought.def.defName == "CompulsionUnmet")
                        {
                            switch (thought.CurStage.label)
                            {
                                case "compulsive itch":
                                    x = 0.2f / skillCount;
                                    return add(x, "FreeWillPriorityCompulsiveItch".TranslateSimple() + " " + skillDef.skillLabel);
                                case "compulsive need":
                                    x = 0.4f / skillCount;
                                    return add(x, "FreeWillPriorityCompulsiveNeed".TranslateSimple() + " " + skillDef.skillLabel);
                                case "compulsive obsession":
                                    x = 0.6f / skillCount;
                                    return add(x, "FreeWillPriorityCompulsiveObsession".TranslateSimple() + " " + skillDef.skillLabel);
                                default:
                                    Log.Message("could not read compulsion label");
                                    return this;
                            }
                        }
                        if (thought.def.defName == "NeuroticCompulsionUnmet")
                        {
                            switch (thought.CurStage.label)
                            {
                                case "compulsive itch":
                                    x = 0.3f / skillCount;
                                    return add(x, "FreeWillPriorityCompulsiveItch".TranslateSimple() + " " + skillDef.skillLabel);
                                case "compulsive demand":
                                    x = 0.6f / skillCount;
                                    return add(x, "FreeWillPriorityCompulsiveDemand".TranslateSimple() + " " + skillDef.skillLabel);
                                case "compulsive withdrawal":
                                    x = 0.9f / skillCount;
                                    return add(x, "FreeWillPriorityCompulsiveWithdrawl".TranslateSimple() + " " + skillDef.skillLabel);
                                default:
                                    Log.Message("could not read compulsion label");
                                    return this;
                            }
                        }
                        if (thought.def.defName == "VeryNeuroticCompulsionUnmet")
                        {
                            switch (thought.CurStage.label)
                            {
                                case "compulsive yearning":
                                    x = 0.4f / skillCount;
                                    return add(x, "FreeWillPriorityCompulsiveYearning".TranslateSimple() + " " + skillDef.skillLabel);
                                case "compulsive tantrum":
                                    x = 0.8f / skillCount;
                                    return add(x, "FreeWillPriorityCompulsiveTantrum".TranslateSimple() + " " + skillDef.skillLabel);
                                case "compulsive hysteria":
                                    x = 1.2f / skillCount;
                                    return add(x, "FreeWillPriorityCompulsiveHysteria".TranslateSimple() + " " + skillDef.skillLabel);
                                default:
                                    Log.Message("could not read compulsion label");
                                    return this;
                            }
                        }
                    }
                    return this;
                case "DInvigorating":
                    x = 0.1f / skillCount;
                    return add(x, "FreeWillPriorityInvigorating".TranslateSimple() + " " + skillDef.skillLabel);
                case "DInspiring":
                    return this;
                case "DStagnant":
                    return this;
                case "DForgetful":
                    return this;
                case "DVocalHatred":
                    return this;
                case "DNaturalGenius":
                    return this;
                case "DBored":
                    if (pawn.mindState.IsIdle)
                    {
                        return this;
                    }
                    return neverDo("FreeWillPriorityBoredBy".TranslateSimple() + " " + skillDef.skillLabel);
                case "DAllergic":
                    foreach (var hediff in pawn.health.hediffSet.GetHediffs<Hediff>())
                    {
                        if (hediff.def.defName == "DAllergicReaction")
                        {
                            switch (hediff.CurStage.label)
                            {
                                case "initial":
                                    x = -0.2f / skillCount;
                                    return add(x, "FreeWillPriorityReactionInitial".TranslateSimple() + " " + skillDef.skillLabel);
                                case "itching":
                                    x = -0.5f / skillCount;
                                    return add(x, "FreeWillPriorityReactionItching".TranslateSimple() + " " + skillDef.skillLabel);
                                case "sneezing":
                                    x = -0.8f / skillCount;
                                    return add(x, "FreeWillPriorityReactionSneezing".TranslateSimple() + " " + skillDef.skillLabel);
                                case "swelling":
                                    x = -1.1f / skillCount;
                                    return add(x, "FreeWillPriorityReactionSwelling".TranslateSimple() + " " + skillDef.skillLabel);
                                case "anaphylaxis":
                                    return neverDo("FreeWillPriorityReactionAnaphylaxis".TranslateSimple() + " " + skillDef.skillLabel);
                                default:
                                    break;
                            }
                        }
                        x = 0.1f / skillCount;
                        return add(x, "FreeWillPriorityNoReaction".TranslateSimple() + " " + skillDef.skillLabel);
                    }
                    return this;
                default:
                    Log.Message("did not recognize interest: " + skillRecord.passion.ToString());
                    return this;
            }
        }

        private Priority considerDownedColonists()
        {
            if (pawn.Downed)
            {
                if (workTypeDef.defName == PATIENT || workTypeDef.defName == PATIENT_BED_REST)
                {
                    return alwaysDo("FreeWillPriorityPawnDowned".TranslateSimple()).set(1.0f, "FreeWillPriorityPawnDowned".TranslateSimple());
                }
                return neverDo("FreeWillPriorityPawnDowned".TranslateSimple());
            }
            if (mapComp.PercentPawnsDowned <= 0.0f)
            {
                return this;
            }
            if (workTypeDef.defName == DOCTOR)
            {
                return add(mapComp.PercentPawnsDowned, "FreeWillPriorityOtherPawnsDowned".TranslateSimple());
            }
            if (workTypeDef.defName == SMITHING ||
                workTypeDef.defName == TAILORING ||
                workTypeDef.defName == ART ||
                workTypeDef.defName == CRAFTING ||
                workTypeDef.defName == RESEARCHING
                )
            {
                return neverDo("FreeWillPriorityOtherPawnsDowned".TranslateSimple());
            }
            return this;
        }

        private Priority considerColonyPolicy()
        {
            try
            {
                this.add(worldComp.settings.globalWorkAdjustments[this.workTypeDef.defName], "FreeWillPriorityColonyPolicy".TranslateSimple());
            }
            catch (System.Exception)
            {
                worldComp.settings.globalWorkAdjustments[this.workTypeDef.defName] = 0.0f;
            }
            return this;
        }

        private Priority considerRefueling()
        {
            if (workTypeDef.defName != HAULING && workTypeDef.defName != HAULING_URGENT)
            {
                return this;
            }
            if (mapComp.RefuelNeededNow)
            {
                return this.add(0.25f, "FreeWillPriorityRefueling".TranslateSimple());
            }
            if (mapComp.RefuelNeeded)
            {
                return this.add(0.10f, "FreeWillPriorityRefueling".TranslateSimple());
            }
            return this;
        }

        private Priority considerFire()
        {
            if (mapComp.HomeFire)
            {
                if (workTypeDef.defName != FIREFIGHTER)
                {
                    return add(-0.2f, "FreeWillPriorityFireInHomeArea".TranslateSimple());
                }
                return set(1.0f, "FreeWillPriorityFireInHomeArea".TranslateSimple());
            }
            if (mapComp.MapFires > 0 && workTypeDef.defName == FIREFIGHTER)
            {
                return add(Mathf.Clamp01(mapComp.MapFires * 0.01f), "FreeWillPriorityFireOnMap".TranslateSimple());
            }
            return this;
        }

        private Priority considerBuildingImmunity()
        {
            try
            {
                if (!pawn.health.hediffSet.HasImmunizableNotImmuneHediff())
                {
                    return this;
                }
                if (workTypeDef.defName == PATIENT_BED_REST)
                {
                    return add(0.4f, "FreeWillPriorityBuildingImmunity".TranslateSimple());
                }
                if (workTypeDef.defName == PATIENT)
                {
                    return this;
                }
                return add(-0.2f, "FreeWillPriorityBuildingImmunity".TranslateSimple());
            }
            catch
            {
                Log.Message("could not consider pawn building immunity");
                return this;
            }
        }

        private Priority considerColonistsNeedingTreatment()
        {
            if (mapComp.PercentPawnsNeedingTreatment <= 0.0f)
            {
                return this;
            }

            if (pawn.health.HasHediffsNeedingTend())
            {
                // this pawn needs treatment
                return this.considerThisPawnNeedsTreatment();
            }
            else
            {
                // another pawn needs treatment
                return this.considerAnotherPawnNeedsTreatment();
            }
        }

        private Priority considerThisPawnNeedsTreatment()
        {

            if (workTypeDef.defName == PATIENT || workTypeDef.defName == PATIENT_BED_REST)
            {
                // patient and bed rest are activated and set to 100%
                return this
                    .alwaysDo("FreeWillPriorityNeedTreatment".TranslateSimple())
                    .set(1.0f, "FreeWillPriorityNeedTreatment".TranslateSimple())
                    ;
            }
            if (workTypeDef.defName == DOCTOR)
            {
                if (pawn.playerSettings.selfTend)
                {
                    // this pawn can self tend, so activate doctor skill and set
                    // to 100%
                    return this
                        .alwaysDo("FreeWillPriorityNeedTreatmentSelfTend".TranslateSimple())
                        .set(1.0f, "FreeWillPriorityNeedTreatmentSelfTend".TranslateSimple())
                        ;
                }
                // doctoring stays the same
                return this;
            }
            // don't do other work types
            return neverDo("FreeWillPriorityNeedTreatment".TranslateSimple());
        }

        private Priority considerAnotherPawnNeedsTreatment()
        {
            if (workTypeDef.defName == FIREFIGHTER ||
                workTypeDef.defName == PATIENT_BED_REST
                )
            {
                // don't adjust these work types
                return this;
            }

            // increase doctor priority for all pawns
            if (workTypeDef.defName == DOCTOR)
            {
                // increase the doctor priority by the percentage of pawns
                // needing treatment
                //
                // so if 25% of the colony is injured, doctoring for all
                // non-injured pawns will increase by 25%
                return add(mapComp.PercentPawnsNeedingTreatment, "FreeWillPriorityOthersNeedTreatment".TranslateSimple());
            }

            if (workTypeDef.defName == RESEARCHING)
            {
                // don't research when someone is dying please... it's rude
                return neverDo("FreeWillPriorityOthersNeedTreatment".TranslateSimple());
            }

            if (workTypeDef.defName == SMITHING ||
                workTypeDef.defName == TAILORING ||
                workTypeDef.defName == ART ||
                workTypeDef.defName == CRAFTING
                )
            {
                // crafting work types are low priority when someone is injured
                if (this.value > 0.3f)
                {
                    return add(-(this.value - 0.3f), "FreeWillPriorityOthersNeedTreatment".TranslateSimple());
                }
                return this;
            }

            // any other work type is capped at 0.6
            if (this.value > 0.6f)
            {
                return add(-(this.value - 0.6f), "FreeWillPriorityOthersNeedTreatment".TranslateSimple());
            }
            return this;
        }

        private Priority considerHealth()
        {
            if (this.workTypeDef.defName == PATIENT || this.workTypeDef.defName == PATIENT_BED_REST)
            {
                return add(1 - Mathf.Pow(this.pawn.health.summaryHealth.SummaryHealthPercent, 7.0f), "FreeWillPriorityHealth".TranslateSimple());
            }
            return multiply(this.pawn.health.summaryHealth.SummaryHealthPercent, "FreeWillPriorityHealth".TranslateSimple());
        }

        private Priority considerFoodPoisoning()
        {
            if (worldComp.settings.ConsiderFoodPoisoning == 0.0f)
            {
                return this;
            }
            try
            {
                if (this.workTypeDef.defName != CLEANING && this.workTypeDef.defName != COOKING)
                {
                    return this;
                }

                var adjustment = 0.0f;
                var room = pawn.GetRoom();
                if (room.TouchesMapEdge)
                {
                    return this;
                }
                if (room.IsHuge)
                {
                    return this;
                }
                foreach (Building building in room.ContainedAndAdjacentThings.OfType<Building>())
                {
                    if (building == null)
                    {
                        continue;
                    }
                    if (building.Faction != Faction.OfPlayer)
                    {
                        continue;
                    }
                    if (building.def.building.isMealSource)
                    {
                        adjustment =
                            (
                                worldComp.settings.ConsiderFoodPoisoning
                                * 20.0f
                                * pawn.GetRoom().GetStat(RoomStatDefOf.FoodPoisonChance)
                            );
                        if (this.workTypeDef.defName == CLEANING)
                        {
                            return add(adjustment, "FreeWillPriorityFilthyCookingArea".TranslateSimple());
                        }
                        if (this.workTypeDef.defName == COOKING)
                        {
                            return add(-adjustment, "FreeWillPriorityFilthyCookingArea".TranslateSimple());
                        }
                    }
                }
                return this;
            }
            catch (System.Exception err)
            {
                Log.Error(pawn.Name + " could not consider food poisoning to adjust " + workTypeDef.defName);
                Log.Message(err.ToString());
                Log.Message("this consideration will be disabled in the mod settings to avoid future errors");
                worldComp.settings.ConsiderFoodPoisoning = 0.0f;
                return this;
            }
        }

        private Priority considerOwnRoom()
        {
            if (worldComp.settings.ConsiderOwnRoom == 0.0f)
            {
                return this;
            }
            try
            {
                if (this.workTypeDef.defName != CLEANING)
                {
                    return this;
                }
                var room = pawn.GetRoom();
                var isPawnsRoom = false;
                foreach (Pawn owner in room.Owners)
                {
                    if (pawn == owner)
                    {
                        isPawnsRoom = true;
                        break;
                    }
                }
                if (!isPawnsRoom)
                {
                    return this;
                }
                return multiply(worldComp.settings.ConsiderOwnRoom * 2.0f, "FreeWillPriorityOwnRoom".TranslateSimple());
            }
            catch (System.Exception err)
            {
                Log.Message(pawn.Name + " could not consider being in own room to adjust " + workTypeDef.defName);
                Log.Message(err.ToString());
                Log.Message("this consideration will be disabled in the mod settings to avoid future errors");
                worldComp.settings.ConsiderOwnRoom = 0.0f;
                return this;
            }
        }

        private Priority considerIsAnyoneElseDoing()
        {
            float pawnSkill = this.pawn.skills.AverageOfRelevantSkillsFor(this.workTypeDef);
            foreach (Pawn other in this.pawn.Map.mapPawns.FreeColonistsSpawned)
            {
                if (other == this.pawn)
                {
                    continue;
                }
                if (!other.Awake() || other.Downed || other.Dead)
                {
                    continue;
                }
                if (other.workSettings.GetPriority(this.workTypeDef) != 0)
                {
                    return this; // someone else is doing
                }
            }
            return this.alwaysDo("FreeWillPriorityNoOneElseDoing".TranslateSimple());
        }

        private Priority considerInjuredPets()
        {
            if (workTypeDef.defName == DOCTOR)
            {
                int n = mapComp.NumPawns;
                if (n == 0)
                {
                    return this;
                }
                float numPetsNeedingTreatment = mapComp.NumPetsNeedingTreatment;
                return add(UnityEngine.Mathf.Clamp01(numPetsNeedingTreatment / ((float)n)) * 0.5f, "FreeWillPriorityPetsInjured".TranslateSimple());
            }
            return this;
        }

        private Priority considerLowFood()
        {
            if (this.mapComp.TotalFood < 4f * (float)this.mapComp.NumPawns)
            {
                if (this.workTypeDef.defName == COOKING)
                {
                    return this.add(0.4f, "FreeWillPriorityLowFood".TranslateSimple());
                }
                if (this.workTypeDef.defName == HUNTING || this.workTypeDef.defName == PLANT_CUTTING)
                {
                    return this.add(0.2f, "FreeWillPriorityLowFood".TranslateSimple());
                }
                if ((this.workTypeDef.defName == HAULING || this.workTypeDef.defName == HAULING_URGENT)
                    && this.pawn.Map.GetComponent<FreeWill_MapComponent>().ThingsDeteriorating)
                {
                    return this.add(0.15f, "FreeWillPriorityLowFood".TranslateSimple());
                }
            }
            return this;
        }

        private Priority considerAteRawFood()
        {
            if (this.workTypeDef.defName != COOKING)
            {
                return this;
            }

            List<Thought> allThoughts = new List<Thought>();
            this.pawn.needs.mood.thoughts.GetAllMoodThoughts(allThoughts);
            for (int i = 0; i < allThoughts.Count; i++)
            {
                Thought thought = allThoughts[i];
                if (thought.def.defName == "AteRawFood")
                {
                    if (0.6f > value)
                    {
                        return this.set(0.6f, "FreeWillPriorityAteRawFood".TranslateSimple());
                    }
                }
            }
            return this;
        }

        private Priority considerThingsDeteriorating()
        {
            if (this.workTypeDef.defName == HAULING || this.workTypeDef.defName == HAULING_URGENT)
            {
                if (this.pawn.Map.GetComponent<FreeWill_MapComponent>().ThingsDeteriorating)
                {
                    return this.add(0.2f, "FreeWillPriorityThingsDeteriorating".TranslateSimple());
                }
            }
            return this;
        }

        private Priority considerPlantsBlighted()
        {
            try
            {
                if (worldComp.settings.ConsiderPlantsBlighted == 0.0f)
                {
                    // no point checking if it is disabled
                    return this;
                }
                if (this.mapComp.PlantsBlighted)
                {
                    return this.add(0.4f * worldComp.settings.ConsiderPlantsBlighted, "FreeWillPriorityBlight".TranslateSimple());
                }
            }
            catch (System.Exception err)
            {
                Log.Message("could not consider blight levels");
                Log.Message(err.ToString());
                Log.Message("this consideration will be disabled in the mod settings to avoid future errors");
                worldComp.settings.ConsiderPlantsBlighted = 0.0f;
            }
            return this;
        }

        private Priority considerGauranlenPruning()
        {
            try
            {
                if (workTypeDef.defName != PLANT_CUTTING)
                {
                    return this;
                }
                foreach (Thing connectedThing in pawn.connections.ConnectedThings)
                {
                    CompTreeConnection compTreeConnection = connectedThing.TryGetComp<CompTreeConnection>();
                    if (compTreeConnection != null && compTreeConnection.Mode != null)
                    {
                        if (!compTreeConnection.ShouldBePrunedNow(false))
                        {
                            return this;
                        }
                        {
                            return this.multiply(2.0f * worldComp.settings.ConsiderGauranlenPruning, "FreeWillPriorityPruneGauranlenTree".TranslateSimple());
                        }
                    }
                }
            }
            catch (System.Exception err)
            {
                Log.ErrorOnce("could not consider pruning gauranlen tree: " + "this consideration will be disabled in the mod settings to avoid future errors" + err.ToString(), 45846314);
                worldComp.settings.ConsiderGauranlenPruning = 0.0f;
            }
            return this;
        }

        private Priority considerBeautyExpectations()
        {
            if (this.workTypeDef.defName != CLEANING && this.workTypeDef.defName != HAULING && this.workTypeDef.defName != HAULING_URGENT)
            {
                return this;
            }
            try
            {
                float e = expectationGrid[ExpectationsUtility.CurrentExpectationFor(this.pawn).defName][this.pawn.needs.beauty.CurCategory];
                if (e < 0.2f)
                {
                    return this.set(e, "FreeWillPriorityExpectionsExceeded".TranslateSimple());
                }
                if (e < 0.4f)
                {
                    return this.set(e, "FreeWillPriorityExpectionsMet".TranslateSimple());
                }
                if (e < 0.6f)
                {
                    return this.set(e, "FreeWillPriorityExpectionsUnmet".TranslateSimple());
                }
                if (e < 0.8f)
                {
                    return this.set(e, "FreeWillPriorityExpectionsLetDown".TranslateSimple());
                }
                return this.set(e, "FreeWillPriorityExpectionsIgnored".TranslateSimple());
            }
            catch
            {
                return this.set(0.3f, "FreeWillPriorityBeautyDefault".TranslateSimple());
            }
        }

        private Priority considerRelevantSkills()
        {
            float _badSkillCutoff = Mathf.Min(3f, this.mapComp.NumPawns);
            float _goodSkillCutoff = _badSkillCutoff + (20f - _badSkillCutoff) / 2f;
            float _greatSkillCutoff = _goodSkillCutoff + (20f - _goodSkillCutoff) / 2f;
            float _excellentSkillCutoff = _greatSkillCutoff + (20f - _greatSkillCutoff) / 2f;

            float _avg = this.pawn.skills.AverageOfRelevantSkillsFor(this.workTypeDef);
            if (_avg >= _excellentSkillCutoff)
            {
                return this.set(0.9f, string.Format("{0} {1:f0}", "FreeWillPrioritySkillLevel".TranslateSimple(), _avg));
            }
            if (_avg >= _greatSkillCutoff)
            {
                return this.set(0.7f, string.Format("{0} {1:f0}", "FreeWillPrioritySkillLevel".TranslateSimple(), _avg));
            }
            if (_avg >= _goodSkillCutoff)
            {
                return this.set(0.5f, string.Format("{0} {1:f0}", "FreeWillPrioritySkillLevel".TranslateSimple(), _avg));
            }
            if (_avg >= _badSkillCutoff)
            {
                return this.set(0.3f, string.Format("{0} {1:f0}", "FreeWillPrioritySkillLevel".TranslateSimple(), _avg));
            }
            return this.set(0.1f, string.Format("{0} {1:f0}", "FreeWillPrioritySkillLevel".TranslateSimple(), _avg));
        }

        private bool notInHomeArea(Pawn pawn)
        {
            return !this.pawn.Map.areaManager.Home[pawn.Position];
        }

        private static Dictionary<string, Dictionary<BeautyCategory, float>> expectationGrid =
            new Dictionary<string, Dictionary<BeautyCategory, float>>
            {
                {
                    "ExtremelyLow", new Dictionary<BeautyCategory, float>
                        {
                            { BeautyCategory.Hideous, 0.3f },
                            { BeautyCategory.VeryUgly, 0.2f },
                            { BeautyCategory.Ugly, 0.1f },
                            { BeautyCategory.Neutral, 0.0f },
                            { BeautyCategory.Pretty, 0.0f },
                            { BeautyCategory.VeryPretty, 0.0f },
                            { BeautyCategory.Beautiful, 0.0f },
                        }
                },
                {
                    "VeryLow", new Dictionary<BeautyCategory, float>
                        {
                            { BeautyCategory.Hideous, 0.5f },
                            { BeautyCategory.VeryUgly, 0.3f },
                            { BeautyCategory.Ugly, 0.2f },
                            { BeautyCategory.Neutral, 0.1f },
                            { BeautyCategory.Pretty, 0.0f },
                            { BeautyCategory.VeryPretty, 0.0f },
                            { BeautyCategory.Beautiful, 0.0f },
                        }
                },
                {
                    "Low", new Dictionary<BeautyCategory, float>
                        {
                            { BeautyCategory.Hideous, 0.7f },
                            { BeautyCategory.VeryUgly, 0.5f },
                            { BeautyCategory.Ugly, 0.3f },
                            { BeautyCategory.Neutral, 0.2f },
                            { BeautyCategory.Pretty, 0.1f },
                            { BeautyCategory.VeryPretty, 0.0f },
                            { BeautyCategory.Beautiful, 0.0f },
                        }
                },
                {
                    "Moderate", new Dictionary<BeautyCategory, float>
                        {
                            { BeautyCategory.Hideous, 0.8f },
                            { BeautyCategory.VeryUgly, 0.7f },
                            { BeautyCategory.Ugly, 0.5f },
                            { BeautyCategory.Neutral, 0.3f },
                            { BeautyCategory.Pretty, 0.2f },
                            { BeautyCategory.VeryPretty, 0.1f },
                            { BeautyCategory.Beautiful, 0.0f },
                        }
                },
                {
                    "High", new Dictionary<BeautyCategory, float>
                        {
                            { BeautyCategory.Hideous, 0.9f },
                            { BeautyCategory.VeryUgly, 0.8f },
                            { BeautyCategory.Ugly, 0.7f },
                            { BeautyCategory.Neutral, 0.5f },
                            { BeautyCategory.Pretty, 0.3f },
                            { BeautyCategory.VeryPretty, 0.2f },
                            { BeautyCategory.Beautiful, 0.1f },
                        }
                },
                {
                    "SkyHigh", new Dictionary<BeautyCategory, float>
                        {
                            { BeautyCategory.Hideous, 1.0f },
                            { BeautyCategory.VeryUgly, 0.9f },
                            { BeautyCategory.Ugly, 0.8f },
                            { BeautyCategory.Neutral, 0.7f },
                            { BeautyCategory.Pretty, 0.5f },
                            { BeautyCategory.VeryPretty, 0.3f },
                            { BeautyCategory.Beautiful, 0.2f },
                        }
                },
                {
                    "Noble", new Dictionary<BeautyCategory, float>
                        {
                            { BeautyCategory.Hideous, 1.0f },
                            { BeautyCategory.VeryUgly, 1.0f },
                            { BeautyCategory.Ugly, 0.9f },
                            { BeautyCategory.Neutral, 0.8f },
                            { BeautyCategory.Pretty, 0.7f },
                            { BeautyCategory.VeryPretty, 0.5f },
                            { BeautyCategory.Beautiful, 0.3f },
                        }
                },
                {
                    "Royal", new Dictionary<BeautyCategory, float>
                        {
                            { BeautyCategory.Hideous, 1.0f },
                            { BeautyCategory.VeryUgly, 1.0f },
                            { BeautyCategory.Ugly, 1.0f },
                            { BeautyCategory.Neutral, 0.9f },
                            { BeautyCategory.Pretty, 0.8f },
                            { BeautyCategory.VeryPretty, 0.7f },
                            { BeautyCategory.Beautiful, 0.5f },
                        }
                },
            };
    }
}
