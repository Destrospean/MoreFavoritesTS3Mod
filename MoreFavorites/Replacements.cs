using System;
using System.Collections.Generic;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.Appliances;
using Sims3.Gameplay.Objects.CookingObjects;
using Sims3.Gameplay.Objects.Counters;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.Objects.Electronics;
using Sims3.Gameplay.Objects.Seating;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.SimIFace.Enums;
using Sims3.UI;
using Sims3.UI.CAS;

namespace Destrospean.MoreFavorites
{
    public class Replacements
    {
        public class ChangeFavoritesDialogPatch : ChangeFavoritesDialog
        {
            public ChangeFavoritesDialogPatch(ISimDescription sim) : base(sim)
            {
            }

            public new void OnFavoritesVisibilityChange(WindowBase sender, UIVisibilityChangeEventArgs args)
            {
                if (args.Visible)
                {
                    sender.Tick += OnFavoritesVisibilityTick;
                    switch (sender.ID)
                    {
                        case 100663297:
                            {
                                Array unblacklistedFavorites = Array.FindAll((FavoriteFoodType[])GetInstalledFavoriteFoodList(), x => !x.IsBlacklisted());
                                PopulateFavoritesGrid(mGridFavoriteFood, unblacklistedFavorites, 0, unblacklistedFavorites.Length);
                                break;
                            }
                        case 100663301:
                            {
                                Array unblacklistedFavorites = Array.FindAll((FavoriteMusicType[])GetInstalledFavoriteMusicList(), x => !x.IsBlacklisted());
                                PopulateFavoritesGrid(mGridFavoriteMusic, unblacklistedFavorites, 0, unblacklistedFavorites.Length);
                                break;
                            }
                        case 100663305:
                            {
                                Array unblacklistedFavorites = Array.FindAll(CASCharacter.kColors, x => !x.IsBlacklisted());
                                PopulateFavoritesGrid(mGridFavoriteColor, unblacklistedFavorites, 0, unblacklistedFavorites.Length);
                                break;
                            }
                    }
                }
                else
                {
                    sender.Tick -= OnFavoritesVisibilityTick;
                }
            }

            public new void RandomizeAllFavorites(bool missingFavoritesOnly)
            {
                Random random = new Random();
                FavoriteFoodType favoriteFood = mResult.mFavoriteFood;
                if (!missingFavoritesOnly || favoriteFood == FavoriteFoodType.None)
                {
                    Array unblacklistedFavoriteFoodList = Array.FindAll((FavoriteFoodType[])GetInstalledFavoriteFoodList(), x => !x.IsBlacklisted());
                    favoriteFood = (FavoriteFoodType)unblacklistedFavoriteFoodList.GetValue(random.Next(1, unblacklistedFavoriteFoodList.Length));
                    if (mSim.IsVegetarian)
                    {
                        IRecipe recipe = Responder.Instance.CASModel.GetRecipe(favoriteFood);
                        while (!recipe.IsVegetarian && !recipe.HasVegetarianAlternative)
                        {
                            favoriteFood = (FavoriteFoodType)unblacklistedFavoriteFoodList.GetValue(random.Next(1, unblacklistedFavoriteFoodList.Length));
                            recipe = Responder.Instance.CASModel.GetRecipe(favoriteFood);
                        }
                    }
                }
                FavoriteMusicType favoriteMusic = mResult.mFavoriteMusic;
                Array blacklistedFavoriteMusicList = Array.FindAll((FavoriteMusicType[])GetInstalledFavoriteMusicList(), x => !x.IsBlacklisted());
                for (favoriteMusic = FavoriteMusicType.Custom; favoriteMusic == FavoriteMusicType.Custom; favoriteMusic = (FavoriteMusicType)blacklistedFavoriteMusicList.GetValue(random.Next(1, blacklistedFavoriteMusicList.Length)))
                {
                }
                Color favoriteColor = mResult.mFavoriteColor;
                if (!missingFavoritesOnly || favoriteColor.ARGB == 0)
                {
                    Array unblacklistedFavoriteColorList = Array.FindAll(CASCharacter.kColors, x => !x.IsBlacklisted());
                    favoriteColor = ((CASCharacter.NameColorPair)unblacklistedFavoriteColorList.GetValue(random.Next(0, unblacklistedFavoriteColorList.Length))).mColor;
                }
                mResult.mFavoriteFood = favoriteFood;
                mResult.mFavoriteMusic = favoriteMusic;
                mResult.mFavoriteColor = favoriteColor;
                UpdateFavoritesButtons();
            }
        }

