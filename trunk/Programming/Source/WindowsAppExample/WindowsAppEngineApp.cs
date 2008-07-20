// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Windows.Forms;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.FileSystem;
using Engine.UISystem;
using Engine.SoundSystem;
using Engine.Utils;

namespace WindowsAppExample
{
	class WindowsAppEngineApp : EngineApp
	{
		static WindowsAppEngineApp instance;

		[Config( "Sound", "soundVolume" )]
		static float soundVolume = .5f;

		//

		public static new WindowsAppEngineApp Instance
		{
			get { return instance; }
		}

		public WindowsAppEngineApp()
		{
			instance = this;
		}

		protected override bool OnCreate()
		{
			if( !base.OnCreate() )
				return false;

			Config.RegisterClassParameters( typeof( WindowsAppEngineApp ) );

			ScreenGuiRenderer.AllowCorrectAspectRatio = true;
			ControlsWorld.Init();

			if( SoundWorld.Instance != null )
				SoundWorld.Instance.Set3DSettings( 10, 1 );
			SoundVolume = soundVolume;

			if( !EntitySystemWorld.Init( new EntitySystemWorld() ) )
				return false;

			return true;
		}

		protected override void OnDestroy()
		{
			EntitySystemWorld.Shutdown();
			ControlsWorld.Shutdown();

			instance = null;

			base.OnDestroy();
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			//Tick a world
			if( EntitySystemWorld.Instance != null )
				EntitySystemWorld.Instance.Tick( false );
		}

		MapCamera GetMapCamera()
		{
			MapCamera mapCamera = null;
			foreach( Entity entity in Map.Instance.Children )
			{
				mapCamera = entity as MapCamera;
				if( mapCamera != null )
					break;
			}
			return mapCamera;
		}

		protected override void OnRender()
		{
			base.OnRender();

			Camera camera = RendererWorld.Instance.DefaultCamera;

			//Update camera
			if( Map.Instance != null && Map.Instance.IsPostCreated )
			{
				Vec3 position;
				Vec3 forward;
				Degree fov;

				MapCamera mapCamera = GetMapCamera();
				if( mapCamera != null )
				{
					position = mapCamera.Position;
					forward = mapCamera.Rotation * new Vec3( 1, 0, 0 );
					fov = mapCamera.Fov;
				}
				else
				{
					position = Map.Instance.EditorCameraPosition;
					forward = Map.Instance.EditorCameraDirection.GetVector();
					fov = Map.Instance.Fov;
				}

				if( fov == 0 )
					fov = Map.Instance.Fov;
				if( fov == 0 )
					fov = Map.Instance.Type.DefaultFov;

				camera.NearClipDistance = Map.Instance.GetRealNearFarClipDistance().Minimum;
				camera.FarClipDistance = Map.Instance.GetRealNearFarClipDistance().Maximum;
				camera.FixedUp = Vec3.ZAxis;
				camera.Fov = fov;
				camera.Position = position;
				camera.Direction = forward;// LookAt( position + forward * 100 );

				Map.Instance.DoRender( camera );
			}

			//Update sound listener
			if( SoundWorld.Instance != null )
			{
				SoundWorld.Instance.SetListener( camera.Position, Vec3.Zero,
					camera.Direction, camera.Up );
			}
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );

			if( Map.Instance != null )
				Map.Instance.DoDebugRenderUI( renderer );

			if( Map.Instance == null )
			{
				renderer.AddText( "Map not loaded", new Vec2( .5f, .5f ),
					HorizontalAlign.Center, VerticalAlign.Center,
					new ColorValue( 1, 1, 1 ), Rect.Cleared, true );
			}

			if( Map.Instance != null )
				RenderEntityOverCursor( renderer );
		}

		void RenderEntityOverCursor( GuiRenderer renderer )
		{
			Ray ray = RendererWorld.Instance.DefaultCamera.GetCameraToViewportRay(
				EngineApp.Instance.MousePosition );

			MapObject mapObject = null;

			Map.Instance.GetObjects( ray, delegate( MapObject obj, float scale )
			{
				mapObject = obj;
				return false;
			} );

			if( mapObject != null )
			{
				DebugGeometry.Instance.Color = new ColorValue( 1, 1, 0 );
				DebugGeometry.Instance.AddBounds( mapObject.MapBounds );

				renderer.AddText( mapObject.ToString(), new Vec2( .5f, .8f ),
					HorizontalAlign.Center, VerticalAlign.Center,
					new ColorValue( 1, 1, 0 ), Rect.Cleared, true );
			}
		}

		public static float SoundVolume
		{
			get { return soundVolume; }
			set
			{
				MathFunctions.Clamp( ref value, 0, 1 );

				soundVolume = value;

				if( EngineApp.Instance.DefaultSoundChannelGroup != null )
					EngineApp.Instance.DefaultSoundChannelGroup.Volume = soundVolume;
			}
		}
	}
}
