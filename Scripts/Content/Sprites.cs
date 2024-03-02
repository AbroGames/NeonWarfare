using KludgeBox.Collections;

namespace KludgeBox.Events.Global
{
	public static class Sprites
	{
		public const string TexturesDir = "res://Assets/Textures";
		public const string SpritesDir = $"{TexturesDir}/Sprites";
		public const string ParticlesDir = $"{TexturesDir}/Particles";

		// glow
		public static RandomPicker<string> Spotlight { get; } = new RandomPicker<string>(
			$"{ParticlesDir}/Glow/256/spotlight_{{0}}.png".BatchNumber(1,8)
			);
		public static string Spotlight1 { get; } = $"{ParticlesDir}/Glow/256/spotlight_1.png";
		public static string Spotlight2 { get; } = $"{ParticlesDir}/Glow/256/spotlight_2.png";
		public static string Spotlight3 { get; } = $"{ParticlesDir}/Glow/256/spotlight_3.png";
		public static string Spotlight4 { get; } = $"{ParticlesDir}/Glow/256/spotlight_4.png";
		public static string Spotlight5 { get; } = $"{ParticlesDir}/Glow/256/spotlight_5.png";
		public static string Spotlight6 { get; } = $"{ParticlesDir}/Glow/256/spotlight_6.png";
		public static string Spotlight7 { get; } = $"{ParticlesDir}/Glow/256/spotlight_7.png";
		public static string Spotlight8 { get; } = $"{ParticlesDir}/Glow/256/spotlight_8.png";

		// star
		public static RandomPicker<string> Star { get; } = new RandomPicker<string>(
			$"{ParticlesDir}/Effects/star_0{{0}}.png".BatchNumber(1, 9)
			);
		public static string Star1 { get; } = $"{ParticlesDir}/Effects/star_01.png";
		public static string Star2 { get; } = $"{ParticlesDir}/Effects/star_02.png";
		public static string Star3 { get; } = $"{ParticlesDir}/Effects/star_03.png";
		public static string Star4 { get; } = $"{ParticlesDir}/Effects/star_04.png";
		public static string Star5 { get; } = $"{ParticlesDir}/Effects/star_05.png";
		public static string Star6 { get; } = $"{ParticlesDir}/Effects/star_06.png";
		public static string Star7 { get; } = $"{ParticlesDir}/Effects/star_07.png";
		public static string Star8 { get; } = $"{ParticlesDir}/Effects/star_08.png";
		public static string Star9 { get; } = $"{ParticlesDir}/Effects/star_09.png";

		// smoke
		public static RandomPicker<string> Smoke { get; } = new RandomPicker<string>(
			$"{ParticlesDir}/Effects/smoke_0{{0}}.png".BatchNumber(1, 8)
			);
		public static string Smoke1 { get; } = $"{ParticlesDir}/Effects/smoke_01.png";
		public static string Smoke2 { get; } = $"{ParticlesDir}/Effects/smoke_02.png";
		public static string Smoke3 { get; } = $"{ParticlesDir}/Effects/smoke_03.png";
		public static string Smoke4 { get; } = $"{ParticlesDir}/Effects/smoke_04.png";
		public static string Smoke5 { get; } = $"{ParticlesDir}/Effects/smoke_05.png";
		public static string Smoke6 { get; } = $"{ParticlesDir}/Effects/smoke_06.png";
		public static string Smoke7 { get; } = $"{ParticlesDir}/Effects/smoke_07.png";
		public static string Smoke8 { get; } = $"{ParticlesDir}/Effects/smoke_08.png";
	}
}