        public class EatHeldFoodPatch : EatHeldFood
        {
            public override bool Run()
            {
                if (Actor.Posture != null && !Actor.Posture.Satisfies(CommodityKind.Sitting, null) && !(Target is PoisonedApple))
                {
                    Actor.Wander(ServingContainerGroup.kMinDistanceToMoveAwayAfterGrabbingPlate, ServingContainerGroup.kMaxDistanceToMoveAwayAfterGrabbingPlate, true, RouteDistancePreference.NoPreference, true);
                }
                Target.BeforeEatingCallback(Actor);
                if (!InteractionTest(Actor, Target))
                {
                    if (Target.Parent == Actor)
                    {
                        Actor.InteractionQueue.PushAsContinuation(CarrySystem.PutDownHeldObject.Singleton, Target, false, Actor.InheritedPriority(), true);
                    }
                    return false;
                }
                bool addToUseList = true;
                if (Target.Parent == Actor)
                {
                    addToUseList = false;
                }
                if (Autonomous)
                {
                    mPriority = new InteractionPriority(InteractionPriorityLevel.UserDirected);
                }
                StandardEntry(addToUseList);
                ChairDining chairDining = Actor.Posture.Container as ChairDining;
                if (chairDining != null && chairDining.Parent != null && chairDining.ChairState == ChairDining.State.Angled && Target.Parent != Actor)
                {
                    SitTransitionAngledToStraight.Singleton.CreateInstance(Actor.Posture.Container, Actor, GetPriority(), false, false).RunInteraction();
                }
                Food.PreEat(Actor, Target as GameObject, ref mIsSufficientlyFullForStuffed, ref mHasFatDelta);
                mCurrentStateMachine = StateMachineClient.Acquire(Actor, "eat", AnimationPriority.kAPCarryRightPlus);
                SetActor("x", Actor);
                SetParameter("isWerewolf", chairDining != null && Actor.BuffManager.HasElement(BuffNames.Werewolf));
                SetActor("ServingContainer", Target);
                SetActor("thingToEat", Target.ThingToEat);
                Counter counter = Target.Parent as Counter;
                if (counter != null)
                {
                    SetActor("Counter", counter);
                    SetParameter("IKSuffix", counter.IkSuffix);
                }
                mCurrentStateMachine.EnterState("x", "Enter");
                if (Actor.Posture != null && Actor.Posture.Satisfies(CommodityKind.Sitting, Target))
                {
                    SetActor("sitTemplate", Actor.Posture.Container);
                    SetParameter("sitTemplateSuffix", ((SittingPosture)Actor.Posture).Part.Target.IKSuffix);
                }
                EatingPosture eatingPosture = GetPostureParam();
                SetParameter("eatPosture", eatingPosture);
                FavoritesUtils.FavoriteFood favoriteFood;
                SetParameter("isFavorite", Actor.SimDescription.FavoriteFood != 0 && Target.Recipe != null && (Target.Recipe.Favorite == Actor.SimDescription.FavoriteFood || FavoritesUtils.FavoriteFoodDictionary.TryGetValue(Actor.SimDescription.FavoriteFood, out favoriteFood) && (favoriteFood.Name == Target.Recipe.Key || Array.Exists(favoriteFood.Children, x => x == Target.Recipe.Key))));
                SetParameter("isSloppy", Actor.HasTrait(TraitNames.Slob));
                SetParameter("isSpoiled", Target.Spoiled);
                SetParameter("isIceCream", Target is SnackIceCream);
                NectarBottle.NectarGlass nectarGlass = Target as NectarBottle.NectarGlass;
                if (nectarGlass != null)
                {
                    nectarGlass.SetParameters(Actor, this);
                }
                if (Target is FutureBar.FutureBarGlass && Target.Spoiled)
                {
                    SetParameter("isServoJuice", true);
                }
                UtensilType utensilType = Target.UtensilType;
                if (Target.UtensilName == "fork" && (Actor.HasTrait(TraitNames.CultureChina) || GameUtils.GetCurrentWorld() == WorldName.China))
                {
                    utensilType = UtensilType.chopsticks;
                }
                SetParameter("UtensilType", utensilType);
                if (Target.CountsAsFood && !Target.IsDrinkable)
                {
                    switch (eatingPosture)
                    {
                        case EatingPosture.diningIn:
                            if (utensilType == UtensilType.fork || utensilType == UtensilType.hand)
                            {
                                mAllowsThrowScrap = true;
                            }
                            break;
                        case EatingPosture.diningOut:
                            if (utensilType == UtensilType.fork)
                            {
                                mAllowsThrowScrap = true;
                            }
                            break;
                    }
                    mPetWatchSimEatingHelper.Watchable = true;
                    mPetWatchSimEatingHelper.TriggerReactionBroadcaster(Actor);
                }
                mPropHandle = ObjectGuid.InvalidObjectGuid;
                if (utensilType != UtensilType.hand)
                {
                    if (utensilType == UtensilType.chopsticks)
                    {
                        mPropHandle = GlobalFunctions.CreateProp("UtensilChopsticks", Vector3.OutOfWorld, 0, Vector3.UnitZ);
                    }
                    else
                    {
                        mPropHandle = GlobalFunctions.CreateProp(GetUtensilMedatorName(Target.UtensilName), Vector3.OutOfWorld, 0, Vector3.UnitZ);
                    }
                    if (mPropHandle != ObjectGuid.InvalidObjectGuid)
                    {
                        mCurrentStateMachine.SetPropActor("utensil", mPropHandle);
                    }
                }
                mCurrentStateMachine.AddOneShotScriptEventHandler(101, new ObjectHideHelper(Target).Callback);
                mCurrentStateMachine.AddOneShotScriptEventHandler(105, ParentFoodToContainer);
                mCurrentStateMachine.AddPersistentScriptEventHandler(501, StartSloppyVFX);
                mCurrentStateMachine.AddPersistentScriptEventHandler(502, StopSloppyVFX);
                if (eatingPosture == EatingPosture.living)
                {
                    Seat.EnsureLivingChairPosture(Actor);
                }
                AnimateSim(Target.EatLoopStateName);
                SetParameter("wasInterrupted", true);
                AddMotiveArrow(CommodityKind.Hunger, true);
                BeginCommodityUpdates();
                if (Target.IsVampireFood)
                {
                    AddMotiveDelta(CommodityKind.VampireThirst, SnackVampireJuice.kThirstPerHour);
                }
                ReactionBroadcaster reactionBroadcaster = null;
                if (Target.Recipe != null && Target.Recipe.Key == "BrainFreeze")
                {
                    AddMotiveDelta(CommodityKind.BeAZombie, BuffZombieFreeze.kZombieCommodityChangeRate);
                    reactionBroadcaster = new ReactionBroadcaster(Actor, BuffZombieFreeze.kBrainFreezeCreepedOutBroadcasterParams, OnEnterCreepedOut);
                }
                Actor.RegisterGroupTalk();
                OccultImaginaryFriend.GrantMilestoneBuff(Actor, BuffNames.ImaginaryFriendAteFood, Origin.FromImaginaryFriendFirstTime, false, true, false);
                bool succeeded = DoLoop(ExitReason.Default, LoopCallback, mCurrentStateMachine);
                HotBeverageMachine.Cup cup = Target.ThingToEat as HotBeverageMachine.Cup;
                if (succeeded && GameUtils.IsInstalled(ProductVersion.EP9) && cup != null && cup.IsEnergyDrink)
                {
                    Actor.BuffManager.AddElement(BuffNames.LiquidEnergy, Origin.FromEnergyDrink);
                }
                Actor.UnregisterGroupTalk();
                EndCommodityUpdates(succeeded);
                RemoveMotiveDelta(CommodityKind.VampireThirst);
                RemoveMotiveDelta(CommodityKind.BeAZombie);
                Herb.HerbData herbData = default(Herb.HerbData);
                if (Target.GetAddedHerb(ref herbData))
                {
                    Herb.ApplyHerbEffects(Actor, Herb.IngestionType.EatWithFood, herbData);
                }
                if (reactionBroadcaster != null)
                {
                    reactionBroadcaster.EndBroadcast();
                    reactionBroadcaster.Dispose();
                    reactionBroadcaster = null;
                }
                SetParameter("wasInterrupted", !Actor.HasExitReason(ExitReason.Finished));
                mCurrentStateMachine.RemoveEventHandler(StartSloppyVFX);
                mCurrentStateMachine.RemoveEventHandler(StopSloppyVFX);
                AnimateSim("Exit");
                mPetWatchSimEatingHelper.Watchable = false;
                if (mPropHandle != ObjectGuid.InvalidObjectGuid)
                {
                    Simulator.DestroyObject(mPropHandle);
                }
                if (Actor.HasExitReason())
                {
                    Actor.RemoveExitReason(ExitReason.MoodFailure);
                    if (Actor.OnlyHasExitReason(ExitReason.Finished))
                    {
                        Target.FinishedEatingCallback(Actor);
                        if (Autonomous && Actor.Posture.Container != null && Actor.Posture.Container.Parent != null)
                        {
                            IEatingSurface eatingSurface = Actor.Posture.Container.Parent as IEatingSurface;
                            if (eatingSurface != null && eatingSurface.AllowWaitForOthers && (eatingSurface.NumOtherSimsAtSurface(Actor) > 0 || ((GameObject)eatingSurface).ReferenceList.Count > 0))
                            {
                                Actor.LoopIdle();
                                Actor.RegisterGroupTalk();
                                mTimeWaitForOtherStarted = Sims3.Gameplay.Utilities.SimClock.CurrentTime();
                                Actor.RemoveExitReason(ExitReason.Finished);
                                Actor.TryGroupTalk();
                                DoLoop(ExitReason.Default, WaitForOthersLoopCallback, null, 5);
                                Actor.UnregisterGroupTalk();
                            }
                        }
                        if (Target.CanBeCleanedUp && !ShouldDestroyContainer)
                        {
                            if (RandomUtil.RandomChance(Actor.HasTrait(TraitNames.Slob) || Target.Recipe != null && Actor.SimDescription.IsGhost && Target.Recipe.Key == "Ambrosia" || Actor.LotCurrent.IsCommunityLot && Target is IPaperContainer ? 0 : Actor.HasTrait(TraitNames.Neat) ? 100 : Food.CleanupChance))
                            {
                                TryPushCleanup();
                            }
                            else if (Target.Parent == Actor)
                            {
                                Actor.InteractionQueue.PushAsContinuation(CarrySystem.PutDownHeldObject.Singleton, Target, false, new InteractionPriority(InteractionPriorityLevel.UserDirected), true);
                            }
                        }
                        ISingleServingContainer singleServingContainer = Target as ISingleServingContainer;
                        if (singleServingContainer != null && singleServingContainer.FromResortBuffetTable && !Actor.BuffManager.HasElement(BuffNames.Stuffed) && Actor.Motives.GetMotiveValue(CommodityKind.Hunger) <= (float)Sims3.Gameplay.Objects.Resort.ResortBuffetTable.kHungerToStopFeeding)
                        {
                            Sims3.Gameplay.Objects.Resort.ResortBuffetTable.ConsumeMoreFood(Actor);
                        }
                    }
                    else
                    {
                        ServingContainer servingContainer = Target as ServingContainer;
                        if (Actor.HasTrait(TraitNames.Vegetarian) && servingContainer != null)
                        {
                            TraitFunctions.VegetarianTraitEatingMeatCallback(Actor, servingContainer.CookingProcess.StartedByVegetarian, servingContainer.CookingProcess.Recipe);
                        }
                        else if (Target.Parent == Actor && !ShouldDestroyContainer)
                        {
                            Actor.InteractionQueue.PushAsContinuation(CarrySystem.PutDownHeldObject.Singleton, Target, false, new InteractionPriority(InteractionPriorityLevel.UserDirected), true);
                        }
                    }
                }
                Food.PostEat(Actor, Target as GameObject, mIsSufficientlyFullForStuffed, HungerGiven > 0, mHasFatDelta);
                if (Target.Recipe != null && !Target.Recipe.IsSnack)
                {
                    Target.Recipe.LearnFromEating(Actor);
                }
                bool isSpoiledGlass = false;
                Bar.Glass glass = Target as Bar.Glass;
                if (glass != null)
                {
                    isSpoiledGlass = glass.Spoiled;
                }
                if (Actor.SimDescription.IsBonehilda && Target is IGlass)
                {
                    Sims3.Gameplay.Controllers.PuddleManager.AddPuddle(Actor.Position);
                }
                if (nectarGlass != null)
                {
                    nectarGlass.DoPostDrink(Actor, GetPriority().Level == InteractionPriorityLevel.Autonomous);
                }
                else if (glass != null && !isSpoiledGlass)
                {
                    Actor.BuffManager.AddElement(BuffNames.SugarRush, Origin.FromJuice);
                }
                if (glass != null)
                {
                    Actor.BuffManager.RemoveElement(BuffNames.TooSpicy);
                    if (Actor.BuffManager.HasElement(BuffNames.Spicy) && Actor.Motives.GetValue(CommodityKind.Hunger) == Actor.Motives.GetMax(CommodityKind.Hunger))
                    {
                        Actor.BuffManager.RemoveElement(BuffNames.Spicy);
                    }
                }
                if (!Target.CanBeCleanedUp)
                {
                    CarrySystem.ExitCarry(Actor);
                    addToUseList = false;
                    DestroyObject(Target);
                }
                if (isSpoiledGlass && RandomUtil.RandomChance(kChanceToThrowUpFromBarGlass))
                {
                    Actor.PlayReaction(ReactionTypes.ThrowUp, new InteractionPriority(InteractionPriorityLevel.High), null, ReactionSpeed.AfterInteraction);
                }
                if (Actor.LotCurrent.IsCommunityLot && Target is IPaperContainer || ShouldDestroyContainer)
                {
                    Actor.InteractionQueue.PushAsContinuation(FinishEatingOutside.Singleton, Target, true);
                }
                Actor.BuffManager.RemoveElement(BuffNames.MintyBreath);
                StandardExit(addToUseList, addToUseList);
                return succeeded;
            }
        }

