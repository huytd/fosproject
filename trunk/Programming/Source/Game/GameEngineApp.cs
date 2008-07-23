// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using Engine;
using Engine.MathEx;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.FileSystem;
using Engine.MapSystem;
using Engine.UISystem;
using Engine.SoundSystem;
using Engine.PhysicsSystem;
using Engine.Utils;
using GameCommon;
using GameEntities;

namespace Game
{
	/// <summary>
	/// Defines a game application.
	/// </summary>
	public class GameEngineApp : EngineApp
	{
		static GameEngineApp instance;

		string needMapLoadName;

		static float gamma = 1;
		[Config( "Video", "gamma" )]
		public static float _Gamma
		{
			get { return gamma; }
			set
			{
				gamma = value;
				EngineApp.Instance.Gamma = gamma;
			}
		}

		static bool showSystemCursor;
		[Config( "Video", "showSystemCursor" )]
		public static bool _ShowSystemCursor
		{
			get { return showSystemCursor; }
			set
			{
				showSystemCursor = value;

				EngineApp.Instance.ShowSystemCursor = value;

				if( EngineApp.Instance.ShowSystemCursor )
				{
					if( ScreenControlManager.Instance != null )
						ScreenControlManager.Instance.DefaultCursor = null;
				}
				else
				{
					string cursorName = "Cursors\\Default.png";
					if( !VirtualFile.Exists( cursorName ) )
						cursorName = null;
					if( ScreenControlManager.Instance != null )
						ScreenControlManager.Instance.DefaultCursor = cursorName;
					if( cursorName == null )
						EngineApp.Instance.ShowSystemCursor = true;
				}
			}
		}

		static bool drawFPS;
		[Config( "Video", "drawFPS" )]
		public static bool _DrawFPS
		{
			get { return drawFPS; }
			set
			{
				drawFPS = value;
				EngineApp.Instance.ShowFPS = value;
			}
		}

		static ShadowTechniques shadowTechnique = ShadowTechniques.ShadowmapHigh;
		[Config( "Video", "shadowTechnique" )]
		public static ShadowTechniques ShadowTechnique
		{
			get { return shadowTechnique; }
			set { shadowTechnique = value; }
		}

		//static ShadowTextureCameraSetups shadowCameraSetup = ShadowTextureCameraSetups.Uniform;
		//[Config( "Video", "shadowCameraSetup" )]
		//public static ShadowTextureCameraSetups ShadowCameraSetup
		//{
		//   get { return shadowCameraSetup; }
		//   set { shadowCameraSetup = value; }
		//}

		static ColorValue shadowColor = new ColorValue( .75f, .75f, .75f, .75f );
		[Config( "Video", "shadowColor" )]
		public static ColorValue ShadowColor
		{
			get { return shadowColor; }
			set { shadowColor = value; }
		}

		static bool shadowFarDistanceUseMapSettings = true;
		[Config( "Video", "shadowFarDistanceUseMapSettings" )]
		public static bool ShadowFarDistanceUseMapSettings
		{
			get { return shadowFarDistanceUseMapSettings; }
			set { shadowFarDistanceUseMapSettings = value; }
		}

		static float shadowFarDistance = 30.0f;
		[Config( "Video", "shadowFarDistance" )]
		public static float ShadowFarDistance
		{
			get { return shadowFarDistance; }
			set { shadowFarDistance = value; }
		}

		static int shadow2DTextureSize = 1024;
		[Config( "Video", "shadow2DTextureSize" )]
		public static int Shadow2DTextureSize
		{
			get { return shadow2DTextureSize; }
			set { shadow2DTextureSize = value; }
		}

		static int shadow2DTextureCount = 2;
		[Config( "Video", "shadow2DTextureCount" )]
		public static int Shadow2DTextureCount
		{
			get { return shadow2DTextureCount; }
			set { shadow2DTextureCount = value; }
		}

		static int shadowCubicTextureSize = 512;
		[Config( "Video", "shadowCubicTextureSize" )]
		public static int ShadowCubicTextureSize
		{
			get { return shadowCubicTextureSize; }
			set { shadowCubicTextureSize = value; }
		}

		static int shadowCubicTextureCount = 1;
		[Config( "Video", "shadowCubicTextureCount" )]
		public static int ShadowCubicTextureCount
		{
			get { return shadowCubicTextureCount; }
			set { shadowCubicTextureCount = value; }
		}

