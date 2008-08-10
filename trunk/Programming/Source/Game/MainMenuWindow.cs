// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Engine;
using Engine.UISystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.SoundSystem;
using GameCommon;
using GameEntities;

namespace Game
{
	/// <summary>
	/// Defines a main menu.
	/// </summary>
	public class MainMenuWindow : EControl
	{
		EControl window;
		ETextBox versionTextBox;

		Map mapInstance;

		///////////////////////////////////////////

		/// <summary>
		/// Creates a window of the main menu and creates the background world.
		/// </summary>
		protected override void OnAttach()
		{
			base.OnAttach();

			//create main menu window
			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\MainMenuWindow.gui" );
			window.ColorMultiplier = new ColorValue( 1, 1, 1, 0 );
			Controls.Add( window );

			//Run button handler
			( (EButton)window.Controls[ "Run" ] ).Click += delegate( EButton sender )
			{
				GameEngineApp.Instance.SetNeedMapLoad( "Maps\\Vietheroes\\Map.map" );
			};

			//add version info control
			versionTextBox = new ETextBox();
			versionTextBox.TextHorizontalAlign = HorizontalAlign.Left;
			versionTextBox.TextVerticalAlign = VerticalAlign.Bottom;
			versionTextBox.Text = "Version " + EngineVersionInformation.Version;
			versionTextBox.ColorMultiplier = new ColorValue( 1, 1, 1, 0 );
			Controls.Add( versionTextBox );

			//play background music
			GameMusic.MusicPlay( "Sounds\\Music\\MainMenu.ogg", true );

			//update sound listener
			SoundWorld.Instance.SetListener( new Vec3( 1000, 1000, 1000 ),
				Vec3.Zero, new Vec3( 1, 0, 0 ), new Vec3( 0, 0, 1 ) );

			//create the background world
			CreateMap();

			ResetTime();
		}

		/// <summary>
		/// Destroys the background world at closing the main menu.
		/// </summary>
		protected override void OnDetach()
		{
			//destroy the background world
			DestroyMap();

			base.OnDetach();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;

			if( e.Key == EKeys.Escape )
			{
				Controls.Add( new MenuWindow() );
				return true;
			}

			return false;
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			//Change window transparency
			{
				float alpha = 0;

				if( Time > 2 && Time <= 4 )
					alpha = ( Time - 2 ) / 2;
				else if( Time > 4 )
					alpha = 1;

				window.ColorMultiplier = new ColorValue( 1, 1, 1, alpha );
				versionTextBox.ColorMultiplier = new ColorValue( 1, 1, 1, alpha );
			}

			//Change pictures
			{
				float period = 6 * 10;

				float t = Time % period;

				for( int n = 1; ; n++ )
				{
					EControl control = window.Controls[ "Picture" + n.ToString() ];
					if( control == null )
						break;

					float a = 3 + t / 2 - n * 3;
					MathFunctions.Clamp( ref a, 0, 1 );
					if( t > period - 2 )
					{
						float a2 = ( period - t ) / 2;
						a = Math.Min( a, a2 );

						if( window.Controls[ "Picture" + ( n + 1 ).ToString() ] != null )
							a = 0;
					}
					control.BackColor = new ColorValue( 1, 1, 1, a );
				}
			}

			//update sound listener
			SoundWorld.Instance.SetListener( new Vec3( 1000, 1000, 1000 ),
				Vec3.Zero, new Vec3( 1, 0, 0 ), new Vec3( 0, 0, 1 ) );

			//Tick a background world
			if( EntitySystemWorld.Instance != null )
				EntitySystemWorld.Instance.Tick( false );
		}

		protected override void OnRender()
		{
			base.OnRender();

			//Update camera orientation
			if( Map.Instance != null )
			{
				float dir = Time / 10.0f;

				Vec3 from = new Vec3(
					MathFunctions.Cos( dir ) * 29.0f / 1.5f,
					MathFunctions.Sin( dir * 1.50f ) * 10.0f / 1.5f,
					( MathFunctions.Cos( dir * 1.2f ) + 1.4f ) * 17.0f / 1.5f );
				Vec3 to = Vec3.Zero;
				float fov = 80;

				Camera camera = RendererWorld.Instance.DefaultCamera;
				camera.NearClipDistance = Map.Instance.GetRealNearFarClipDistance().Minimum;
				camera.FarClipDistance = Map.Instance.GetRealNearFarClipDistance().Maximum;
				camera.FixedUp = Vec3.ZAxis;
				camera.Fov = fov;
				camera.Position = from;
				camera.LookAt( to );
			}
		}

		/// <summary>
		/// Creates the background world.
		/// </summary>
		void CreateMap()
		{
			string worldTypeName = "ManualMainMenuWorld";

			WorldType worldType = (WorldType)EntityTypes.Instance.GetByName( worldTypeName );
			if( worldType == null )
			{
				worldType = (WorldType)EntityTypes.Instance.ManualCreateType(
					worldTypeName, EntityTypes.Instance.GetClassInfoByEntityClassName( "World" ) );
			}

			if( !GameEngineApp.Instance.MapLoad( "Maps\\MainMenu\\Map.map", worldType, true ) )
				return;

			mapInstance = Map.Instance;

			EntitySystemWorld.Instance.Simulation = true;
		}

		/// <summary>
		/// Destroys the background world.
		/// </summary>
		void DestroyMap()
		{
			if( mapInstance == Map.Instance )
			{
				MapSystemWorld.MapDestroy();
				EntitySystemWorld.Instance.WorldDestroy();
			}
		}

	}
}