        public class OrderDrinksPatch : FutureBar.OrderDrinks
        {
            public override bool Run()
            {
                Definition definition = (Definition)InteractionDefinition;
                if (definition.IsPushedRound)
                {
                    FutureBar.FutureBarGlass glass = definition.GlassObjectId.ObjectFromId<FutureBar.FutureBarGlass>();
                    if (glass != null)
                    {
                        glass.SetPosition(glass.GetSlotPosition(Slot.ContainmentSlot_0));
                        glass.SetOpacity(0, 0);
                        glass.AddToWorld();
                        glass.FadeIn();
                        VisualEffect.FireOneShotEffect("ep11BarDrinkPoof_main", Target, Slot.FXJoint_0, VisualEffect.TransitionType.SoftTransition);
                        glass.ParentToSlot(Target, Slot.ContainmentSlot_0);
                    }
                    return true;
                }
                if (!Target.RouteAndCheckInUse(Actor))
                {
                    return false;
                }
                bool isSeated = Actor.Posture is SittingPosture;
                StandardEntry();
                BeginCommodityUpdates();
                GameObject gameObject = GameObject.GetObject(FutureBar.ContainmentSlotInUse(Target));
                if (gameObject != null)
                {
                    gameObject.FadeOut();
                    gameObject.Destroy();
                }
                string materialStateName = "drink" + definition.DrinkName;
                if (!FavoritesUtils.OriginalFavoriteColors.Contains(definition.DrinkName))
                {
                    materialStateName = "MoreFavs_" + FavoritesUtils.FindClosestColor(Array.ConvertAll(FavoritesUtils.FutureBarGlassRGBValues, x => new Color(x | 0xFF000000)), definition.DrinkColor).ARGB.ToString("X8").Substring(2);
                }
                FutureBar.FutureBarGlass futureBarGlass = (FutureBar.FutureBarGlass)GlobalFunctions.CreateObjectOutOfWorld("accessoryGlassEP11", ProductVersion.EP11);
                futureBarGlass.SetOpacity(0, 0);
                if (definition.DrinkName == "ServoJuice")
                {
                    futureBarGlass.SetGeometryState("servojuice");
                    futureBarGlass.mCurrentGeoState = "servojuice";
                    futureBarGlass.mCurrentMatState = "drinkAqua";
                }
                else
                {
                    futureBarGlass.SetGeometryState("full");
                    futureBarGlass.SetMaterial(materialStateName);
                    futureBarGlass.mCurrentGeoState = "full";
                    futureBarGlass.mCurrentMatState = materialStateName;
                }
                futureBarGlass.AddToWorld();
                futureBarGlass.Contents.mDrinkName = definition.DrinkName;
                futureBarGlass.Contents.mObjectCreatorId = Target.ObjectId;
                EnterStateMachine("FutureBar", "EnterFutureBar", "x", "futurebar");
                SetParameter("isSeated", isSeated);
                SetParameter("Glass", futureBarGlass);
                AnimateSim("OrderDrink");
                Slots.AttachToSlot(futureBarGlass.ObjectId, Target.ObjectId, 2820733094, true);
                VisualEffect.FireOneShotEffect("ep11BarDrinkPoof_main", Target, Slot.FXJoint_0, VisualEffect.TransitionType.SoftTransition);
                futureBarGlass.FadeIn();
                if (definition.ServingType == FutureBar.ServingType.Servo)
                {
                    FutureBar.DoServoJuiceAction(Actor, futureBarGlass, isSeated);
                }
                else
                {
                    if (definition.IsMultipleServing)
                    {
                        Lot lotCurrent = Actor.LotCurrent;
                        EventTracker.SendEvent(EventTypeId.kOrderedARound, Actor, Target);
                        foreach (FutureBar futureBar in lotCurrent.GetObjects<FutureBar>())
                        {
                            if (futureBar != Target)
                            {
                                gameObject = GameObject.GetObject(FutureBar.ContainmentSlotInUse(futureBar));
                                if (gameObject != null)
                                {
                                    gameObject.FadeOut();
                                    gameObject.Destroy();
                                }
                                FutureBar.FutureBarGlass glass = (FutureBar.FutureBarGlass)GlobalFunctions.CreateObjectOutOfWorld("accessoryGlassEP11", ProductVersion.EP11);
                                if (definition.DrinkName == "ServoJuice")
                                {
                                    glass.SetGeometryState("servojuice");
                                    glass.mCurrentGeoState = "servojuice";
                                    glass.mCurrentMatState = "drinkAqua";
                                }
                                else
                                {
                                    glass.SetGeometryState("full");
                                    glass.SetMaterial(materialStateName);
                                    glass.mCurrentGeoState = "full";
                                    glass.mCurrentMatState = materialStateName;
                                }
                                glass.AddToWorld();
                                glass.Contents.mDrinkName = definition.DrinkName;
                                glass.Contents.mObjectCreatorId = futureBar.ObjectId;
                                FutureBar.OrderDrinks orderDrinks = (FutureBar.OrderDrinks)SingletonRoundPush.CreateInstance(futureBar, null, new InteractionPriority(InteractionPriorityLevel.NonCriticalNPCBehavior), true, true);
                                orderDrinks.SetDrinkNameAndColor(definition.ServingType, definition.DrinkName, definition.DrinkColor, glass.ObjectId);
                                orderDrinks.Run();
                            }
                        }
                    }
                    AnimateSim("ExitFutureBar");
                    if (Actor.SimDescription.IsEP11Bot && definition.DrinkName == "ServoJuice")
                    {
                        FutureBar.DoServoJuiceAction(Actor, futureBarGlass, isSeated);
                    }
                    else if (!Actor.SimDescription.IsEP11Bot && definition.DrinkName != "ServoJuice" && CarrySystem.PickUpWithoutRouting(Actor, futureBarGlass, true))
                    {
                        futureBarGlass.PushDrinkAsContinuation(Actor);
                    }
                }
                EndCommodityUpdates(true);
                StandardExit();
                EventTracker.SendEvent(EventTypeId.kOrderedSynthDrink, Actor, Target);
                return true;
            }
        }