		static WaterPlane.ReflectionLevels waterReflectionLevel = WaterPlane.ReflectionLevels.OnlyModels;
		[Config( "Video", "waterReflectionLevel" )]
		public static WaterPlane.ReflectionLevels WaterReflectionLevel
		{
			get { return waterReflectionLevel; }
			set { waterReflectionLevel = value; }
		}

		static float soundVolume = .8f;
		[Config( "Sound", "soundVolume" )]
		public static float SoundVolume
		{
			get { return soundVolume; }
			set
			{
				soundVolume = value;
				if( EngineApp.Instance.DefaultSoundChannelGroup != null )
					EngineApp.Instance.DefaultSoundChannelGroup.Volume = soundVolume;
			}
		}

		static float musicVolume = .8f;
		[Config( "Sound", "musicVolume" )]
		public static float MusicVolume
		{
			get { return musicVolume; }
			set
			{
				musicVolume = value;
				if( GameMusic.MusicChannelGroup != null )
					GameMusic.MusicChannelGroup.Volume = musicVolume;
			}
		}

		[Config( "Environment", "autorunMapName" )]
		public static string autorunMapName = "";

		//

		public static new GameEngineApp Instance
		{
			get { return instance; }
		}

		void ChangeToBetterDefaultSettings()
		{
			bool shadowTechniqueInitialized = false;
			bool shadowTextureSizeInitialized = false;

			string error;
			TextBlock block = TextBlockUtils.LoadFromRealFile( EngineApp.ConfigName, out error );
			if( block != null )
			{
				TextBlock blockVideo = block.FindChild( "Video" );
				if( blockVideo != null )
				{
					if( blockVideo.IsAttributeExist( "shadowTechnique" ) )
						shadowTechniqueInitialized = true;
					if( blockVideo.IsAttributeExist( "shadowTextureSize" ) )
						shadowTextureSizeInitialized = true;
				}
			}

			//shadowTechnique
			if( !shadowTechniqueInitialized )
			{
				if( RenderSystem.Instance.GPUIsGeForce() &&
					RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_NV10 &&
					RenderSystem.Instance.GPUCodeName <= GPUCodeNames.GeForce_NV40 )
				{
					shadowTechnique = ShadowTechniques.ShadowmapLow;
				}
				if( RenderSystem.Instance.GPUIsRadeon() &&
					RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R100 &&
					RenderSystem.Instance.GPUCodeName <= GPUCodeNames.Radeon_R400 )
				{
					shadowTechnique = ShadowTechniques.ShadowmapLow;
				}
			}

			//shadow texture size
			if( !shadowTextureSizeInitialized )
			{
				if( RenderSystem.Instance.GPUIsGeForce() &&
					RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_G80 )
				{
					shadow2DTextureSize = 2048;
				}
				if( RenderSystem.Instance.GPUIsRadeon() &&
					RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R600 )
				{
					shadow2DTextureSize = 2048;
				}
			}
		}

