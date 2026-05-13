using System;
using System.Collections.Generic;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
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

namespace Destrospean.MoreFavorites
{
    public static class Replacements
    {
        public class EatHeldFood : Sims3.Gameplay.Objects.CookingObjects.EatHeldFood
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
                SetParameter("isFavorite", Actor.SimDescription.FavoriteFood != 0 && Target.Recipe != null && (Target.Recipe.Favorite == Actor.SimDescription.FavoriteFood || FavoritesUtils.FavoriteFoodDictionary.TryGetValue(Actor.SimDescription.FavoriteFood, out favoriteFood) && favoriteFood.Recipe.Key == Target.Recipe.Key));
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
                bool loopDone = DoLoop(ExitReason.Default, LoopCallback, mCurrentStateMachine);
                HotBeverageMachine.Cup cup = Target.ThingToEat as HotBeverageMachine.Cup;
                if (loopDone && GameUtils.IsInstalled(ProductVersion.EP9) && cup != null && cup.IsEnergyDrink)
                {
                    Actor.BuffManager.AddElement(BuffNames.LiquidEnergy, Origin.FromEnergyDrink);
                }
                Actor.UnregisterGroupTalk();
                EndCommodityUpdates(loopDone);
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
                            float chance = Food.CleanupChance;
                            if (Actor.HasTrait(TraitNames.Slob) || Target.Recipe != null && Actor.SimDescription.IsGhost && Target.Recipe.Key == "Ambrosia" || Actor.LotCurrent.IsCommunityLot && Target is IPaperContainer)
                            {
                                chance = 0;
                            }
                            else if (Actor.HasTrait(TraitNames.Neat))
                            {
                                chance = 100;
                            }
                            if (Sims3.Gameplay.Core.RandomUtil.RandomChance(chance))
                            {
                                TryPushCleanup();
                            }
                            else if (Target.Parent == Actor)
                            {
                                Actor.InteractionQueue.PushAsContinuation(CarrySystem.PutDownHeldObject.Singleton, Target, false, new InteractionPriority(InteractionPriorityLevel.UserDirected), true);
                            }
                        }
                        Sims3.Gameplay.Interfaces.ISingleServingContainer singleServingContainer = Target as Sims3.Gameplay.Interfaces.ISingleServingContainer;
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
                if (Actor.SimDescription.IsBonehilda && Target is Sims3.Gameplay.Interfaces.IGlass)
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
                if (isSpoiledGlass && Sims3.Gameplay.Core.RandomUtil.RandomChance(kChanceToThrowUpFromBarGlass))
                {
                    Actor.PlayReaction(ReactionTypes.ThrowUp, new InteractionPriority(InteractionPriorityLevel.High), null, ReactionSpeed.AfterInteraction);
                }
                if (Actor.LotCurrent.IsCommunityLot && Target is IPaperContainer || ShouldDestroyContainer)
                {
                    Actor.InteractionQueue.PushAsContinuation(FinishEatingOutside.Singleton, Target, true);
                }
                Actor.BuffManager.RemoveElement(BuffNames.MintyBreath);
                StandardExit(addToUseList, addToUseList);
                return loopDone;
            }
        }

        public class OrderDrinks : FutureBar.OrderDrinks
        {
            public override bool Run()
            {
                Definition definition = InteractionDefinition as Definition;
                if (definition.IsPushedRound)
                {
                    FutureBar.FutureBarGlass futureBarGlass = definition.GlassObjectId.ObjectFromId<FutureBar.FutureBarGlass>();
                    if (futureBarGlass != null)
                    {
                        futureBarGlass.SetPosition(futureBarGlass.GetSlotPosition(Slot.ContainmentSlot_0));
                        futureBarGlass.SetOpacity(0, 0);
                        futureBarGlass.AddToWorld();
                        futureBarGlass.FadeIn();
                        VisualEffect.FireOneShotEffect("ep11BarDrinkPoof_main", Target, Slot.FXJoint_0, VisualEffect.TransitionType.SoftTransition);
                        futureBarGlass.ParentToSlot(Target, Slot.ContainmentSlot_0);
                    }
                    return true;
                }
                if (!Target.RouteAndCheckInUse(Actor))
                {
                    return false;
                }
                bool isSitting = Actor.Posture is SittingPosture;
                StandardEntry();
                BeginCommodityUpdates();
                GameObject gameObject = GameObject.GetObject(FutureBar.ContainmentSlotInUse(Target));
                if (gameObject != null)
                {
                    gameObject.FadeOut();
                    gameObject.Destroy();
                }
                string materialStateName = "drink" + definition.DrinkName;
                if (FavoritesUtils.FavoriteColorList.Contains(definition.DrinkColor.ARGB))
                {
                    materialStateName = "MoreFavs_" + FavoritesUtils.FindClosestColor(Array.ConvertAll(FavoritesUtils.FutureBarGlassRGBValues, x => new Color(x | 0xFF000000)), definition.DrinkColor).ARGB.ToString("X8").Substring(2);
                }
                FutureBar.FutureBarGlass futureBarGlass1 = GlobalFunctions.CreateObjectOutOfWorld("accessoryGlassEP11", ProductVersion.EP11) as FutureBar.FutureBarGlass;
                futureBarGlass1.SetOpacity(0, 0);
                if (definition.DrinkName == "ServoJuice")
                {
                    futureBarGlass1.SetGeometryState("servojuice");
                    futureBarGlass1.mCurrentGeoState = "servojuice";
                    futureBarGlass1.mCurrentMatState = "drinkAqua";
                }
                else
                {
                    futureBarGlass1.SetGeometryState("full");
                    futureBarGlass1.SetMaterial(materialStateName);
                    futureBarGlass1.mCurrentGeoState = "full";
                    futureBarGlass1.mCurrentMatState = materialStateName;
                }
                futureBarGlass1.AddToWorld();
                futureBarGlass1.Contents.mDrinkName = definition.DrinkName;
                futureBarGlass1.Contents.mObjectCreatorId = Target.ObjectId;
                EnterStateMachine("FutureBar", "EnterFutureBar", "x", "futurebar");
                SetParameter("isSeated", isSitting);
                SetParameter("Glass", futureBarGlass1);
                AnimateSim("OrderDrink");
                Slots.AttachToSlot(futureBarGlass1.ObjectId, Target.ObjectId, 2820733094, true);
                VisualEffect.FireOneShotEffect("ep11BarDrinkPoof_main", Target, Slot.FXJoint_0, VisualEffect.TransitionType.SoftTransition);
                futureBarGlass1.FadeIn();
                if (definition.ServingType == FutureBar.ServingType.Servo)
                {
                    FutureBar.DoServoJuiceAction(Actor, futureBarGlass1, isSitting);
                }
                else
                {
                    if (definition.IsMultipleServing)
                    {
                        Sims3.Gameplay.Core.Lot lotCurrent = Actor.LotCurrent;
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
                                FutureBar.FutureBarGlass futureBarGlass2 = GlobalFunctions.CreateObjectOutOfWorld("accessoryGlassEP11", ProductVersion.EP11) as FutureBar.FutureBarGlass;
                                if (definition.DrinkName == "ServoJuice")
                                {
                                    futureBarGlass2.SetGeometryState("servojuice");
                                    futureBarGlass2.mCurrentGeoState = "servojuice";
                                    futureBarGlass2.mCurrentMatState = "drinkAqua";
                                }
                                else
                                {
                                    futureBarGlass2.SetGeometryState("full");
                                    futureBarGlass2.SetMaterial(materialStateName);
                                    futureBarGlass2.mCurrentGeoState = "full";
                                    futureBarGlass2.mCurrentMatState = materialStateName;
                                }
                                futureBarGlass2.AddToWorld();
                                futureBarGlass2.Contents.mDrinkName = definition.DrinkName;
                                futureBarGlass2.Contents.mObjectCreatorId = futureBar.ObjectId;
                                OrderDrinks orderDrinks = SingletonRoundPush.CreateInstance(futureBar, null, new InteractionPriority(InteractionPriorityLevel.NonCriticalNPCBehavior), true, true) as OrderDrinks;
                                orderDrinks.SetDrinkNameAndColor(definition.ServingType, definition.DrinkName, definition.DrinkColor, futureBarGlass2.ObjectId);
                                orderDrinks.Run();
                            }
                        }
                    }
                    AnimateSim("ExitFutureBar");
                    if (Actor.SimDescription.IsEP11Bot && definition.DrinkName == "ServoJuice")
                    {
                        FutureBar.DoServoJuiceAction(Actor, futureBarGlass1, isSitting);
                    }
                    else if (!Actor.SimDescription.IsEP11Bot && definition.DrinkName != "ServoJuice" && CarrySystem.PickUpWithoutRouting(Actor, futureBarGlass1, true))
                    {
                        futureBarGlass1.PushDrinkAsContinuation(Actor);
                    }
                }
                EndCommodityUpdates(true);
                StandardExit();
                EventTracker.SendEvent(EventTypeId.kOrderedSynthDrink, Actor, Target);
                return true;
            }
        }

        public static string GetFavoriteFood(FavoriteFoodType foodType)
        {
            return foodType == FavoriteFoodType.None ? Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/CAS/Favorites:None") : Responder.Instance.LocalizationModel.LocalizeString(foodType > FavoriteFoodType.Count ? FavoritesUtils.FavoriteFoodDictionary[foodType].Recipe.mGenericName : "Gameplay/Excel/RecipeMasterList/Data:" + foodType);
        }

        public static string GetFavoriteFoodPngName(FavoriteFoodType foodType)
        {
            if (foodType == FavoriteFoodType.None)
            {
                foodType = FavoriteFoodType.Hamburger;
            }
            return foodType > FavoriteFoodType.Count ? FavoritesUtils.FavoriteFoodDictionary[foodType].IconKey ?? "cas_favorites_food_i_hamburger_r2" : "cas_favorites_food_i_" + foodType + "_r2";
        }

        public static UIImage GetFavoriteFoodSmallIcon(FavoriteFoodType foodType)
        {
            return UIManager.LoadUIImage(ResourceKey.CreatePNGKey(foodType > FavoriteFoodType.Count ? FavoritesUtils.FavoriteFoodDictionary[foodType].SmallIconKey ?? "cas_favorites_food_i_hamburger_s_r2" : "cas_favorites_food_i_" + foodType + "_s_r2", 0));
        }

        public static string GetFavoriteMusic(FavoriteMusicType musicType)
        {
            return musicType == FavoriteMusicType.None ? Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/CAS/Favorites:None") : Responder.Instance.LocalizationModel.LocalizeString(musicType > FavoriteMusicType.Count ? FavoritesUtils.FavoriteMusicDictionary[musicType].StereoStationData.mStationName : "Gameplay/Excel/Stereo/Stations:" + musicType);
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
                    return musicType > FavoriteMusicType.Count ? FavoritesUtils.FavoriteMusicDictionary[musicType].IconKey ?? "cas_favorites_music_i_electronica_r2" : "cas_favs_music_i_" + musicType;
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
                    imageFileName = musicType > FavoriteMusicType.Count ? FavoritesUtils.FavoriteMusicDictionary[musicType].SmallIconKey ?? "cas_favorites_music_i_electronica_s_r2" : "cas_favs_music_i_" + musicType + "_s";
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
            favoriteFoodTypes.AddRange(FavoritesUtils.FavoriteFoodDictionary.Keys);
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
            favoriteMusicTypes.AddRange(FavoritesUtils.FavoriteMusicDictionary.Keys);
            return favoriteMusicTypes.ToArray();
        }

        public static UIImage GetMusicIcon(string imageFileName, FavoriteMusicType musicType)
        {
            return UIManager.LoadUIImage(ResourceKey.CreatePNGKey(imageFileName, musicType > FavoriteMusicType.Count ? 0 : ResourceUtils.ProductVersionToGroupId(Responder.Instance.GetProductVersionForStereoStation(musicType))));
        }

        public static Sims3.UI.CAS.IRecipe GetRecipe(FavoriteFoodType foodType)
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
                stereoStationNames.Add(favoriteMusic.StereoStationData.mStationName);
            }
            return stereoStationNames.Count == 0 ? null : Sims3.Gameplay.Core.RandomUtil.GetRandomObjectFromList(stereoStationNames);
        }
    }
}