        public abstract class StereoPatch : Stereo
        {
            public new void AddEnjoyingMusicCallback(Sim sim, ReactionBroadcaster reactionBroadcaster)
            {
                if (!sim.IsPet)
                {
                    ulong id = 0;
                    bool isPeripheralStereoSpeaker = false;
                    Stereo stereo = reactionBroadcaster.BroadcastingObject as Stereo;
                    IPeripheralStereoSpeaker peripheralStereoSpeaker = reactionBroadcaster.BroadcastingObject as IPeripheralStereoSpeaker;
                    if (peripheralStereoSpeaker != null && stereo == null)
                    {
                        isPeripheralStereoSpeaker = true;
                        stereo = peripheralStereoSpeaker.StereoMaster as Stereo;
                        id = peripheralStereoSpeaker.ObjectId.Value;
                    }
                    else if (stereo != null)
                    {
                        id = stereo.ObjectId.Value;
                    }
                    if (sim.IsSleeping)
                    {
                        bool isDisturbed = true;
                        if (mCurrentLotStereoRanking == 0 && sim.RoomId != RoomId)
                        {
                            foreach (IPeripheralStereoSpeaker additionalSpeaker in mAdditionalSpeakers)
                            {
                                if (sim.RoomId == additionalSpeaker.RoomId && !additionalSpeaker.IsOn)
                                {
                                    isDisturbed = false;
                                    break;
                                }
                            }
                        }
                        if (isDisturbed)
                        {
                            Sims3.Gameplay.InteractionsShared.ReactToDisturbance.NoiseBroadcastCallback(sim, reactionBroadcaster);
                        }
                    }
                    else if (sim.LotCurrent == mLotCurrent)
                    {
                        int moodScore = 0;
                        if (!isPeripheralStereoSpeaker || isPeripheralStereoSpeaker && peripheralStereoSpeaker.RoomId == stereo.RoomId)
                        {
                            moodScore += TuningStereo.MoodScoreForEnjoyMusicBuff + WallMountedSpeaker.SpeakerBoostForStereo(this);
                        }
                        else if (isPeripheralStereoSpeaker)
                        {
                            moodScore += peripheralStereoSpeaker.StandaloneMoodScore;
                        }
                        FavoritesUtils.FavoriteMusic favoriteMusic;
                        if (mPlayingStationsData != null && (mPlayingStationsData.MusicType == sim.SimDescription.FavoriteMusic || FavoritesUtils.FavoriteMusicDictionary.TryGetValue(sim.SimDescription.FavoriteMusic, out favoriteMusic) && (favoriteMusic.Name == mPlayingStationsData.mStationName || Array.Exists(favoriteMusic.Children, x => x == mPlayingStationsData.mStationName))))
                        {
                            moodScore += BuffEnjoyingMusic.FavoriteMusicMoodScore;
                        }
                        if (Upgradable.SoupUpSpeakers)
                        {
                            moodScore += BuffEnjoyingMusic.IncreasedMoodScore;
                        }
                        sim.BuffManager.RemoveElement(BuffNames.EnjoyingMusic, id);
                        sim.BuffManager.AddElement(BuffNames.EnjoyingMusic, moodScore, Origin.FromStereo, id);
                        if (stereo != null && mPlayingStationsData != null && mPlayingStationsData.IsKidsStation)
                        {
                            if (!stereo.mSimsWithinStereoBroadcast.Contains(sim))
                            {
                                stereo.mSimsWithinStereoBroadcast.Add(sim);
                            }
                            if (sim.SimDescription.IsPregnant)
                            {
                                sim.SimDescription.Pregnancy.StartListeningKidsRadio();
                            }
                        }
                    }
                    sim.RemoveInteractionByType(typeof(DanceTogetherA.Definition));
                    sim.AddInteraction(isPeripheralStereoSpeaker ? new DanceTogetherA.Definition(peripheralStereoSpeaker.GetIDanceable()) : new DanceTogetherA.Definition(stereo), true);
                }
                else if (sim.HasTrait(TraitNames.QuietPet))
                {
                    sim.BuffManager.AddElement(BuffNames.TooNoisyPet, Origin.FromStereo);
                }
            }
        }