		protected override bool OnCreate()
		{
			instance = this;

			ChangeToBetterDefaultSettings();

			if( !base.OnCreate() )
				return false;

			//set main form logo
			Form form = WindowControl as Form;
			if( form != null )
				form.Icon = Game.Properties.Resources.Logo;

			SoundVolume = soundVolume;
			MusicVolume = musicVolume;

			ScreenControlManager.Init();
			if( !ControlsWorld.Init() )
				return false;

			EControl programLoadingWindow = ControlDeclarationManager.Instance.CreateControl(
				"Gui\\ProgramLoadingWindow.gui" );
			if( programLoadingWindow != null )
				ScreenControlManager.Instance.Controls.Add( programLoadingWindow );

			RenderScene();

			_ShowSystemCursor = _ShowSystemCursor;
			_DrawFPS = _DrawFPS;

			EngineConsole.Instance.Texture = TextureManager.Instance.Load( "Utils/Console.png",
				Texture.Type.Type2D, 0 );
			EngineConsole.Instance.Font = FontManager.Instance.LoadFont( "Default", .025f );
			EngineConsole.Instance.StaticText = "Version " + EngineVersionInformation.Version;

			Log.Handlers.InfoHandler += delegate( string text )
			{
				if( EngineConsole.Instance != null )
					EngineConsole.Instance.Print( text );
			};

			Log.Handlers.WarningHandler -= Program.Log_WarningHandler;
			Log.Handlers.WarningHandler += delegate( string text )
			{
				if( EngineConsole.Instance != null )
				{
					EngineConsole.Instance.Print( "Warning: " + text, new ColorValue( 1, 0, 0 ) );
					EngineConsole.Instance.Active = true;
				}
				else
				{
					MessageBox.Show( text, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning );
				}
			};

			Log.Handlers.ErrorHandler -= Program.Log_ErrorHandler;
			Log.Handlers.ErrorHandler += delegate( string text )
			{
				if( ScreenControlManager.Instance != null )
				{
					//find already created MessageBoxWindow
					foreach( EControl control in ScreenControlManager.Instance.Controls )
					{
						if( control is MessageBoxWindow )
							return;
					}

					if( Map.Instance != null )
						EntitySystemWorld.Instance.Simulation = false;

					EngineApp.Instance.MouseRelativeMode = false;

					ScreenControlManager.Instance.Controls.Add( new MessageBoxWindow( text, "Error",
						delegate( EButton sender )
						{
							ScreenControlManager.Instance.Controls.Clear();

							if( EntitySystemWorld.Instance == null )
							{
								EngineApp.Instance.ShouldExit();
								return;
							}

							EntitySystemWorld.Instance.Simulation = false;

							ScreenControlManager.Instance.Controls.Add( new MainMenuWindow() );

						} ) );
				}
				else
				{
					MessageBox.Show( text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning );
				}
			};

			Log.Handlers.FatalHandler += delegate( string text, ref bool handled )
			{
				if( ScreenControlManager.Instance != null )
				{
					//find already created MessageBoxWindow
					foreach( EControl control in ScreenControlManager.Instance.Controls )
					{
						if( control is MessageBoxWindow )
						{
							handled = true;
							return;
						}
					}
				}
			};

			//MotionBlur for a player unit contusion
			{
				Compositor compositor = CompositorManager.Instance.GetByName( "MotionBlur" );
				if( compositor != null && compositor.IsSupported() )
					RendererWorld.Instance.DefaultViewport.AddCompositor( "MotionBlur" );
			}

			//Camera
			Camera camera = RendererWorld.Instance.DefaultCamera;
			camera.NearClipDistance = .1f;
			camera.FarClipDistance = 1000.0f;
			camera.FixedUp = Vec3.ZAxis;
			camera.Fov = 90;
			camera.Position = new Vec3( -10, -10, 10 );
			camera.LookAt( new Vec3( 0, 0, 0 ) );

			if( programLoadingWindow != null )
				programLoadingWindow.SetShouldDetach();

			//Game controls
			GameControlsManager.Init();

			//EntitySystem
			if( !EntitySystemWorld.Init( new EntitySystemWorld() ) )
				return true;// false;

			string mapName = "";

			if( autorunMapName != "" && autorunMapName.Length > 2 )
			{
				mapName = autorunMapName;
				if( !mapName.Contains( "\\" ) && !mapName.Contains( "/" ) )
					mapName = "Maps/" + mapName + "/Map.map";
			}

			string[] commandLineArgs = Environment.GetCommandLineArgs();
			if( commandLineArgs.Length > 1 )
			{
				string name = commandLineArgs[ 1 ];
				if( name[ 0 ] == '\"' && name[ name.Length - 1 ] == '\"' )
					name = name.Substring( 1, name.Length - 2 );
				name = name.Replace( '/', '\\' );

				string dataDirectory = Path.GetDirectoryName( VirtualFileSystem.
					ApplicationExecutablePath ) + "\\" + VirtualFileSystem.ResourceDirectory;
				dataDirectory = dataDirectory.Replace( '/', '\\' );

				if( name.Length > dataDirectory.Length )
					if( string.Compare( name.Substring( 0, dataDirectory.Length ), dataDirectory, true ) == 0 )
						name = name.Substring( dataDirectory.Length + 1 );

				mapName = name;
			}

			if( mapName != "" )
			{
				if( !MapLoad( mapName ) )
				{
					//Error
					foreach( EControl control in ScreenControlManager.Instance.Controls )
					{
						if( control is MessageBoxWindow )
							return true;
					}

					GameMusic.MusicPlay( "Sounds\\Music\\MainMenu.ogg", true );
					ScreenControlManager.Instance.Controls.Add( new EngineLogoWindow() );
				}
			}
			else
			{
				GameMusic.MusicPlay( "Sounds\\Music\\MainMenu.ogg", true );
				ScreenControlManager.Instance.Controls.Add( new EngineLogoWindow() );
			}

			//example of custom input device
			//ExampleCustomInputDevice.InitDevice();

			return true;
		}

