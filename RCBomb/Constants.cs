using GTA;
using GTA.Math;

namespace RCBomb
{
    public class Constants
    {
        public static readonly Vector3 SpawnerPos = new Vector3(974.068f, -139.491f, 74.0638f);
        public const float SpawnerRot = 60.0f;
        public const int RequiredHoldingTime = 500;
        public const int FadeTime = 500;
        public const int UpdateInterval = 250;
        public const int CheckInterval = 2000;

        public const string TimecycleModifier = "CAMERA_secuirity_FUZZ";
        public const float TimecycleStrengthBase = 0.15f;
        public const float TimecycleStrengthDistMultiplier = 0.005f;

        public static readonly Control[] ControlsToDisable = { Control.CharacterWheel, Control.SelectWeapon, Control.VehicleExit, Control.VehicleSelectNextWeapon, Control.VehicleSelectPrevWeapon };
    }
}