        public static string GetFavoriteFood(FavoriteFoodType foodType)
        {
            return foodType == FavoriteFoodType.None ? Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/CAS/Favorites:None") : Responder.Instance.LocalizationModel.LocalizeString("Gameplay/Excel/RecipeMasterList/Data:" + (foodType > FavoriteFoodType.Count ? FavoritesUtils.FavoriteFoodDictionary[foodType].Name : foodType.ToString()));
        }

        public static string GetFavoriteFoodPngName(FavoriteFoodType foodType)
        {
            if (foodType == FavoriteFoodType.None)
            {
                foodType = FavoriteFoodType.Hamburger;
            }
            return foodType > FavoriteFoodType.Count ? string.IsNullOrEmpty(FavoritesUtils.FavoriteFoodDictionary[foodType].IconKey) ? "cas_favorites_food_i_hamburger_r2" : FavoritesUtils.FavoriteFoodDictionary[foodType].IconKey : "cas_favorites_food_i_" + foodType + "_r2";
        }

        public static UIImage GetFavoriteFoodSmallIcon(FavoriteFoodType foodType)
        {
            return UIManager.LoadUIImage(ResourceKey.CreatePNGKey(foodType > FavoriteFoodType.Count ? string.IsNullOrEmpty(FavoritesUtils.FavoriteFoodDictionary[foodType].SmallIconKey) ? "cas_favorites_food_i_hamburger_s_r2" : FavoritesUtils.FavoriteFoodDictionary[foodType].SmallIconKey : "cas_favorites_food_i_" + foodType + "_s_r2", 0));
        }

