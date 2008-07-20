// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Engine;
using Engine.UISystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.SoundSystem;
using Engine.FileSystem;
using GameCommon;
using GameEntities;

namespace Game
{
	/// <summary>
	/// Defines a base window of game.
	/// </summary>
	public abstract class GameWindow : EControl
	{
		static GameWindow instance;

		//General
		[Config( "Map", "drawStaticPhysics" )]
		public static bool mapDrawStaticPhysics = false;
		[Config( "Map", "drawDynamicPhysics" )]
		public static bool mapDrawDynamicPhysics = false;
		[Config( "Map", "drawSceneGraphInfo" )]
		public static bool mapDrawSceneGraphInfo = false;
		[Config( "Map", "drawRegions" )]
		public static bool mapDrawRegions = false;
		[Config( "Map", "drawMapObjectBounds" )]
		public static bool mapDrawMapObjectBounds = false;
		[Config( "Map", "drawSceneNodeBounds" )]
		public static bool mapDrawSceneNodeBounds = false;
		[Config( "Map", "drawStaticMeshObjectBounds" )]
		public static bool mapDrawStaticMeshObjectBounds = false;
		[Config( "Map", "drawZonesPortalsOccluders" )]
		public static bool mapDrawZonesPortalsOccluders = false;
		[Config( "Map", "drawLights" )]
		public static bool mapDrawLights = false;
		[Config( "Map", "drawStaticGeometry" )]
		public static bool mapDrawStaticGeometry = true;
		[Config( "Map", "drawModels" )]
		public static bool mapDrawModels = true;
		[Config( "Map", "drawEffects" )]
		public static bool mapDrawEffects = true;
		[Config( "Map", "drawGui" )]
		public static bool mapDrawGui = true;
		[Config( "Map", "drawWireframe" )]
		public static bool mapDrawWireframe = false;
		[Config( "Map", "frustumTest" )]
		public static bool frustumTest = false;
		[Config( "Map", "drawPostEffects" )]
		public static bool drawPostEffects = true;
		[Config( "Map", "drawDebugGeometry" )]
		public static bool drawDebugGeometry = true;
		[Config( "Map", "drawPerformanceCounter" )]
		public static bool drawPerformanceCounter = false;
		[Config( "Map", "drawGameSpecificDebugGeometry" )]
		public static bool drawGameSpecificDebugGeometry = false;

		bool simulationAfterCloseMenuWindow;

		[Config( "Camera", "fov" )]
		public static Degree fov = 0;

		//free camera
		bool freeCameraEnabled;

		Vec3 freeCameraPosition;
		SphereDir freeCameraDirection;
		bool freeCameraMouseRotating;
		Vec2 freeCameraRotatingStartPos;

		//

		public static GameWindow Instance
		{
			get { return instance; }
		}

		protected override void OnAttach()
		{
			base.OnAttach();
			instance = this;

			freeCameraPosition = Map.Instance.EditorCameraPosition;
			freeCameraDirection = Map.Instance.EditorCameraDirection;

			EngineApp.Instance.Config.RegisterClassParameters( GetType() );

			EntitySystemWorld.Instance.Simulation = true;
		}

		protected override void OnDetach()
		{
			EngineApp.Instance.MouseRelativeMode = false;

			instance = null;
			base.OnDetach();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( Controls.Count != 1 )
				return base.OnKeyDown( e );

			if( e.Key == EKeys.Escape )
			{
				Controls.Add( new MenuWindow() );
				return true;
			}

			if( e.Key == EKeys.M )
			{
				EntitySystemWorld.Instance.Simulation = !EntitySystemWorld.Instance.Simulation;
				return true;
			}

			return base.OnKeyDown( e );
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			if( Controls.Count != 1 )
				return;

			EntitySystemWorld.Instance.Tick( false );

			//Shound change map
			if( GameWorld.Instance != null && GameWorld.Instance.ShouldChangeMapName != null )
			{
				GameEngineApp.Instance.MapLoad( GameWorld.Instance.ShouldChangeMapName,
					GameWorld.Instance.ShouldChangeMapSpawnPointName, null, false );
			}

			//moving free camera by keys
			if( FreeCameraEnabled && !EngineConsole.Instance.Active )
			{
				float cameraVelocity;
				if( EngineApp.Instance.IsKeyPressed( EKeys.Shift ) )
					cameraVelocity = 100.0f;
				else
					cameraVelocity = 20.0f;

				Vec3 pos = freeCameraPosition;
				SphereDir dir = freeCameraDirection;

				float step = cameraVelocity * delta;

				if( !EngineConsole.Instance.Active )
				{
					if( EngineApp.Instance.IsKeyPressed( EKeys.W ) ||
						EngineApp.Instance.IsKeyPressed( EKeys.Up ) )
					{
						pos += dir.GetVector() * step;
					}
					if( EngineApp.Instance.IsKeyPressed( EKeys.S ) ||
						EngineApp.Instance.IsKeyPressed( EKeys.Down ) )
					{
						pos -= dir.GetVector() * step;
					}
					if( EngineApp.Instance.IsKeyPressed( EKeys.A ) ||
						EngineApp.Instance.IsKeyPressed( EKeys.Left ) )
					{
						pos += new SphereDir( 
							dir.Horizontal + MathFunctions.PI / 2, 0 ).GetVector() * step;
					}
					if( EngineApp.Instance.IsKeyPressed( EKeys.D ) ||
						EngineApp.Instance.IsKeyPressed( EKeys.Right ) )
					{
						pos += new SphereDir( 
							dir.Horizontal - MathFunctions.PI / 2, 0 ).GetVector() * step;
					}
					if( EngineApp.Instance.IsKeyPressed( EKeys.Q ) )
						pos += new Vec3( 0, 0, step );
					if( EngineApp.Instance.IsKeyPressed( EKeys.E ) )
						pos += new Vec3( 0, 0, -step );
				}

				freeCameraPosition = pos;
			}

			if( freeCameraMouseRotating && !FreeCameraEnabled )
				freeCameraMouseRotating = false;
		}

