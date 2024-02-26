using KludgeBox.Collections;

namespace AbroDraft
{
	public static class Sfx
	{
		public const string SoundsDir = "res://Assets/Audio/Sounds";
		
		// impacts
		public static RandomPicker<string> Hit { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/impact/hit{{0}}.wav".BatchNumber(1, 3)
		);

		public static RandomPicker<string> FuturisticHit { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/impact/hit_futuristic{{0}}.wav".BatchNumber(1, 4)
		);

		public static RandomPicker<string> FuturisticCrack { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/impact/futuristic_crack{{0}}.wav".BatchNumber(1, 5)
		);

		public static RandomPicker<string> Zap { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/impact/zap_impact{{0}}.wav".BatchNumber(1, 3)
		);

		public static RandomPicker<string> GlitchyImpact { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/impact/glitchy_impact{{0}}.wav".BatchNumber(1, 5)
		);

		public static RandomPicker<string> HornImpact { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/impact/horn_impact{{0}}.wav".BatchNumber(1, 3)
		);

		public static RandomPicker<string> HornImpact1 { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/impact/horn_impact1.wav"
		);
		public static RandomPicker<string> HornImpact2 { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/impact/horn_impact2.wav"
		);
		public static RandomPicker<string> HornImpact3 { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/impact/horn_impact3.wav"
		);
		public static RandomPicker<string> DeepImpact { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/impact/deep_impact{{0}}.wav".BatchNumber(1, 4)
		);

		
		

		// bass
		public static RandomPicker<string> BassDrop { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/bass/bass_drop{{0}}.wav".BatchNumber(1, 2)
		);

		public static RandomPicker<string> BassDowner { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/bass/bass_downer1.wav"
		);

		public static RandomPicker<string> Bass { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/bass/bass1.wav"
		);

		
		

		// magic
		public static RandomPicker<string> FireBeam { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/magic/fire_beam{{0}}.wav".BatchNumber(1, 3)
		);

		public static RandomPicker<string> FireIgnite { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/magic/fire_ignite{{0}}.wav".BatchNumber(1, 2)
		);

		public static RandomPicker<string> FireWhoosh { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/magic/fire_whoosh{{0}}.wav".BatchNumber(1, 3)
		);

		public static RandomPicker<string> Spell { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/magic/spell1.wav"
		);

		public static RandomPicker<string> ThunderClap { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/magic/thunder_close{{0}}.wav".BatchNumber(1, 4)
		);

		public static RandomPicker<string> TurbulentFire { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/magic/turbulent_fire.wav"
		);

		public static RandomPicker<string> WindGust { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/magic/wind_gust.wav"
		);

		public static RandomPicker<string> WooshShort { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/magic/woosh_short{{0}}.wav".BatchNumber(1, 4)
		);


		
		
		// misc
		public static RandomPicker<string> Bubbles { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/misc/bubble{{0}}.wav".BatchNumber(1, 4)
		);

		public static RandomPicker<string> Arc { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/misc/arc.ogg"
		);

		public static RandomPicker<string> Buzz { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/misc/buzz.wav"
		);

		public static RandomPicker<string> ChestOpen { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/misc/chest_open.wav"
		);

		public static RandomPicker<string> ChestClose { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/misc/chest_close.wav"
		);

		public static RandomPicker<string> FireworkLaunch { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/misc/firework_launch{{0}}.wav".BatchNumber(1, 4)
		);

		public static RandomPicker<string> Grab { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/misc/grab.wav"
		);

		public static RandomPicker<string> Pop { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/misc/pop.ogg"
		);

		public static RandomPicker<string> Shrill { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/misc/shrill.wav"
		);

		public static RandomPicker<string> Throw { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/misc/throw{{0}}.wav".BatchNumber(1, 5)
		);

		public static RandomPicker<string> LevelUp { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/misc/level_up.wav"
		);

		public static RandomPicker<string> Beep { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/misc/beep.wav"
		);

		
		
		// weapons
		public static RandomPicker<string> ElectronicShot { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/electronic_shot{{0}}.wav".BatchNumber(1, 3)
		);

		public static RandomPicker<string> HandgunShot { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/handgun_shot{{0}}.wav".BatchNumber(1, 2)
		);

		public static RandomPicker<string> HandgunShotAlt { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/handgun_shot.wav"
		);

		public static RandomPicker<string> GrenadelauncherShot { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/grenade_launcher_launch.wav"
		);

		public static RandomPicker<string> HandgunReload { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/handgun_reload.wav"
		);

		public static RandomPicker<string> HeavyRifleShot { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/heavy_rifle_shot.ogg"
		);

		public static RandomPicker<string> LaserShot { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/laser_shot.wav"
		);

		public static RandomPicker<string> LaserShots { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/laser_shots.wav"
		);

		public static RandomPicker<string> LaserShotShort { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/laser_shot_short.wav"
		);

		public static RandomPicker<string> HeavyLauncher { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/launcher_1.wav"
		);

		public static RandomPicker<string> PlasmaShot { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/plasma_shot{{0}}.wav".BatchNumber(1, 4)
		);

		public static RandomPicker<string> RocketLaunch { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/rocket_launch{{0}}.wav".BatchNumber(1, 3)
		);

		public static RandomPicker<string> ScifiLaunch { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/scifi_launch{{0}}.wav".BatchNumber(1, 5)
		);

		public static RandomPicker<string> ScifiShot { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/scifi_shot.wav"
        );

		public static RandomPicker<string> SmallLaserShot { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/small_laser_shot.wav"
		);

		public static RandomPicker<string> SupressedShot { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/supressed_shot.wav"
		);

		public static RandomPicker<string> Beam { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/beam.ogg"
		);

		public static RandomPicker<string> LaserBig { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/laserbig.ogg"
		);

		public static RandomPicker<string> LaserBeam { get; } = new RandomPicker<string>(
			$"{SoundsDir}/normal/weapons/laserbeam.ogg"
		);
		
		
		
		
		// UI
		public static RandomPicker<string> UiSelect { get; } = new RandomPicker<string>(
			$"{SoundsDir}/ui/select{{0}}.wav".BatchNumber(1, 4)
		);
		
		public static RandomPicker<string> UiSelectNeutral { get; } = new RandomPicker<string>(
			$"{SoundsDir}/ui/neutral_select.wav"
		);
		
		public static RandomPicker<string> UiClick { get; } = new RandomPicker<string>(
			$"{SoundsDir}/ui/click{{0}}.wav".BatchNumber(1, 3)
		);
	}
}
