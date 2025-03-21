using System;
using System.Collections;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Collections;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;

namespace NeonWarfare.Scripts.KludgeBox.Godot.Services;

[GlobalClass]
public partial class Audio2D : Node2D
{
	
	private static Audio2D Instance
	{
		get
		{
			if (_instance == null)
			{
				// Check if we were loaded via Autoload
				_instance = ((SceneTree)Engine.GetMainLoop()).Root.GetNodeOrNull<Audio2D>(typeof(Audio2D).Name);
				if (_instance == null)
				{
					// Instantiate to root at runtime
					_instance = new Audio2D();
					_instance.Name = typeof(Audio2D).Name;
					_instance.CallDeferred(nameof(InitGlobalInstance));
				}
			}
			return _instance;
		}
	}
	
	private void InitGlobalInstance()
	{
		((SceneTree)Engine.GetMainLoop()).Root.AddChild(this);
	}
	
	private static Audio2D _instance;

	private static HashSet<AudioStreamPlayer> _uiSounds = new HashSet<AudioStreamPlayer>();
    private static HashSet<AudioStreamPlayer2D> _worldSounds = new HashSet<AudioStreamPlayer2D>();
    
    
    internal const string MasterBus = "Master";

    internal const string MusicBus = "Music";

    internal const string SoundsBus = "Sounds";

    internal static int MasterIndex => AudioServer.GetBusIndex(MasterBus);

    internal static int MusicIndex => AudioServer.GetBusIndex(MusicBus);

    internal static int SoundsIndex => AudioServer.GetBusIndex(SoundsBus);
    
    /// <summary>
    /// The currently playing music stream player.
    /// </summary>
    public static AudioStreamPlayer CurrentMusic { get; private set; } = null;

    /// <summary>
    /// Read-only collection of currently playing UI sounds.
    /// </summary>
    public static ReadOnlyHashSet<AudioStreamPlayer> PlayingUiSounds => _uiSounds.AsReadOnly();

    /// <summary>
    /// Read-only collection of currently playing world sounds.
    /// </summary>
    public static ReadOnlyHashSet<AudioStreamPlayer2D> PlayingWorldSounds => _worldSounds.AsReadOnly();
    
    /// <summary>
    /// Master volume in linear scale (0.0 to 1.0).
    /// </summary>
    public static float MasterVolume
    {
        get => Mathf.DbToLinear(AudioServer.GetBusVolumeDb(MasterIndex));
        set => AudioServer.SetBusVolumeDb(MasterIndex, Mathf.LinearToDb(value));
    }

    /// <summary>
    /// Master volume in decibels.
    /// </summary>
    public static float MasterVolumeDb
    {
        get => AudioServer.GetBusVolumeDb(MasterIndex);
        set => AudioServer.SetBusVolumeDb(MasterIndex, value);
    }

    /// <summary>
    /// Music volume in linear scale (0.0 to 1.0).
    /// </summary>
    public static float MusicVolume
    {
        get => Mathf.DbToLinear(AudioServer.GetBusVolumeDb(MusicIndex));
        set => AudioServer.SetBusVolumeDb(MusicIndex, Mathf.LinearToDb(value));
    }
    
    /// <summary>
    /// Music volume in decibels.
    /// </summary>
    public static float MusicVolumeDb
    {
        get => AudioServer.GetBusVolumeDb(MusicIndex);
        set => AudioServer.SetBusVolumeDb(MusicIndex, value);
    }

    /// <summary>
    /// Sounds volume in linear scale (0.0 to 1.0).
    /// </summary>
    public static float SoundsVolume
    {
        get => Mathf.DbToLinear(AudioServer.GetBusVolumeDb(SoundsIndex));
        set => AudioServer.SetBusVolumeDb(SoundsIndex, Mathf.LinearToDb(value));
    }

    /// <summary>
    /// Sounds volume in decibels.
    /// </summary>
    public static float SoundsVolumeDb
    {
        get => AudioServer.GetBusVolumeDb(SoundsIndex);
        set => AudioServer.SetBusVolumeDb(SoundsIndex, value);
    }

    /// <summary>
    /// Plays music at the specified path.
    /// </summary>
    /// <param name="path">Path to the music resource.</param>
    public static AudioStreamPlayer PlayMusic(string path, float volume = 1f, Node target = null)
	{
		StopMusic();

		if (!target.IsValid())
		{
			target = Instance;
		}
		
		var stream = new AudioStreamPlayer();
		stream.Stream = GD.Load<AudioStream>(path);
		stream.Bus = MusicBus;
		stream.Autoplay = true;
		stream.VolumeDb = Mathf.LinearToDb(volume);

		CurrentMusic = stream;
		target.AddChild(stream);
		return stream;
	}

    /// <summary>
    /// Запустит воспроизведение музыки в указанном порядке. Этим методом можно генерировать плейлист динамически.
    /// </summary>
    /// <param name="playlist">Перечислитель саундтреков</param>
    /// <param name="target"></param>
    /// <returns></returns>
	public static PlaylistHandle PlayMusicSequence(IEnumerator<string> playlist, float volume = 1f,
		Node target = null)
	{
		if(target is null)
			target = Instance;
			
		var handle = new PlaylistHandle(playlist, target, volume);
		handle.PlayNextBgm();
		
		return handle;
	}