		protected override void OnRender()
		{
			base.OnRender();

			//update camera orientation
			{
				Vec3 position, forward, up;
				Degree cameraFov = ( fov != 0 ) ? fov : Map.Instance.Fov;

				if( !FreeCameraEnabled )
				{
					OnGetCameraTransform( out position, out forward, out up, ref cameraFov );
				}
				else
				{
					position = freeCameraPosition;
					forward = freeCameraDirection.GetVector();
					up = Vec3.ZAxis;
				}

				if( cameraFov == 0 )
					cameraFov = ( fov != 0 ) ? fov : Map.Instance.Fov;// mapOriginalFov;

				Camera camera = RendererWorld.Instance.DefaultCamera;
				camera.NearClipDistance = Map.Instance.GetRealNearFarClipDistance().Minimum;
				camera.FarClipDistance = Map.Instance.GetRealNearFarClipDistance().Maximum;

				camera.FixedUp = up;
				camera.Fov = cameraFov;
				camera.Position = position;
				camera.Direction = forward;	//camera.LookAt( position + forward * 100 );
			}

			//update debug options to a map
			{
				Map.Instance.DrawStaticPhysics = mapDrawStaticPhysics;
				Map.Instance.DrawDynamicPhysics = mapDrawDynamicPhysics;
				Map.Instance.DrawSceneGraphInfo = mapDrawSceneGraphInfo;
				Map.Instance.DrawRegions = mapDrawRegions;
				Map.Instance.DrawLights = mapDrawLights;
				Map.Instance.DrawStaticGeometry = mapDrawStaticGeometry;
				Map.Instance.DrawModels = mapDrawModels;
				Map.Instance.DrawEffects = mapDrawEffects;
				Map.Instance.DrawGui = mapDrawGui;
				Map.Instance.DrawWireframe = mapDrawWireframe;
				Map.Instance.DrawMapObjectBounds = mapDrawMapObjectBounds;
				Map.Instance.DrawSceneNodeBounds = mapDrawSceneNodeBounds;
				Map.Instance.DrawStaticMeshObjectBounds = mapDrawStaticMeshObjectBounds;
				Map.Instance.DrawZonesPortalsOccluders = mapDrawZonesPortalsOccluders;
				Map.Instance.FrustumTest = frustumTest;
				Map.Instance.DrawGameSpecificDebugGeometry = drawGameSpecificDebugGeometry;
				RendererWorld.Instance.ForceEnableCompositors = drawPostEffects;
			}

			//update shadow settings
			{
				Map map = Map.Instance;

				map.ShadowTechnique = GameEngineApp.ShadowTechnique;
				map.ShadowColor = GameEngineApp.ShadowColor;

				if( GameEngineApp.ShadowFarDistanceUseMapSettings )
					GameEngineApp.ShadowFarDistance = map.InitializedShadowFarDistance;
				map.ShadowFarDistance = GameEngineApp.ShadowFarDistance;

				//map.ShadowCameraSetup = GameEngineApp.ShadowCameraSetup;
				map.Shadow2DTextureCount = GameEngineApp.Shadow2DTextureCount;
				map.ShadowCubicTextureCount = GameEngineApp.ShadowCubicTextureCount;
				switch( GameEngineApp.Shadow2DTextureSize )
				{
				case 256: map.Shadow2DTextureSize = Map.ShadowTextureSizes.Size256x256; break;
				case 512: map.Shadow2DTextureSize = Map.ShadowTextureSizes.Size512x512; break;
				case 1024: map.Shadow2DTextureSize = Map.ShadowTextureSizes.Size1024x1024; break;
				case 2048: map.Shadow2DTextureSize = Map.ShadowTextureSizes.Size2048x2048; break;
				default: Log.Warning( "Invalid GameEngineApp.Shadow2DTextureSize value." ); break;
				}
				switch( GameEngineApp.ShadowCubicTextureSize )
				{
				case 256: map.ShadowCubicTextureSize = Map.ShadowTextureSizes.Size256x256; break;
				case 512: map.ShadowCubicTextureSize = Map.ShadowTextureSizes.Size512x512; break;
				case 1024: map.ShadowCubicTextureSize = Map.ShadowTextureSizes.Size1024x1024; break;
				case 2048: map.ShadowCubicTextureSize = Map.ShadowTextureSizes.Size2048x2048; break;
				default: Log.Warning( "Invalid GameEngineApp.ShadowCubicTextureSize value." ); break;
				}
			}

			//update game specific options
			{
				//water reflection level
				foreach( WaterPlane waterPlane in WaterPlane.Instances )
					waterPlane.ReflectionLevel = GameEngineApp.WaterReflectionLevel;
			}

			//update sound listener
			if( SoundWorld.Instance != null )
			{
				Vec3 position, velocity, forward, up;
				OnGetSoundListenerTransform( out position, out velocity, out forward, out up );
				SoundWorld.Instance.SetListener( position, velocity, forward, up );
			}
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );

			if( drawPerformanceCounter )
				Engine.PerformanceCounter.DoRenderUI( renderer );
		}

		protected override void OnControlAttach( EControl control )
		{
			base.OnControlAttach( control );
			if( control as MenuWindow != null )
			{
				simulationAfterCloseMenuWindow = EntitySystemWorld.Instance.Simulation;
				EntitySystemWorld.Instance.Simulation = false;
				EngineApp.Instance.MouseRelativeMode = false;
			}
		}

		protected override void OnControlDetach( EControl control )
		{
			if( control as MenuWindow != null )
				EntitySystemWorld.Instance.Simulation = simulationAfterCloseMenuWindow;

			base.OnControlDetach( control );
		}

		protected abstract void OnGetCameraTransform( out Vec3 position, out Vec3 forward, out Vec3 up,
			ref Degree cameraFov );

		protected virtual void OnGetSoundListenerTransform( out Vec3 position, out Vec3 velocity,
			out Vec3 forward, out Vec3 up )
		{
			Camera camera = RendererWorld.Instance.DefaultCamera;

			position = camera.Position;
			velocity = Vec3.Zero;
			forward = camera.Direction;
			up = camera.Up;
		}

		protected override bool OnMouseDown( EMouseButtons button )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseDown( button );

			if( base.OnMouseDown( button ) )
				return true;

			//free camera rotating
			if( FreeCameraEnabled )
			{
				if( button == EMouseButtons.Right )
				{
					freeCameraMouseRotating = true;
					freeCameraRotatingStartPos = EngineApp.Instance.MousePosition;
				}
			}

			return true;
		}

		protected override bool OnMouseUp( EMouseButtons button )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseUp( button );

			//free camera rotating
			if( button == EMouseButtons.Right && freeCameraMouseRotating )
			{
				EngineApp.Instance.MouseRelativeMode = false;
				freeCameraMouseRotating = false;
			}

			return base.OnMouseUp( button );
		}

		protected override bool OnMouseMove()
		{
			bool ret = base.OnMouseMove();

			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return ret;

			//free camera rotating
			if( FreeCameraEnabled && freeCameraMouseRotating )
			{
				if( !EngineApp.Instance.MouseRelativeMode )
				{
					Vec2 diffPixels = ( MousePosition - freeCameraRotatingStartPos ) *
						new Vec2( EngineApp.Instance.VideoMode.Size.X, EngineApp.Instance.VideoMode.Size.Y );
					if( Math.Abs( diffPixels.X ) >= 3 || Math.Abs( diffPixels.Y ) >= 3 )
					{
						EngineApp.Instance.MouseRelativeMode = true;
					}
				}
				else
				{
					SphereDir dir = freeCameraDirection;
					dir.Horizontal -= MousePosition.X;// *cameraRotateSensitivity;
					dir.Vertical -= MousePosition.Y;// *cameraRotateSensitivity;

					dir.Horizontal = MathFunctions.RadiansNormalize360( dir.Horizontal );

					const float vlimit = MathFunctions.PI / 2 - .001f;
					if( dir.Vertical > vlimit ) dir.Vertical = vlimit;
					if( dir.Vertical < -vlimit ) dir.Vertical = -vlimit;

					freeCameraDirection = dir;
				}
			}

			return ret;
		}

		public bool FreeCameraEnabled
		{
			get { return freeCameraEnabled; }
			set { freeCameraEnabled = value; }
		}

		public bool FreeCameraMouseRotating
		{
			get { return freeCameraMouseRotating; }
		}

	}
}
