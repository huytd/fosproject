// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
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
		bool needMapCreateForDynamicMapExample;
		string needWorldLoadName;

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

		static MaterialSchemes materialScheme = MaterialSchemes.Default;
		[Config( "Video", "materialScheme" )]
		public static MaterialSchemes MaterialScheme
		{
			get { return materialScheme; }
			set
			{
				materialScheme = value;
				if( RendererWorld.Instance != null )
					RendererWorld.Instance.DefaultViewport.MaterialScheme = materialScheme.ToString();
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

			SoundVolume = soundVolume;
			MusicVolume = musicVolume;

			ScreenControlManager.Init();
			if( !ControlsWorld.Init() )
				return false;

			_ShowSystemCursor = _ShowSystemCursor;
			_DrawFPS = _DrawFPS;
			MaterialScheme = materialScheme;

			EControl programLoadingWindow = ControlDeclarationManager.Instance.CreateControl(
				"Gui\\ProgramLoadingWindow.gui" );
			if( programLoadingWindow != null )
				ScreenControlManager.Instance.Controls.Add( programLoadingWindow );

			RenderScene();

			EngineConsole.Instance.Texture = TextureManager.Instance.Load( "Utils/Console.png",
				Texture.Type.Type2D, 0 );
			EngineConsole.Instance.Font = FontManager.Instance.LoadFont( "Default", .025f );
			EngineConsole.Instance.StaticText = "Version " + EngineVersionInformation.Version;

			Log.Handlers.InfoHandler += delegate( string text )
			{
				if( EngineConsole.Instance != null )
					EngineConsole.Instance.Print( text );
			};

			Log.Handlers.WarningHandler += delegate( string text, ref bool handled )
			{
				if( EngineConsole.Instance != null )
				{
					handled = true;
					EngineConsole.Instance.Print( "Warning: " + text, new ColorValue( 1, 0, 0 ) );
					EngineConsole.Instance.Active = false;
				}
			};

			Log.Handlers.ErrorHandler += delegate( string text, ref bool handled )
			{
				if( ScreenControlManager.Instance != null )
				{
					handled = true;

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

                    GameMusic.MusicPlay("Sounds\\Vietheroes\\NewLegend.mp3", true);
					ScreenControlManager.Instance.Controls.Add( new EngineLogoWindow() );
				}
			}
			else
			{
                GameMusic.MusicPlay("Sounds\\Vietheroes\\NewLegend.mp3", true);
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

			//Debug information window
			if( e.Key == EKeys.F11 )
			{
				if( DebugInformationWindow.Instance == null )
				{
					DebugInformationWindow window = new DebugInformationWindow();
					ScreenControlManager.Instance.Controls.Add( window );
				}
				else
				{
					DebugInformationWindow.Instance.SetShouldDetach();
				}
				return true;
			}

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
			if( needMapCreateForDynamicMapExample )
			{
				needMapCreateForDynamicMapExample = false;
				MapCreateForDynamicMapExample();
			}
			if( needWorldLoadName != null )
			{
				string name = needWorldLoadName;
				needWorldLoadName = null;
				WorldLoad( name );
			}

			EngineConsole.Instance.OnTick( delta );
			ScreenControlManager.Instance.DoTick( delta );
		}

		protected override void OnRenderFrame()
		{
			base.OnRenderFrame();
			ScreenControlManager.Instance.DoRender();
		}

		protected override void OnRenderScreenUI( GuiRenderer renderer )
		{
			base.OnRenderScreenUI( renderer );
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
				Log.Error( "Only one instance of World type is supported." );
				return null;
			}
			return (WorldType)list[ 0 ];
		}

		void CreateGameWindowForMap()
		{
			ScreenControlManager.Instance.Controls.Clear();

			GameWindow gameWindow = null;

			//Create specific game window
			if( GameMap.Instance != null )
				gameWindow = CreateGameWindowByGameType( GameMap.Instance.GameType );

			if( gameWindow == null )
				gameWindow = new VHFOSGameWindows();

			ScreenControlManager.Instance.Controls.Add( gameWindow );
		}

		void DeleteAllGameWindows()
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

		public bool MapLoad( string fileName, WorldType worldType, bool noChangeWindows )
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

			//delete all GameWindow's
			DeleteAllGameWindows();

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

				if( !EntitySystemWorld.Instance.WorldCreate( WorldSimulationType.Single,
					needWorldType ) )
				{
					Log.Fatal( "EntitySystemWorld.Instance.WorldCreate failed." );
				}
			}

			if( !MapSystemWorld.MapLoad( fileName ) )
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
				CreateGameWindowForMap();

			//play music
			if( string.Compare( fileName, "Maps\\MainMenu\\Map.map", true ) != 0 )
                GameMusic.MusicPlay("Sounds\\Vietheroes\\NewLegend.mp3", true);

			return true;
		}

		public bool MapLoad( string fileName )
		{
			return MapLoad( fileName, null, false );
		}

		GameMapType GetGameMapType()
		{
			List<EntityType> list = EntityTypes.Instance.GetTypesBasedOnClass(
				EntityTypes.Instance.GetClassInfoByEntityClassName( "GameMap" ) );

			if( list.Count == 0 )
			{
				Log.Error( "GameMap type not defined." );
				return null;
			}
			if( list.Count != 1 )
			{
				Log.Error( "Only one instance of GameMap type is supported." );
				return null;
			}
			return (GameMapType)list[ 0 ];
		}

		public bool MapCreateForDynamicMapExample()
		{
			EControl mapLoadingWindow = null;

			//if( !noChangeWindows )
			{
				mapLoadingWindow = ControlDeclarationManager.Instance.CreateControl(
					"Gui\\MapLoadingWindow.gui" );
				if( mapLoadingWindow != null )
				{
					mapLoadingWindow.Text = "[Dynamic map creating]";
					ScreenControlManager.Instance.Controls.Add( mapLoadingWindow );
				}
				RenderScene();
			}

			//delete all GameWindow's
			DeleteAllGameWindows();

			MapSystemWorld.MapDestroy();

			WorldType needWorldType = null;//worldType
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

				if( !EntitySystemWorld.Instance.WorldCreate( WorldSimulationType.Single,
					needWorldType ) )
				{
					Log.Fatal( "EntitySystemWorld.Instance.WorldCreate failed." );
				}
			}

			//create entites
			{
				//create map
				GameMapType gameMapType = GetGameMapType();
				GameMap gameMap = (GameMap)Entities.Instance.Create( gameMapType, World.Instance );
				gameMap.ShadowFarDistance = 60;
				gameMap.PostCreate();

				//ground
				{
					StaticMesh staticMesh = (StaticMesh)Entities.Instance.Create(
						"StaticMesh", Map.Instance );
					staticMesh.SplitGeometry = StaticMesh.SplitGeometryTypes.Yes;
					staticMesh.SplitGeometryPieceSize = new Vec3( 30, 30, 30 );
					staticMesh.MeshName = "Models\\DefaultBox\\DefaultBox.mesh";
					staticMesh.ForceMaterial = "DarkGreen";
					staticMesh.Position = new Vec3( 0, 0, -.5f );
					staticMesh.Scale = new Vec3( 200, 200, 1 );
					staticMesh.PostCreate();
				}

				//SkyBox
				{
					Entity skyBox = Entities.Instance.Create( "SkyBox", Map.Instance );
					skyBox.PostCreate();
				}

				//light
				{
					Light light = (Light)Entities.Instance.Create( "Light", Map.Instance );
					light.LightType = RenderLightType.Directional;
					light.SpecularColor = new ColorValue( 1, 1, 1 );
					light.Position = new Vec3( 0, 0, 10 );
					light.Rotation = new Angles( 120, 50, 330 ).ToQuat();
					light.PostCreate();
				}

				//SpawnPoint
				{
					SpawnPoint spawnPoint = (SpawnPoint)Entities.Instance.Create(
						"SpawnPoint", Map.Instance );
					spawnPoint.Position = new Vec3( 0, 0, 1 );
					spawnPoint.PostCreate();
				}

				//Maple's
				{
					for( int n = 0; n < 70; n++ )
					{
						for( int attempt = 0; attempt < 20; attempt++ )
						{
							Radian angle = World.Instance.Random.NextFloat() * MathFunctions.PI * 2;
							float radius = World.Instance.Random.NextFloat() * 40 + 10;

							Vec3 position = new Vec3( MathFunctions.Cos( angle ) * radius,
								MathFunctions.Sin( angle ) * radius, 0 );

							bool free = true;

							Bounds bounds = new Bounds( position - new Vec3( 2, 2, 2 ),
								position + new Vec3( 2, 2, 6 ) );
							Map.Instance.GetObjects( bounds, delegate( MapObject obj )
							{
								if( obj is StaticMesh )
									return;
								free = false;
							} );

							if( free )
							{
								MapObject mapObject = (MapObject)Entities.Instance.Create(
									"Maple", Map.Instance );
								mapObject.Position = position;
								mapObject.Rotation = Mat3.FromRotateByZ(
									World.Instance.Random.NextFloat() * MathFunctions.PI * 2 ).ToQuat();
								mapObject.PostCreate();

								break;
							}
						}
					}
				}

				//Box's
				{
					for( int n = 0; n < 50; n++ )
					{
						Vec3 position = new Vec3(
							World.Instance.Random.NextFloatCenter() * 10,
							World.Instance.Random.NextFloatCenter() * 10,
							40 + (float)n * 1.1f );

						MapObject mapObject = (MapObject)Entities.Instance.Create( "Box", Map.Instance );
						mapObject.Position = position;
						mapObject.Rotation = new Angles(
							World.Instance.Random.NextFloat() * 360,
							World.Instance.Random.NextFloat() * 360,
							World.Instance.Random.NextFloat() * 360 ).ToQuat();
						mapObject.PostCreate();
					}
				}

			}

			//Error
			foreach( EControl control in ScreenControlManager.Instance.Controls )
			{
				if( control is MessageBoxWindow )
					return false;
			}

			//if( !noChangeWindows )
			{
				CreateGameWindowForMap();
			}

			//play music
            GameMusic.MusicPlay("Sounds\\Vietheroes\\NewLegend.mp3", true);

			return true;
		}

		public bool WorldLoad( string fileName )
		{
			EControl worldLoadingWindow = null;

			//world loading window
			{
				worldLoadingWindow = ControlDeclarationManager.Instance.CreateControl(
					"Gui\\WorldLoadingWindow.gui" );
				if( worldLoadingWindow != null )
				{
					worldLoadingWindow.Text = fileName;
					ScreenControlManager.Instance.Controls.Add( worldLoadingWindow );
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

			if( !MapSystemWorld.WorldLoad( WorldSimulationType.Single, fileName ) )
			{
				if( worldLoadingWindow != null )
					worldLoadingWindow.SetShouldDetach();
				return false;
			}

			//Error
			foreach( EControl control in ScreenControlManager.Instance.Controls )
			{
				if( control is MessageBoxWindow )
					return false;
			}

			//create game window
			{
				ScreenControlManager.Instance.Controls.Clear();

				GameWindow gameWindow = null;

				//Create specific game window
				if( GameMap.Instance != null )
					gameWindow = CreateGameWindowByGameType( GameMap.Instance.GameType );

				if( gameWindow == null )
					gameWindow = new VHFOSGameWindows();

				ScreenControlManager.Instance.Controls.Add( gameWindow );
			}

			if( string.Compare( fileName, "Maps\\Vietheroes\\Map.map", true ) != 0 )
                GameMusic.MusicPlay("Sounds\\Vietheroes\\NewLegend.mp3", true);

			return true;
		}

		public bool WorldSave( string fileName )
		{
			EControl worldSavingWindow = null;

			//world loading window
			{
				worldSavingWindow = ControlDeclarationManager.Instance.CreateControl(
					"Gui\\WorldSavingWindow.gui" );
				if( worldSavingWindow != null )
				{
					worldSavingWindow.Text = fileName;
					ScreenControlManager.Instance.Controls.Add( worldSavingWindow );
				}
				RenderScene();
			}

			GameWindow gameWindow = null;
			foreach( EControl control in ScreenControlManager.Instance.Controls )
			{
				gameWindow = control as GameWindow;
				if( gameWindow != null )
					break;
			}
			if( gameWindow != null )
				gameWindow.OnBeforeWorldSave();

			bool result = MapSystemWorld.WorldSave( fileName );

			if( worldSavingWindow != null )
				worldSavingWindow.SetShouldDetach();

			return result;
		}

		public void SetNeedMapLoad( string fileName )
		{
			needMapLoadName = fileName;
		}

		public void SetNeedMapCreateForDynamicMapExample()
		{
			needMapCreateForDynamicMapExample = true;
		}

		public void SetNeedWorldLoad( string fileName )
		{
			needWorldLoadName = fileName;
		}

		GameWindow CreateGameWindowByGameType( GameMap.GameTypes gameType )
		{
			switch( gameType )
			{
			case GameMap.GameTypes.Action:
			case GameMap.GameTypes.TPSArcade:
				return new VHFOSGameWindows();

			case GameMap.GameTypes.RTS:
				return new RTSGameWindow();

			//Here it is necessary to add a your specific game mode.
			//..

			}

			return null;
		}

	}
}