	private class LoopedStringEnumerator : IEnumerator<string>
	{
		private List<string> _cachedList = new ();
		private IEnumerator<string> _current;
		private bool _firstPass = true;
		public LoopedStringEnumerator(IEnumerator<string> enumerator)
		{
			_current = enumerator;
		}
		public bool MoveNext()
		{
			if (_current.MoveNext())
			{
				if (_firstPass)
				{
					_cachedList.Add(_current.Current);
				}
				return true;
			}

			if (_cachedList.Count == 0) // Если список пустой, прекращаем.
				return false;

			_firstPass = false;
			_current = _cachedList.GetEnumerator();
			return _current.MoveNext();
		}

		public void Reset()
		{
			_current.Reset();
		}

		public string Current => _current.Current;

		object IEnumerator.Current => Current;

		public void Dispose()
		{
			_current.Dispose();
		}
	}
	
	/// <summary>
	/// Запускает музыку из указанного плейлиста.
	/// </summary>
	/// <param name="playlist">Плейлист для проигрывания</param>
	/// <param name="loop">Для false - проиграет плейлист до конца и остановится. Для true - закэширует элементы плейлиста и продолжит воспроизводить их по кругу.</param>
	/// <param name="target">Узел, к которому будет прикреплена музыка</param>
	/// <returns></returns>
	/// <remarks>
	/// Важно: в случае loop = true оригинальный IEnumerable будет обойден только ОДИН раз, после чего вместо него будет использован закэшированный список.
	/// </remarks>
	public static PlaylistHandle PlayMusicSequence(IEnumerable<string> playlist, float volume = 1f, bool loop = false, Node target = null)
	{
		Func<IEnumerator<string>> playlistEnumeratorProvider;
		
		if (!loop)
		{
			playlistEnumeratorProvider = playlist.GetEnumerator; // используем стандартный перечислитель для однократного прохода
		}
		else
		{
			playlistEnumeratorProvider = () => new LoopedStringEnumerator(playlist.GetEnumerator()); // используем кэширующую обертку для зацикливания плейлиста
		}
		
		return PlayMusicSequence(playlistEnumeratorProvider(), volume, target);
	}
    

	/// <summary>
	/// Plays a UI sound at the specified path with optional volume.
	/// </summary>
	/// <param name="path">Path to the sound resource.</param>
	/// <param name="volume">Volume of the sound (0.0 to 1.0).</param>
	public static AudioStreamPlayer PlayUiSound(string path, float volume = 1)
	{
		var res = GD.Load<AudioStream>(path);
		var stream = new AudioStreamPlayer();
		stream.Stream = res;
		stream.Bus = SoundsBus;
		stream.VolumeDb = Mathf.LinearToDb(volume);
		_uiSounds.Add(stream);
		stream.Finished += () => 
		{ 
			stream.QueueFree();
			_uiSounds.Remove(stream);
		};
		stream.TreeExited += () =>
		{
			_uiSounds.Remove(stream);
		};
		stream.Autoplay = true;

		Instance.AddChild(stream);
		return stream;
	}

	/// <summary>
	/// Plays a sound at the specified position in the game world with optional volume.
	/// </summary>
	/// <param name="path">Path to the sound resource.</param>
	/// <param name="position">Position in the game world.</param>
	/// <param name="volume">Volume of the sound (0.0 to 1.0).</param>
	public static AudioStreamPlayer2D PlaySoundAt(string path, Vector2 position, float volume = 1, Node parent = null)
	{
		var stream = ConfigureSound(path,volume);
		parent ??= Instance;
		
		stream.Position = position;
		stream.Autoplay = true;
		Callable.From(() =>
		{
			if(parent.IsValid())
				parent.AddChild(stream);
			else
				stream.QueueFree();
		}).CallDeferred();
		return stream;
	}

	public static AudioStreamPlayer2D PlaySoundAt(PlaybackOptions options, Vector2 position, Node parent = null)
	{
		var stream = PlaySoundAt(options.Path, position, options.Volume, parent);
		stream.Attenuation = options.Attenuation;
		stream.MaxDistance = options.MaxDistance;
		stream.PanningStrength = options.PanningStrength;
		stream.PitchScale = options.PitchScale;
		
		return stream;
	}

	/// <summary>
	/// Plays a sound attached to the specified node with optional volume.
	/// </summary>
	/// <param name="path">Path to the sound resource.</param>
	/// <param name="node">Node2D to attach the sound to.</param>
	/// <param name="volume">Volume of the sound (0.0 to 1.0).</param>
	public static AudioStreamPlayer2D PlaySoundOn(string path, Node2D node, float volume = 1)
	{
		var stream = ConfigureSound(path, volume);
		stream.Autoplay = true;

		node.AddChild(stream);
		return stream;
	}


