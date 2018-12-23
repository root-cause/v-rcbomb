using System;
using System.IO;
using System.Drawing;
using GTA;
using GTA.Native;
using GTA.Math;
using NativeUI;

namespace RCBomb
{
    public class Main : Script
    {
        // Script variables
        public Vehicle RCSpawner = null;
        public Vehicle RCVehicle = null;
        public Prop RCSpawnerProp = null;

        public bool ScriptLoaded = false;
        public bool RCBombActive = false;
        public int SpawnControlPressedAt = 0;
        public int PrevCamMode = 0;
        public int NextUpdate = 0;
        public int NextVehicleCheck = 0;

        // NativeUI
        public MenuPool RCMenuPool = null;
        public UIMenu RCSpawnerMenu = null;

        // Config variables
        public bool ShowControls = true;
        public bool FPSCamera = false;
        public float MaxDistance = 200.0f;
        public int SpawnControl = 51;
        public int DetonateControl = 47;

        #region Methods
        public void DisplayHelpText(string message)
        {
            Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, "STRING");
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, message);
            Function.Call(Hash._0x238FFE5C7B0498A6, 0, 0, 1, -1);
        }

        public void CreateSpawnerVehicle()
        {
            RCSpawner?.Delete();
            RCSpawner = World.CreateVehicle(VehicleHash.GBurrito, Constants.SpawnerPos, Constants.SpawnerRot);
            RCSpawner.NumberPlate = "KABOOM";
            RCSpawner.IsRadioEnabled = false;
            RCSpawner.CanBeVisiblyDamaged = false;
            RCSpawner.IsInvincible = true;

            // Absolutely useless but it looks cool
            RCSpawnerProp?.Delete();
            RCSpawnerProp = World.CreateProp("hei_prop_mini_sever_02", Constants.SpawnerPos + new Vector3(0f, 0f, -5.0f), false, false);
            Function.Call(
                Hash.ATTACH_ENTITY_TO_ENTITY,

                RCSpawnerProp.Handle,
                RCSpawner.Handle,

                Function.Call<int>(Hash.GET_ENTITY_BONE_INDEX_BY_NAME, RCSpawner.Handle, "Prop_MB_crate_01A"),
                0f, 1.1f, -0.39f,
                0f, 0f, 0f,

                true, false, false, false, 2, true
            );

            Blip spawnerBlip = RCSpawner.AddBlip();
            spawnerBlip.IsShortRange = true;
            spawnerBlip.Sprite = (BlipSprite)646;
            spawnerBlip.Scale = 1.0f;
            spawnerBlip.Name = "RC Vehicle Van";

            RCSpawner.IsPersistent = true;
        }

        public void Detonate()
        {
            RCBombActive = false;

            RCVehicle.Explode();
            Wait(2000);

            Game.FadeScreenOut(Constants.FadeTime);
            Wait(Constants.FadeTime);

            Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, 1.0f);
            Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
            Function.Call(Hash.DISPLAY_RADAR, true);

            Game.Player.Character.SetIntoVehicle(RCSpawner, VehicleSeat.Driver);
            RCVehicle.MarkAsNoLongerNeeded();

            if (FPSCamera) Function.Call(Hash.SET_FOLLOW_VEHICLE_CAM_VIEW_MODE, PrevCamMode);

            Wait(Constants.FadeTime);
            Game.FadeScreenIn(Constants.FadeTime);

            Game.Player.Character.IsExplosionProof = false;
            RCVehicle = null;
        }
        #endregion

        public Main()
        {
            // Load config
            try
            {
                string configPath = Path.Combine("scripts", "rcbomb_config.ini");
                ScriptSettings config = ScriptSettings.Load(configPath);

                if (File.Exists(configPath))
                {
                    ShowControls = config.GetValue("CONFIG", "ShowControls", true);
                    FPSCamera = config.GetValue("CONFIG", "FPSCamera", false);
                    MaxDistance = config.GetValue("CONFIG", "MaxDistance", 200.0f);
                    SpawnControl = config.GetValue("CONTROLS", "SpawnControl", 51);
                    DetonateControl = config.GetValue("CONTROLS", "DetonateControl", 47);
                }
                else
                {
                    config.SetValue("CONFIG", "ShowControls", ShowControls);
                    config.SetValue("CONFIG", "FPSCamera", FPSCamera);
                    config.SetValue("CONFIG", "MaxDistance", MaxDistance);
                    config.SetValue("CONTROLS", "SpawnControl", SpawnControl);
                    config.SetValue("CONTROLS", "DetonateControl", DetonateControl);
                }

                config.Save();
            }
            catch (Exception e)
            {
                UI.Notify($"~r~RCBomb settings error: {e.Message}");
            }

            // Credits to jedijosh920 for this bannerless menu trick
            RCMenuPool = new MenuPool();

            RCSpawnerMenu = new UIMenu("", "SELECT A MODEL", new Point(0, -107));
            RCSpawnerMenu.SetBannerType(new UIResRectangle(Point.Empty, Size.Empty, Color.Empty));

            // This makes me cry
            RCSpawnerMenu.AddItem(new UIMenuItem("Stock"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Offroad"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Offroad & Spoiler"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Offroad & Nets"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Offroad Combined"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Trophy Truck"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Trophy Truck & Spoiler"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Trophy Truck & Nets"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Trophy Truck Combined"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Gang Burrito"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Gang Burrito & Spoiler"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Gang Burrito & Bullbar"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Gang Burrito Combined"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Big Brat"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Big Brat & Cage"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Big Brat & Bullbar"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Big Brat Combined"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Midnight Pumping"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Midnight Pumping & Cage"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Midnight Pumping & Flames"));
            RCSpawnerMenu.AddItem(new UIMenuItem("Midnight Pumping Combined"));
            RCMenuPool.Add(RCSpawnerMenu);

            // Set up event handlers
            Tick += Script_Tick;
            Aborted += Script_Aborted;
            RCSpawnerMenu.OnItemSelect += RCSpawnerMenu_ItemSelected;
        }

        #region Event: Tick
        public void Script_Tick(object sender, EventArgs e)
        {
            // Vehicle creation
            if (!ScriptLoaded && !Game.IsLoading && Game.Player.CanControlCharacter)
            {
                ScriptLoaded = true;
                CreateSpawnerVehicle();
                return;
            }

            if (RCSpawner == null) return;
            int gameTime = Game.GameTime;

            // Workaround for spawner vehicle disappearing
            if (gameTime > NextVehicleCheck)
            {
                NextVehicleCheck = gameTime + Constants.CheckInterval;

                if (!RCSpawner.Exists())
                {
                    UI.Notify("The Lost MC took their RC vehicle van back.");
                    CreateSpawnerVehicle();
                }
            }

            // Handle drawing etc.
            if (Game.Player.Character.IsInVehicle(RCSpawner))
            {
                if (ShowControls && !RCSpawnerMenu.Visible) DisplayHelpText($"Hold {HelpTextKeys.Get(SpawnControl)} to spawn a RC car.");

                Game.DisableControlThisFrame(2, Control.VehicleHorn);
                RCMenuPool.ProcessMenus();
            }

            // Handle player input
            if (RCBombActive)
            {
                if (ShowControls) DisplayHelpText($"Press {HelpTextKeys.Get(DetonateControl)} to detonate the RC car.");

                foreach (Control control in Constants.ControlsToDisable) Game.DisableControlThisFrame(2, control);
                if (FPSCamera) Game.DisableControlThisFrame(2, Control.NextCamera);

                if (Game.IsControlJustPressed(2, (Control)DetonateControl)) Detonate();

                if (gameTime > NextUpdate)
                {
                    NextUpdate = gameTime + Constants.UpdateInterval;

                    if (Game.Player.Character.IsInVehicle(RCVehicle))
                    {
                        float distance = RCVehicle.Position.DistanceTo(RCSpawner.Position);
                        if (distance <= MaxDistance)
                        {
                            Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, Constants.TimecycleStrengthBase + (distance * Constants.TimecycleStrengthDistMultiplier));
                        }
                        else
                        {
                            UI.Notify("Lost control of the RC car.");
                            Detonate();
                        }
                    }
                    else
                    {
                        UI.Notify("Left the RC car.");

                        RCVehicle.Delete();
                        Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, 1.0f);
                        Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
                        Function.Call(Hash.DISPLAY_RADAR, true);

                        if (FPSCamera) Function.Call(Hash.SET_FOLLOW_VEHICLE_CAM_VIEW_MODE, PrevCamMode);

                        Game.Player.Character.IsExplosionProof = false;
                        RCBombActive = false;
                        RCVehicle = null;
                    }
                }
            }
            else
            {
                if (Game.IsControlJustPressed(2, (Control)SpawnControl) && Game.Player.Character.IsInVehicle(RCSpawner))
                {
                    SpawnControlPressedAt = gameTime;
                }
                else if (Game.IsControlJustReleased(2, (Control)SpawnControl))
                {
                    SpawnControlPressedAt = 0;
                }

                if (SpawnControlPressedAt > 0)
                {
                    if (gameTime - SpawnControlPressedAt >= Constants.RequiredHoldingTime && Game.Player.Character.IsInVehicle(RCSpawner))
                    {
                        SpawnControlPressedAt = 0;
                        RCSpawnerMenu.Visible = !RCSpawnerMenu.Visible;
                    }
                }
            }
        }
        #endregion

        #region Event: Aborted
        public void Script_Aborted(object sender, EventArgs e)
        {
            if (RCBombActive)
            {
                if (FPSCamera) Function.Call(Hash.SET_FOLLOW_VEHICLE_CAM_VIEW_MODE, PrevCamMode);

                Game.Player.Character.IsExplosionProof = false;
                Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, 1.0f);
                Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
                Function.Call(Hash.DISPLAY_RADAR, true);
            }

            RCSpawnerProp?.Delete();
            RCSpawner?.Delete();
            RCVehicle?.Delete();

            RCBombActive = false;
            RCSpawnerProp = null;
            RCSpawner = RCVehicle = null;
            RCMenuPool = null;
            RCSpawnerMenu = null;
        }
        #endregion

        #region MenuEvent: ItemSelected
        public void RCSpawnerMenu_ItemSelected(UIMenu menu, UIMenuItem selectedItem, int index)
        {
            Game.FadeScreenOut(Constants.FadeTime);
            Wait(Constants.FadeTime);

            Function.Call(Hash.SET_TIMECYCLE_MODIFIER, Constants.TimecycleModifier);
            Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, Constants.TimecycleStrengthBase);
            Function.Call(Hash.DISPLAY_RADAR, false);

            RCVehicle = World.CreateVehicle("rcbandito", RCSpawner.Position + RCSpawner.ForwardVector * -4.0f, RCSpawner.Heading + 180.0f);
            RCVehicle.InstallModKit();
            RCVehicle.SetMod(VehicleMod.Frame, index - 1, false);
            RCVehicle.SetMod(VehicleMod.Fender, 0, false);

            Game.Player.Character.IsExplosionProof = true;
            Game.Player.Character.SetIntoVehicle(RCVehicle, VehicleSeat.Driver);
            Game.Player.Character.Weapons.Select(WeaponHash.Unarmed);

            if (FPSCamera)
            {
                PrevCamMode = Function.Call<int>(Hash.GET_FOLLOW_VEHICLE_CAM_VIEW_MODE);
                Function.Call(Hash.SET_FOLLOW_VEHICLE_CAM_VIEW_MODE, 4);
            }

            Game.FadeScreenIn(Constants.FadeTime);

            RCBombActive = true;
            menu.Visible = false;
        }
        #endregion
    }
}