		protected override void OnDestroy()
		{
			MapSystemWorld.MapDestroy();
			EntitySystemWorld.Shutdown();

			GameControlsManager.Shutdown();

			ControlsWorld.Shutdown();
			ScreenControlManager.Shutdown();

			EngineConsole.Shutdown();

			instance = null;
			base.OnDestroy();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( EngineConsole.Instance.OnKeyDown( e ) )
				return true;
			if( ScreenControlManager.Instance != null )
				if( ScreenControlManager.Instance.DoKeyDown( e ) )
					return true;

			//make screenshot
			if( e.Key == EKeys.F12 )
			{
				if( !Directory.Exists( "Screenshots" ) )
					Directory.CreateDirectory( "Screenshots" );

				string format = "Screenshots\\Screenshot{0}.tga";

				for( int n = 1; n < 1000; n++ )
				{
					string v = n.ToString();
					if( n < 10 )
						v = "0" + v;
					if( n < 100 )
						v = "0" + v;

					string fileName = string.Format( format, v );

					if( !File.Exists( fileName ) )
					{
						RendererWorld.Instance.RenderWindow.WriteContentsToFile( fileName );
						break;
					}
				}

				return true;
			}

			return base.OnKeyDown( e );
		}

		protected override bool OnKeyPress( KeyPressEvent e )
		{
			if( EngineConsole.Instance.OnKeyPress( e ) )
				return true;
			if( ScreenControlManager.Instance != null )
				if( ScreenControlManager.Instance.DoKeyPress( e ) )
					return true;
			return base.OnKeyPress( e );
		}

		protected override bool OnKeyUp( KeyEvent e )
		{
			if( ScreenControlManager.Instance != null )
				if( ScreenControlManager.Instance.DoKeyUp( e ) )
					return true;
			return base.OnKeyUp( e );
		}

		protected override bool OnMouseDown( EMouseButtons button )
		{
			if( ScreenControlManager.Instance != null )
				if( ScreenControlManager.Instance.DoMouseDown( button ) )
					return true;
			return base.OnMouseDown( button );
		}

		protected override bool OnMouseUp( EMouseButtons button )
		{
			if( ScreenControlManager.Instance != null )
				if( ScreenControlManager.Instance.DoMouseUp( button ) )
					return true;
			return base.OnMouseUp( button );
		}

		protected override bool OnMouseDoubleClick( EMouseButtons button )
		{
			if( ScreenControlManager.Instance != null )
				if( ScreenControlManager.Instance.DoMouseDoubleClick( button ) )
					return true;
			return base.OnMouseDoubleClick( button );
		}

		protected override void OnMouseMove( Vec2 mouse )
		{
			base.OnMouseMove( mouse );
			if( ScreenControlManager.Instance != null )
				ScreenControlManager.Instance.DoMouseMove( mouse );
		}

		protected override bool OnMouseWheel( int delta )
		{
			if( ScreenControlManager.Instance != null )
				if( ScreenControlManager.Instance.DoMouseWheel( delta ) )
					return true;
			return base.OnMouseWheel( delta );
		}

		protected override bool OnJoystickEvent( JoystickInputEvent e )
		{
			if( ScreenControlManager.Instance != null )
				if( ScreenControlManager.Instance.DoJoystickEvent( e ) )
					return true;
			return base.OnJoystickEvent( e );
		}

		protected override bool OnCustomInputDeviceEvent( InputEvent e )
		{
			if( ScreenControlManager.Instance != null )
				if( ScreenControlManager.Instance.DoCustomInputDeviceEvent( e ) )
					return true;
			return base.OnCustomInputDeviceEvent( e );
		}

		protected override void OnSystemPause( bool pause )
		{
			base.OnSystemPause( pause );

			if( EntitySystemWorld.Instance != null )
				EntitySystemWorld.Instance.SystemPauseOfSimulation = pause;
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			if( needMapLoadName != null )
			{
				string name = needMapLoadName;
				needMapLoadName = null;
				MapLoad( name );
			}

			EngineConsole.Instance.OnTick( delta );
			ScreenControlManager.Instance.DoTick( delta );
		}

		protected override void OnRender()
		{
			base.OnRender();
			ScreenControlManager.Instance.DoRender();
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );
			if( Map.Instance != null )
				Map.Instance.DoDebugRenderUI( renderer );
			ScreenControlManager.Instance.DoRenderUI( renderer );
			EngineConsole.Instance.OnRenderUI( renderer );
		}