	/// <summary>
	/// Clears all currently playing UI and world sounds.
	/// </summary>
	/// <remarks>
	/// This method stops and removes all UI and world sounds that are currently playing, 
	/// both from the UI sound pool and the world sound pool. 
	/// After calling this method, no UI or world sounds will be audible.
	/// </remarks>
	public static void ClearAllSounds()
	{
		foreach (var stream in _uiSounds)
			stream.QueueFree();

		foreach (var stream in _worldSounds)
			stream.QueueFree();
	}

	public static Tween StopMusic(double fadeOut = 0)
	{
		if (!CurrentMusic.IsValid())
			return null;
		
		var tween = CurrentMusic.GetParent().CreateTween();
		var music = CurrentMusic;
		tween.TweenProperty(music, "volume_db", -30, fadeOut);
		tween.TweenCallback(Callable.From(() =>
		{
			if(music.IsValid())
				music.QueueFree();
		}));
		tween.Play();
		CurrentMusic = null;
		return tween;
	}

	/// <summary>
	/// Configures a 2D sound with the specified path and volume. The sound will be removed after finishing.
	/// </summary>
	/// <param name="path">Path to the sound resource.</param>
	/// <param name="volume">Volume of the sound (0.0 to 1.0).</param>
	/// <returns>The configured AudioStreamPlayer2D instance.</returns>
	public static AudioStreamPlayer2D ConfigureSound(string path, float volume = 1)
	{
		var res = GD.Load<AudioStream>(path);
		var stream = new AudioStreamPlayer2D();
		stream.Stream = res;
		stream.Bus = SoundsBus;
		stream.VolumeDb = Mathf.LinearToDb(volume);
		_worldSounds.Add(stream);
		stream.Finished += () =>
		{
			stream.QueueFree();
			_worldSounds.Remove(stream);
		};
		stream.TreeExited += () =>
		{
			_worldSounds.Remove(stream);
		};

		return stream;
	}
	
	/// <summary>
	/// Эта штука будет воспроизводить звуковые файлы из перечисления до тех пор, пока перечисление не закончится, либо пока текущий трек не будет принудительно остановлен.
	/// </summary>
	public class PlaylistHandle
	{
		public AudioStreamPlayer CurrentMusicPlayer { get; private set; } // текущий трек
		public bool IsPlaying { get; private set; }
		
		private IEnumerator<string> _playlistEnumerator;
		private Action _safeNextAction;
		private bool _firstLoop = true;
		private Node _target;
		private float _volume;
		public PlaylistHandle(IEnumerator<string> playlistEnumerator, Node target, float volume = 1)
		{
			_playlistEnumerator = playlistEnumerator;
			_target = target;
			_safeNextAction = PlayNextBgm;
			_volume = volume;
		}
		
		/// <summary>
		/// Остановит воспроизведение плейлиста. После остановки нельзя продолжить воспроизведение.
		/// </summary>
		public void StopBgm()
		{
			if (CurrentMusicPlayer.IsValid())
			{
				if (CurrentMusicPlayer == Audio2D.CurrentMusic)
				{
					Audio2D.StopMusic();
				}
				else
				{
					CurrentMusicPlayer.QueueFree();
				}
			}

			IsPlaying = false;
			_firstLoop = false;
		}

		/// <summary>
		/// Остановит текущий трек и воспроизведет следующий трек в списке, если такой имеется
		/// </summary>
		public void PlayNextBgm()
		{
			var isReadyToPlay = IsPlaying || _firstLoop;
			var isStopped = !CurrentMusicPlayer.IsValid() && !_firstLoop;
			
			if (!isReadyToPlay || isStopped)
			{
				IsPlaying = false;
				return;
			}

			Node nextTarget;
			float nextVolume;
			if (_firstLoop)
			{
				nextTarget = _target;
				nextVolume = _volume;
			}
			else
			{
				nextTarget = CurrentMusicPlayer.GetParent();
				nextVolume = _volume;
			}

			if (_playlistEnumerator.MoveNext())
			{
				Audio2D.StopMusic();
				var bgm = PlayMusic(_playlistEnumerator.Current, nextVolume, nextTarget);
				bgm.Finished += _safeNextAction;
				CurrentMusicPlayer = bgm;
			}
		}
	}
}






public static class AudioExtensions
{
	public static AudioStreamPlayer PitchVariation(this AudioStreamPlayer stream, float min, float max)
	{
		stream.PitchScale = Rand.Range(min, max);
		return stream;
	}
	
	public static AudioStreamPlayer PitchVariation(this AudioStreamPlayer stream, float range)
	{
		if (range < 0 || range > 1)
		{
			throw new ArgumentOutOfRangeException(nameof(range));
		}
		
		return stream.PitchVariation(1 / (1+range), 1 * (1+range));
	}
	public static AudioStreamPlayer2D PitchVariation(this AudioStreamPlayer2D stream, float min, float max)
	{
		stream.PitchScale = Rand.Range(min, max);
		return stream;
	}
	
	public static AudioStreamPlayer2D PitchVariation(this AudioStreamPlayer2D stream, float range)
	{
		if (range < 0 || range > 1)
		{
			throw new ArgumentOutOfRangeException(nameof(range));
		}
		
		return stream.PitchVariation(1 / (1+range), 1 * (1+range));
	}
}
