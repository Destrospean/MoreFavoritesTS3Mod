using System;
using System.Collections.Generic;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
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
                    InteractionInstance interactionInstance = SitTransitionAngledToStraight.Singleton.CreateInstance(Actor.Posture.Container, Actor, GetPriority(), false, false);
                    interactionInstance.RunInteraction();
                }
                Food.PreEat(Actor, Target as Sims3.Gameplay.Abstracts.GameObject, ref mIsSufficientlyFullForStuffed, ref mHasFatDelta);
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
                    SittingPosture sittingPosture = Actor.Posture as SittingPosture;
                    SetParameter("sitTemplateSuffix", sittingPosture.Part.Target.IKSuffix);
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
                if (Target is Sims3.Gameplay.Objects.Appliances.FutureBar.FutureBarGlass && Target.Spoiled)
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
                        mPropHandle = Sims3.Gameplay.GlobalFunctions.CreateProp("UtensilChopsticks", Vector3.OutOfWorld, 0, Vector3.UnitZ);
                    }
                    else
                    {
                        mPropHandle = Sims3.Gameplay.GlobalFunctions.CreateProp(GetUtensilMedatorName(Target.UtensilName), Vector3.OutOfWorld, 0, Vector3.UnitZ);
                    }
                    if (mPropHandle != ObjectGuid.InvalidObjectGuid)
                    {
                        mCurrentStateMachine.SetPropActor("utensil", mPropHandle);
                    }
                }
                ObjectHideHelper objectHideHelper = new ObjectHideHelper(Target);
                mCurrentStateMachine.AddOneShotScriptEventHandler(101, objectHideHelper.Callback);
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
                Sims3.Gameplay.Objects.Appliances.HotBeverageMachine.Cup cup = Target.ThingToEat as Sims3.Gameplay.Objects.Appliances.HotBeverageMachine.Cup;
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
                bool wasInterrupted = !Actor.HasExitReason(ExitReason.Finished);
                SetParameter("wasInterrupted", wasInterrupted);
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
                            if (eatingSurface != null && eatingSurface.AllowWaitForOthers && (eatingSurface.NumOtherSimsAtSurface(Actor) > 0 || ((Sims3.Gameplay.Abstracts.GameObject)eatingSurface).ReferenceList.Count > 0))
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
                Food.PostEat(Actor, Target as Sims3.Gameplay.Abstracts.GameObject, mIsSufficientlyFullForStuffed, HungerGiven > 0, mHasFatDelta);
                if (Target.Recipe != null && !Target.Recipe.IsSnack)
                {
                    Target.Recipe.LearnFromEating(Actor);
                }
                bool glassIsSpoiled = false;
                Bar.Glass glass = Target as Bar.Glass;
                if (glass != null)
                {
                    glassIsSpoiled = glass.Spoiled;
                }
                if (Actor.SimDescription.IsBonehilda && Target is Sims3.Gameplay.Interfaces.IGlass)
                {
                    Sims3.Gameplay.Controllers.PuddleManager.AddPuddle(Actor.Position);
                }
                if (nectarGlass != null)
                {
                    nectarGlass.DoPostDrink(Actor, GetPriority().Level == InteractionPriorityLevel.Autonomous);
                }
                else if (Target is Bar.Glass && !glassIsSpoiled)
                {
                    Actor.BuffManager.AddElement(BuffNames.SugarRush, Origin.FromJuice);
                }
                if (Target is Bar.Glass)
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
                if (glassIsSpoiled && Sims3.Gameplay.Core.RandomUtil.RandomChance(kChanceToThrowUpFromBarGlass))
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

        public static string GetFavoriteFood(FavoriteFoodType foodType)
        {
            return foodType == FavoriteFoodType.None ? Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/CAS/Favorites:None") : Responder.Instance.LocalizationModel.LocalizeString(foodType < FavoriteFoodType.Count ? "Gameplay/Excel/RecipeMasterList/Data:" + foodType : FavoritesUtils.FavoriteFoodDictionary[foodType].Recipe.mGenericName);
        }

        public static UIImage GetFavoriteFoodIcon(FavoriteFoodType foodType)
        {
            return UIManager.LoadUIImage(ResourceKey.CreatePNGKey(GetFavoriteFoodPngName(foodType), 0));
        }

        public static string GetFavoriteFoodPngName(FavoriteFoodType foodType)
        {
            if (foodType == FavoriteFoodType.None)
            {
                foodType = FavoriteFoodType.Hamburger;
            }
            return foodType < FavoriteFoodType.Count ? "cas_favorites_food_i_" + foodType + "_r2" : FavoritesUtils.FavoriteFoodDictionary[foodType].IconKey ?? "cas_favorites_food_i_hamburger_r2";
        }

        public static UIImage GetFavoriteFoodSmallIcon(FavoriteFoodType foodType)
        {
            return UIManager.LoadUIImage(ResourceKey.CreatePNGKey(foodType < FavoriteFoodType.Count ? "cas_favorites_food_i_" + foodType + "_s_r2" : FavoritesUtils.FavoriteFoodDictionary[foodType].SmallIconKey ?? "cas_favorites_food_i_hamburger_s_r2", 0));
        }

        public static string GetFavoriteMusic(FavoriteMusicType musicType)
        {
            return musicType == FavoriteMusicType.None ? Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/CAS/Favorites:None") : Responder.Instance.LocalizationModel.LocalizeString(musicType < FavoriteMusicType.Count ? "Gameplay/Excel/Stereo/Stations:" + musicType : FavoritesUtils.FavoriteMusicDictionary[musicType].StereoStationData.mStationName);
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
                    return musicType < FavoriteMusicType.Count ? "cas_favs_music_i_" + musicType + "_r2" : FavoritesUtils.FavoriteMusicDictionary[musicType].IconKey ?? "cas_favorites_music_i_electronica_r2";
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
                    imageFileName = musicType < FavoriteMusicType.Count ? "cas_favs_music_i_" + musicType + "_s" : FavoritesUtils.FavoriteMusicDictionary[musicType].SmallIconKey ?? "cas_favorites_music_i_electronica_s_r2";
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
            return UIManager.LoadUIImage(ResourceKey.CreatePNGKey(imageFileName, musicType < FavoriteMusicType.Count ? ResourceUtils.ProductVersionToGroupId(Responder.Instance.GetProductVersionForStereoStation(musicType)) : 0));
        }

        public static Sims3.UI.CAS.IRecipe GetRecipe(FavoriteFoodType foodType)
        {
            Sims3.Gameplay.Objects.FoodObjects.Recipe recipe;
            FavoritesUtils.FavoriteFood favoriteFood;
            return foodType < FavoriteFoodType.Count && Sims3.Gameplay.Objects.FoodObjects.Recipe.NameToRecipeHash.TryGetValue(foodType.ToString(), out recipe) ? recipe : FavoritesUtils.FavoriteFoodDictionary.TryGetValue(foodType, out favoriteFood) ? favoriteFood.Recipe : null;
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