        public static string GetFavoriteMusic(FavoriteMusicType musicType)
        {
            return musicType == FavoriteMusicType.None ? Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/CAS/Favorites:None") : Responder.Instance.LocalizationModel.LocalizeString(musicType > FavoriteMusicType.Count ? FavoritesUtils.FavoriteMusicDictionary[musicType].Name : "Gameplay/Excel/Stereo/Stations:" + musicType);
        }

        public static string GetFavoriteMusicPngName(FavoriteMusicType musicType)
        {
            if (musicType == FavoriteMusicType.None)
            {
                musicType = FavoriteMusicType.Electronica;
            }
            switch (musicType)
            {
                case FavoriteMusicType.Electronica:
                case FavoriteMusicType.Pop:
                case FavoriteMusicType.Latin:
                case FavoriteMusicType.Indie:
                case FavoriteMusicType.Classical:
                case FavoriteMusicType.Kids:
                case FavoriteMusicType.France:
                case FavoriteMusicType.China:
                case FavoriteMusicType.Egypt:
                case FavoriteMusicType.Roots:
                case FavoriteMusicType.Soul:
                case FavoriteMusicType.Rockabilly:
                case FavoriteMusicType.Custom:
                    return "cas_favorites_music_i_" + musicType + "_r2";
                default:
                    return musicType > FavoriteMusicType.Count ? string.IsNullOrEmpty(FavoritesUtils.FavoriteMusicDictionary[musicType].IconKey) ? "cas_favorites_music_i_electronica_r2" : FavoritesUtils.FavoriteMusicDictionary[musicType].IconKey : "cas_favs_music_i_" + musicType;
            }
        }