		WorldType GetWorldType()
		{
			List<EntityType> list = EntityTypes.Instance.GetTypesBasedOnClass(
				EntityTypes.Instance.GetClassInfoByEntityClassName( "World" ) );

			for( int n = 0; n < list.Count; n++ )
			{
				if( list[ n ].ManualCreated )
				{
					list.RemoveAt( n );
					n--;
				}
			}

			if( list.Count == 0 )
			{
				Log.Error( "World type not defined." );
				return null;
			}
			if( list.Count != 1 )
			{
				Log.Error( "World type need alone." );
				return null;
			}
			return (WorldType)list[ 0 ];
		}

		public bool MapLoad( string fileName, string playerSpawnPointName, WorldType worldType,
			bool noChangeWindows )
		{
			EControl mapLoadingWindow = null;

			if( !noChangeWindows )
			{
				mapLoadingWindow = ControlDeclarationManager.Instance.CreateControl(
					"Gui\\MapLoadingWindow.gui" );
				if( mapLoadingWindow != null )
				{
					mapLoadingWindow.Text = fileName;
					ScreenControlManager.Instance.Controls.Add( mapLoadingWindow );
				}
				RenderScene();
			}

			//Delete all GameWindow's
			{
				ttt:
				foreach( EControl control in ScreenControlManager.Instance.Controls )
				{
					if( control is GameWindow )
					{
						ScreenControlManager.Instance.Controls.Remove( control );
						goto ttt;
					}
				}
			}

			MapSystemWorld.MapDestroy();

			WorldType needWorldType = worldType;
			if( needWorldType == null )
				needWorldType = GetWorldType();

			if( World.Instance == null || World.Instance.Type != needWorldType )
			{
				if( needWorldType == null )
				{
					if( mapLoadingWindow != null )
						mapLoadingWindow.SetShouldDetach();
					return false;
				}

				if( !EntitySystemWorld.Instance.WorldCreate( WorldSimulationType.Single, needWorldType ) )
					Log.Fatal( "EntitySystemWorld.Instance.WorldCreate failed." );
			}

			if( !MapSystemWorld.MapLoad( fileName, false ) )
			{
				if( mapLoadingWindow != null )
					mapLoadingWindow.SetShouldDetach();
				return false;
			}

			//Simulate physics for 5 seconds. That the physics has fallen asleep.
			if( PhysicsWorld.Instance != null )
			{
				PhysicsWorld.Instance.EnableCollisionEvents = false;

				const float seconds = 5;

				for( float time = 0; time < seconds; time += Entity.TickDelta )
				{
					PhysicsWorld.Instance.Simulate( Entity.TickDelta );

					//WaterPlane specific
					foreach( WaterPlane waterPlane in WaterPlane.Instances )
						waterPlane.TickPhysicsInfluence( false );
				}

				PhysicsWorld.Instance.EnableCollisionEvents = true;
			}

			//Error
			foreach( EControl control in ScreenControlManager.Instance.Controls )
			{
				if( control is MessageBoxWindow )
					return false;
			}

			if( !noChangeWindows )
			{
				ScreenControlManager.Instance.Controls.Clear();

				GameWindow gameWindow = null;

				//Create specific game window
				if( GameMap.Instance != null )
					gameWindow = CreateGameWindowByGameType( GameMap.Instance.GameType );

				if( gameWindow == null )
					gameWindow = new ActionGameWindow();

				ScreenControlManager.Instance.Controls.Add( gameWindow );
			}

            if (string.Compare(fileName, "Maps\\Vietheroes\\Map.map", true) != 0)
			{
				GameMusic.MusicPlay( "Sounds\\Music\\Game.ogg", true );
			}

			return true;
		}

		public bool MapLoad( string fileName, WorldType worldType, bool noCreateGameWindow )
		{
			return MapLoad( fileName, null, worldType, noCreateGameWindow );
		}

		public bool MapLoad( string fileName )
		{
			return MapLoad( fileName, null, null, false );
		}

		public void SetNeedMapLoad( string fileName )
		{
			needMapLoadName = fileName;
		}

		GameWindow CreateGameWindowByGameType( GameMap.GameTypes gameType )
		{
			switch( gameType )
			{
			case GameMap.GameTypes.Action:
			case GameMap.GameTypes.TPSArcade:
				return new ActionGameWindow();

			case GameMap.GameTypes.RTS:
				return new RTSGameWindow();

			//Here it is necessary to add a your specific game mode.
			//..

			}

			return null;
		}

	}
}