        public static UIImage GetFavoriteMusicSmallIcon(FavoriteMusicType musicType)
        {
            string imageFileName;
            switch (musicType)
            {
                case FavoriteMusicType.Electronica:
                case FavoriteMusicType.Pop:
                case FavoriteMusicType.Latin:
                case FavoriteMusicType.Indie:
                case FavoriteMusicType.Classical:
                case FavoriteMusicType.Kids:
                case FavoriteMusicType.France:
                case FavoriteMusicType.China:
                case FavoriteMusicType.Egypt:
                case FavoriteMusicType.Roots:
                case FavoriteMusicType.Soul:
                case FavoriteMusicType.Rockabilly:
                case FavoriteMusicType.Custom:
                    imageFileName = "cas_favorites_music_i_" + musicType + "_s_r2";
                    break;
                default:
                    imageFileName = musicType > FavoriteMusicType.Count ? string.IsNullOrEmpty(FavoritesUtils.FavoriteMusicDictionary[musicType].SmallIconKey) ? "cas_favorites_music_i_electronica_s_r2" : FavoritesUtils.FavoriteMusicDictionary[musicType].SmallIconKey : "cas_favs_music_i_" + musicType + "_s";
                    break;
            }
            return GetMusicIcon(imageFileName, musicType);
        }

        public static Array GetInstalledFavoriteFoodList()
        {
            List<FavoriteFoodType> favoriteFoodTypes = new List<FavoriteFoodType>();
            foreach (FavoriteFoodType foodType in Enum.GetValues(typeof(FavoriteFoodType)))
            {
                if (foodType != FavoriteFoodType.VampireFood && foodType != FavoriteFoodType.Kelp && Responder.Instance.CASModel.GetRecipe(foodType) != null)
                {
                    favoriteFoodTypes.Add(foodType);
                }
            }
            favoriteFoodTypes.AddRange(new List<FavoriteFoodType>(FavoritesUtils.FavoriteFoodDictionary.Keys));
            return favoriteFoodTypes.ToArray();
        }

        public static Array GetInstalledFavoriteMusicList()
        {
            List<FavoriteMusicType> favoriteMusicTypes = new List<FavoriteMusicType>();
            foreach (FavoriteMusicType musicType in Enum.GetValues(typeof(FavoriteMusicType)))
            {
                if (Responder.Instance.CASModel.IsMusicTypeInstalled(musicType))
                {
                    favoriteMusicTypes.Add(musicType);
                }
            }
            favoriteMusicTypes.AddRange(new List<FavoriteMusicType>(FavoritesUtils.FavoriteMusicDictionary.Keys));
            return favoriteMusicTypes.ToArray();
        }

        public static UIImage GetMusicIcon(string imageFileName, FavoriteMusicType musicType)
        {
            return UIManager.LoadUIImage(ResourceKey.CreatePNGKey(imageFileName, musicType > FavoriteMusicType.Count ? 0 : ResourceUtils.ProductVersionToGroupId(Responder.Instance.GetProductVersionForStereoStation(musicType))));
        }

        public static IRecipe GetRecipe(FavoriteFoodType foodType)
        {
            Sims3.Gameplay.Objects.FoodObjects.Recipe recipe;
            FavoritesUtils.FavoriteFood favoriteFood;
            return foodType > FavoriteFoodType.Count && FavoritesUtils.FavoriteFoodDictionary.TryGetValue(foodType, out favoriteFood) ? favoriteFood.Recipe : Sims3.Gameplay.Objects.FoodObjects.Recipe.NameToRecipeHash.TryGetValue(foodType.ToString(), out recipe) ? recipe: null;
        }

        public static string GetStationName(FavoriteMusicType musicType)
        {
            List<string> stereoStationNames = new List<string>();
            foreach (string key in StereoStationData.sStereoStationDictionary.Keys)
            {
                if (StereoStationData.sStereoStationDictionary[key].mFavouriteMusicType == musicType)
                {
                    stereoStationNames.Add(key);
                }
            }
            FavoritesUtils.FavoriteMusic favoriteMusic;
            if (FavoritesUtils.FavoriteMusicDictionary.TryGetValue(musicType, out favoriteMusic))
            {
                stereoStationNames.Add(favoriteMusic.Name);
            }
            return stereoStationNames.Count == 0 ? null : RandomUtil.GetRandomObjectFromList(stereoStationNames);
        }

        public void OnFavoritesVisibilityChange(WindowBase sender, UIVisibilityChangeEventArgs args)
        {
            CASCharacter self = (CASCharacter)(object)this;
            if (args.Visible)
            {
                sender.Tick += self.OnFavoritesVisibilityTick;
                switch (sender.ID)
                {
                    case 100663297:
                        {
                            Array unblacklistedFavorites = Array.FindAll((FavoriteFoodType[])GetInstalledFavoriteFoodList(), x => !x.IsBlacklisted());
                            self.PopulateFavoritesGrid(self.mGridFavoriteFood, unblacklistedFavorites, 0, unblacklistedFavorites.Length);
                            break;
                        }
                    case 100663301:
                        {
                            Array unblacklistedFavorites = Array.FindAll((FavoriteMusicType[])GetInstalledFavoriteMusicList(), x => !x.IsBlacklisted());
                            self.PopulateFavoritesGrid(self.mGridFavoriteMusic, unblacklistedFavorites, 0, unblacklistedFavorites.Length);
                            break;
                        }
                    case 100663305:
                        {
                            Array unblacklistedFavorites = Array.FindAll(CASCharacter.kColors, x => !x.IsBlacklisted());
                            self.PopulateFavoritesGrid(self.mGridFavoriteColor, unblacklistedFavorites, 0, unblacklistedFavorites.Length);
                            break;
                        }
                }
            }
            else
            {
                sender.Tick -= self.OnFavoritesVisibilityTick;
            }
        }

        public static void RandomizeAllFavorites(bool missingFavoritesOnly)
        {
            ICASModel casModel = Responder.Instance.CASModel;
            Random random = new Random();
            FavoriteFoodType favoriteFood = casModel.FavoriteFoodType;
            if (!missingFavoritesOnly || favoriteFood == FavoriteFoodType.None)
            {
                Array unblacklistedFavorites = Array.FindAll((FavoriteFoodType[])GetInstalledFavoriteFoodList(), x => !x.IsBlacklisted());
                favoriteFood = (FavoriteFoodType)unblacklistedFavorites.GetValue(random.Next(1, unblacklistedFavorites.Length));
                if (casModel.IsVegetarian())
                {
                    IRecipe recipe = casModel.GetRecipe(favoriteFood);
                    while (!recipe.IsVegetarian && !recipe.HasVegetarianAlternative)
                    {
                        favoriteFood = (FavoriteFoodType)unblacklistedFavorites.GetValue(random.Next(1, unblacklistedFavorites.Length));
                        recipe = casModel.GetRecipe(favoriteFood);
                    }
                }
            }
            FavoriteMusicType favoriteMusic = casModel.FavoriteMusicType;
            if (!missingFavoritesOnly || favoriteMusic == FavoriteMusicType.None)
            {
                Array unblacklistedFavorites = Array.FindAll((FavoriteMusicType[])GetInstalledFavoriteMusicList(), x => !x.IsBlacklisted());
                for (favoriteMusic = FavoriteMusicType.Custom; favoriteMusic == FavoriteMusicType.Custom; favoriteMusic = (FavoriteMusicType)unblacklistedFavorites.GetValue(random.Next(1, unblacklistedFavorites.Length)))
                {
                }
            }
            Color favoriteColor = casModel.FavoriteColor;
            if (!missingFavoritesOnly || favoriteColor.ARGB == 0)
            {
                Array unblacklistedFavorites = Array.FindAll(CASCharacter.kColors, x => !x.IsBlacklisted());
                favoriteColor = ((CASCharacter.NameColorPair)unblacklistedFavorites.GetValue(random.Next(0, unblacklistedFavorites.Length))).mColor;
            }
            casModel.RequestRandomFavorites(favoriteFood, favoriteMusic, favoriteColor);
        }

        public void RandomizeFavoriteMusic()
        {
            SimDescription self = (SimDescription)(object)this;
            Array unblacklistedFavoriteMusicList = Array.FindAll((FavoriteMusicType[])GetInstalledFavoriteMusicList(), x => !x.IsBlacklisted());
            self.mFavouriteMusicType = FavoriteMusicType.None;
            while (self.mFavouriteMusicType == FavoriteMusicType.Custom || self.mFavouriteMusicType == FavoriteMusicType.None)
            {
                self.mFavouriteMusicType = (FavoriteMusicType)unblacklistedFavoriteMusicList.GetValue(RandomUtil.GetInt(1, unblacklistedFavoriteMusicList.Length - 1));
            }
        }

        public void RandomizePreferences()
        {
            SimDescription self = (SimDescription)(object)this;
            self.mZodiacSign = (Zodiac)RandomUtil.GetInt(0, 11);
            self.RandomizeFavoriteMusic();
            Array unblacklistedFavoriteFoodList = Array.FindAll((FavoriteFoodType[])GetInstalledFavoriteFoodList(), x => !x.IsBlacklisted());
            self.mFavouriteFoodType = (FavoriteFoodType)unblacklistedFavoriteFoodList.GetValue(RandomUtil.GetInt(1, unblacklistedFavoriteFoodList.Length - 1));
            Array unblacklistedFavoriteColorList = Array.FindAll(CASCharacter.kColors, x => !x.IsBlacklisted());
            self.mFavouriteColor = ((CASCharacter.NameColorPair)unblacklistedFavoriteColorList.GetValue(RandomUtil.GetInt(0, unblacklistedFavoriteColorList.Length - 1))).mColor;
        }
    }
}
